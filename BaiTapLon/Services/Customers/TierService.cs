using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class TierService
{
    private readonly AppDbContext _context;

    public TierService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns the loyalty point multiplier for a given tier.
    /// Standard = 1.0x, VIP = 1.5x, Diamond = 2.0x.
    /// </summary>
    public decimal GetMultiplier(string tier) => tier switch
    {
        "VIP"     => 1.5m,
        "Diamond" => 2.0m,
        _         => 1.0m
    };

    /// <summary>
    /// Evaluates whether a customer should be promoted to a higher tier
    /// based on their TotalSpent. Saves changes if a promotion occurs.
    /// </summary>
    public async Task<(bool Promoted, string? NewTier)> EvaluatePromotionAsync(int customerId)
    {
        var customer = await _context.Customers.FindAsync(customerId);
        if (customer == null)
            return (false, null);

        string? newTier = null;

        if (customer.Tier == "Standard" && customer.TotalSpent >= 2_000_000m)
        {
            newTier = "VIP";
        }
        else if (customer.Tier == "VIP" && customer.TotalSpent >= 10_000_000m)
        {
            newTier = "Diamond";
        }

        if (newTier != null)
        {
            customer.Tier = newTier;
            await _context.SaveChangesAsync();
            return (true, newTier);
        }

        return (false, null);
    }
}
