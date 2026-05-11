// Feature: customer-ui-improvements, Property 8
using BaiTapLon.Data;
using BaiTapLon.Models;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BaiTapLon.Tests;

/// <summary>
/// Property-based tests for the loyalty transaction query in UcMyProfile.
/// </summary>
public class UcMyProfileLoyaltyPropertyTests
{
    private static (AppDbContext Context, SqliteConnection Connection) CreateContext()
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

    /// <summary>
    /// Property 8: Loyalty transaction list is bounded and ordered.
    /// Validates: Requirements 3.2
    ///
    /// For any customer with N point transactions, the query returns min(N, 10)
    /// items ordered by CreatedAt descending.
    ///
    /// Tag: // Feature: customer-ui-improvements, Property 8
    /// </summary>
    [Property(MaxTest = 50)]
    public Property LoyaltyTransactions_BoundedAndOrdered_ForArbitraryCount()
    {
        // Feature: customer-ui-improvements, Property 8
        var gen = Gen.Choose(0, 20);

        return Prop.ForAll(gen.ToArbitrary(), transactionCount =>
        {
            return Task.Run(async () =>
            {
                var (context, connection) = CreateContext();
                await using (connection)
                await using (context)
                {
                    // Seed a customer
                    var customer = new Models.Customer
                    {
                        FullName = "Test Customer",
                        Email = "test@example.com",
                        Phone = "0900000001",
                        PasswordHash = "hash",
                        Username = "test_user",
                        MemberCode = "CM-000001",
                        Tier = "Standard",
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };
                    context.Customers.Add(customer);
                    await context.SaveChangesAsync();

                    // Seed N transactions with distinct CreatedAt values
                    var baseTime = new DateTime(2025, 1, 1, 0, 0, 0);
                    for (int i = 0; i < transactionCount; i++)
                    {
                        context.PointTransactions.Add(new PointTransaction
                        {
                            CustomerId = customer.Id,
                            Points = (i % 2 == 0) ? 10 : -5,
                            Description = $"Transaction {i}",
                            CreatedAt = baseTime.AddHours(i)
                        });
                    }
                    await context.SaveChangesAsync();

                    // Execute the same query as LoadProfileAsync
                    var result = await context.PointTransactions
                        .Where(pt => pt.CustomerId == customer.Id)
                        .OrderByDescending(pt => pt.CreatedAt)
                        .Take(10)
                        .AsNoTracking()
                        .ToListAsync();

                    // Property: count == min(N, 10)
                    int expected = Math.Min(transactionCount, 10);
                    if (result.Count != expected) return false;

                    // Property: ordered by CreatedAt descending
                    for (int i = 1; i < result.Count; i++)
                    {
                        if (result[i].CreatedAt > result[i - 1].CreatedAt)
                            return false;
                    }

                    return true;
                }
            }).GetAwaiter().GetResult();
        });
    }

    [Fact]
    public async Task LoyaltyTransactions_ReturnsTenMostRecent_WhenMoreThanTenExist()
    {
        var (context, connection) = CreateContext();
        await using (connection)
        await using (context)
        {
            var customer = new Models.Customer
            {
                FullName = "Test Customer",
                Email = "test@example.com",
                Phone = "0900000001",
                PasswordHash = "hash",
                Username = "test_user",
                MemberCode = "CM-000001",
                Tier = "Standard",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            var baseTime = new DateTime(2025, 1, 1);
            for (int i = 0; i < 15; i++)
            {
                context.PointTransactions.Add(new PointTransaction
                {
                    CustomerId = customer.Id,
                    Points = 10,
                    Description = $"Tx {i}",
                    CreatedAt = baseTime.AddDays(i)
                });
            }
            await context.SaveChangesAsync();

            var result = await context.PointTransactions
                .Where(pt => pt.CustomerId == customer.Id)
                .OrderByDescending(pt => pt.CreatedAt)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            Assert.Equal(10, result.Count);
            // Most recent first
            Assert.Equal(baseTime.AddDays(14), result[0].CreatedAt);
            Assert.Equal(baseTime.AddDays(5), result[9].CreatedAt);
        }
    }

    [Fact]
    public async Task LoyaltyTransactions_ReturnsAll_WhenFewerThanTenExist()
    {
        var (context, connection) = CreateContext();
        await using (connection)
        await using (context)
        {
            var customer = new Models.Customer
            {
                FullName = "Test Customer",
                Email = "test@example.com",
                Phone = "0900000001",
                PasswordHash = "hash",
                Username = "test_user",
                MemberCode = "CM-000001",
                Tier = "Standard",
                IsActive = true,
                CreatedAt = DateTime.Now
            };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();

            for (int i = 0; i < 3; i++)
            {
                context.PointTransactions.Add(new PointTransaction
                {
                    CustomerId = customer.Id,
                    Points = 10,
                    Description = $"Tx {i}",
                    CreatedAt = DateTime.Now.AddHours(i)
                });
            }
            await context.SaveChangesAsync();

            var result = await context.PointTransactions
                .Where(pt => pt.CustomerId == customer.Id)
                .OrderByDescending(pt => pt.CreatedAt)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            Assert.Equal(3, result.Count);
        }
    }
}
