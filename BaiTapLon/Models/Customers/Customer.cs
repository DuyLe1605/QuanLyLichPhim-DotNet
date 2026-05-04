namespace BaiTapLon.Models;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string MemberCode { get; set; } = string.Empty; // QR/Barcode: "CM-XXXXXX"
    public string Tier { get; set; } = "Standard"; // "Standard" | "VIP" | "Diamond"
    public int TotalPoints { get; set; } = 0;
    public decimal TotalSpent { get; set; } = 0;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
    public ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
}
