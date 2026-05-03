using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Helpers;
using BaiTapLon.Models;

namespace BaiTapLon;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Tạo DbContext options
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(AppConfig.ConnectionString);

        // Đảm bảo database đã được tạo và migration mới nhất
        using (var context = new AppDbContext(optionsBuilder.Options))
        {
            context.Database.Migrate();
            SeedUsers(context);
        }

        // Chạy form đăng nhập
        Application.Run(new Forms.FrmLogin());
    }

    /// <summary>
    /// Seed tài khoản mặc định (chỉ chạy lần đầu khi DB chưa có user).
    /// Dùng runtime seed để BCrypt hash đúng cách.
    /// </summary>
    private static void SeedUsers(AppDbContext context)
    {
        if (context.Users.Any()) return;

        context.Users.AddRange(
            new User
            {
                FullName = "Quản trị viên",
                Username = "admin",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
                Role = "Admin",
                Phone = "0123456789",
                IsActive = true,
                CreatedAt = DateTime.Now
            },
            new User
            {
                FullName = "Nhân viên bán vé",
                Username = "staff",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("staff123"),
                Role = "Staff",
                Phone = "0987654321",
                IsActive = true,
                CreatedAt = DateTime.Now
            }
        );
        context.SaveChanges();
    }

    /// <summary>
    /// Tạo DbContext mới (sử dụng ở khắp nơi trong app).
    /// </summary>
    public static AppDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(AppConfig.ConnectionString);
        return new AppDbContext(optionsBuilder.Options);
    }
}