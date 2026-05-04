using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class ShowtimeService
{
    private readonly AppDbContext _context;
    private const int CleanupMinutes = 15; // Thời gian dọn rạp

    public ShowtimeService(AppDbContext context) { _context = context; }

    public async Task<List<Showtime>> GetAllAsync(DateTime? date = null, int? movieId = null, int? roomId = null)
    {
        var query = _context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .Where(s => s.IsActive)
            .AsNoTracking();

        if (date.HasValue)
            query = query.Where(s => s.StartTime.Date == date.Value.Date);

        if (movieId.HasValue)
            query = query.Where(s => s.MovieId == movieId.Value);

        if (roomId.HasValue)
            query = query.Where(s => s.RoomId == roomId.Value);

        return await query.OrderBy(s => s.StartTime).ToListAsync();
    }

    public async Task<Showtime?> GetByIdAsync(int id)
    {
        return await _context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);
    }

    public async Task<(bool Success, string Message)> CreateAsync(Showtime showtime)
    {
        // Lấy thời lượng phim
        var movie = await _context.Movies.FindAsync(showtime.MovieId);
        if (movie == null) return (false, "Phim không tồn tại!");

        // Auto tính EndTime
        showtime.EndTime = showtime.StartTime.AddMinutes(movie.Duration + CleanupMinutes);

        // Kiểm tra trùng lịch
        var conflict = await CheckConflict(showtime.RoomId, showtime.StartTime, showtime.EndTime, excludeId: 0);
        if (conflict != null)
            return (false, $"Trùng lịch với \"{conflict.Movie.Title}\" ({conflict.StartTime:HH:mm} - {conflict.EndTime:HH:mm})!");

        showtime.IsActive = true;
        _context.Showtimes.Add(showtime);
        await _context.SaveChangesAsync();

        return (true, "Tạo lịch chiếu thành công!");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(Showtime showtime)
    {
        var existing = await _context.Showtimes.FindAsync(showtime.Id);
        if (existing == null) return (false, "Lịch chiếu không tồn tại!");

        var movie = await _context.Movies.FindAsync(showtime.MovieId);
        if (movie == null) return (false, "Phim không tồn tại!");

        var newEnd = showtime.StartTime.AddMinutes(movie.Duration + CleanupMinutes);

        var conflict = await CheckConflict(showtime.RoomId, showtime.StartTime, newEnd, excludeId: showtime.Id);
        if (conflict != null)
            return (false, $"Trùng lịch với \"{conflict.Movie.Title}\" ({conflict.StartTime:HH:mm} - {conflict.EndTime:HH:mm})!");

        existing.MovieId = showtime.MovieId;
        existing.RoomId = showtime.RoomId;
        existing.StartTime = showtime.StartTime;
        existing.EndTime = newEnd;
        existing.BasePrice = showtime.BasePrice;

        await _context.SaveChangesAsync();
        return (true, "Cập nhật lịch chiếu thành công!");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var showtime = await _context.Showtimes
            .Include(s => s.Tickets)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (showtime == null) return (false, "Lịch chiếu không tồn tại!");

        if (showtime.Tickets.Any())
            return (false, "Không thể xóa lịch chiếu đã bán vé!");

        showtime.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, "Đã xóa lịch chiếu!");
    }

    /// <summary>
    /// Kiểm tra trùng lịch chiếu trong cùng phòng.
    /// Trả về Showtime bị trùng nếu có, null nếu không.
    /// </summary>
    private async Task<Showtime?> CheckConflict(int roomId, DateTime newStart, DateTime newEnd, int excludeId)
    {
        return await _context.Showtimes
            .Include(s => s.Movie)
            .Where(s => s.RoomId == roomId
                     && s.IsActive
                     && s.Id != excludeId
                     && newStart < s.EndTime
                     && newEnd > s.StartTime)
            .FirstOrDefaultAsync();
    }
}
