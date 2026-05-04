namespace BaiTapLon.Models;

public class InvoiceSnack
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public int SnackId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    // Navigation
    public Invoice Invoice { get; set; } = null!;
    public Snack Snack { get; set; } = null!;
}
