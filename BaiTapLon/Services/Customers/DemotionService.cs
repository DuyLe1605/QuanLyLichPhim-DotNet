using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class DemotionService
{
    private readonly AppDbContext _context;

    public DemotionService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns true if demotion has NOT yet run this month (i.e., it should run).
    /// Returns false if a Demotion transaction already exists for the current month.
    /// </summary>
    public async Task<bool> ShouldRunAsync()
    {
        var now = DateTime.Now;
        var alreadyRan = await _context.PointTransactions
            .AnyAsync(pt =>
                pt.Type == "Demotion" &&
                pt.CreatedAt.Year == now.Year &&
                pt.CreatedAt.Month == now.Month);

        return !alreadyRan;
    }

    /// <summary>
    /// Executes the monthly demotion logic:
    /// - Demotes Diamond customers with MonthlySpent &lt; 500,000 to VIP
    /// - Demotes VIP customers with MonthlySpent &lt; 200,000 to Standard
    /// - Resets MonthlySpent to 0 for ALL active customers
    /// - Saves all changes in a single SaveChangesAsync call
    /// </summary>
    /// <returns>List of (CustomerId, OldTier, NewTier) for each demoted customer.</returns>
    public async Task<List<(int CustomerId, string OldTier, string NewTier)>> ExecuteMonthlyDemotionAsync()
    {
        var results = new List<(int CustomerId, string OldTier, string NewTier)>();

        var eligibleCustomers = await _context.Customers
            .Where(c => c.IsActive && (c.Tier == "Diamond" || c.Tier == "VIP"))
            .ToListAsync();

        foreach (var customer in eligibleCustomers)
        {
            string? newTier = null;
            string description = string.Empty;

            if (customer.Tier == "Diamond" && customer.MonthlySpent < 500_000m)
            {
                newTier = "VIP";
                description = "Giáng hạng: Diamond → VIP (chi tiêu tháng không đủ)";
            }
            else if (customer.Tier == "VIP" && customer.MonthlySpent < 200_000m)
            {
                newTier = "Standard";
                description = "Giáng hạng: VIP → Standard (chi tiêu tháng không đủ)";
            }

            if (newTier != null)
            {
                string oldTier = customer.Tier;
                customer.Tier = newTier;

                _context.PointTransactions.Add(new PointTransaction
                {
                    CustomerId = customer.Id,
                    Type = "Demotion",
                    Points = 0,
                    Description = description
                });

                results.Add((customer.Id, oldTier, newTier));
            }
        }

        // Reset MonthlySpent for ALL active customers
        var allActiveCustomers = await _context.Customers
            .Where(c => c.IsActive)
            .ToListAsync();

        foreach (var customer in allActiveCustomers)
        {
            customer.MonthlySpent = 0;
        }

        await _context.SaveChangesAsync();

        return results;
    }
}
