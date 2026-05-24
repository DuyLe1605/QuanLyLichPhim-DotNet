using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class VoucherService
{
    private readonly AppDbContext _context;
    public VoucherService(AppDbContext context) { _context = context; }

    public async Task<List<Voucher>> GetAllAsync(bool? isActive = null, string? keyword = null)
    {
        var query = _context.Vouchers.AsNoTracking().AsQueryable();

        if (isActive.HasValue)
            query = query.Where(v => v.IsActive == isActive.Value);

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(v => v.Code.Contains(keyword));

        return await query.OrderByDescending(v => v.Id).ToListAsync();
    }

    public async Task<Voucher?> GetByIdAsync(int id)
        => await _context.Vouchers.FindAsync(id);

    public async Task<Voucher?> GetByCodeAsync(string code)
        => await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code && v.IsActive);

    public async Task<(bool Ok, string Msg, int Id)> CreateAsync(Voucher voucher)
    {
        if (await _context.Vouchers.AnyAsync(v => v.Code == voucher.Code))
            return (false, "Mã voucher đã tồn tại!", 0);

        if (voucher.EndDate <= voucher.StartDate)
            return (false, "Ngày kết thúc phải sau ngày bắt đầu!", 0);

        _context.Vouchers.Add(voucher);
        await _context.SaveChangesAsync();
        return (true, "Tạo voucher thành công!", voucher.Id);
    }

    public async Task<(bool Ok, string Msg)> UpdateAsync(Voucher voucher)
    {
        var existing = await _context.Vouchers.FindAsync(voucher.Id);
        if (existing == null) return (false, "Không tìm thấy voucher!");

        existing.Type = voucher.Type;
        existing.Value = voucher.Value;
        existing.MaxDiscount = voucher.MaxDiscount;
        existing.MaxUses = voucher.MaxUses;
        existing.StartDate = voucher.StartDate;
        existing.EndDate = voucher.EndDate;
        existing.IsActive = voucher.IsActive;

        await _context.SaveChangesAsync();
        return (true, "Cập nhật voucher thành công!");
    }

    public async Task<(bool Ok, string Msg)> DeleteAsync(int id)
    {
        var voucher = await _context.Vouchers
            .Include(v => v.Invoices)
            .FirstOrDefaultAsync(v => v.Id == id);
        if (voucher == null) return (false, "Không tìm thấy voucher!");

        if (voucher.Invoices.Any())
            return (false, "Không thể xóa voucher đã được sử dụng trong hóa đơn!");

        _context.Vouchers.Remove(voucher);
        await _context.SaveChangesAsync();
        return (true, "Đã xóa voucher!");
    }

    /// <summary>
    /// Validate và tính giảm giá cho 1 mã voucher.
    /// </summary>
    public async Task<(bool Ok, string Msg, decimal Discount)> ApplyVoucherAsync(string code, decimal orderTotal)
    {
        var voucher = await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
        if (voucher == null)
            return (false, "Mã voucher không tồn tại!", 0);

        if (!voucher.IsActive)
            return (false, "Voucher đã bị vô hiệu hóa!", 0);

        if (DateTime.Now < voucher.StartDate || DateTime.Now > voucher.EndDate)
            return (false, $"Voucher chỉ có hiệu lực từ {voucher.StartDate:dd/MM/yyyy} đến {voucher.EndDate:dd/MM/yyyy}!", 0);

        if (voucher.UsedCount >= voucher.MaxUses)
            return (false, "Voucher đã hết lượt sử dụng!", 0);

        decimal discount = voucher.Type switch
        {
            "Percent" => Math.Min(orderTotal * voucher.Value / 100,
                          voucher.MaxDiscount ?? decimal.MaxValue),
            "Fixed" => Math.Min(voucher.Value, orderTotal),
            "FreeTicket" => orderTotal, // Miễn phí toàn bộ
            _ => 0
        };

        return (true, $"Giảm {discount:N0} đ ({voucher.Type}: {voucher.Value})", discount);
    }

    /// <summary>
    /// Tăng UsedCount sau khi áp dụng thành công.
    /// </summary>
    public async Task IncrementUsageAsync(int voucherId)
    {
        var voucher = await _context.Vouchers.FindAsync(voucherId);
        if (voucher != null)
        {
            voucher.UsedCount++;
            await _context.SaveChangesAsync();
        }
    }
}
