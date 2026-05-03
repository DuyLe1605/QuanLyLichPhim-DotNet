namespace BaiTapLon.Models;

public class Showtime
{
    public int Id { get; set; }
    public int MovieId { get; set; }
    public int RoomId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; } // Auto = StartTime + Duration + 15 phút
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Movie Movie { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
