using BaiTapLon.Services;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace BaiTapLon.Tests.Properties;

/// <summary>
/// Property-based tests for point calculation logic.
/// Covers Properties 7–9 from the design document.
/// </summary>
[Trait("Feature", "loyalty-coupon-system")]
public class PointCalculationProperties
{
    private static readonly string[] Tiers = { "Standard", "VIP", "Diamond" };
    private static readonly decimal[] Multipliers = { 1.0m, 1.5m, 2.0m };

    // ─────────────────────────────────────────────────────────────────────────
    // Property 7: Post-Payment Loyalty Accrual with Tier Multiplier
    // Validates: Requirements 6.1, 8.1, 8.2, 8.3
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// For any payment amount and any tier, LoyaltyEarned must equal
    /// floor(floor(amount / 10,000) × multiplier(tier)).
    /// **Validates: Requirements 6.1, 8.1, 8.2, 8.3**
    /// </summary>
    [Property]
    [Trait("Feature", "loyalty-coupon-system")]
    public Property LoyaltyAccrual_CorrectMultiplier(PositiveInt amountThousands)
    {
        var amount = amountThousands.Get * 1000m;

        // Test all three tiers for this amount
        return Prop.ForAll(
            Gen.Choose(0, 2).ToArbitrary(),
            tierIdx =>
            {
                var (ctx, conn) = TestDbHelper.CreateSqliteContext();
                using (conn)
                using (ctx)
                {
                    ctx.Customers.Add(TestDbHelper.CreateCustomer(1, Tiers[tierIdx]));
                    ctx.SaveChanges();

                    var tierSvc = new TierService(ctx);
                    var loyaltySvc = new LoyaltyService(ctx, tierSvc);
                    var (loyaltyEarned, _) = loyaltySvc.AccruePointsAsync(1, amount, null)
                                                        .GetAwaiter().GetResult();

                    int basePoints = (int)Math.Floor(amount / 10_000m);
                    int expected = (int)Math.Floor(basePoints * Multipliers[tierIdx]);

                    return loyaltyEarned == expected;
                }
            });
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Property 8: Post-Payment Membership Accrual (no multiplier)
    // Validates: Requirements 6.2, 6.4
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// MembershipEarned must always equal floor(amount / 10,000) regardless of tier.
    /// **Validates: Requirements 6.2, 6.4**
    /// </summary>
    [Property]
    [Trait("Feature", "loyalty-coupon-system")]
    public Property MembershipAccrual_NoMultiplier(PositiveInt amountThousands)
    {
        var amount = amountThousands.Get * 1000m;
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            // VIP has 1.5x loyalty multiplier but membership should still be 1x
            ctx.Customers.Add(TestDbHelper.CreateCustomer(1, "VIP"));
            ctx.SaveChanges();

            var tierSvc = new TierService(ctx);
            var loyaltySvc = new LoyaltyService(ctx, tierSvc);
            var (_, membershipEarned) = loyaltySvc.AccruePointsAsync(1, amount, null)
                                                   .GetAwaiter().GetResult();

            int expected = (int)Math.Floor(amount / 10_000m);
            return (membershipEarned == expected).ToProperty();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Property 9: Points Discount Bounded by Balance and Order Total
    // Validates: Requirements 5.3, 5.4, 5.5, 5.6
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The discount granted must never exceed the customer's balance (in VND)
    /// and must never exceed the order total.
    /// **Validates: Requirements 5.3, 5.4, 5.5, 5.6**
    /// </summary>
    [Property]
    [Trait("Feature", "loyalty-coupon-system")]
    public Property PointsDiscount_BoundedByBalanceAndOrderTotal(
        PositiveInt balancePts,
        PositiveInt redeemPts,
        PositiveInt orderThousands)
    {
        var balance = Math.Min(balancePts.Get, 10_000);
        // Ensure redeem <= balance so the request is valid (not an insufficient-balance case)
        var redeem = Math.Min(redeemPts.Get, balance);
        if (redeem == 0) redeem = 1; // avoid zero-points edge case
        var orderTotal = orderThousands.Get * 1000m;

        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Customers.Add(TestDbHelper.CreateCustomer(1, loyaltyPoints: balance));
            ctx.SaveChanges();

            var tierSvc = new TierService(ctx);
            var loyaltySvc = new LoyaltyService(ctx, tierSvc);
            var result = loyaltySvc.RedeemPointsForDiscountAsync(1, redeem, orderTotal)
                                   .GetAwaiter().GetResult();

            return (result.Success
                    && result.DiscountAmount <= (decimal)balance * 1_000m
                    && result.DiscountAmount <= orderTotal).ToProperty();
        }
    }
}
