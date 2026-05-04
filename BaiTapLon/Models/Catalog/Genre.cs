namespace BaiTapLon.Models;

public class Genre
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    // Navigation (N-N with Movie)
    public ICollection<MovieGenre> MovieGenres { get; set; } = new List<MovieGenre>();
}
