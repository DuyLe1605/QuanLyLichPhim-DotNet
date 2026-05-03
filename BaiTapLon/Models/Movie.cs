namespace BaiTapLon.Models;

public class Movie
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Director { get; set; }
    public string? Actors { get; set; }
    public int Duration { get; set; } // phút
    public string AgeRating { get; set; } = "P"; // P, C13, C16, C18
    public string? Description { get; set; }
    public byte[]? Poster { get; set; }
    public string? TrailerUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ReleaseDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Navigation
    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
    public ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();
}
