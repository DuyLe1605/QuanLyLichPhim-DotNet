using BaiTapLon.Data;
using BaiTapLon.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Tests;

public static class TestDbHelper
{
    /// <summary>
    /// Creates an in-memory SQLite AppDbContext with schema created.
    /// The caller is responsible for disposing both the context and the connection.
    /// </summary>
    public static (AppDbContext Context, SqliteConnection Connection) CreateSqliteContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return (context, connection);
    }

    public static Customer CreateCustomer(
        int id = 1,
        string tier = "Standard",
        int loyaltyPoints = 0,
        decimal totalSpent = 0,
        decimal monthlySpent = 0)
    {
        return new Customer
        {
            Id = id,
            FullName = $"Test Customer {id}",
            Email = $"test{id}@example.com",
            Phone = "0900000000",
            PasswordHash = "hash",
            MemberCode = $"CM-{id:D6}",
            Tier = tier,
            LoyaltyPoints = loyaltyPoints,
            MembershipPoints = 0,
            TotalPoints = loyaltyPoints,
            TotalSpent = totalSpent,
            MonthlySpent = monthlySpent,
            IsActive = true,
            CreatedAt = DateTime.Now
        };
    }

    public static Coupon CreateCoupon(
        int id = 1,
        string code = "TEST100",
        int pointsAwarded = 100,
        int maxRedemptions = 10,
        int usedCount = 0,
        bool isActive = true,
        DateTime? expirationDate = null)
    {
        return new Coupon
        {
            Id = id,
            Code = code,
            PointsAwarded = pointsAwarded,
            ExpirationDate = expirationDate ?? DateTime.Now.AddDays(30),
            MaxRedemptions = maxRedemptions,
            UsedCount = usedCount,
            IsActive = isActive,
            CreatedAt = DateTime.Now
        };
    }
}
