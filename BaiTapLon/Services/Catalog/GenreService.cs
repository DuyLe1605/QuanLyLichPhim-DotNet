using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class GenreService
{
    private readonly AppDbContext _context;
    public GenreService(AppDbContext context) { _context = context; }

    public async Task<List<Genre>> GetAllAsync()
        => await _context.Genres.Include(g => g.MovieGenres).OrderBy(g => g.Name).AsNoTracking().ToListAsync();

    public async Task<(bool Ok, string Msg)> CreateAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return (false, "Tên thể loại không được để trống!");

        if (await _context.Genres.AnyAsync(g => g.Name == name.Trim()))
            return (false, "Thể loại đã tồn tại!");

        _context.Genres.Add(new Genre { Name = name.Trim() });
        await _context.SaveChangesAsync();
        return (true, "Thêm thể loại thành công!");
    }

    public async Task<(bool Ok, string Msg)> UpdateAsync(int id, string newName)
    {
        var genre = await _context.Genres.FindAsync(id);
        if (genre == null) return (false, "Không tìm thấy thể loại!");

        if (await _context.Genres.AnyAsync(g => g.Name == newName.Trim() && g.Id != id))
            return (false, "Tên thể loại đã tồn tại!");

        genre.Name = newName.Trim();
        await _context.SaveChangesAsync();
        return (true, "Cập nhật thành công!");
    }

    public async Task<(bool Ok, string Msg)> DeleteAsync(int id)
    {
        var genre = await _context.Genres
            .Include(g => g.MovieGenres)
            .FirstOrDefaultAsync(g => g.Id == id);

        if (genre == null) return (false, "Không tìm thấy thể loại!");

        if (genre.MovieGenres.Any())
            return (false, $"Không thể xóa! Có {genre.MovieGenres.Count} phim đang sử dụng thể loại này.");

        _context.Genres.Remove(genre);
        await _context.SaveChangesAsync();
        return (true, "Đã xóa thể loại!");
    }
}
