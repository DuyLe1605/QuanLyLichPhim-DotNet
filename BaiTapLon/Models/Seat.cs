namespace BaiTapLon.Models;

public class Seat
{
    public int Id { get; set; }
    public int RoomId { get; set; }
    public string RowLabel { get; set; } = string.Empty; // "A", "B", "C"...
    public int SeatNumber { get; set; } // 1, 2, 3...
    public string Type { get; set; } = "Standard"; // "Standard", "VIP", "Couple"
    public decimal PriceMultiplier { get; set; } = 1.0m; // 1.0, 1.5, 2.0

    // Navigation
    public Room Room { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    // Computed
    public string Label => $"{RowLabel}{SeatNumber}";
}
