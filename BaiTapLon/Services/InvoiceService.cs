using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;

namespace BaiTapLon.Services;

public class InvoiceService
{
    private readonly AppDbContext _context;
    public InvoiceService(AppDbContext context) { _context = context; }

    /// <summary>
    /// Tạo hóa đơn + vé trong một transaction.
    /// </summary>
    public async Task<(bool Success, string Message, int InvoiceId)> CreateAsync(
        Invoice invoice, List<Ticket> tickets)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Kiểm tra ghế đã bán chưa (double-check)
            var showtimeId = tickets.First().ShowtimeId;
            var requestedSeatIds = tickets.Select(t => t.SeatId).ToList();

            var alreadySold = await _context.Tickets
                .Where(t => t.ShowtimeId == showtimeId && requestedSeatIds.Contains(t.SeatId))
                .AnyAsync();

            if (alreadySold)
            {
                await transaction.RollbackAsync();
                return (false, "Một số ghế đã được bán! Vui lòng chọn ghế khác.", 0);
            }

            // Lưu Invoice
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            // Lưu Tickets
            foreach (var ticket in tickets)
            {
                ticket.InvoiceId = invoice.Id;
                _context.Tickets.Add(ticket);
            }
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return (true, $"Thanh toán thành công! Mã hóa đơn: #{invoice.Id}", invoice.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return (false, $"Lỗi thanh toán: {ex.Message}", 0);
        }
    }
}
