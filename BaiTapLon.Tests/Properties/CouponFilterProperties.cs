using BaiTapLon.Services;
using Xunit;

namespace BaiTapLon.Tests.Properties;

/// <summary>
/// Property-based tests for coupon filter and search correctness.
/// Covers Property 12 from the design document.
/// </summary>
[Trait("Feature", "loyalty-coupon-system")]
public class CouponFilterProperties
{
    // ─────────────────────────────────────────────────────────────────────────
    // Property 12: Coupon Filter Correctness
    // Validates: Requirements 2.4
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Filtering by activeFilter=true must return only active coupons.
    /// </summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task Filter_ActiveOnlyReturnsActiveCoupons()
    {
        // **Validates: Requirements 2.4**
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Coupons.AddRange(
                TestDbHelper.CreateCoupon(1, "ACTIVE1", isActive: true),
                TestDbHelper.CreateCoupon(2, "INACTIVE1", isActive: false),
                TestDbHelper.CreateCoupon(3, "ACTIVE2", isActive: true)
            );
            await ctx.SaveChangesAsync();

            var svc = new CouponService(ctx);
            var result = await svc.GetAllAsync(activeFilter: true, searchCode: null);

            Assert.All(result, c => Assert.True(c.IsActive));
            Assert.Equal(2, result.Count);
        }
    }

    /// <summary>
    /// Filtering by activeFilter=false must return only inactive coupons.
    /// </summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task Filter_InactiveOnlyReturnsInactiveCoupons()
    {
        // **Validates: Requirements 2.4**
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Coupons.AddRange(
                TestDbHelper.CreateCoupon(1, "ACTIVE1", isActive: true),
                TestDbHelper.CreateCoupon(2, "INACTIVE1", isActive: false),
                TestDbHelper.CreateCoupon(3, "INACTIVE2", isActive: false)
            );
            await ctx.SaveChangesAsync();

            var svc = new CouponService(ctx);
            var result = await svc.GetAllAsync(activeFilter: false, searchCode: null);

            Assert.All(result, c => Assert.False(c.IsActive));
            Assert.Equal(2, result.Count);
        }
    }

    /// <summary>
    /// Searching by a code substring must return only coupons whose code contains that substring.
    /// </summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task Filter_SearchByCodeSubstring()
    {
        // **Validates: Requirements 2.4**
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Coupons.AddRange(
                TestDbHelper.CreateCoupon(1, "SUMMER2026"),
                TestDbHelper.CreateCoupon(2, "WINTER2026"),
                TestDbHelper.CreateCoupon(3, "SPRING2026")
            );
            await ctx.SaveChangesAsync();

            var svc = new CouponService(ctx);
            var result = await svc.GetAllAsync(activeFilter: null, searchCode: "SUMMER");

            Assert.Single(result);
            Assert.Equal("SUMMER2026", result[0].Code);
        }
    }

    /// <summary>
    /// Searching with no filter must return all coupons.
    /// </summary>
    [Fact]
    [Trait("Feature", "loyalty-coupon-system")]
    public async Task Filter_NoFilterReturnsAll()
    {
        // **Validates: Requirements 2.4**
        var (ctx, conn) = TestDbHelper.CreateSqliteContext();
        using (conn)
        using (ctx)
        {
            ctx.Coupons.AddRange(
                TestDbHelper.CreateCoupon(1, "CODE1", isActive: true),
                TestDbHelper.CreateCoupon(2, "CODE2", isActive: false),
                TestDbHelper.CreateCoupon(3, "CODE3", isActive: true)
            );
            await ctx.SaveChangesAsync();

            var svc = new CouponService(ctx);
            var result = await svc.GetAllAsync(activeFilter: null, searchCode: null);

            Assert.Equal(3, result.Count);
        }
    }
}
