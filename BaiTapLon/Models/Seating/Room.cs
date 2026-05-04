namespace BaiTapLon.Models;

public class Room
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty; // "Phòng 1", "Phòng 2"
    public string Type { get; set; } = "2D"; // "2D", "3D", "IMAX"
    public int TotalSeats { get; set; }
    public int Rows { get; set; }
    public int Columns { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
