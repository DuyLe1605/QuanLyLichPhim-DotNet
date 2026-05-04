using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class PointService
{
    private readonly AppDbContext _context;

    public PointService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tích điểm cho khách hàng sau khi thanh toán.
    /// Tỷ lệ: Standard 1%, VIP 1.5%, Diamond 2% (1 điểm = 1 đồng).
    /// VD: Mua 100,000đ, hạng VIP → 100,000 × 1.5% = 1,500 điểm.
    /// </summary>
    public async Task<int> EarnPointsAsync(int customerId, int invoiceId, decimal amount)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null || !customer.IsActive) return 0;

        decimal rate = customer.Tier switch
        {
            "Diamond" => 0.02m,
            "VIP" => 0.015m,
            _ => 0.01m
        };

        int points = (int)Math.Floor(amount * rate);
        if (points <= 0) return 0;

        var transaction = new PointTransaction
        {
            CustomerId = customerId,
            InvoiceId = invoiceId,
            Points = points,
            Type = "Earn",
            Description = $"Tích điểm từ hóa đơn #{invoiceId} ({customer.Tier}: {rate * 100}%)",
            CreatedAt = DateTime.Now
        };

        _context.PointTransactions.Add(transaction);
        customer.TotalPoints += points;
        await _context.SaveChangesAsync();

        return points;
    }

    /// <summary>
    /// Đổi điểm thưởng. 1000 điểm = 10,000đ giảm giá.
    /// Trả về số tiền giảm giá tương ứng.
    /// </summary>
    public async Task<(bool Success, string Message, decimal DiscountAmount)> RedeemPointsAsync(
        int customerId, int pointsToRedeem)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            return (false, "Không tìm thấy khách hàng!", 0);

        if (pointsToRedeem <= 0)
            return (false, "Số điểm đổi phải lớn hơn 0!", 0);

        if (pointsToRedeem > customer.TotalPoints)
            return (false, $"Không đủ điểm! Bạn có {customer.TotalPoints:N0} điểm.", 0);

        // Phải đổi bội số 100 điểm
        if (pointsToRedeem % 100 != 0)
            return (false, "Số điểm đổi phải là bội số của 100!", 0);

        decimal discount = pointsToRedeem * 10m; // 1000 điểm = 10,000đ → 1 điểm = 10đ

        var transaction = new PointTransaction
        {
            CustomerId = customerId,
            Points = -pointsToRedeem,
            Type = "Redeem",
            Description = $"Đổi {pointsToRedeem:N0} điểm → giảm {discount:N0}đ",
            CreatedAt = DateTime.Now
        };

        _context.PointTransactions.Add(transaction);
        customer.TotalPoints -= pointsToRedeem;
        await _context.SaveChangesAsync();

        return (true, $"Đổi {pointsToRedeem:N0} điểm thành công! Giảm {discount:N0}đ.", discount);
    }

    /// <summary>
    /// Lấy lịch sử tích/đổi điểm của khách hàng.
    /// </summary>
    public async Task<List<PointTransaction>> GetHistoryAsync(int customerId)
    {
        return await _context.PointTransactions
            .AsNoTracking()
            .Where(pt => pt.CustomerId == customerId)
            .OrderByDescending(pt => pt.CreatedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Lấy số điểm hiện tại.
    /// </summary>
    public async Task<int> GetBalanceAsync(int customerId)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId);

        return customer?.TotalPoints ?? 0;
    }

    /// <summary>
    /// Lấy thống kê điểm tổng hợp cho 1 khách hàng.
    /// </summary>
    public async Task<(int TotalEarned, int TotalRedeemed, int Balance)> GetSummaryAsync(int customerId)
    {
        var transactions = await _context.PointTransactions
            .AsNoTracking()
            .Where(pt => pt.CustomerId == customerId)
            .ToListAsync();

        int earned = transactions.Where(t => t.Type == "Earn").Sum(t => t.Points);
        int redeemed = transactions.Where(t => t.Type == "Redeem").Sum(t => Math.Abs(t.Points));
        int balance = earned - redeemed;

        return (earned, redeemed, balance);
    }
}
