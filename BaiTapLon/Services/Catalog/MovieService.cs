using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class MovieService
{
    private readonly AppDbContext _context;

    public MovieService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Movie>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .AsNoTracking();

        if (!includeInactive)
            query = query.Where(m => m.IsActive);

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
    }

    public async Task<Movie?> GetByIdAsync(int id)
    {
        return await _context.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .FirstOrDefaultAsync(m => m.Id == id);
    }

    public async Task<List<Movie>> SearchAsync(string keyword, int? genreId = null)
    {
        var query = _context.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .Where(m => m.IsActive)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
            query = query.Where(m => m.Code.Contains(keyword) || m.Title.Contains(keyword) || (m.Director != null && m.Director.Contains(keyword)));

        if (genreId.HasValue)
            query = query.Where(m => m.MovieGenres.Any(mg => mg.GenreId == genreId.Value));

        return await query.OrderByDescending(m => m.CreatedAt).ToListAsync();
    }

    public async Task<(bool Success, string Message)> CreateAsync(Movie movie, List<int> genreIds)
    {
        if (string.IsNullOrWhiteSpace(movie.Code))
            return (false, "Mã phim không được để trống!");

        if (string.IsNullOrWhiteSpace(movie.Title))
            return (false, "Tên phim không được để trống!");

        if (movie.Duration <= 0)
            return (false, "Thời lượng phải lớn hơn 0!");

        // Kiểm tra mã phim trùng
        bool codeExists = await _context.Movies.AnyAsync(m => m.Code == movie.Code);
        if (codeExists)
            return (false, $"Mã phim '{movie.Code}' đã tồn tại!");

        movie.CreatedAt = DateTime.Now;
        _context.Movies.Add(movie);
        await _context.SaveChangesAsync();

        // Thêm thể loại
        foreach (var gId in genreIds)
        {
            _context.MovieGenres.Add(new MovieGenre { MovieId = movie.Id, GenreId = gId });
        }
        await _context.SaveChangesAsync();

        return (true, "Thêm phim thành công!");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(Movie movie, List<int> genreIds)
    {
        var existing = await _context.Movies
            .Include(m => m.MovieGenres)
            .FirstOrDefaultAsync(m => m.Id == movie.Id);

        if (existing == null)
            return (false, "Phim không tồn tại!");

        existing.Code = movie.Code;
        existing.Title = movie.Title;
        existing.Director = movie.Director;
        existing.Actors = movie.Actors;
        existing.Duration = movie.Duration;
        existing.AgeRating = movie.AgeRating;
        existing.Description = movie.Description;
        existing.TrailerUrl = movie.TrailerUrl;
        existing.ReleaseDate = movie.ReleaseDate;

        if (!string.IsNullOrWhiteSpace(movie.PosterPath))
        {
            existing.PosterPath = movie.PosterPath;
            existing.Poster = movie.Poster;
        }
        else if (movie.Poster != null)
        {
            existing.Poster = movie.Poster;
        }

        // Cập nhật thể loại: xóa cũ, thêm mới
        _context.MovieGenres.RemoveRange(existing.MovieGenres);
        foreach (var gId in genreIds)
        {
            _context.MovieGenres.Add(new MovieGenre { MovieId = movie.Id, GenreId = gId });
        }

        await _context.SaveChangesAsync();
        return (true, "Cập nhật phim thành công!");
    }

    public async Task<(bool Success, string Message)> SoftDeleteAsync(int id)
    {
        var movie = await _context.Movies.FindAsync(id);
        if (movie == null)
            return (false, "Phim không tồn tại!");

        movie.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, "Đã xóa phim!");
    }

    public async Task<List<Genre>> GetAllGenresAsync()
    {
        return await _context.Genres.AsNoTracking().OrderBy(g => g.Name).ToListAsync();
    }
}
