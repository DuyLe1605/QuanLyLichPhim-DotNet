using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;

namespace BaiTapLon.Services;

public class TicketService
{
    private readonly AppDbContext _context;
    public TicketService(AppDbContext context) { _context = context; }

    /// <summary>
    /// Lấy danh sách SeatId đã bán cho một suất chiếu.
    /// </summary>
    public async Task<HashSet<int>> GetSoldSeatIdsAsync(int showtimeId)
    {
        var ids = await _context.Tickets
            .Where(t => t.ShowtimeId == showtimeId)
            .Select(t => t.SeatId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    /// <summary>
    /// Đếm số vé đã bán cho suất chiếu.
    /// </summary>
    public async Task<int> CountSoldAsync(int showtimeId)
    {
        return await _context.Tickets.CountAsync(t => t.ShowtimeId == showtimeId);
    }
}
