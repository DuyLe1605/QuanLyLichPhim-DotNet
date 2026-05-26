using BaiTapLon.Data;
using BaiTapLon.Models;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Services;

public class ReviewService
{
    private const string ReviewRewardSettingKey = "ReviewRewardPoints";
    private const int DefaultReviewRewardPoints = 5;
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

    public async Task<int> AddReviewAsync(int customerId, int movieId, int rating, string? comment)
    {
        var hasPreviousReview = await _context.MovieReviews
            .AnyAsync(r => r.CustomerId == customerId && r.MovieId == movieId);
        var movie = await _context.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == movieId);
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive);
        if (customer is null) return 0;

        _context.MovieReviews.Add(new MovieReview
        {
            CustomerId = customerId,
            MovieId = movieId,
            Rating = rating,
            Comment = comment,
            CreatedAt = DateTime.Now
        });

        var pointsAwarded = hasPreviousReview ? 0 : await GetReviewRewardPointsAsync();
        if (pointsAwarded > 0)
        {
            customer.LoyaltyPoints += pointsAwarded;
            customer.TotalPoints += pointsAwarded;
            _context.PointTransactions.Add(new PointTransaction
            {
                CustomerId = customer.Id,
                Points = pointsAwarded,
                Type = "Earn",
                Description = $"Thưởng đánh giá phim: {movie?.Title ?? movieId.ToString()}",
                CreatedAt = DateTime.Now
            });
        }

        await _context.SaveChangesAsync();
        return pointsAwarded;
    }

    private async Task<int> GetReviewRewardPointsAsync()
    {
        var raw = await _context.SystemSettings
            .AsNoTracking()
            .Where(s => s.Key == ReviewRewardSettingKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        return int.TryParse(raw, out var points) && points >= 0
            ? points
            : DefaultReviewRewardPoints;
    }

    public async Task<double> GetAverageRatingAsync(int movieId)
    {
        var reviews = await _context.MovieReviews.Where(r => r.MovieId == movieId).ToListAsync();
        if (reviews.Count == 0) return 0;
        return reviews.Average(r => r.Rating);
    }
}
