namespace BaiTapLon.Helpers;

/// <summary>
/// Một dòng vé trong hóa đơn.
/// </summary>
public record TicketLine(string SeatLabel, string SeatType, decimal Price);

/// <summary>
/// Một dòng bắp nước trong hóa đơn.
/// </summary>
public record SnackLine(string Name, int Quantity, decimal UnitPrice, decimal LineTotal);

/// <summary>
/// DTO chứa toàn bộ dữ liệu cần để render hóa đơn / receipt.
/// Được tạo bởi <see cref="BaiTapLon.Services.InvoiceQueryService"/>.
/// </summary>
public class ReceiptData
{
    // ── Header ──────────────────────────────────────────────────────────────
    public string InvoiceCode { get; init; } = "";
    public DateTime CreatedAt { get; init; }
    public string StaffName { get; init; } = "";
    public string? CustomerName { get; init; }
    public string? CustomerPhone { get; init; }
    public string? MemberCode { get; init; }
    public string? CustomerTier { get; init; }
    public string PaymentMethod { get; init; } = "Cash";

    // ── Movie / Showtime ────────────────────────────────────────────────────
    public string? MovieTitle { get; init; }
    public string? ShowtimeText { get; init; }
    public string? RoomName { get; init; }
    public string? RoomType { get; init; }
    public string? SeatSummary { get; init; }

    // ── Line items ──────────────────────────────────────────────────────────
    public List<TicketLine> Tickets { get; init; } = new();
    public List<SnackLine> Snacks { get; init; } = new();

    // ── Totals ──────────────────────────────────────────────────────────────
    public decimal TicketTotal { get; init; }
    public decimal SnackTotal { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal GrandTotal { get; init; }
    public decimal ReceivedAmount { get; init; }
    public decimal ChangeAmount { get; init; }

    // ── Loyalty ─────────────────────────────────────────────────────────────
    public int? EarnedPoints { get; init; }

    // ── Booking (customer online) ───────────────────────────────────────────
    public string? BookingCode { get; set; }

    // ── QR content ──────────────────────────────────────────────────────────
    public string QrContent => string.IsNullOrEmpty(BookingCode)
        ? InvoiceCode
        : $"{InvoiceCode}|{BookingCode}";
}
