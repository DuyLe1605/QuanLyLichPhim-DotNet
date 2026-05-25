using BaiTapLon.Api.Dtos;
using BaiTapLon.Api.Services;
using BaiTapLon.Data;
using BaiTapLon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Api.Controllers;

[Route("api/profile")]
public class ProfileController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;

    public ProfileController(AppDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetProfile()
    {
        var customerId = GetCustomerId(_tokens);
        if (customerId is null) return Unauthorized();

        var profile = await _db.Customers
            .AsNoTracking()
            .Where(c => c.Id == customerId.Value)
            .Select(c => new
            {
                c.Id,
                c.FullName,
                c.Username,
                c.Email,
                c.Phone,
                c.MemberCode,
                c.Tier,
                c.TotalPoints,
                c.LoyaltyPoints,
                c.MembershipPoints,
                c.MonthlySpent,
                c.TotalSpent,
                c.CreatedAt
            })
            .FirstOrDefaultAsync();

        return profile is null ? NotFound() : profile;
    }

    [HttpPut]
    public async Task<ActionResult<CustomerDto>> UpdateProfile(ProfileUpdateRequest request)
    {
        var customerId = GetCustomerId(_tokens);
        if (customerId is null) return Unauthorized();

        var customer = await _db.Customers.FindAsync(customerId.Value);
        if (customer is null) return NotFound();

        var email = request.Email.Trim();
        var emailTaken = await _db.Customers.AnyAsync(c => c.Id != customer.Id && c.Email == email);
        if (emailTaken) return Conflict(new { message = "Email đã được sử dụng." });

        customer.FullName = request.FullName.Trim();
        customer.Email = email;
        customer.Phone = request.Phone.Trim();
        await _db.SaveChangesAsync();

        return customer.ToDto();
    }

    [HttpGet("points")]
    public async Task<ActionResult<IEnumerable<object>>> GetPoints()
    {
        var customerId = GetCustomerId(_tokens);
        if (customerId is null) return Unauthorized();

        return await _db.PointTransactions
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId.Value)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new { p.Id, p.Points, p.Type, p.Description, p.CreatedAt })
            .ToListAsync();
    }

    [HttpPost("coupons/redeem")]
    public async Task<ActionResult<object>> RedeemCoupon(CouponRedeemRequest request)
    {
        var customerId = GetCustomerId(_tokens);
        if (customerId is null) return Unauthorized();

        var code = request.Code.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(code))
            return BadRequest(new { message = "Vui lòng nhập mã coupon." });

        await using var tx = await _db.Database.BeginTransactionAsync();

        var coupon = await _db.Coupons.FirstOrDefaultAsync(c => c.Code == code);
        if (coupon is null) return NotFound(new { message = "Mã coupon không hợp lệ." });
        if (!coupon.IsActive) return BadRequest(new { message = "Mã coupon không còn hoạt động." });
        if (coupon.ExpirationDate <= DateTime.Now) return BadRequest(new { message = "Mã coupon đã hết hạn." });
        if (coupon.UsedCount >= coupon.MaxRedemptions) return BadRequest(new { message = "Mã coupon đã hết lượt sử dụng." });

        var alreadyRedeemed = await _db.CouponRedemptions
            .AnyAsync(r => r.CouponId == coupon.Id && r.CustomerId == customerId.Value);
        if (alreadyRedeemed) return Conflict(new { message = "Bạn đã sử dụng mã coupon này rồi." });

        var customer = await _db.Customers.FindAsync(customerId.Value);
        if (customer is null) return NotFound(new { message = "Không tìm thấy khách hàng." });

        customer.LoyaltyPoints += coupon.PointsAwarded;
        customer.TotalPoints += coupon.PointsAwarded;
        coupon.UsedCount++;

        _db.CouponRedemptions.Add(new CouponRedemption
        {
            CouponId = coupon.Id,
            CustomerId = customer.Id,
            RedeemedAt = DateTime.Now
        });
        _db.PointTransactions.Add(new PointTransaction
        {
            CustomerId = customer.Id,
            Points = coupon.PointsAwarded,
            Type = "Earn",
            Description = $"Nhập mã coupon: {coupon.Code}",
            CreatedAt = DateTime.Now
        });

        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return new
        {
            coupon.Code,
            coupon.PointsAwarded,
            customer.TotalPoints,
            customer.LoyaltyPoints,
            message = $"Đổi mã thành công. Bạn nhận được {coupon.PointsAwarded:N0} điểm."
        };
    }
}
