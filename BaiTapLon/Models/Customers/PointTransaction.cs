namespace BaiTapLon.Models;

public class PointTransaction
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int? InvoiceId { get; set; } // Nullable: đổi điểm không cần invoice
    public int Points { get; set; } // Dương = tích, Âm = đổi
    public string Type { get; set; } = "Earn"; // "Earn" | "Redeem"
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public Customer Customer { get; set; } = null!;
    public Invoice? Invoice { get; set; }
}
