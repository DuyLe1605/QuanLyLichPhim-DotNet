using BaiTapLon.Forms.Controls;
using BaiTapLon.Models;
using BaiTapLon.Services;

namespace BaiTapLon.Forms.Customer;

public class CustomerBookingState
{
    public Showtime Showtime { get; init; } = null!;
    public Movie Movie { get; init; } = null!;
    public Room Room { get; init; } = null!;
    public List<SeatMapControl.SeatInfo> Seats { get; init; } = new();
    public List<BookingSnackSelection> Snacks { get; init; } = new();
    public List<string> SnackLines { get; init; } = new();
    public decimal TicketTotal { get; init; }
    public decimal SnackTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public int? VoucherId { get; init; }
    public decimal GrandTotal => TicketTotal + SnackTotal - DiscountAmount;
}
