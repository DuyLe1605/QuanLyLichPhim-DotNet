namespace BaiTapLon.Models;

public class Invoice
{
    public int Id { get; set; }
    public int UserId { get; set; } // Nhân viên bán
    public int? CustomerId { get; set; } // Khách hàng thành viên (nullable)
    public int? ShiftId { get; set; } // Ca làm việc (nullable)
    public int? VoucherId { get; set; } // Mã giảm giá (nullable)
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public string PaymentMethod { get; set; } = "Cash"; // "Cash" | "Card" | "Transfer" | "QR"
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; } = 0; // Số tiền giảm giá
    public decimal ReceivedAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public User User { get; set; } = null!;
    public Customer? Customer { get; set; }
    public Shift? Shift { get; set; }
    public Voucher? Voucher { get; set; }
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<InvoiceSnack> InvoiceSnacks { get; set; } = new List<InvoiceSnack>();
    public ICollection<PointTransaction> PointTransactions { get; set; } = new List<PointTransaction>();
}
