using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class LoyaltyService
{
    public const decimal EarnSpendUnit = 10_000m;
    public const decimal RedeemValuePerPoint = 100m;

    private readonly AppDbContext _context;
    private readonly TierService _tierService;

    public LoyaltyService(AppDbContext context, TierService tierService)
    {
        _context = context;
        _tierService = tierService;
    }

    /// <summary>
    /// Accrues loyalty and membership points after a successful payment.
    /// LoyaltyEarned applies the tier multiplier; MembershipEarned does not.
    /// Also updates TotalSpent and MonthlySpent, then evaluates tier promotion.
    /// </summary>
    public async Task<(int LoyaltyEarned, int MembershipEarned)> AccruePointsAsync(
        int customerId, decimal paymentAmount, int? invoiceId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            return (0, 0);

        int basePoints = (int)Math.Floor(paymentAmount / EarnSpendUnit);
        decimal multiplier = _tierService.GetMultiplier(customer.Tier);
        int loyaltyEarned = (int)Math.Floor(basePoints * multiplier);
        int membershipEarned = basePoints;

        // Update customer balances
        customer.LoyaltyPoints    += loyaltyEarned;
        customer.MembershipPoints += membershipEarned;
        customer.TotalPoints      += loyaltyEarned;
        customer.TotalSpent       += paymentAmount;
        customer.MonthlySpent     += paymentAmount;

        // Build description
        string description = invoiceId.HasValue
            ? $"Tích điểm từ thanh toán #{invoiceId}"
            : $"Tích điểm từ thanh toán";

        // Record transaction
        var transaction = new PointTransaction
        {
            CustomerId  = customerId,
            InvoiceId   = invoiceId,
            Points      = loyaltyEarned,
            Type        = "Earn",
            Description = description,
            CreatedAt   = DateTime.Now
        };
        _context.PointTransactions.Add(transaction);

        await _context.SaveChangesAsync();

        // Evaluate tier promotion after spending is updated
        await _tierService.EvaluatePromotionAsync(customerId);

        return (loyaltyEarned, membershipEarned);
    }

    /// <summary>
    /// Redeems loyalty points for a discount at checkout.
    /// 1 point = 100 VND. Discount is capped at the order total.
    /// </summary>
    public async Task<(bool Success, string Message, decimal DiscountAmount)> RedeemPointsForDiscountAsync(
        int customerId, int pointsToRedeem, decimal orderTotal)
    {
        if (pointsToRedeem <= 0)
            return (false, "Số điểm đổi phải lớn hơn 0", 0m);

        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            return (false, "Không tìm thấy khách hàng!", 0m);

        if (pointsToRedeem > customer.LoyaltyPoints)
            return (false, $"Không đủ điểm! Bạn có {customer.LoyaltyPoints:N0} điểm.", 0m);

        decimal discount = Math.Min((decimal)pointsToRedeem * RedeemValuePerPoint, orderTotal);
        int pointsConsumed = (int)Math.Ceiling(discount / RedeemValuePerPoint);

        customer.LoyaltyPoints -= pointsConsumed;
        customer.TotalPoints = Math.Max(0, customer.TotalPoints - pointsConsumed);

        var transaction = new PointTransaction
        {
            CustomerId  = customerId,
            Points      = -pointsConsumed,
            Type        = "Redeem",
            Description = $"Đổi {pointsConsumed:N0} điểm → giảm {discount:N0}đ",
            CreatedAt   = DateTime.Now
        };
        _context.PointTransactions.Add(transaction);

        await _context.SaveChangesAsync();

        return (true, $"Đổi {pointsConsumed:N0} điểm thành công! Giảm {discount:N0}đ.", discount);
    }

    /// <summary>
    /// Returns the current spendable loyalty point balance for a customer.
    /// </summary>
    public async Task<int> GetLoyaltyBalanceAsync(int customerId)
    {
        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == customerId);

        return customer?.LoyaltyPoints ?? 0;
    }

    /// <summary>
    /// Returns the full point transaction history for a customer, newest first.
    /// </summary>
    public async Task<List<PointTransaction>> GetHistoryAsync(int customerId)
    {
        return await _context.PointTransactions
            .AsNoTracking()
            .Where(pt => pt.CustomerId == customerId)
            .OrderByDescending(pt => pt.CreatedAt)
            .ToListAsync();
    }
}
