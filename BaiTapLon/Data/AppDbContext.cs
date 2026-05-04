using Microsoft.EntityFrameworkCore;
using BaiTapLon.Models;

namespace BaiTapLon.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Genre> Genres => Set<Genre>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<MovieGenre> MovieGenres => Set<MovieGenre>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Showtime> Showtimes => Set<Showtime>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<Snack> Snacks => Set<Snack>();
    public DbSet<InvoiceSnack> InvoiceSnacks => Set<InvoiceSnack>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== MovieGenre (N-N composite key) =====
        modelBuilder.Entity<MovieGenre>()
            .HasKey(mg => new { mg.MovieId, mg.GenreId });

        modelBuilder.Entity<MovieGenre>()
            .HasOne(mg => mg.Movie)
            .WithMany(m => m.MovieGenres)
            .HasForeignKey(mg => mg.MovieId);

        modelBuilder.Entity<MovieGenre>()
            .HasOne(mg => mg.Genre)
            .WithMany(g => g.MovieGenres)
            .HasForeignKey(mg => mg.GenreId);

        // ===== Seat =====
        modelBuilder.Entity<Seat>()
            .HasOne(s => s.Room)
            .WithMany(r => r.Seats)
            .HasForeignKey(s => s.RoomId);

        modelBuilder.Entity<Seat>()
            .Property(s => s.PriceMultiplier)
            .HasPrecision(4, 2);

        // ===== Showtime =====
        modelBuilder.Entity<Showtime>()
            .HasOne(st => st.Movie)
            .WithMany(m => m.Showtimes)
            .HasForeignKey(st => st.MovieId);

        modelBuilder.Entity<Showtime>()
            .HasOne(st => st.Room)
            .WithMany(r => r.Showtimes)
            .HasForeignKey(st => st.RoomId);

        modelBuilder.Entity<Showtime>()
            .Property(st => st.BasePrice)
            .HasPrecision(12, 2);

        // ===== Movie =====
        modelBuilder.Entity<Movie>()
            .Property(m => m.PosterPath)
            .HasMaxLength(260);

        // ===== Invoice =====
        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.User)
            .WithMany(u => u.Invoices)
            .HasForeignKey(i => i.UserId);

        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalAmount).HasPrecision(12, 2);
        modelBuilder.Entity<Invoice>()
            .Property(i => i.ReceivedAmount).HasPrecision(12, 2);
        modelBuilder.Entity<Invoice>()
            .Property(i => i.ChangeAmount).HasPrecision(12, 2);

        // ===== Ticket =====
        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Showtime)
            .WithMany(st => st.Tickets)
            .HasForeignKey(t => t.ShowtimeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Seat)
            .WithMany(s => s.Tickets)
            .HasForeignKey(t => t.SeatId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(t => t.Invoice)
            .WithMany(i => i.Tickets)
            .HasForeignKey(t => t.InvoiceId);

        modelBuilder.Entity<Ticket>()
            .Property(t => t.Price).HasPrecision(12, 2);

        modelBuilder.Entity<Ticket>()
            .HasIndex(t => new { t.ShowtimeId, t.SeatId })
            .IsUnique();

        // ===== Snack =====
        modelBuilder.Entity<Snack>()
            .Property(s => s.ImagePath)
            .HasMaxLength(260);

        modelBuilder.Entity<Snack>()
            .Property(s => s.Price).HasPrecision(12, 2);

        // ===== InvoiceSnack =====
        modelBuilder.Entity<InvoiceSnack>()
            .HasOne(ins => ins.Invoice)
            .WithMany(i => i.InvoiceSnacks)
            .HasForeignKey(ins => ins.InvoiceId);

        modelBuilder.Entity<InvoiceSnack>()
            .HasOne(ins => ins.Snack)
            .WithMany(s => s.InvoiceSnacks)
            .HasForeignKey(ins => ins.SnackId);

        modelBuilder.Entity<InvoiceSnack>()
            .Property(ins => ins.UnitPrice).HasPrecision(12, 2);

        // ===== Unique constraints =====
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username).IsUnique();

        // ===== Seed Data =====
        SeedData(modelBuilder);
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Genres
        modelBuilder.Entity<Genre>().HasData(
            new Genre { Id = 1, Name = "Hành động" },
            new Genre { Id = 2, Name = "Hài hước" },
            new Genre { Id = 3, Name = "Kinh dị" },
            new Genre { Id = 4, Name = "Tình cảm" },
            new Genre { Id = 5, Name = "Viễn tưởng" },
            new Genre { Id = 6, Name = "Hoạt hình" },
            new Genre { Id = 7, Name = "Tâm lý" },
            new Genre { Id = 8, Name = "Phiêu lưu" }
        );

        // Sample Rooms
        modelBuilder.Entity<Room>().HasData(
            new Room { Id = 1, Name = "Phòng 1", Type = "2D", TotalSeats = 80, Rows = 8, Columns = 10, IsActive = true },
            new Room { Id = 2, Name = "Phòng 2", Type = "3D", TotalSeats = 60, Rows = 6, Columns = 10, IsActive = true },
            new Room { Id = 3, Name = "Phòng 3", Type = "IMAX", TotalSeats = 100, Rows = 10, Columns = 10, IsActive = true }
        );

        // Seed seats for Room 1 (8 rows × 10 cols)
        SeedSeatsForRoom(modelBuilder, roomId: 1, rows: 8, cols: 10, startId: 1);
        // Seed seats for Room 2 (6 rows × 10 cols)
        SeedSeatsForRoom(modelBuilder, roomId: 2, rows: 6, cols: 10, startId: 81);
        // Seed seats for Room 3 (10 rows × 10 cols)
        SeedSeatsForRoom(modelBuilder, roomId: 3, rows: 10, cols: 10, startId: 141);

        // Sample Snacks
        modelBuilder.Entity<Snack>().HasData(
            new Snack { Id = 1, Name = "Bắp rang bơ (Nhỏ)", Price = 49000, Category = "Food", ImagePath = "popcorn-small.png" },
            new Snack { Id = 2, Name = "Bắp rang bơ (Lớn)", Price = 69000, Category = "Food", ImagePath = "popcorn-large.png" },
            new Snack { Id = 3, Name = "Coca-Cola", Price = 29000, Category = "Drink", ImagePath = "coca-cola.png" },
            new Snack { Id = 4, Name = "Pepsi", Price = 29000, Category = "Drink", ImagePath = "pepsi.png" },
            new Snack { Id = 5, Name = "Nước suối", Price = 15000, Category = "Drink", ImagePath = "water.png" },
            new Snack { Id = 6, Name = "Combo Couple (2 Bắp + 2 Nước)", Price = 129000, Category = "Combo", ImagePath = "combo-couple.png" },
            new Snack { Id = 7, Name = "Combo Single (1 Bắp + 1 Nước)", Price = 69000, Category = "Combo", ImagePath = "combo-single.png" }
        );
    }

    private static void SeedSeatsForRoom(ModelBuilder modelBuilder, int roomId, int rows, int cols, int startId)
    {
        var seats = new List<Seat>();
        int id = startId;

        for (int r = 0; r < rows; r++)
        {
            string rowLabel = ((char)('A' + r)).ToString();

            // Xác định loại ghế dựa trên vị trí hàng
            string seatType;
            decimal multiplier;

            if (r >= rows - 1) // Hàng cuối = Couple
            {
                seatType = "Couple";
                multiplier = 2.0m;
            }
            else if (r >= rows - 3) // 2 hàng trước cuối = VIP
            {
                seatType = "VIP";
                multiplier = 1.5m;
            }
            else // Còn lại = Standard
            {
                seatType = "Standard";
                multiplier = 1.0m;
            }

            for (int c = 1; c <= cols; c++)
            {
                seats.Add(new Seat
                {
                    Id = id++,
                    RoomId = roomId,
                    RowLabel = rowLabel,
                    SeatNumber = c,
                    GridRow = r,
                    GridColumn = c - 1,
                    GridSpan = 1,
                    Type = seatType,
                    PriceMultiplier = multiplier
                });
            }
        }

        modelBuilder.Entity<Seat>().HasData(seats.ToArray());
    }
}
