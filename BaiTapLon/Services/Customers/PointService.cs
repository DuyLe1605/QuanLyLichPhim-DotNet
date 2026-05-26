using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class PointService
{
    private const decimal EarnSpendUnit = 10_000m;
    private const decimal RedeemValuePerPoint = 100m;
    private readonly AppDbContext _context;

    public PointService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tích điểm sau thanh toán: Standard 1 điểm / 10.000đ, VIP x1.5, Diamond x2.
    /// Ví dụ: 100.000đ hạng VIP -> 10 x 1.5 = 15 điểm.
    /// </summary>
    public async Task<int> EarnPointsAsync(int customerId, int invoiceId, decimal amount)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null || !customer.IsActive) return 0;

        decimal multiplier = customer.Tier switch
        {
            "Diamond" => 2m,
            "VIP" => 1.5m,
            _ => 1m
        };

        int basePoints = (int)Math.Floor(amount / EarnSpendUnit);
        int points = (int)Math.Floor(basePoints * multiplier);
        if (points <= 0) return 0;

        _context.PointTransactions.Add(new PointTransaction
        {
            CustomerId = customerId,
            InvoiceId = invoiceId,
            Points = points,
            Type = "Earn",
            Description = $"Tích điểm từ hóa đơn #{invoiceId} ({customer.Tier}: x{multiplier})",
            CreatedAt = DateTime.Now
        });

        customer.TotalPoints += points;
        customer.LoyaltyPoints += points;
        customer.MembershipPoints += basePoints;
        await _context.SaveChangesAsync();

        return points;
    }

    /// <summary>
    /// Đổi điểm thưởng. 1 điểm = 100đ giảm giá.
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

        decimal discount = pointsToRedeem * RedeemValuePerPoint;

        _context.PointTransactions.Add(new PointTransaction
        {
            CustomerId = customerId,
            Points = -pointsToRedeem,
            Type = "Redeem",
            Description = $"Đổi {pointsToRedeem:N0} điểm -> giảm {discount:N0}đ",
            CreatedAt = DateTime.Now
        });

        customer.TotalPoints -= pointsToRedeem;
        customer.LoyaltyPoints = Math.Max(0, customer.LoyaltyPoints - pointsToRedeem);
        await _context.SaveChangesAsync();

        return (true, $"Đổi {pointsToRedeem:N0} điểm thành công! Giảm {discount:N0}đ.", discount);
    }

    public async Task<List<PointTransaction>> GetHistoryAsync(int customerId)
    {
        return await _context.PointTransactions
            .AsNoTracking()
            .Where(pt => pt.CustomerId == customerId)
            .OrderByDescending(pt => pt.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetBalanceAsync(int customerId)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId);

        return customer?.TotalPoints ?? 0;
    }

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
