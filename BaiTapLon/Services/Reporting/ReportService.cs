using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;

namespace BaiTapLon.Services;

public class ReportService
{
    private readonly AppDbContext _context;
    public ReportService(AppDbContext context) { _context = context; }

    // ==================== DTO ====================

    public record RevenueByDate(DateTime Date, decimal Revenue, int TicketCount);
    public record RevenueByMonth(int Month, int Year, decimal Revenue, int TicketCount);
    public record TopMovie(string Title, int TicketCount, decimal Revenue);
    public record RoomOccupancy(string RoomName, string RoomType, int TotalSeats, int SoldSeats, double OccupancyRate);
    
    public record DashboardStats(
        int TotalMovies, int TotalRooms, int TotalShowtimesToday,
        int TotalTicketsSold, decimal TotalRevenue, int TotalInvoices,
        decimal TotalSnackRevenue, int NewCustomersMonth, decimal AOV,
        int TotalReviews, double AverageRating);

    // ==================== QUERIES ====================

    public async Task<DashboardStats> GetStatsAsync()
    {
        int movies = await _context.Movies.CountAsync(m => m.IsActive);
        int rooms = await _context.Rooms.CountAsync(r => r.IsActive);
        int showsToday = await _context.Showtimes
            .CountAsync(s => s.IsActive && s.StartTime.Date == DateTime.Today);
        int tickets = await _context.Tickets.CountAsync();
        decimal revenue = await _context.Invoices.SumAsync(i => (decimal?)i.TotalAmount) ?? 0;
        int invoices = await _context.Invoices.CountAsync();
        
        decimal snackRevenue = await _context.InvoiceSnacks.SumAsync(s => (decimal?)(s.Quantity * s.UnitPrice)) ?? 0;
        int newCust = await _context.Customers.CountAsync(c => c.CreatedAt.Year == DateTime.Now.Year && c.CreatedAt.Month == DateTime.Now.Month);
        decimal aov = invoices > 0 ? revenue / invoices : 0;
        int reviews = await _context.MovieReviews.CountAsync();
        double avgRating = await _context.MovieReviews.AverageAsync(r => (double?)r.Rating) ?? 0;

        return new DashboardStats(movies, rooms, showsToday, tickets, revenue, invoices, snackRevenue, newCust, aov, reviews, avgRating);
    }

    /// <summary>
    /// Doanh thu theo ngày — project trước, group trên client để tránh LINQ translation error.
    /// </summary>
    public async Task<List<RevenueByDate>> GetRevenueByDateAsync(DateTime from, DateTime to)
    {
        var rawData = await _context.Invoices
            .Where(i => i.CreatedAt.Date >= from.Date && i.CreatedAt.Date <= to.Date)
            .Select(i => new { Date = i.CreatedAt.Date, i.TotalAmount, TicketCount = i.Tickets.Count })
            .ToListAsync();

        return rawData
            .GroupBy(x => x.Date)
            .Select(g => new RevenueByDate(g.Key, g.Sum(x => x.TotalAmount), g.Sum(x => x.TicketCount)))
            .OrderBy(r => r.Date)
            .ToList();
    }

    /// <summary>
    /// Doanh thu theo tháng — project trước, group trên client.
    /// </summary>
    public async Task<List<RevenueByMonth>> GetRevenueByMonthAsync(int year)
    {
        var rawData = await _context.Invoices
            .Where(i => i.CreatedAt.Year == year)
            .Select(i => new { i.CreatedAt.Month, i.CreatedAt.Year, i.TotalAmount, TicketCount = i.Tickets.Count })
            .ToListAsync();

        return rawData
            .GroupBy(x => new { x.Month, x.Year })
            .Select(g => new RevenueByMonth(g.Key.Month, g.Key.Year, g.Sum(x => x.TotalAmount), g.Sum(x => x.TicketCount)))
            .OrderBy(r => r.Month)
            .ToList();
    }

    public async Task<List<TopMovie>> GetTopMoviesAsync(int topN = 5, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.Tickets.AsNoTracking().AsQueryable();

        if (from.HasValue)
            query = query.Where(t => t.Invoice.CreatedAt.Date >= from.Value.Date);
        if (to.HasValue)
            query = query.Where(t => t.Invoice.CreatedAt.Date <= to.Value.Date);

        var rawData = await query
            .Select(t => new { MovieTitle = t.Showtime.Movie.Title, t.Price })
            .ToListAsync();

        return rawData
            .GroupBy(t => t.MovieTitle)
            .Select(g => new TopMovie(g.Key, g.Count(), g.Sum(t => t.Price)))
            .OrderByDescending(m => m.TicketCount)
            .Take(topN)
            .ToList();
    }

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
