namespace BaiTapLon.Models;

public class Invoice
{
    public int Id { get; set; }
    public int UserId { get; set; } // Nhân viên bán
    public string? CustomerName { get; set; }
    public string? CustomerPhone { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal ReceivedAmount { get; set; }
    public decimal ChangeAmount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    public ICollection<InvoiceSnack> InvoiceSnacks { get; set; } = new List<InvoiceSnack>();
}
