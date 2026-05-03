using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;
using BaiTapLon.Forms.Admin; // RowConfig

namespace BaiTapLon.Services;

public class RoomService
{
    private readonly AppDbContext _context;
    public RoomService(AppDbContext context) { _context = context; }

    public async Task<List<Room>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Rooms.Include(r => r.Seats).AsNoTracking();
        if (!includeInactive) query = query.Where(r => r.IsActive);
        return await query.OrderBy(r => r.Name).ToListAsync();
    }

    public async Task<Room?> GetByIdAsync(int id)
    {
        return await _context.Rooms.Include(r => r.Seats).FirstOrDefaultAsync(r => r.Id == id);
    }

    /// <summary>
    /// Tạo phòng với cấu hình ghế theo từng hàng (variable columns per row).
    /// </summary>
    public async Task<(bool Success, string Message)> CreateAsync(Room room, List<RowConfig> rowConfigs)
    {
        if (string.IsNullOrWhiteSpace(room.Name))
            return (false, "Tên phòng không được để trống!");

        if (rowConfigs.Count == 0)
            return (false, "Chưa có hàng ghế nào!");

        // Tính lại TotalSeats & Columns từ RowConfigs
        room.TotalSeats = rowConfigs.Sum(r => r.SeatCount);
        room.Rows = rowConfigs.Count;
        room.Columns = rowConfigs.Max(r => r.SeatCount);

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        // Tạo ghế theo từng hàng
        foreach (var config in rowConfigs)
        {
            for (int c = 1; c <= config.SeatCount; c++)
            {
                _context.Seats.Add(new Seat
                {
                    RoomId = room.Id,
                    RowLabel = config.RowLabel,
                    SeatNumber = c,
                    Type = config.SeatType,
                    PriceMultiplier = config.PriceMultiplier
                });
            }
        }

        await _context.SaveChangesAsync();
        return (true, $"Tạo phòng thành công! ({room.Rows} hàng, {room.TotalSeats} ghế)");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(Room room)
    {
        var existing = await _context.Rooms.FindAsync(room.Id);
        if (existing == null) return (false, "Phòng không tồn tại!");

        existing.Name = room.Name;
        existing.Type = room.Type;
        await _context.SaveChangesAsync();
        return (true, "Cập nhật phòng thành công!");
    }

    public async Task<(bool Success, string Message)> SoftDeleteAsync(int id)
    {
        var room = await _context.Rooms.FindAsync(id);
        if (room == null) return (false, "Phòng không tồn tại!");

        // Kiểm tra lịch chiếu đang active
        var hasShowtimes = await _context.Showtimes.AnyAsync(s => s.RoomId == id && s.IsActive);
        if (hasShowtimes) return (false, "Không thể xóa phòng đang có lịch chiếu!");

        room.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, "Đã xóa phòng!");
    }
}
