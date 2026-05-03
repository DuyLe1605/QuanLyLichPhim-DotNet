namespace BaiTapLon.Models;

public class Ticket
{
    public int Id { get; set; }
    public int ShowtimeId { get; set; }
    public int SeatId { get; set; }
    public int InvoiceId { get; set; }
    public decimal Price { get; set; } // BasePrice × PriceMultiplier

    // Navigation
    public Showtime Showtime { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
    public Invoice Invoice { get; set; } = null!;
}
