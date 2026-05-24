using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class SnackService
{
    private static readonly string[] ValidCategories = ["Food", "Drink", "Combo"];
    private readonly AppDbContext _context;

    public SnackService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<Snack>> GetAllActiveAsync()
    {
        return await _context.Snacks
            .AsNoTracking()
            .Where(s => s.IsActive)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<Snack>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Snacks.AsNoTracking();
        if (!includeInactive) query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<Snack>> GetByCategoryAsync(string category)
    {
        var normalized = NormalizeCategory(category);
        return await _context.Snacks
            .AsNoTracking()
            .Where(s => s.IsActive && s.Category == normalized)
            .OrderBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<List<Snack>> SearchAsync(string keyword, string? category = null, bool includeInactive = true)
    {
        var query = _context.Snacks.AsNoTracking();

        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var term = keyword.Trim();
            query = query.Where(s => s.Name.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "All")
        {
            var normalized = NormalizeCategory(category);
            query = query.Where(s => s.Category == normalized);
        }

        return await query
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Name)
            .ToListAsync();
    }

    public async Task<Snack?> GetByIdAsync(int id)
    {
        return await _context.Snacks.FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<(bool Success, string Message)> CreateAsync(Snack snack)
    {
        var validation = Validate(snack);
        if (!validation.Success) return validation;

        snack.Name = snack.Name.Trim();
        snack.Category = NormalizeCategory(snack.Category);
        snack.IsActive = true;

        _context.Snacks.Add(snack);
        await _context.SaveChangesAsync();
        return (true, "Thêm món thành công!");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(Snack snack)
    {
        var validation = Validate(snack);
        if (!validation.Success) return validation;

        var existing = await _context.Snacks.FindAsync(snack.Id);
        if (existing == null) return (false, "Món không tồn tại!");

        existing.Name = snack.Name.Trim();
        existing.Price = snack.Price;
        existing.Category = NormalizeCategory(snack.Category);
        existing.IsActive = snack.IsActive;
        existing.ImagePath = snack.ImagePath;

        await _context.SaveChangesAsync();
        return (true, "Cập nhật món thành công!");
    }

    public async Task<(bool Success, string Message)> SoftDeleteAsync(int id)
    {
        var snack = await _context.Snacks.FindAsync(id);
        if (snack == null) return (false, "Món không tồn tại!");

        snack.IsActive = false;
        await _context.SaveChangesAsync();
        return (true, "Đã ẩn món khỏi menu bán hàng!");
    }

    private static (bool Success, string Message) Validate(Snack snack)
    {
        if (string.IsNullOrWhiteSpace(snack.Name))
            return (false, "Tên món không được để trống!");

        if (snack.Price <= 0)
            return (false, "Giá món phải lớn hơn 0!");

        if (!ValidCategories.Contains(NormalizeCategory(snack.Category)))
            return (false, "Phân loại không hợp lệ!");

        return (true, "");
    }

    private static string NormalizeCategory(string category)
    {
        return ValidCategories.FirstOrDefault(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase)) ?? "Food";
    }
}
