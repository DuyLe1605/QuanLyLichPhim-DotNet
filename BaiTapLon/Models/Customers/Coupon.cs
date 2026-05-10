namespace BaiTapLon.Models;

public class Coupon
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;       // Unique, e.g. "REWARD2026"
    public int PointsAwarded { get; set; }                  // Points granted on redemption
    public DateTime ExpirationDate { get; set; }            // Must be in the future at creation
    public int MaxRedemptions { get; set; }                 // Max number of unique customers
    public int UsedCount { get; set; } = 0;                 // Current redemption count
    public bool IsActive { get; set; } = true;              // Admin can deactivate
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public ICollection<CouponRedemption> Redemptions { get; set; } = new List<CouponRedemption>();
}
