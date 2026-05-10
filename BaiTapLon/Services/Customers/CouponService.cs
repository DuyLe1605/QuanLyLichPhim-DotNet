using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class CouponService
{
    private readonly AppDbContext _context;

    public CouponService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Tạo mã coupon mới.
    /// Validate: code duy nhất, ngày hết hạn ở tương lai, maxRedemptions >= 1.
    /// </summary>
    public async Task<(bool Success, string Message, Coupon? Coupon)> CreateAsync(
        string code, int pointsAwarded, DateTime expirationDate, int maxRedemptions)
    {
        // Validate: code uniqueness
        bool codeExists = await _context.Coupons
            .AnyAsync(c => c.Code == code);
        if (codeExists)
            return (false, "Mã coupon đã tồn tại", null);

        // Validate: expiration date in future
        if (expirationDate <= DateTime.Now)
            return (false, "Ngày hết hạn phải ở tương lai", null);

        // Validate: maxRedemptions >= 1
        if (maxRedemptions < 1)
            return (false, "Số lượt sử dụng tối thiểu là 1", null);

        var coupon = new Coupon
        {
            Code = code,
            PointsAwarded = pointsAwarded,
            ExpirationDate = expirationDate,
            MaxRedemptions = maxRedemptions,
            UsedCount = 0,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Coupons.Add(coupon);
        await _context.SaveChangesAsync();

        return (true, "Tạo coupon thành công", coupon);
    }

    /// <summary>
    /// Cập nhật coupon. Không cho phép thay đổi code.
    /// Chỉ áp dụng các tham số không null.
    /// </summary>
    public async Task<(bool Success, string Message)> UpdateAsync(
        int couponId, DateTime? expirationDate, int? maxRedemptions, bool? isActive)
    {
        var coupon = await _context.Coupons.FindAsync(couponId);
        if (coupon == null)
            return (false, "Không tìm thấy coupon");

        if (expirationDate.HasValue)
            coupon.ExpirationDate = expirationDate.Value;

        if (maxRedemptions.HasValue)
            coupon.MaxRedemptions = maxRedemptions.Value;

        if (isActive.HasValue)
            coupon.IsActive = isActive.Value;

        await _context.SaveChangesAsync();

        return (true, "Cập nhật coupon thành công");
    }

    /// <summary>
    /// Lấy danh sách coupon, hỗ trợ lọc theo IsActive và tìm kiếm theo code (không phân biệt hoa thường).
    /// </summary>
    public async Task<List<Coupon>> GetAllAsync(bool? activeFilter, string? searchCode)
    {
        var query = _context.Coupons.AsNoTracking().AsQueryable();

        if (activeFilter.HasValue)
            query = query.Where(c => c.IsActive == activeFilter.Value);

        if (!string.IsNullOrWhiteSpace(searchCode))
            query = query.Where(c => c.Code.ToLower().Contains(searchCode.ToLower()));

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    /// <summary>
    /// Lấy coupon theo ID, bao gồm eager-load Redemptions.
    /// </summary>
    public async Task<Coupon?> GetByIdAsync(int id)
    {
        return await _context.Coupons
            .Include(c => c.Redemptions)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    /// <summary>
    /// Lấy danh sách lượt đổi của một coupon, bao gồm thông tin Customer.
    /// </summary>
    public async Task<List<CouponRedemption>> GetRedemptionsAsync(int couponId)
    {
        return await _context.CouponRedemptions
            .AsNoTracking()
            .Include(cr => cr.Customer)
            .Where(cr => cr.CouponId == couponId)
            .OrderByDescending(cr => cr.RedeemedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Đổi mã coupon để nhận điểm thưởng.
    /// Thứ tự validate: tồn tại → đang hoạt động → chưa hết hạn → còn lượt → chưa dùng.
    /// Bọc trong transaction.
    /// </summary>
    public async Task<(bool Success, string Message, int PointsAwarded)> RedeemAsync(
        int customerId, string code)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // (1) Code exists
            var coupon = await _context.Coupons
                .FirstOrDefaultAsync(c => c.Code == code);
            if (coupon == null)
                return (false, "Mã không hợp lệ", 0);

            // (2) Coupon is active
            if (!coupon.IsActive)
                return (false, "Mã không còn hoạt động", 0);

            // (3) Not expired
            if (coupon.ExpirationDate <= DateTime.Now)
                return (false, "Mã đã hết hạn", 0);

            // (4) UsedCount < MaxRedemptions
            if (coupon.UsedCount >= coupon.MaxRedemptions)
                return (false, "Mã đã hết lượt sử dụng", 0);

            // (5) Customer hasn't redeemed this coupon
            bool alreadyRedeemed = await _context.CouponRedemptions
                .AnyAsync(cr => cr.CouponId == coupon.Id && cr.CustomerId == customerId);
            if (alreadyRedeemed)
                return (false, "Bạn đã sử dụng mã này rồi", 0);

            // All validations passed — apply changes
            var customer = await _context.Customers.FindAsync(customerId);
            if (customer == null)
                return (false, "Không tìm thấy khách hàng", 0);

            // Add points to customer
            customer.LoyaltyPoints += coupon.PointsAwarded;

            // Create PointTransaction
            var pointTransaction = new PointTransaction
            {
                CustomerId = customerId,
                Points = coupon.PointsAwarded,
                Type = "Earn",
                Description = $"Nhập mã thưởng: {code}",
                CreatedAt = DateTime.Now
            };
            _context.PointTransactions.Add(pointTransaction);

            // Create CouponRedemption record
            var redemption = new CouponRedemption
            {
                CouponId = coupon.Id,
                CustomerId = customerId,
                RedeemedAt = DateTime.Now
            };
            _context.CouponRedemptions.Add(redemption);

            // Increment UsedCount
            coupon.UsedCount++;

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, $"Đổi mã thành công! Bạn nhận được {coupon.PointsAwarded:N0} điểm.", coupon.PointsAwarded);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    /// <summary>
    /// Kiểm tra mã coupon cho checkout (không lưu thay đổi).
    /// Trả về số điểm sẽ được cộng nếu hợp lệ.
    /// </summary>
    public async Task<(bool Success, string Message, int PointsAwarded)> ValidateForCheckoutAsync(
        int customerId, string code)
    {
        // (1) Code exists
        var coupon = await _context.Coupons
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == code);
        if (coupon == null)
            return (false, "Mã không hợp lệ", 0);

        // (2) Coupon is active
        if (!coupon.IsActive)
            return (false, "Mã không còn hoạt động", 0);

        // (3) Not expired
        if (coupon.ExpirationDate <= DateTime.Now)
            return (false, "Mã đã hết hạn", 0);

        // (4) UsedCount < MaxRedemptions
        if (coupon.UsedCount >= coupon.MaxRedemptions)
            return (false, "Mã đã hết lượt sử dụng", 0);

        // (5) Customer hasn't redeemed this coupon
        bool alreadyRedeemed = await _context.CouponRedemptions
            .AnyAsync(cr => cr.CouponId == coupon.Id && cr.CustomerId == customerId);
        if (alreadyRedeemed)
            return (false, "Bạn đã sử dụng mã này rồi", 0);

        return (true, $"Mã hợp lệ! Bạn sẽ nhận được {coupon.PointsAwarded:N0} điểm.", coupon.PointsAwarded);
    }
}
