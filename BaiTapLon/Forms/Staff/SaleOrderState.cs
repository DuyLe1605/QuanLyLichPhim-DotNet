using BaiTapLon.Models;
using BaiTapLon.Forms.Controls;

namespace BaiTapLon.Forms.Staff;

public class SaleOrderState
{
    public Showtime Showtime { get; init; } = null!;
    public Movie Movie { get; init; } = null!;
    public Room Room { get; init; } = null!;
    public List<SeatMapControl.SeatInfo> Seats { get; init; } = new();
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public decimal TicketTotal { get; init; }
}

public class SnackCartItem
{
    public Snack Snack { get; init; } = null!;
    public int Quantity { get; set; }
    public decimal LineTotal => Snack.Price * Quantity;
}
