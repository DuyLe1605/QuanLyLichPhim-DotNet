// Feature: customer-ui-improvements, Property 9
using System.Drawing;
using BaiTapLon.Forms.Customer;
using BaiTapLon.Models;
using FsCheck;
using FsCheck.Xunit;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Tests;

/// <summary>
/// Property-based tests for UcMyProfile.
/// </summary>
public class UcMyProfilePropertyTests
{
    /// <summary>
    /// Property 9: Point change formatting is sign-consistent.
    /// Validates: Requirements 3.3, 3.4
    ///
    /// For any integer <paramref name="points"/>:
    ///   - When points >= 0: result text starts with "+" and color is green (80, 200, 120).
    ///   - When points  < 0: result text starts with "-" and color is red  (220, 80, 80).
    /// </summary>
    [Property]
    public bool FormatPointChange_SignConsistent(int points)
    {
        // Feature: customer-ui-improvements, Property 9
        var (text, color) = UcMyProfile.FormatPointChange(points);

        if (points >= 0)
        {
            bool startsWithPlus = text.StartsWith("+");
            bool isGreen = color == Color.FromArgb(80, 200, 120);
            return startsWithPlus && isGreen;
        }
        else
        {
            bool startsWithMinus = text.StartsWith("-");
            bool isRed = color == Color.FromArgb(220, 80, 80);
            return startsWithMinus && isRed;
        }
    }

    /// <summary>
    /// Property 8: Loyalty transaction list is bounded and ordered.
    /// Validates: Requirements 3.2
    ///
    /// For any customer with N point transactions (0 ≤ N ≤ 25), the query used in
    /// LoadProfileAsync SHALL return min(N, 10) transactions ordered by CreatedAt descending.
    /// </summary>
    [Property(MaxTest = 100)]
    public bool LoyaltyTransactions_BoundedAndOrdered(NonNegativeInt rawN)
    {
        // Feature: customer-ui-improvements, Property 8
        // Clamp N to [0, 25] so tests run quickly while covering both sides of the 10-item cap.
        int n = rawN.Get % 26; // 0..25

        return Task.Run(async () =>
        {
            var (context, connection) = TestDbHelper.CreateSqliteContext();
            await using (connection)
            await using (context)
            {
                // Seed one customer
                var customer = new Customer
                {
                    FullName = "Prop8 Customer",
                    Email = $"prop8_{n}@test.com",
                    Phone = "0900000099",
                    PasswordHash = "hash",
                    MemberCode = $"CM-P8-{n:D3}",
                    Tier = "Standard",
                    LoyaltyPoints = 0,
                    MembershipPoints = 0,
                    TotalPoints = 0,
                    TotalSpent = 0,
                    MonthlySpent = 0,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    Username = $"prop8_user_{n}"
                };
                context.Customers.Add(customer);
                await context.SaveChangesAsync();

                // Seed N transactions with distinct, spread-out CreatedAt values
                var baseTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                for (int i = 0; i < n; i++)
                {
                    context.PointTransactions.Add(new PointTransaction
                    {
                        CustomerId = customer.Id,
                        Points = i + 1,
                        Type = "Earn",
                        Description = $"Transaction {i}",
                        CreatedAt = baseTime.AddHours(i) // each transaction 1 hour apart
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

                // Assert count == min(N, 10)
                int expectedCount = Math.Min(n, 10);
                if (result.Count != expectedCount)
                    return false;

                // Assert ordering: each item's CreatedAt >= the next item's CreatedAt (descending)
                for (int i = 0; i < result.Count - 1; i++)
                {
                    if (result[i].CreatedAt < result[i + 1].CreatedAt)
                        return false;
                }

                return true;
            }
        }).GetAwaiter().GetResult();
    }
}
