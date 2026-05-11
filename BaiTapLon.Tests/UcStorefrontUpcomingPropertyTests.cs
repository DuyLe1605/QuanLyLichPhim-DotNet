// Feature: customer-ui-improvements, Property 5
using BaiTapLon.Data;
using BaiTapLon.Forms.Customer;
using BaiTapLon.Models;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BaiTapLon.Tests;

/// <summary>
/// Property-based tests for UcStorefront upcoming movies query.
/// </summary>
public class UcStorefrontUpcomingPropertyTests
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
    /// Property 5: Home page upcoming-movies list is bounded and sorted.
    /// Validates: Requirements 2.3
    ///
    /// For any collection of movies with varying release dates:
    ///   - Result count &lt;= 6
    ///   - All items have ReleaseDate > today
    ///   - Items are ordered by ReleaseDate ascending
    ///
    /// Tag: // Feature: customer-ui-improvements, Property 5
    /// </summary>
    [Property(MaxTest = 50)]
    public Property UpcomingMovies_BoundedAndSorted_ForArbitraryMovieCounts()
    {
        // Feature: customer-ui-improvements, Property 5
        var gen = Gen.Choose(0, 20);

        return Prop.ForAll(gen.ToArbitrary(), movieCount =>
        {
            return Task.Run(async () =>
            {
                var (context, connection) = CreateContext();
                await using (connection)
                await using (context)
                {
                    var today = new DateTime(2030, 6, 1);

                    // Seed movieCount active movies with future release dates
                    for (int i = 1; i <= movieCount; i++)
                    {
                        context.Movies.Add(new Movie
                        {
                            Code = $"UP{i:D3}",
                            Title = $"Upcoming Movie {i}",
                            Duration = 90,
                            AgeRating = "P",
                            IsActive = true,
                            ReleaseDate = today.AddDays(i), // all in the future
                            CreatedAt = DateTime.Now
                        });
                    }
                    await context.SaveChangesAsync();

                    var result = await UcStorefront.GetUpcomingMoviesAsync(context, today);

                    // Property: count <= 6
                    if (result.Count > 6) return false;

                    // Property: all items have ReleaseDate > today
                    foreach (var movie in result)
                    {
                        if (!movie.ReleaseDate.HasValue || movie.ReleaseDate.Value.Date <= today.Date)
                            return false;
                    }

                    // Property: ordered by ReleaseDate ascending
                    for (int i = 1; i < result.Count; i++)
                    {
                        if (result[i].ReleaseDate < result[i - 1].ReleaseDate)
                            return false;
                    }

                    return true;
                }
            }).GetAwaiter().GetResult();
        });
    }

    [Fact]
    public async Task UpcomingMovies_CappedAtSix_WhenMoreThanSixExist()
    {
        var (context, connection) = CreateContext();
        await using (connection)
        await using (context)
        {
            var today = new DateTime(2030, 1, 1);

            for (int i = 1; i <= 10; i++)
            {
                context.Movies.Add(new Movie
                {
                    Code = $"UP{i:D3}",
                    Title = $"Upcoming Movie {i}",
                    Duration = 90,
                    AgeRating = "P",
                    IsActive = true,
                    ReleaseDate = today.AddDays(i),
                    CreatedAt = DateTime.Now
                });
            }
            await context.SaveChangesAsync();

            var result = await UcStorefront.GetUpcomingMoviesAsync(context, today);

            Assert.True(result.Count <= 6, $"Expected at most 6 upcoming movies but got {result.Count}.");
        }
    }

    [Fact]
    public async Task UpcomingMovies_ExcludesPastReleaseDates()
    {
        var (context, connection) = CreateContext();
        await using (connection)
        await using (context)
        {
            var today = new DateTime(2030, 6, 1);

            // 3 future movies
            for (int i = 1; i <= 3; i++)
            {
                context.Movies.Add(new Movie
                {
                    Code = $"FUT{i:D3}",
                    Title = $"Future Movie {i}",
                    Duration = 90,
                    AgeRating = "P",
                    IsActive = true,
                    ReleaseDate = today.AddDays(i),
                    CreatedAt = DateTime.Now
                });
            }

            // 3 past movies (should be excluded)
            for (int i = 1; i <= 3; i++)
            {
                context.Movies.Add(new Movie
                {
                    Code = $"PST{i:D3}",
                    Title = $"Past Movie {i}",
                    Duration = 90,
                    AgeRating = "P",
                    IsActive = true,
                    ReleaseDate = today.AddDays(-i),
                    CreatedAt = DateTime.Now
                });
            }
            await context.SaveChangesAsync();

            var result = await UcStorefront.GetUpcomingMoviesAsync(context, today);

            Assert.Equal(3, result.Count);
            Assert.All(result, m => Assert.StartsWith("Future Movie", m.Title));
        }
    }

    [Fact]
    public async Task UpcomingMovies_OrderedByReleaseDateAscending()
    {
        var (context, connection) = CreateContext();
        await using (connection)
        await using (context)
        {
            var today = new DateTime(2030, 1, 1);

            // Add movies in reverse order to verify sorting
            for (int i = 6; i >= 1; i--)
            {
                context.Movies.Add(new Movie
                {
                    Code = $"UP{i:D3}",
                    Title = $"Movie {i}",
                    Duration = 90,
                    AgeRating = "P",
                    IsActive = true,
                    ReleaseDate = today.AddDays(i),
                    CreatedAt = DateTime.Now
                });
            }
            await context.SaveChangesAsync();

            var result = await UcStorefront.GetUpcomingMoviesAsync(context, today);

            Assert.Equal(6, result.Count);
            for (int i = 1; i < result.Count; i++)
            {
                Assert.True(result[i].ReleaseDate >= result[i - 1].ReleaseDate,
                    $"Upcoming movies not sorted: index {i - 1} = {result[i - 1].ReleaseDate}, index {i} = {result[i].ReleaseDate}");
            }
        }
    }
}
