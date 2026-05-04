namespace BaiTapLon.Models;

public class Booking
{
    public int Id { get; set; }
    public string BookingCode { get; set; } = string.Empty; // "BK-XXXXXX"
    public int CustomerId { get; set; }
    public int ShowtimeId { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending" | "Paid" | "CheckedIn" | "Cancelled"
    public string PaymentMethod { get; set; } = "QR"; // "QR" | "Cash" | "Card" | "Transfer"
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public int? VoucherId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public Customer Customer { get; set; } = null!;
    public Showtime Showtime { get; set; } = null!;
    public Voucher? Voucher { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
