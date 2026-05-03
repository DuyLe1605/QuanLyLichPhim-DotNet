using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using BaiTapLon.Helpers;

namespace BaiTapLon.Data;

/// <summary>
/// Factory class cho EF Core CLI tools (dotnet ef migrations).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(AppConfig.ConnectionString);
        return new AppDbContext(optionsBuilder.Options);
    }
}
