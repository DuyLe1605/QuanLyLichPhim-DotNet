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
    public async Task<(bool Success, string Message)> CreateAsync(Room room, List<RowConfig> rowConfigs, List<SeatLayoutItem>? seatLayoutItems = null)
    {
        if (string.IsNullOrWhiteSpace(room.Name))
            return (false, "Tên phòng không được để trống!");

        if (rowConfigs.Count == 0 && (seatLayoutItems == null || seatLayoutItems.Count == 0))
            return (false, "Chưa có hàng ghế nào!");

        // Tính lại TotalSeats & Columns từ RowConfigs
        if (seatLayoutItems is { Count: > 0 })
        {
            room.TotalSeats = seatLayoutItems.Count;
            room.Rows = seatLayoutItems.Max(s => s.GridRow) + 1;
            room.Columns = seatLayoutItems.Max(s => s.GridColumn + Math.Max(1, s.GridSpan));
        }
        else
        {
            room.TotalSeats = rowConfigs.Sum(r => r.SeatCount);
            room.Rows = rowConfigs.Count;
            room.Columns = rowConfigs.Max(r => r.SeatCount);
        }

        _context.Rooms.Add(room);
        await _context.SaveChangesAsync();

        // Tạo ghế theo từng hàng
        if (seatLayoutItems is { Count: > 0 })
        {
            foreach (var item in seatLayoutItems)
            {
                _context.Seats.Add(new Seat
                {
                    RoomId = room.Id,
                    RowLabel = item.RowLabel,
                    SeatNumber = item.SeatNumber,
                    GridRow = item.GridRow,
                    GridColumn = item.GridColumn,
                    GridSpan = Math.Max(1, item.GridSpan),
                    Type = item.SeatType,
                    PriceMultiplier = item.PriceMultiplier
                });
            }

            await _context.SaveChangesAsync();
            return (true, $"Táº¡o phÃ²ng thÃ nh cÃ´ng! ({room.Rows} hÃ ng, {room.TotalSeats} gháº¿)");
        }

        for (int rowIndex = 0; rowIndex < rowConfigs.Count; rowIndex++)
        {
            var config = rowConfigs[rowIndex];
            for (int c = 1; c <= config.SeatCount; c++)
            {
                _context.Seats.Add(new Seat
                {
                    RoomId = room.Id,
                    RowLabel = config.RowLabel,
                    SeatNumber = c,
                    GridRow = rowIndex,
                    GridColumn = c - 1,
                    GridSpan = 1,
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

    public async Task<(bool Success, string Message)> UpdateAsync(
        Room room,
        List<RowConfig> rowConfigs,
        List<SeatLayoutItem>? seatLayoutItems = null)
    {
        if (string.IsNullOrWhiteSpace(room.Name))
            return (false, "Tên phòng không được để trống!");

        if (rowConfigs.Count == 0 && (seatLayoutItems == null || seatLayoutItems.Count == 0))
            return (false, "Chưa có hàng ghế nào!");

        var existing = await _context.Rooms
            .Include(r => r.Seats)
            .ThenInclude(s => s.Tickets)
            .FirstOrDefaultAsync(r => r.Id == room.Id);

        if (existing == null)
            return (false, "Phòng không tồn tại!");

        var desiredSeats = BuildDesiredSeats(rowConfigs, seatLayoutItems);
        var desiredKeys = desiredSeats.Select(s => (s.RowLabel, s.SeatNumber)).ToHashSet();

        var seatsToRemove = existing.Seats
            .Where(s => !desiredKeys.Contains((s.RowLabel, s.SeatNumber)))
            .ToList();

        if (seatsToRemove.Any(s => s.Tickets.Count > 0))
            return (false, "Không thể xóa ghế đã phát sinh vé. Chỉ nên đổi loại ghế/hệ số giá cho phòng đã bán vé.");

        existing.Name = room.Name;
        existing.Type = room.Type;
        existing.Rows = desiredSeats.Count == 0 ? 0 : desiredSeats.Max(s => s.GridRow) + 1;
        existing.Columns = desiredSeats.Count == 0 ? 0 : desiredSeats.Max(s => s.GridColumn + Math.Max(1, s.GridSpan));
        existing.TotalSeats = desiredSeats.Count;

        foreach (var seat in seatsToRemove)
            _context.Seats.Remove(seat);

        foreach (var desired in desiredSeats)
        {
            var seat = existing.Seats.FirstOrDefault(s =>
                s.RowLabel == desired.RowLabel && s.SeatNumber == desired.SeatNumber);

            if (seat == null)
            {
                _context.Seats.Add(new Seat
                {
                    RoomId = existing.Id,
                    RowLabel = desired.RowLabel,
                    SeatNumber = desired.SeatNumber,
                    GridRow = desired.GridRow,
                    GridColumn = desired.GridColumn,
                    GridSpan = desired.GridSpan,
                    Type = desired.SeatType,
                    PriceMultiplier = desired.PriceMultiplier
                });
                continue;
            }

            seat.GridRow = desired.GridRow;
            seat.GridColumn = desired.GridColumn;
            seat.GridSpan = desired.GridSpan;
            seat.Type = desired.SeatType;
            seat.PriceMultiplier = desired.PriceMultiplier;
        }

        await _context.SaveChangesAsync();
        return (true, "Cập nhật phòng và sơ đồ ghế thành công!");
    }

    private static List<SeatLayoutItem> BuildDesiredSeats(List<RowConfig> rowConfigs, List<SeatLayoutItem>? seatLayoutItems)
    {
        if (seatLayoutItems is { Count: > 0 })
        {
            return seatLayoutItems
                .OrderBy(s => s.GridRow)
                .ThenBy(s => s.GridColumn)
                .Select(s => new SeatLayoutItem
                {
                    RowLabel = s.RowLabel,
                    SeatNumber = s.SeatNumber,
                    GridRow = s.GridRow,
                    GridColumn = s.GridColumn,
                    GridSpan = Math.Max(1, s.GridSpan),
                    SeatType = s.SeatType,
                    PriceMultiplier = s.PriceMultiplier
                })
                .ToList();
        }

        var seats = new List<SeatLayoutItem>();
        for (int rowIndex = 0; rowIndex < rowConfigs.Count; rowIndex++)
        {
            var config = rowConfigs[rowIndex];
            var seatCount = Math.Clamp(config.SeatCount, 1, 30);
            var multiplier = config.PriceMultiplier <= 0 ? 1.0m : config.PriceMultiplier;

            for (int columnIndex = 0; columnIndex < seatCount; columnIndex++)
            {
                seats.Add(new SeatLayoutItem
                {
                    RowLabel = config.RowLabel,
                    SeatNumber = columnIndex + 1,
                    GridRow = rowIndex,
                    GridColumn = columnIndex,
                    GridSpan = 1,
                    SeatType = config.SeatType,
                    PriceMultiplier = multiplier
                });
            }
        }

        return seats;
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
