using BaiTapLon.Models;
using BaiTapLon.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace BaiTapLon.Tests;

public class ShowtimeServiceEndDateTests
{
    [Fact]
    public async Task CreateAsync_RejectsShowtimeBeyondMovieEndDate()
    {
        var (context, connection) = TestDbHelper.CreateSqliteContext();
        await using (connection)
        await using (context)
        {
            var movie = new Movie
            {
                Code = "MV001",
                Title = "Test Movie",
                Duration = 100,
                AgeRating = "P",
                IsActive = true,
                ReleaseDate = DateTime.Today.AddDays(-10),
                EndDate = DateTime.Today
            };

            var room = new Room
            {
                Name = "Room 1",
                Type = "2D",
                Rows = 5,
                Columns = 5,
                TotalSeats = 25,
                IsActive = true
            };

            context.Movies.Add(movie);
            context.Rooms.Add(room);
            await context.SaveChangesAsync();

            var service = new ShowtimeService(context);
            var showtime = new Showtime
            {
                MovieId = movie.Id,
                RoomId = room.Id,
                StartTime = DateTime.Today.AddDays(1).AddHours(9),
                BasePrice = 75000
            };

            var (ok, msg) = await service.CreateAsync(showtime);

            Assert.False(ok);
            Assert.Contains("hết chiếu", msg, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task CreateManyAsync_CreatesMultipleShowtimes_WhenNoConflicts()
    {
        var (context, connection) = TestDbHelper.CreateSqliteContext();
        await using (connection)
        await using (context)
        {
            var movie = new Movie
            {
                Code = "MV002",
                Title = "Batch Movie",
                Duration = 90,
                AgeRating = "P",
                IsActive = true,
                EndDate = DateTime.Today.AddDays(30)
            };

            var room1 = new Room { Name = "Room A", Type = "2D", Rows = 5, Columns = 5, TotalSeats = 25, IsActive = true };
            var room2 = new Room { Name = "Room B", Type = "2D", Rows = 5, Columns = 5, TotalSeats = 25, IsActive = true };

            context.Movies.Add(movie);
            context.Rooms.AddRange(room1, room2);
            await context.SaveChangesAsync();

            var date = DateTime.Today.AddDays(1).Date;
            var batch = new List<Showtime>
            {
                new() { MovieId = movie.Id, RoomId = room1.Id, StartTime = date.AddHours(9), BasePrice = 75000 },
                new() { MovieId = movie.Id, RoomId = room1.Id, StartTime = date.AddHours(12), BasePrice = 75000 },
                new() { MovieId = movie.Id, RoomId = room2.Id, StartTime = date.AddHours(9), BasePrice = 75000 },
                new() { MovieId = movie.Id, RoomId = room2.Id, StartTime = date.AddHours(12), BasePrice = 75000 }
            };

            var service = new ShowtimeService(context);
            var (ok, msg) = await service.CreateManyAsync(batch);

            Assert.True(ok, msg);
            Assert.Equal(4, await context.Showtimes.CountAsync());
        }
    }

    [Fact]
    public async Task CreateManyAsync_RejectsConflictsWithExistingShowtimes()
    {
        var (context, connection) = TestDbHelper.CreateSqliteContext();
        await using (connection)
        await using (context)
        {
            var movie = new Movie
            {
                Code = "MV003",
                Title = "Conflict Movie",
                Duration = 120,
                AgeRating = "P",
                IsActive = true,
                EndDate = DateTime.Today.AddDays(30)
            };

            var room = new Room { Name = "Room X", Type = "2D", Rows = 5, Columns = 5, TotalSeats = 25, IsActive = true };
            context.Movies.Add(movie);
            context.Rooms.Add(room);
            await context.SaveChangesAsync();

            // Existing showtime at 09:00
            context.Showtimes.Add(new Showtime
            {
                MovieId = movie.Id,
                RoomId = room.Id,
                StartTime = DateTime.Today.AddDays(1).Date.AddHours(9),
                EndTime = DateTime.Today.AddDays(1).Date.AddHours(11).AddMinutes(15),
                BasePrice = 75000,
                IsActive = true
            });
            await context.SaveChangesAsync();

            var service = new ShowtimeService(context);
            var batch = new List<Showtime>
            {
                // Overlaps with existing
                new() { MovieId = movie.Id, RoomId = room.Id, StartTime = DateTime.Today.AddDays(1).Date.AddHours(10), BasePrice = 75000 }
            };

            var (ok, msg) = await service.CreateManyAsync(batch);

            Assert.False(ok);
            Assert.Contains("Trùng lịch", msg, StringComparison.OrdinalIgnoreCase);
        }
    }
}
