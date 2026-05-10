using BaiTapLon.Services;
using Xunit;

namespace BaiTapLon.Tests.Properties;

/// <summary>
/// Property-based tests for tier promotion and demotion lifecycle.
/// Covers Properties 10–11 from the design document.
/// </summary>
[Trait("Feature", "loyalty-coupon-system")]
public class TierLifecycleProperties
{
    // ─────────────────────────────────────────────────────────────────────────
    // Property 10: Tier Promotion at Spending Thresholds
    // Validates: Requirements 7.1, 7.2
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A Standard customer whose TotalSpent reaches 2,000,000 VND must be promoted to VIP.
    /// </summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task TierPromotion_StandardToVipAt2M()
    {
        // **Validates: Requirements 7.1**
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            // Customer is just below the 2M threshold
            ctx.Customers.Add(TestDbHelper.CreateCustomer(1, "Standard", totalSpent: 1_999_000m));
            await ctx.SaveChangesAsync();

            var tierSvc = new TierService(ctx);
            var loyaltySvc = new LoyaltyService(ctx, tierSvc);
            // A 1,000 VND payment pushes TotalSpent to exactly 2,000,000
            await loyaltySvc.AccruePointsAsync(1, 1_000m, null);

            var customer = await ctx.Customers.FindAsync(1);
            Assert.Equal("VIP", customer!.Tier);
        }
    }

    /// <summary>
    /// A VIP customer whose TotalSpent reaches 10,000,000 VND must be promoted to Diamond.
    /// </summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task TierPromotion_VipToDiamondAt10M()
    {
        // **Validates: Requirements 7.2**
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Customers.Add(TestDbHelper.CreateCustomer(1, "VIP", totalSpent: 9_999_000m));
            await ctx.SaveChangesAsync();

            var tierSvc = new TierService(ctx);
            var loyaltySvc = new LoyaltyService(ctx, tierSvc);
            await loyaltySvc.AccruePointsAsync(1, 1_000m, null);

            var customer = await ctx.Customers.FindAsync(1);
            Assert.Equal("Diamond", customer!.Tier);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Property 11: Monthly Demotion at Insufficient Spending
    // Validates: Requirements 9.3, 9.4, 9.5
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A Diamond customer with monthly spending below 500,000 VND must be demoted to VIP.
    /// </summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task Demotion_DiamondDemotedWhenMonthlySpentLow()
    {
        // **Validates: Requirements 9.3, 9.4**
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Customers.Add(TestDbHelper.CreateCustomer(1, "Diamond", monthlySpent: 100_000m));
            await ctx.SaveChangesAsync();

            var svc = new DemotionService(ctx);
            await svc.ExecuteMonthlyDemotionAsync();

            var customer = await ctx.Customers.FindAsync(1);
            Assert.Equal("VIP", customer!.Tier);
        }
    }

    /// <summary>
    /// A VIP customer with monthly spending below 200,000 VND must be demoted to Standard.
    /// </summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task Demotion_VipDemotedWhenMonthlySpentLow()
    {
        // **Validates: Requirements 9.3, 9.5**
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Customers.Add(TestDbHelper.CreateCustomer(1, "VIP", monthlySpent: 50_000m));
            await ctx.SaveChangesAsync();

            var svc = new DemotionService(ctx);
            await svc.ExecuteMonthlyDemotionAsync();

            var customer = await ctx.Customers.FindAsync(1);
            Assert.Equal("Standard", customer!.Tier);
        }
    }

    /// <summary>
    /// After demotion, a PointTransaction of Type="Demotion" must be recorded for each demoted customer.
    /// </summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task Demotion_CreatesPointTransactionRecord()
    {
        // **Validates: Requirements 9.5**
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Customers.Add(TestDbHelper.CreateCustomer(1, "VIP", monthlySpent: 50_000m));
            await ctx.SaveChangesAsync();

            var svc = new DemotionService(ctx);
            await svc.ExecuteMonthlyDemotionAsync();

            var txn = ctx.PointTransactions
                         .FirstOrDefault(pt => pt.CustomerId == 1 && pt.Type == "Demotion");
            Assert.NotNull(txn);
        }
    }
}
