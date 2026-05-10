namespace BaiTapLon.Models;

public class CouponRedemption
{
    public int Id { get; set; }
    public int CouponId { get; set; }
    public int CustomerId { get; set; }
    public DateTime RedeemedAt { get; set; } = DateTime.Now;

    // Navigation
    public Coupon Coupon { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
}
