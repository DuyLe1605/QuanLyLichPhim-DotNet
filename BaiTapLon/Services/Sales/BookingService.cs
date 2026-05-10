using System.Data;
using BaiTapLon.Data;
using BaiTapLon.Helpers;
using BaiTapLon.Models;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Services;

public sealed record BookingSnackSelection(int SnackId, int Quantity);

public class BookingService
{
    private readonly AppDbContext _context;

    public BookingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, int BookingId, string BookingCode)> CreatePaidBookingAsync(
        int customerId,
        int showtimeId,
        IReadOnlyCollection<int> seatIds,
        IReadOnlyCollection<BookingSnackSelection> snackSelections,
        decimal discountAmount = 0,
        int? voucherId = null)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

        try
        {
            if (seatIds.Count == 0)
                return (false, "Vui lòng chọn ít nhất một ghế.", 0, "");

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.IsActive);
            if (customer == null)
                return (false, "Không tìm thấy tài khoản khách hàng.", 0, "");

            var showtime = await _context.Showtimes
                .Include(s => s.Movie)
                .Include(s => s.Room)
                .FirstOrDefaultAsync(s => s.Id == showtimeId && s.IsActive);

            if (showtime == null)
                return (false, "Suất chiếu không còn khả dụng.", 0, "");

            if (showtime.StartTime <= DateTime.Now)
                return (false, "Suất chiếu đã bắt đầu, không thể đặt vé online.", 0, "");

            var distinctSeatIds = seatIds.Distinct().ToList();
            var seats = await _context.Seats
                .Where(s => s.RoomId == showtime.RoomId && distinctSeatIds.Contains(s.Id))
                .ToListAsync();

            if (seats.Count != distinctSeatIds.Count)
                return (false, "Một số ghế không thuộc phòng chiếu này.", 0, "");

            var soldCount = await _context.Tickets
                .Where(t => t.ShowtimeId == showtimeId && distinctSeatIds.Contains(t.SeatId))
                .CountAsync();

            if (soldCount > 0)
            {
                await transaction.RollbackAsync();
                return (false, "Một số ghế vừa được người khác đặt. Vui lòng chọn lại.", 0, "");
            }

            var snackIds = snackSelections
                .Where(s => s.Quantity > 0)
                .Select(s => s.SnackId)
                .Distinct()
                .ToList();

            var snacks = snackIds.Count == 0
                ? new List<Snack>()
                : await _context.Snacks
                    .Where(s => s.IsActive && snackIds.Contains(s.Id))
                    .ToListAsync();

            if (snacks.Count != snackIds.Count)
                return (false, "Một số món bắp nước không còn bán.", 0, "");

            var snackMap = snacks.ToDictionary(s => s.Id);
            var ticketTotal = seats.Sum(s => showtime.BasePrice * s.PriceMultiplier);
            var snackTotal = snackSelections
                .Where(s => s.Quantity > 0)
                .Sum(s => snackMap[s.SnackId].Price * s.Quantity);
            var grossTotal = ticketTotal + snackTotal;
            var validDiscount = Math.Clamp(discountAmount, 0, grossTotal);
            var total = grossTotal - validDiscount;

            var sellerId = await GetOnlineSellerUserIdAsync();
            if (sellerId == 0)
                return (false, "Chưa có tài khoản nhân viên hệ thống để ghi nhận hóa đơn online.", 0, "");

            var bookingCode = await GenerateUniqueBookingCodeAsync();
            var booking = new Booking
            {
                BookingCode = bookingCode,
                CustomerId = customerId,
                ShowtimeId = showtimeId,
                Status = "Paid",
                PaymentMethod = "QR",
                TotalAmount = total,
                DiscountAmount = validDiscount,
                VoucherId = voucherId,
                CreatedAt = DateTime.Now
            };

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            var invoice = new Invoice
            {
                UserId = sellerId,
                CustomerId = customerId,
                VoucherId = voucherId,
                CustomerName = customer.FullName,
                CustomerPhone = customer.Phone,
                PaymentMethod = "QR",
                TotalAmount = total,
                DiscountAmount = validDiscount,
                ReceivedAmount = total,
                ChangeAmount = 0,
                CreatedAt = DateTime.Now
            };

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            foreach (var seat in seats)
            {
                _context.Tickets.Add(new Ticket
                {
                    ShowtimeId = showtimeId,
                    SeatId = seat.Id,
                    InvoiceId = invoice.Id,
                    BookingId = booking.Id,
                    Price = showtime.BasePrice * seat.PriceMultiplier
                });
            }

            foreach (var item in snackSelections.Where(s => s.Quantity > 0))
            {
                _context.InvoiceSnacks.Add(new InvoiceSnack
                {
                    InvoiceId = invoice.Id,
                    SnackId = item.SnackId,
                    Quantity = item.Quantity,
                    UnitPrice = snackMap[item.SnackId].Price
                });
            }

            AddEarnedPoints(customer, invoice.Id, total);
            customer.TotalSpent += total;
            customer.Tier = customer.TotalSpent switch
            {
                >= 10_000_000m => "Diamond",
                >= 2_000_000m => "VIP",
                _ => "Standard"
            };

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return (true, "Thanh toán online thành công.", booking.Id, bookingCode);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync();
            return (false, "Ghế vừa được đặt trên hệ thống. Vui lòng chọn lại.", 0, "");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Lỗi đặt vé: {ex.Message}", 0, "");
        }
    }

    public async Task<List<Booking>> GetCustomerBookingsAsync(int customerId)
    {
        return await _context.Bookings
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime).ThenInclude(s => s.Room)
            .Include(b => b.Tickets).ThenInclude(t => t.Seat)
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<Booking?> GetByCodeAsync(string bookingCode)
    {
        return await _context.Bookings
            .Include(b => b.Customer)
            .Include(b => b.Showtime).ThenInclude(s => s.Movie)
            .Include(b => b.Showtime).ThenInclude(s => s.Room)
            .Include(b => b.Tickets).ThenInclude(t => t.Seat)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);
    }

    private async Task<int> GetOnlineSellerUserIdAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.Role == "Admin")
            .ThenBy(u => u.Id)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();
    }

    private async Task<string> GenerateUniqueBookingCodeAsync()
    {
        string code;
        do
        {
            code = BarcodeHelper.GenerateBookingCode();
        }
        while (await _context.Bookings.AnyAsync(b => b.BookingCode == code));

        return code;
    }

    private void AddEarnedPoints(Customer customer, int invoiceId, decimal amount)
    {
        var rate = customer.Tier switch
        {
            "Diamond" => 0.02m,
            "VIP" => 0.015m,
            _ => 0.01m
        };

        var points = (int)Math.Floor(amount * rate);
        if (points <= 0) return;

        customer.TotalPoints += points;
        _context.PointTransactions.Add(new PointTransaction
        {
            CustomerId = customer.Id,
            InvoiceId = invoiceId,
            Points = points,
            Type = "Earn",
            Description = $"Tích điểm từ đặt vé online #{invoiceId}",
            CreatedAt = DateTime.Now
        });
    }
}
