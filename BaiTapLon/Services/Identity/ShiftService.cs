using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class ShiftService
{
    private readonly AppDbContext _context;
    public ShiftService(AppDbContext context) { _context = context; }

    /// <summary>Lấy ca đang mở của user (nếu có).</summary>
    public async Task<Shift?> GetOpenShiftAsync(int userId)
        => await _context.Shifts
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId && s.Status == "Open");

    /// <summary>Lấy tất cả ca làm việc (Admin view).</summary>
    public async Task<List<Shift>> GetAllAsync(DateTime? date = null, int? userId = null)
    {
        var query = _context.Shifts.Include(s => s.User).AsNoTracking().AsQueryable();

        if (date.HasValue)
            query = query.Where(s => s.OpenedAt.Date == date.Value.Date);

        if (userId.HasValue)
            query = query.Where(s => s.UserId == userId.Value);

        return await query.OrderByDescending(s => s.OpenedAt).ToListAsync();
    }

    /// <summary>Mở ca mới.</summary>
    public async Task<(bool Ok, string Msg, Shift? Shift)> OpenShiftAsync(int userId, decimal openingCash)
    {
        // Kiểm tra đã có ca đang mở
        var existing = await GetOpenShiftAsync(userId);
        if (existing != null)
            return (false, "Bạn đã có ca đang mở! Hãy đóng ca trước.", null);

        if (openingCash < 0)
            return (false, "Tiền đầu ca phải >= 0!", null);

        var shift = new Shift
        {
            UserId = userId,
            OpeningCash = openingCash,
            Status = "Open",
            OpenedAt = DateTime.Now
        };

        _context.Shifts.Add(shift);
        await _context.SaveChangesAsync();
        return (true, $"Mở ca thành công! Tiền đầu ca: {openingCash:N0} đ", shift);
    }

    /// <summary>Đóng ca.</summary>
    public async Task<(bool Ok, string Msg)> CloseShiftAsync(int shiftId, decimal closingCash, string? note = null)
    {
        var shift = await _context.Shifts
            .Include(s => s.Invoices)
            .FirstOrDefaultAsync(s => s.Id == shiftId);

        if (shift == null) return (false, "Không tìm thấy ca làm việc!");
        if (shift.Status == "Closed") return (false, "Ca đã đóng rồi!");

        // Tính tiền mặt kỳ vọng
        decimal cashIn = shift.Invoices
            .Where(i => i.PaymentMethod == "Cash")
            .Sum(i => i.ReceivedAmount);
        decimal cashOut = shift.Invoices
            .Where(i => i.PaymentMethod == "Cash")
            .Sum(i => i.ChangeAmount);
        decimal expectedCash = shift.OpeningCash + cashIn - cashOut;

        shift.ClosingCash = closingCash;
        shift.ExpectedCash = expectedCash;
        shift.CashDifference = closingCash - expectedCash;
        shift.Status = "Closed";
        shift.ClosedAt = DateTime.Now;
        shift.Note = note;

        await _context.SaveChangesAsync();
        return (true, $"Đóng ca thành công!\n" +
                       $"Tiền kỳ vọng: {expectedCash:N0} đ\n" +
                       $"Tiền thực tế: {closingCash:N0} đ\n" +
                       $"Chênh lệch: {shift.CashDifference:N0} đ");
    }
}
