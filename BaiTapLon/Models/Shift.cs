namespace BaiTapLon.Models;

public class Shift
{
    public int Id { get; set; }
    public int UserId { get; set; } // Nhân viên mở ca
    public decimal OpeningCash { get; set; } // Tiền đầu ca (thối dự trữ)
    public decimal? ClosingCash { get; set; } // Tiền cuối ca (nhân viên đếm thực tế)
    public decimal? ExpectedCash { get; set; } // Hệ thống tính (OpeningCash + tiền mặt thu - tiền thối)
    public decimal? CashDifference { get; set; } // ClosingCash - ExpectedCash
    public string Status { get; set; } = "Open"; // "Open" | "Closed"
    public DateTime OpenedAt { get; set; } = DateTime.Now;
    public DateTime? ClosedAt { get; set; }
    public string? Note { get; set; }

    // Navigation
    public User User { get; set; } = null!;
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
