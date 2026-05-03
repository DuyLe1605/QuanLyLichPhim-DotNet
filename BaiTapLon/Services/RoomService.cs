using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

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

    public async Task<(bool Success, string Message)> CreateAsync(Room room, int vipFromRow, int coupleLastRow)
    {
        if (string.IsNullOrWhiteSpace(room.Name))
            return (false, "Tên phòng không được để trống!");

        room.TotalSeats = room.Rows * room.Columns;
        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        // Auto-generate ghế
        GenerateSeats(room, vipFromRow, coupleLastRow);
        await _context.SaveChangesAsync();

        return (true, "Tạo phòng thành công!");
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

    private void GenerateSeats(Room room, int vipFromRow, int coupleLastRow)
    {
        for (int r = 0; r < room.Rows; r++)
        {
            string rowLabel = ((char)('A' + r)).ToString();
            string seatType;
            decimal multiplier;

            if (coupleLastRow == 1 && r == room.Rows - 1)
            {
                seatType = "Couple"; multiplier = 2.0m;
            }
            else if (r >= vipFromRow)
            {
                seatType = "VIP"; multiplier = 1.5m;
            }
            else
            {
                seatType = "Standard"; multiplier = 1.0m;
            }

            for (int c = 1; c <= room.Columns; c++)
            {
                _context.Seats.Add(new Seat
                {
                    RoomId = room.Id,
                    RowLabel = rowLabel,
                    SeatNumber = c,
                    Type = seatType,
                    PriceMultiplier = multiplier
                });
            }
        }
    }
}
