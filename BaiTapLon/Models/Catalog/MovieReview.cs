namespace BaiTapLon.Models;

public class MovieReview
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int MovieId { get; set; }
    public int Rating { get; set; } // 1-5
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public virtual Customer Customer { get; set; } = null!;
    public virtual Movie Movie { get; set; } = null!;
}
