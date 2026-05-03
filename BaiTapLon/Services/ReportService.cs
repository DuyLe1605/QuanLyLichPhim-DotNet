using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;

namespace BaiTapLon.Services;

public class ReportService
{
    private readonly AppDbContext _context;
    public ReportService(AppDbContext context) { _context = context; }

    // ==================== DTO ====================

    public record DashboardStats(
        int TotalMovies, int TotalRooms, int TotalShowtimesToday,
        int TotalTicketsSold, decimal TotalRevenue, int TotalInvoices);

    public record RevenueByDate(DateTime Date, decimal Revenue, int TicketCount);
    public record RevenueByMonth(int Month, int Year, decimal Revenue, int TicketCount);
    public record TopMovie(string Title, int TicketCount, decimal Revenue);
    public record RoomOccupancy(string RoomName, string RoomType, int TotalSeats, int SoldSeats, double OccupancyRate);

    // ==================== QUERIES ====================

    /// <summary>
    /// Tổng quan dashboard.
    /// </summary>
    public async Task<DashboardStats> GetStatsAsync()
    {
        int movies = await _context.Movies.CountAsync(m => m.IsActive);
        int rooms = await _context.Rooms.CountAsync(r => r.IsActive);
        int showsToday = await _context.Showtimes
            .CountAsync(s => s.IsActive && s.StartTime.Date == DateTime.Today);
        int tickets = await _context.Tickets.CountAsync();
        decimal revenue = await _context.Invoices.SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
        int invoices = await _context.Invoices.CountAsync();

        return new DashboardStats(movies, rooms, showsToday, tickets, revenue, invoices);
    }

    /// <summary>
    /// Doanh thu theo ngày trong khoảng thời gian.
    /// </summary>
    public async Task<List<RevenueByDate>> GetRevenueByDateAsync(DateTime from, DateTime to)
    {
        return await _context.Invoices
            .Where(i => i.CreatedAt.Date >= from.Date && i.CreatedAt.Date <= to.Date)
            .GroupBy(i => i.CreatedAt.Date)
            .Select(g => new RevenueByDate(
                g.Key,
                g.Sum(i => i.TotalAmount),
                g.Sum(i => i.Tickets.Count)))
            .OrderBy(r => r.Date)
            .ToListAsync();
    }

    /// <summary>
    /// Doanh thu theo tháng trong một năm.
    /// </summary>
    public async Task<List<RevenueByMonth>> GetRevenueByMonthAsync(int year)
    {
        return await _context.Invoices
            .Where(i => i.CreatedAt.Year == year)
            .GroupBy(i => new { i.CreatedAt.Month, i.CreatedAt.Year })
            .Select(g => new RevenueByMonth(
                g.Key.Month, g.Key.Year,
                g.Sum(i => i.TotalAmount),
                g.Sum(i => i.Tickets.Count)))
            .OrderBy(r => r.Month)
            .ToListAsync();
    }

    /// <summary>
    /// Top N phim ăn khách nhất.
    /// </summary>
    public async Task<List<TopMovie>> GetTopMoviesAsync(int topN = 5, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Tickets
            .Include(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(t => t.Invoice)
            .AsNoTracking();

        if (from.HasValue)
            query = query.Where(t => t.Invoice.CreatedAt.Date >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(t => t.Invoice.CreatedAt.Date <= to.Value.Date);

        return await query
            .GroupBy(t => t.Showtime.Movie.Title)
            .Select(g => new TopMovie(
                g.Key,
                g.Count(),
                g.Sum(t => t.Price)))
            .OrderByDescending(m => m.TicketCount)
            .Take(topN)
            .ToListAsync();
    }

    /// <summary>
    /// Tỷ lệ lấp đầy phòng (dựa trên suất chiếu trong khoảng thời gian).
    /// </summary>
    public async Task<List<RoomOccupancy>> GetRoomOccupancyAsync(DateTime? from = null, DateTime? to = null)
    {
        var rooms = await _context.Rooms
            .Where(r => r.IsActive)
            .Include(r => r.Showtimes.Where(s => s.IsActive))
            .AsNoTracking()
            .ToListAsync();

        var result = new List<RoomOccupancy>();

        foreach (var room in rooms)
        {
            var showtimeIds = room.Showtimes
                .Where(s => (!from.HasValue || s.StartTime.Date >= from.Value.Date)
                         && (!to.HasValue || s.StartTime.Date <= to.Value.Date))
                .Select(s => s.Id)
                .ToList();

            if (showtimeIds.Count == 0)
            {
                result.Add(new RoomOccupancy(room.Name, room.Type, room.TotalSeats, 0, 0));
                continue;
            }

            int totalCapacity = room.TotalSeats * showtimeIds.Count;
            int soldSeats = await _context.Tickets
                .CountAsync(t => showtimeIds.Contains(t.ShowtimeId));

            double rate = totalCapacity > 0 ? (double)soldSeats / totalCapacity * 100 : 0;
            result.Add(new RoomOccupancy(room.Name, room.Type, totalCapacity, soldSeats, Math.Round(rate, 1)));
        }

        return result;
    }
}
