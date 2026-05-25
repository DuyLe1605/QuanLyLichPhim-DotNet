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

        if (movie.EndDate.HasValue && showtime.StartTime.Date > movie.EndDate.Value.Date)
            return (false, $"Phim \"{movie.Title}\" đã hết chiếu từ {movie.EndDate:dd/MM/yyyy}!");

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

    /// <summary>
    /// Tạo nhiều lịch chiếu trong một lần (thường dùng cho tạo nhiều suất cho 1 phim).
    /// Dừng và trả lỗi nếu có bất kỳ suất nào bị trùng lịch hoặc vi phạm EndDate.
    /// </summary>
    public async Task<(bool Success, string Message)> CreateManyAsync(IEnumerable<Showtime> showtimes)
    {
        if (showtimes == null) return (false, "Không có lịch chiếu nào để tạo!");

        var items = showtimes
            .Where(s => s != null)
            .ToList();

        if (items.Count == 0) return (false, "Không có lịch chiếu nào để tạo!");

        // Load movies in one query
        var movieIds = items.Select(s => s.MovieId).Distinct().ToList();
        var movies = await _context.Movies
            .Where(m => movieIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id);

        foreach (var st in items)
        {
            if (st.MovieId <= 0) return (false, "Thiếu MovieId.");
            if (st.RoomId <= 0) return (false, "Thiếu RoomId.");
            if (st.BasePrice <= 0) return (false, "Giá vé phải lớn hơn 0.");

            if (!movies.TryGetValue(st.MovieId, out var movie))
                return (false, $"Phim (Id={st.MovieId}) không tồn tại!");

            if (movie.EndDate.HasValue && st.StartTime.Date > movie.EndDate.Value.Date)
                return (false, $"Phim \"{movie.Title}\" đã hết chiếu từ {movie.EndDate:dd/MM/yyyy}!");

            // Compute EndTime
            st.EndTime = st.StartTime.AddMinutes(movie.Duration + CleanupMinutes);
            st.IsActive = true;
        }

        // Check conflicts among new items per room
        foreach (var g in items.GroupBy(s => s.RoomId))
        {
            var ordered = g.OrderBy(s => s.StartTime).ToList();
            for (int i = 0; i < ordered.Count - 1; i++)
            {
                if (ordered[i].StartTime < ordered[i + 1].EndTime && ordered[i].EndTime > ordered[i + 1].StartTime)
                {
                    return (false, $"Các suất mới bị trùng nhau trong phòng (Id={g.Key}): {ordered[i].StartTime:HH:mm} và {ordered[i + 1].StartTime:HH:mm}.");
                }
            }
        }

        // Check conflicts against existing showtimes
        foreach (var g in items.GroupBy(s => s.RoomId))
        {
            var minStart = g.Min(s => s.StartTime);
            var maxEnd = g.Max(s => s.EndTime);

            var existing = await _context.Showtimes
                .Include(s => s.Movie)
                .Where(s => s.IsActive
                         && s.RoomId == g.Key
                         && s.StartTime < maxEnd
                         && s.EndTime > minStart)
                .ToListAsync();

            foreach (var st in g)
            {
                var conflict = existing.FirstOrDefault(s => st.StartTime < s.EndTime && st.EndTime > s.StartTime);
                if (conflict != null)
                {
                    return (false, $"Trùng lịch với \"{conflict.Movie.Title}\" ({conflict.StartTime:HH:mm} - {conflict.EndTime:HH:mm})! (Phòng Id={g.Key})");
                }
            }
        }

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            _context.Showtimes.AddRange(items);
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            return (false, $"Tạo lịch chiếu thất bại: {ex.Message}");
        }

        return (true, $"Đã tạo {items.Count} lịch chiếu.");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(Showtime showtime)
    {
        var existing = await _context.Showtimes.FindAsync(showtime.Id);
        if (existing == null) return (false, "Lịch chiếu không tồn tại!");

        var movie = await _context.Movies.FindAsync(showtime.MovieId);
        if (movie == null) return (false, "Phim không tồn tại!");

        if (movie.EndDate.HasValue && showtime.StartTime.Date > movie.EndDate.Value.Date)
            return (false, $"Phim \"{movie.Title}\" đã hết chiếu từ {movie.EndDate:dd/MM/yyyy}!");

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
