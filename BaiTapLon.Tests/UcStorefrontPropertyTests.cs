// Feature: customer-ui-improvements, Property 4
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
/// Property-based tests for UcStorefront hot-movies query.
/// </summary>
public class UcStorefrontPropertyTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates an isolated in-memory SQLite context with schema created.
    /// The caller is responsible for disposing both the context and the connection.
    /// </summary>
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
    /// Seeds a Room row required by Showtime FK.
    /// Returns the seeded room id (the seed data already has rooms 1-3, so we use 1).
    /// </summary>
    private static int GetOrCreateRoomId(AppDbContext context)
    {
        // The seed data in AppDbContext already inserts rooms with Id 1, 2, 3.
        return 1;
    }

    // -----------------------------------------------------------------------
    // Property 4: Home page hot-movies list is bounded and sorted
    // Validates: Requirements 2.2
    // -----------------------------------------------------------------------

    /// <summary>
    /// Property 4: Home page hot-movies list is bounded and sorted.
    /// Validates: Requirements 2.2
    ///
    /// For any collection of movies with varying showtime counts and dates:
    ///   - Result count ≤ 6
    ///   - All returned movies have at least one active future showtime
    ///   - Items are ordered ascending by nearest showtime start time
    ///
    /// Tag: // Feature: customer-ui-improvements, Property 4
    /// </summary>
    [Fact]
    public async Task HotMovies_BoundedAndSorted_WhenMoreThanSixMoviesExist()
    {
        // Arrange: seed 10 active movies, each with one active future showtime
        var (context, connection) = CreateContext();
        await using (connection)
        await using (context)
        {
            var now = new DateTime(2030, 1, 1, 12, 0, 0);
            int roomId = GetOrCreateRoomId(context);

            for (int i = 1; i <= 10; i++)
            {
                var movie = new Movie
                {
                    Code = $"MV{i:D3}",
                    Title = $"Movie {i}",
                    Duration = 90,
                    AgeRating = "P",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                context.Movies.Add(movie);
                await context.SaveChangesAsync();

                // Stagger start times so ordering is deterministic
                var showtime = new Showtime
                {
                    MovieId = movie.Id,
                    RoomId = roomId,
                    StartTime = now.AddHours(i),   // movie 1 = +1h, movie 10 = +10h
                    EndTime = now.AddHours(i + 2),
                    BasePrice = 100_000,
                    IsActive = true
                };
                context.Showtimes.Add(showtime);
                await context.SaveChangesAsync();
            }

            // Act
            var result = await UcStorefront.GetHotMoviesAsync(context, now);

            // Assert: bounded
            Assert.True(result.Count <= 6,
                $"Expected at most 6 hot movies but got {result.Count}.");

            // Assert: all have at least one active future showtime
            foreach (var movie in result)
            {
                bool hasFutureShowtime = movie.Showtimes.Any(s => s.IsActive && s.StartTime > now);
                Assert.True(hasFutureShowtime,
                    $"Movie '{movie.Title}' has no active future showtime.");
            }

            // Assert: ordered by nearest showtime ascending
            var nearestTimes = result
                .Select(m => m.Showtimes.Where(s => s.IsActive && s.StartTime > now).Min(s => s.StartTime))
                .ToList();

            for (int i = 1; i < nearestTimes.Count; i++)
            {
                Assert.True(nearestTimes[i] >= nearestTimes[i - 1],
                    $"Hot movies are not sorted by nearest showtime: " +
                    $"index {i - 1} = {nearestTimes[i - 1]}, index {i} = {nearestTimes[i]}.");
            }
        }
    }

    [Fact]
    public async Task HotMovies_ExcludesMoviesWithNoFutureShowtime()
    {
        // Arrange: 3 movies with future showtimes, 3 with only past showtimes
        var (context, connection) = CreateContext();
        await using (connection)
        await using (context)
        {
            var now = new DateTime(2030, 6, 1, 12, 0, 0);
            int roomId = GetOrCreateRoomId(context);

            // Movies with future showtimes
            for (int i = 1; i <= 3; i++)
            {
                var movie = new Movie
                {
                    Code = $"MV{i:D3}",
                    Title = $"Future Movie {i}",
                    Duration = 90,
                    AgeRating = "P",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                context.Movies.Add(movie);
                await context.SaveChangesAsync();

                context.Showtimes.Add(new Showtime
                {
                    MovieId = movie.Id,
                    RoomId = roomId,
                    StartTime = now.AddDays(i),
                    EndTime = now.AddDays(i).AddHours(2),
                    BasePrice = 100_000,
                    IsActive = true
                });
                await context.SaveChangesAsync();
            }

            // Movies with only past showtimes (should be excluded)
            for (int i = 4; i <= 6; i++)
            {
                var movie = new Movie
                {
                    Code = $"MV{i:D3}",
                    Title = $"Past Movie {i}",
                    Duration = 90,
                    AgeRating = "P",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                context.Movies.Add(movie);
                await context.SaveChangesAsync();

                context.Showtimes.Add(new Showtime
                {
                    MovieId = movie.Id,
                    RoomId = roomId,
                    StartTime = now.AddDays(-i),   // in the past
                    EndTime = now.AddDays(-i).AddHours(2),
                    BasePrice = 100_000,
                    IsActive = true
                });
                await context.SaveChangesAsync();
            }

            // Act
            var result = await UcStorefront.GetHotMoviesAsync(context, now);

            // Assert: only future-showtime movies are returned
            Assert.Equal(3, result.Count);
            Assert.All(result, m => Assert.StartsWith("Future Movie", m.Title));
        }
    }

    [Fact]
    public async Task HotMovies_ExcludesInactiveMovies()
    {
        // Arrange: 2 active movies with future showtimes, 2 inactive movies with future showtimes
        var (context, connection) = CreateContext();
        await using (connection)
        await using (context)
        {
            var now = new DateTime(2030, 6, 1, 12, 0, 0);
            int roomId = GetOrCreateRoomId(context);

            for (int i = 1; i <= 2; i++)
            {
                var active = new Movie
                {
                    Code = $"ACT{i:D3}",
                    Title = $"Active Movie {i}",
                    Duration = 90,
                    AgeRating = "P",
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };
                context.Movies.Add(active);
                await context.SaveChangesAsync();

                context.Showtimes.Add(new Showtime
                {
                    MovieId = active.Id,
                    RoomId = roomId,
                    StartTime = now.AddDays(i),
                    EndTime = now.AddDays(i).AddHours(2),
                    BasePrice = 100_000,
                    IsActive = true
                });

                var inactive = new Movie
                {
                    Code = $"INA{i:D3}",
                    Title = $"Inactive Movie {i}",
                    Duration = 90,
                    AgeRating = "P",
                    IsActive = false,   // inactive
                    CreatedAt = DateTime.Now
                };
                context.Movies.Add(inactive);
                await context.SaveChangesAsync();

                context.Showtimes.Add(new Showtime
                {
                    MovieId = inactive.Id,
                    RoomId = roomId,
                    StartTime = now.AddDays(i + 10),
                    EndTime = now.AddDays(i + 10).AddHours(2),
                    BasePrice = 100_000,
                    IsActive = true
                });
                await context.SaveChangesAsync();
            }

            // Act
            var result = await UcStorefront.GetHotMoviesAsync(context, now);

            // Assert: only active movies are returned
            Assert.Equal(2, result.Count);
            Assert.All(result, m => Assert.True(m.IsActive));
        }
    }

    /// <summary>
    /// Property 4 (FsCheck): For any non-negative count of movies (0–20),
    /// the hot-movies query result is bounded (≤ 6), all items have at least one
    /// active future showtime, and items are ordered by nearest showtime ascending.
    ///
    /// Tag: // Feature: customer-ui-improvements, Property 4
    /// </summary>
    [Property(MaxTest = 50)]
    public Property HotMovies_BoundedAndSorted_ForArbitraryMovieCounts()
    {
        // Feature: customer-ui-improvements, Property 4
        var gen = Gen.Choose(0, 20);

        return Prop.ForAll(gen.ToArbitrary(), movieCount =>
        {
            // Run async test synchronously for FsCheck compatibility
            return Task.Run(async () =>
            {
                var (context, connection) = CreateContext();
                await using (connection)
                await using (context)
                {
                    var now = new DateTime(2030, 1, 1, 12, 0, 0);
                    int roomId = GetOrCreateRoomId(context);

                    // Seed movieCount active movies, each with one active future showtime
                    for (int i = 1; i <= movieCount; i++)
                    {
                        var movie = new Movie
                        {
                            Code = $"MV{i:D3}",
                            Title = $"Movie {i}",
                            Duration = 90,
                            AgeRating = "P",
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };
                        context.Movies.Add(movie);
                        await context.SaveChangesAsync();

                        context.Showtimes.Add(new Showtime
                        {
                            MovieId = movie.Id,
                            RoomId = roomId,
                            StartTime = now.AddHours(i),
                            EndTime = now.AddHours(i + 2),
                            BasePrice = 100_000,
                            IsActive = true
                        });
                        await context.SaveChangesAsync();
                    }

                    var result = await UcStorefront.GetHotMoviesAsync(context, now);

                    // Property: count ≤ 6
                    if (result.Count > 6) return false;

                    // Property: all items have at least one active future showtime
                    foreach (var movie in result)
                    {
                        if (!movie.Showtimes.Any(s => s.IsActive && s.StartTime > now))
                            return false;
                    }

                    // Property: ordered by nearest showtime ascending
                    var nearestTimes = result
                        .Select(m => m.Showtimes
                            .Where(s => s.IsActive && s.StartTime > now)
                            .Min(s => s.StartTime))
                        .ToList();

                    for (int i = 1; i < nearestTimes.Count; i++)
                    {
                        if (nearestTimes[i] < nearestTimes[i - 1])
                            return false;
                    }

                    return true;
                }
            }).GetAwaiter().GetResult();
        });
    }
}
