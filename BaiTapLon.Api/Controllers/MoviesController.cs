using BaiTapLon.Api.Dtos;
using BaiTapLon.Api.Services;
using BaiTapLon.Data;
using BaiTapLon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Api.Controllers;

[Route("api/movies")]
public class MoviesController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;

    public MoviesController(AppDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetMovies([FromQuery] string? search, [FromQuery] int? genreId)
    {
        var query = _db.Movies
            .AsNoTracking()
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .Where(m => m.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(m => m.Title.Contains(search.Trim()));
        }

        if (genreId is not null)
        {
            query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == genreId));
        }

        var movies = await query
            .OrderByDescending(m => m.ReleaseDate ?? m.CreatedAt)
            .Select(m => new
            {
                m.Id,
                m.Code,
                m.Title,
                m.Duration,
                m.AgeRating,
                m.PosterPath,
                m.ReleaseDate,
                m.EndDate,
                Genres = m.MovieGenres.Select(mg => new { mg.Genre.Id, mg.Genre.Name }),
                AverageRating = _db.MovieReviews.Where(r => r.MovieId == m.Id).Average(r => (double?)r.Rating) ?? 0,
                ReviewCount = _db.MovieReviews.Count(r => r.MovieId == m.Id)
            })
            .ToListAsync();

        return movies;
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<object>> GetMovie(int id)
    {
        var movie = await _db.Movies
            .AsNoTracking()
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .Where(m => m.Id == id && m.IsActive)
            .Select(m => new
            {
                m.Id,
                m.Code,
                m.Title,
                m.Director,
                m.Actors,
                m.Duration,
                m.AgeRating,
                m.Description,
                m.PosterPath,
                m.TrailerUrl,
                m.ReleaseDate,
                m.EndDate,
                Genres = m.MovieGenres.Select(mg => new { mg.Genre.Id, mg.Genre.Name }),
                AverageRating = _db.MovieReviews.Where(r => r.MovieId == m.Id).Average(r => (double?)r.Rating) ?? 0,
                ReviewCount = _db.MovieReviews.Count(r => r.MovieId == m.Id)
            })
            .FirstOrDefaultAsync();

        return movie is null ? NotFound() : movie;
    }

    [HttpGet("{id:int}/showtimes")]
    public async Task<ActionResult<IEnumerable<object>>> GetShowtimes(int id, [FromQuery] DateTime? date)
    {
        var selectedDate = (date ?? DateTime.Today).Date;

        var showtimes = await _db.Showtimes
            .AsNoTracking()
            .Include(s => s.Room)
            .Where(s => s.MovieId == id && s.IsActive && s.StartTime.Date == selectedDate)
            .OrderBy(s => s.StartTime)
            .Select(s => new
            {
                s.Id,
                s.StartTime,
                s.EndTime,
                s.BasePrice,
                Room = new { s.Room.Id, s.Room.Name, s.Room.Type }
            })
            .ToListAsync();

        return showtimes;
    }

    [HttpGet("{id:int}/reviews")]
    public async Task<ActionResult<IEnumerable<object>>> GetReviews(int id)
    {
        return await _db.MovieReviews
            .AsNoTracking()
            .Include(r => r.Customer)
            .Where(r => r.MovieId == id)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.Comment,
                r.CreatedAt,
                Customer = new { r.Customer.Id, r.Customer.FullName }
            })
            .ToListAsync();
    }

    [HttpPost("{id:int}/reviews")]
    public async Task<ActionResult<object>> CreateReview(int id, ReviewRequest request)
    {
        var customerId = GetCustomerId(_tokens);
        if (customerId is null) return Unauthorized();
        if (request.Rating is < 1 or > 5) return BadRequest(new { message = "Rating phải từ 1 đến 5." });

        var exists = await _db.Movies.AnyAsync(m => m.Id == id && m.IsActive);
        if (!exists) return NotFound();

        var review = new MovieReview
        {
            MovieId = id,
            CustomerId = customerId.Value,
            Rating = request.Rating,
            Comment = request.Comment,
            CreatedAt = DateTime.Now
        };

        _db.MovieReviews.Add(review);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetReviews), new { id }, new { review.Id, review.Rating, review.Comment, review.CreatedAt });
    }
}
