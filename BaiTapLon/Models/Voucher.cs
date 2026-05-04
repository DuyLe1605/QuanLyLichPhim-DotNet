namespace BaiTapLon.Models;

public class Voucher
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty; // "SUMMER2026"
    public string Type { get; set; } = "Percent"; // "Percent" | "Fixed" | "FreeTicket"
    public decimal Value { get; set; } // 10 (= 10%) hoặc 50000 (= 50,000đ)
    public decimal? MaxDiscount { get; set; } // Giảm tối đa (chỉ cho Percent)
    public int MaxUses { get; set; } = 100;
    public int UsedCount { get; set; } = 0;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
}
