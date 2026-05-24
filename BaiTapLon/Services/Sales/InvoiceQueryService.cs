using BaiTapLon.Data;
using BaiTapLon.Helpers;
using BaiTapLon.Models;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Services;

/// <summary>
/// Queries invoice data and maps it to <see cref="ReceiptData"/> for receipt rendering.
/// Handles both staff-created invoices and customer online bookings.
/// </summary>
public class InvoiceQueryService
{
    private readonly AppDbContext _context;

    public InvoiceQueryService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Load full invoice detail by Invoice ID and map to ReceiptData.
    /// Used by Staff checkout and Admin invoice management.
    /// </summary>
    public async Task<ReceiptData?> GetReceiptDataByInvoiceIdAsync(int invoiceId)
    {
        var invoice = await _context.Invoices
            .AsNoTracking()
            .Include(i => i.User)
            .Include(i => i.Customer)
            .Include(i => i.Tickets).ThenInclude(t => t.Seat)
            .Include(i => i.Tickets).ThenInclude(t => t.Showtime).ThenInclude(s => s.Movie)
            .Include(i => i.Tickets).ThenInclude(t => t.Showtime).ThenInclude(s => s.Room)
            .Include(i => i.Tickets).ThenInclude(t => t.Booking)
            .Include(i => i.InvoiceSnacks).ThenInclude(s => s.Snack)
            .Include(i => i.PointTransactions)
            .FirstOrDefaultAsync(i => i.Id == invoiceId);

        if (invoice == null) return null;

        return MapInvoiceToReceiptData(invoice);
    }

    /// <summary>
    /// Load invoice detail by Booking code and map to ReceiptData.
    /// Used by Customer flows (payment gateway, ticket history).
    /// </summary>
    public async Task<ReceiptData?> GetReceiptDataByBookingCodeAsync(string bookingCode)
    {
        var booking = await _context.Bookings
            .AsNoTracking()
            .Include(b => b.Tickets).ThenInclude(t => t.Invoice)
            .FirstOrDefaultAsync(b => b.BookingCode == bookingCode);

        if (booking == null) return null;

        // Find the invoice linked to this booking via tickets
        var invoiceId = booking.Tickets.FirstOrDefault()?.InvoiceId;
        if (!invoiceId.HasValue) return null;

        var data = await GetReceiptDataByInvoiceIdAsync(invoiceId.Value);
        if (data != null)
        {
            // Enrich with booking code
            data.BookingCode = booking.BookingCode;
            return data;
        }

        return null;
    }

    private static ReceiptData MapInvoiceToReceiptData(Invoice invoice)
    {
        // Group tickets by showtime (typically one showtime per invoice)
        var firstTicket = invoice.Tickets
            .OrderBy(t => t.Showtime.StartTime)
            .ThenBy(t => t.Seat.RowLabel)
            .ThenBy(t => t.Seat.SeatNumber)
            .FirstOrDefault();

        var ticketLines = invoice.Tickets
            .OrderBy(t => t.Seat.RowLabel)
            .ThenBy(t => t.Seat.SeatNumber)
            .Select(t => new TicketLine(
                $"{t.Seat.RowLabel}{t.Seat.SeatNumber}",
                t.Seat.Type,
                t.Price))
            .ToList();

        var snackLines = invoice.InvoiceSnacks
            .OrderBy(s => s.Snack.Name)
            .Select(s => new SnackLine(
                s.Snack.Name,
                s.Quantity,
                s.UnitPrice,
                s.UnitPrice * s.Quantity))
            .ToList();

        var seatLabels = invoice.Tickets
            .OrderBy(t => t.Seat.RowLabel)
            .ThenBy(t => t.Seat.SeatNumber)
            .Select(t => $"{t.Seat.RowLabel}{t.Seat.SeatNumber}");

        var earnedPoints = invoice.PointTransactions
            .Where(pt => pt.Points > 0)
            .Sum(pt => pt.Points);

        // Customer info: prefer linked customer over inline name
        var customerName = invoice.Customer?.FullName ?? invoice.CustomerName;
        var customerPhone = invoice.Customer?.Phone ?? invoice.CustomerPhone;

        // Booking code: find from ticket if linked
        var bookingCode = invoice.Tickets
            .Where(t => t.BookingId.HasValue)
            .Select(t => t.Booking?.BookingCode)
            .FirstOrDefault();

        return new ReceiptData
        {
            InvoiceCode = $"HD{invoice.Id:D6}",
            CreatedAt = invoice.CreatedAt,
            StaffName = invoice.User.FullName,
            CustomerName = customerName,
            CustomerPhone = customerPhone,
            MemberCode = invoice.Customer?.MemberCode,
            CustomerTier = invoice.Customer?.Tier,
            PaymentMethod = invoice.PaymentMethod,

            MovieTitle = firstTicket?.Showtime.Movie.Title,
            ShowtimeText = firstTicket != null
                ? $"{firstTicket.Showtime.StartTime:HH:mm} - {firstTicket.Showtime.StartTime:dd/MM/yyyy}"
                : null,
            RoomName = firstTicket?.Showtime.Room.Name,
            RoomType = firstTicket?.Showtime.Room.Type,
            SeatSummary = string.Join(", ", seatLabels),

            Tickets = ticketLines,
            Snacks = snackLines,

            TicketTotal = invoice.Tickets.Sum(t => t.Price),
            SnackTotal = invoice.InvoiceSnacks.Sum(s => s.UnitPrice * s.Quantity),
            DiscountAmount = invoice.DiscountAmount,
            GrandTotal = invoice.TotalAmount,
            ReceivedAmount = invoice.ReceivedAmount,
            ChangeAmount = invoice.ChangeAmount,

            EarnedPoints = earnedPoints > 0 ? earnedPoints : null,
            BookingCode = bookingCode
        };
    }
}
