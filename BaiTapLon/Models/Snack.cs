namespace BaiTapLon.Models;

public class Snack
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = "Food"; // "Food", "Drink", "Combo"
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<InvoiceSnack> InvoiceSnacks { get; set; } = new List<InvoiceSnack>();
}
