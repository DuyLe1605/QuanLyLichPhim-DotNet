using Microsoft.EntityFrameworkCore;
using BaiTapLon.Data;
using BaiTapLon.Models;
using System.Data;

namespace BaiTapLon.Services;

public class InvoiceService
{
    private readonly AppDbContext _context;

    public InvoiceService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(bool Success, string Message, int InvoiceId)> CreateAsync(
        Invoice invoice, List<Ticket> tickets)
    {
        return await CreateAsync(invoice, tickets, new List<InvoiceSnack>());
    }

    /// <summary>
    /// Tạo hóa đơn + vé + bắp nước trong một transaction.
    /// </summary>
    public async Task<(bool Success, string Message, int InvoiceId)> CreateAsync(
        Invoice invoice, List<Ticket> tickets, List<InvoiceSnack> invoiceSnacks)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if (tickets.Count == 0)
                return (false, "Chưa có ghế để thanh toán.", 0);

            var showtimeId = tickets.First().ShowtimeId;
            var requestedSeatIds = tickets.Select(t => t.SeatId).ToList();

            var alreadySoldCount = await _context.Tickets
                .Where(t => t.ShowtimeId == showtimeId && requestedSeatIds.Contains(t.SeatId))
                .CountAsync();

            if (alreadySoldCount > 0)
            {
                await transaction.RollbackAsync();
                return (false, "Một số ghế đã được bán! Vui lòng chọn ghế khác.", 0);
            }

            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();

            foreach (var ticket in tickets)
            {
                ticket.InvoiceId = invoice.Id;
                _context.Tickets.Add(ticket);
            }
            await _context.SaveChangesAsync();

            foreach (var invoiceSnack in invoiceSnacks.Where(s => s.Quantity > 0))
            {
                var snack = await _context.Snacks
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == invoiceSnack.SnackId && s.IsActive);

                if (snack == null)
                {
                    await transaction.RollbackAsync();
                    return (false, "Một món bắp nước không còn bán. Vui lòng tải lại menu.", 0);
                }

                invoiceSnack.InvoiceId = invoice.Id;
                invoiceSnack.UnitPrice = snack.Price;
                _context.InvoiceSnacks.Add(invoiceSnack);
            }
            await _context.SaveChangesAsync();

            await transaction.CommitAsync();
            return (true, $"Thanh toán thành công! Mã hóa đơn: #{invoice.Id}", invoice.Id);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            if (ex is DbUpdateException)
                return (false, "Ghế vừa được bán trên hệ thống, vui lòng chọn ghế khác.", 0);

            return (false, $"Lỗi thanh toán: {ex.Message}", 0);
        }
    }
}
