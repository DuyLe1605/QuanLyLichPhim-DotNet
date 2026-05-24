using BaiTapLon.Data;
using BaiTapLon.Models;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Services;

public class ReviewService
{
    private readonly AppDbContext _context;

    public ReviewService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<MovieReview>> GetReviewsByMovieAsync(int movieId)
    {
        return await _context.MovieReviews
            .Include(r => r.Customer)
            .Where(r => r.MovieId == movieId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task AddReviewAsync(int customerId, int movieId, int rating, string? comment)
    {
        var review = new MovieReview
        {
            CustomerId = customerId,
            MovieId = movieId,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.Now
        };
        _context.MovieReviews.Add(review);
        await _context.SaveChangesAsync();
    }

    public async Task<double> GetAverageRatingAsync(int movieId)
    {
        var reviews = await _context.MovieReviews.Where(r => r.MovieId == movieId).ToListAsync();
        if (reviews.Count == 0) return 0;
        return reviews.Average(r => r.Rating);
    }
}
