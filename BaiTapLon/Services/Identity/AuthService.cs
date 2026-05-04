using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class AuthService
{
    private readonly AppDbContext _context;

    public AuthService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Xác thực đăng nhập. Trả về User nếu thành công, null nếu thất bại.
    /// </summary>
    public async Task<User?> LoginAsync(string username, string password)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

        if (user == null)
            return null;

        // Verify password bằng BCrypt
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            return null;

        return user;
    }

    /// <summary>
    /// Tạo tài khoản mới (chỉ Admin mới được gọi).
    /// </summary>
    public async Task<(bool Success, string Message)> CreateUserAsync(
        string fullName, string username, string password, string role, string? phone)
    {
        // Kiểm tra username trùng
        var exists = await _context.Users.AnyAsync(u => u.Username == username);
        if (exists)
            return (false, "Tên đăng nhập đã tồn tại!");

        var user = new User
        {
            FullName = fullName,
            Username = username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            Phone = phone,
            IsActive = true,
            CreatedAt = DateTime.Now
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (true, "Tạo tài khoản thành công!");
    }
}
