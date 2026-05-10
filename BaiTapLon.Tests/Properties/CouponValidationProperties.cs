using BaiTapLon.Services;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Xunit;

namespace BaiTapLon.Tests.Properties;

/// <summary>
/// Property-based tests for coupon validation logic.
/// Covers Properties 1–6 from the design document.
/// </summary>
[Trait("Feature", "loyalty-coupon-system")]
public class CouponValidationProperties
{
    // ─────────────────────────────────────────────────────────────────────────
    // Property 1: Coupon Creation Rejects Invalid Parameters
    // Validates: Requirements 1.3, 1.4
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// For any past expiration date, CreateAsync must fail and leave the table empty.
    /// **Validates: Requirements 1.3**
    /// </summary>
    [Property]
    [Trait("Feature", "loyalty-coupon-system")]
    public Property Creation_RejectsExpiredDate(PositiveInt daysAgo)
    {
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            var svc = new CouponService(ctx);
            var pastDate = DateTime.Now.AddDays(-daysAgo.Get);

            var result = svc.CreateAsync("CODE1", 100, pastDate, 5).GetAwaiter().GetResult();

            return (!result.Success && ctx.Coupons.Count() == 0).ToProperty();
        }
    }

    /// <summary>
    /// For any maxRedemptions value less than 1, CreateAsync must fail and leave the table empty.
    /// **Validates: Requirements 1.4**
    /// </summary>
    [Property]
    [Trait("Feature", "loyalty-coupon-system")]
    public Property Creation_RejectsZeroOrNegativeMaxRedemptions(NegativeInt maxRed)
    {
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            var svc = new CouponService(ctx);
            var result = svc.CreateAsync("CODE2", 100, DateTime.Now.AddDays(30), maxRed.Get)
                            .GetAwaiter().GetResult();

            return (!result.Success && ctx.Coupons.Count() == 0).ToProperty();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Property 2: Coupon Code Uniqueness
    // Validates: Requirements 1.2
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// For any existing coupon code, a second creation with the same code must fail.
    /// **Validates: Requirements 1.2**
    /// </summary>
    [Property]
    [Trait("Feature", "loyalty-coupon-system")]
    public Property CodeUniqueness_DuplicateCodeFails(NonEmptyString code)
    {
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            var svc = new CouponService(ctx);
            // Sanitise: remove spaces, uppercase, cap length
            var c = code.Get.Replace(" ", "A").ToUpper();
            c = c[..Math.Min(c.Length, 20)];

            svc.CreateAsync(c, 100, DateTime.Now.AddDays(30), 5).GetAwaiter().GetResult();
            var result2 = svc.CreateAsync(c, 200, DateTime.Now.AddDays(60), 10)
                             .GetAwaiter().GetResult();

            return (!result2.Success).ToProperty();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Property 3: Coupon Code Immutability
    // Validates: Requirements 2.2
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// After creation, UpdateAsync must not change the coupon code.
    /// **Validates: Requirements 2.2**
    /// </summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task CodeImmutability_UpdateDoesNotChangeCode()
    {
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            var svc = new CouponService(ctx);
            var (_, _, coupon) = await svc.CreateAsync("IMMUTABLE", 100, DateTime.Now.AddDays(30), 5);
            await svc.UpdateAsync(coupon!.Id, DateTime.Now.AddDays(60), 10, false);
            var updated = await svc.GetByIdAsync(coupon.Id);
            Assert.Equal("IMMUTABLE", updated?.Code);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Property 4: Coupon Validation Rejects Invalid Redemptions
    // Validates: Requirements 2.3, 3.4, 3.5, 3.6, 3.7, 3.8, 4.5
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>**Validates: Requirements 3.4**</summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task Redemption_RejectsNonExistentCode()
    {
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Customers.Add(TestDbHelper.CreateCustomer(1));
            await ctx.SaveChangesAsync();
            var svc = new CouponService(ctx);
            var result = await svc.RedeemAsync(1, "NONEXISTENT");
            Assert.False(result.Success);
            Assert.Equal("Mã không hợp lệ", result.Message);
        }
    }

    /// <summary>**Validates: Requirements 3.5**</summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task Redemption_RejectsInactiveCoupon()
    {
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Customers.Add(TestDbHelper.CreateCustomer(1));
            ctx.Coupons.Add(TestDbHelper.CreateCoupon(1, "INACTIVE", isActive: false));
            await ctx.SaveChangesAsync();
            var svc = new CouponService(ctx);
            var result = await svc.RedeemAsync(1, "INACTIVE");
            Assert.False(result.Success);
            Assert.Equal("Mã không còn hoạt động", result.Message);
        }
    }

    /// <summary>**Validates: Requirements 3.6**</summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task Redemption_RejectsExpiredCoupon()
    {
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Customers.Add(TestDbHelper.CreateCustomer(1));
            ctx.Coupons.Add(TestDbHelper.CreateCoupon(1, "EXPIRED", expirationDate: DateTime.Now.AddDays(-1)));
            await ctx.SaveChangesAsync();
            var svc = new CouponService(ctx);
            var result = await svc.RedeemAsync(1, "EXPIRED");
            Assert.False(result.Success);
            Assert.Equal("Mã đã hết hạn", result.Message);
        }
    }

    /// <summary>**Validates: Requirements 3.7**</summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task Redemption_RejectsMaxedOutCoupon()
    {
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Customers.Add(TestDbHelper.CreateCustomer(1));
            ctx.Coupons.Add(TestDbHelper.CreateCoupon(1, "MAXED", maxRedemptions: 5, usedCount: 5));
            await ctx.SaveChangesAsync();
            var svc = new CouponService(ctx);
            var result = await svc.RedeemAsync(1, "MAXED");
            Assert.False(result.Success);
            Assert.Equal("Mã đã hết lượt sử dụng", result.Message);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Property 5: Successful Coupon Redemption Completeness
    // Validates: Requirements 3.2, 3.3, 4.3, 4.4, 10.1, 10.3
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// For any valid coupon with P points and an eligible customer, after successful
    /// redemption all four post-conditions must hold simultaneously.
    /// **Validates: Requirements 3.2, 3.3, 4.3, 4.4, 10.1, 10.3**
    /// </summary>
    [Property]
    [Trait("Feature", "loyalty-coupon-system")]
    public Property SuccessfulRedemption_AllPostConditionsHold(PositiveInt points)
    {
        var pts = Math.Min(points.Get, 10_000);
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Customers.Add(TestDbHelper.CreateCustomer(1));
            ctx.Coupons.Add(TestDbHelper.CreateCoupon(1, "VALID", pointsAwarded: pts));
            ctx.SaveChanges();

            var svc = new CouponService(ctx);
            var result = svc.RedeemAsync(1, "VALID").GetAwaiter().GetResult();

            var customer = ctx.Customers.Find(1)!;
            var coupon = ctx.Coupons.Find(1)!;
            var txn = ctx.PointTransactions
                         .FirstOrDefault(t => t.CustomerId == 1 && t.Type == "Earn");
            var redemption = ctx.CouponRedemptions
                                .FirstOrDefault(r => r.CouponId == 1 && r.CustomerId == 1);

            return (result.Success
                    && customer.LoyaltyPoints == pts
                    && txn != null
                    && redemption != null
                    && coupon.UsedCount == 1).ToProperty();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Property 6: One Redemption Per Customer Per Coupon
    // Validates: Requirements 3.5, 10.2
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// A second redemption attempt by the same customer on the same coupon must fail.
    /// **Validates: Requirements 3.5, 10.2**
    /// </summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task OneRedemptionPerCustomer_SecondAttemptFails()
    {
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Customers.Add(TestDbHelper.CreateCustomer(1));
            ctx.Coupons.Add(TestDbHelper.CreateCoupon(1, "ONCE", maxRedemptions: 10));
            await ctx.SaveChangesAsync();
            var svc = new CouponService(ctx);
            await svc.RedeemAsync(1, "ONCE");
            var result2 = await svc.RedeemAsync(1, "ONCE");
            Assert.False(result2.Success);
            Assert.Equal("Bạn đã sử dụng mã này rồi", result2.Message);
        }
    }
}
