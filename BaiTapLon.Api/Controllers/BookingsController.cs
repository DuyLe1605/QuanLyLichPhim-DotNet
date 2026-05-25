using BaiTapLon.Api.Dtos;
using BaiTapLon.Api.Services;
using BaiTapLon.Data;
using BaiTapLon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Api.Controllers;

[Route("api/bookings")]
public class BookingsController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;

    public BookingsController(AppDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpGet("showtimes/{id:int}/seats")]
    public async Task<ActionResult<object>> GetSeats(int id)
    {
        var showtime = await _db.Showtimes
            .AsNoTracking()
            .Include(s => s.Room)
            .FirstOrDefaultAsync(s => s.Id == id);
        if (showtime is null) return NotFound();
        if (showtime.Room is null)
            return NotFound(new { message = "Không tìm thấy phòng chiếu của suất chiếu này." });

        var soldSeatIds = await _db.Tickets
            .AsNoTracking()
            .Where(t => t.ShowtimeId == id)
            .Select(t => t.SeatId)
            .ToListAsync();

        var soldSeatIdSet = soldSeatIds.Count == 0
            ? null
            : soldSeatIds.ToHashSet();

        // NOTE: Do not compute `Price = showtime.BasePrice * s.PriceMultiplier` inside the SQL query.
        // EF may infer an overly small decimal precision for the BasePrice parameter based on other decimals
        // (e.g. PriceMultiplier is decimal(4,2)), causing SqlClient to throw:
        //   "Parameter value '75000,00' is out of range."
        // Computing in memory is safe here because a room has a small, bounded number of seats.
        var rawSeats = await _db.Seats
            .AsNoTracking()
            .Where(s => s.RoomId == showtime.RoomId)
            .OrderBy(s => s.GridRow).ThenBy(s => s.GridColumn)
            .Select(s => new
            {
                s.Id,
                s.RowLabel,
                s.SeatNumber,
                Label = s.RowLabel + s.SeatNumber,
                s.GridRow,
                s.GridColumn,
                s.GridSpan,
                s.Type,
                s.PriceMultiplier
            })
            .ToListAsync();

        var seats = rawSeats.Select(s => new
        {
            s.Id,
            s.RowLabel,
            s.SeatNumber,
            s.Label,
            s.GridRow,
            s.GridColumn,
            s.GridSpan,
            s.Type,
            s.PriceMultiplier,
            Price = showtime.BasePrice * s.PriceMultiplier,
            Status = soldSeatIdSet is not null && soldSeatIdSet.Contains(s.Id) ? "sold" : "available"
        });

        return new
        {
            Showtime = new { showtime.Id, showtime.StartTime, showtime.EndTime, showtime.BasePrice },
            Room = new { showtime.Room.Id, showtime.Room.Name, showtime.Room.Rows, showtime.Room.Columns },
            Seats = seats
        };
    }

    [HttpPost]
    public async Task<ActionResult<object>> CreateBooking(BookingRequest request)
    {
        var customerId = GetCustomerId(_tokens);
        if (customerId is null) return Unauthorized();
        if (request.SeatIds.Length == 0) return BadRequest(new { message = "Chọn ít nhất 1 ghế." });

        var showtime = await _db.Showtimes.Include(s => s.Room).FirstOrDefaultAsync(s => s.Id == request.ShowtimeId && s.IsActive);
        if (showtime is null) return NotFound(new { message = "Không tìm thấy suất chiếu." });

        var distinctSeatIds = request.SeatIds.Distinct().ToArray();
        var seats = await _db.Seats.Where(s => s.RoomId == showtime.RoomId && distinctSeatIds.Contains(s.Id)).ToListAsync();
        if (seats.Count != distinctSeatIds.Length) return BadRequest(new { message = "Ghế không hợp lệ cho phòng chiếu này." });

        var taken = await _db.Tickets.AnyAsync(t => t.ShowtimeId == request.ShowtimeId && distinctSeatIds.Contains(t.SeatId));
        if (taken) return Conflict(new { message = "Một hoặc nhiều ghế đã được bán." });

        var subtotal = seats.Sum(s => showtime.BasePrice * s.PriceMultiplier);
        var (voucher, discount) = await ResolveVoucher(request.VoucherCode, subtotal);
        var total = Math.Max(0, subtotal - discount);
        var systemUserId = await _db.Users.Select(u => u.Id).FirstOrDefaultAsync();
        if (systemUserId == 0) return BadRequest(new { message = "Chưa có user hệ thống để tạo invoice." });

        await using var tx = await _db.Database.BeginTransactionAsync();

        var invoice = new Invoice
        {
            UserId = systemUserId,
            CustomerId = customerId.Value,
            VoucherId = voucher?.Id,
            PaymentMethod = "QR",
            TotalAmount = total,
            DiscountAmount = discount,
            ReceivedAmount = total,
            ChangeAmount = 0,
            CreatedAt = DateTime.Now
        };
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync();

        var booking = new Booking
        {
            BookingCode = $"BK-{Guid.NewGuid():N}"[..9].ToUpperInvariant(),
            CustomerId = customerId.Value,
            ShowtimeId = request.ShowtimeId,
            Status = "Paid",
            PaymentMethod = "QR",
            TotalAmount = total,
            DiscountAmount = discount,
            VoucherId = voucher?.Id,
            CreatedAt = DateTime.Now
        };
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync();

        foreach (var seat in seats)
        {
            _db.Tickets.Add(new Ticket
            {
                ShowtimeId = request.ShowtimeId,
                SeatId = seat.Id,
                InvoiceId = invoice.Id,
                BookingId = booking.Id,
                Price = showtime.BasePrice * seat.PriceMultiplier
            });
        }

        if (voucher is not null) voucher.UsedCount++;
        await _db.SaveChangesAsync();
        await tx.CommitAsync();

        return CreatedAtAction(nameof(GetBooking), new { code = booking.BookingCode }, new
        {
            booking.BookingCode,
            booking.Status,
            booking.TotalAmount,
            booking.DiscountAmount,
            Seats = seats.Select(s => s.Label)
        });
    }

    [HttpGet("my")]
    public async Task<ActionResult<IEnumerable<object>>> MyBookings()
    {
        var customerId = GetCustomerId(_tokens);
        if (customerId is null) return Unauthorized();

        return await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Where(b => b.CustomerId == customerId.Value)
            .OrderByDescending(b => b.CreatedAt)
            .Select(b => new
            {
                b.BookingCode,
                b.Status,
                b.TotalAmount,
                b.DiscountAmount,
                b.CreatedAt,
                Showtime = new { b.Showtime.Id, b.Showtime.StartTime, Movie = b.Showtime.Movie.Title }
            })
            .ToListAsync();
    }

    [HttpGet("{code}")]
    public async Task<ActionResult<object>> GetBooking(string code)
    {
        var customerId = GetCustomerId(_tokens);
        if (customerId is null) return Unauthorized();

        var booking = await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime).ThenInclude(s => s.Room)
            .Include(b => b.Tickets).ThenInclude(t => t.Seat)
            .Where(b => b.BookingCode == code && b.CustomerId == customerId.Value)
            .Select(b => new
            {
                b.BookingCode,
                b.Status,
                b.PaymentMethod,
                b.TotalAmount,
                b.DiscountAmount,
                b.CreatedAt,
                Showtime = new
                {
                    b.Showtime.Id,
                    b.Showtime.StartTime,
                    Movie = b.Showtime.Movie.Title,
                    Room = b.Showtime.Room.Name
                },
                Seats = b.Tickets.Select(t => new { t.Seat.Id, t.Seat.Label, t.Seat.Type, t.Price })
            })
            .FirstOrDefaultAsync();

        return booking is null ? NotFound() : booking;
    }

    private async Task<(Voucher? voucher, decimal discount)> ResolveVoucher(string? code, decimal subtotal)
    {
        if (string.IsNullOrWhiteSpace(code)) return (null, 0);

        var now = DateTime.Now;
        var voucher = await _db.Vouchers.FirstOrDefaultAsync(v =>
            v.Code == code.Trim() && v.IsActive && v.StartDate <= now && v.EndDate >= now && v.UsedCount < v.MaxUses);

        if (voucher is null) return (null, 0);

        var discount = voucher.Type switch
        {
            "Percent" => subtotal * voucher.Value / 100m,
            "Fixed" => voucher.Value,
            "FreeTicket" => subtotal,
            _ => 0
        };

        if (voucher.MaxDiscount is not null)
        {
            discount = Math.Min(discount, voucher.MaxDiscount.Value);
        }

        return (voucher, Math.Min(discount, subtotal));
    }
}
