using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BaiTapLon.Helpers;

/// <summary>
/// Renders a thermal-style receipt PDF (80mm wide) from <see cref="ReceiptData"/> using QuestPDF.
/// </summary>
public static class ReceiptBuilder
{
    // Receipt width: 80mm ≈ 226 points
    private const float ReceiptWidthMm = 80f;
    private const float MarginMm = 5f;

    // Colors
    private static readonly string PrimaryColor = Colors.Grey.Darken4;
    private static readonly string MutedColor = Colors.Grey.Darken1;
    private static readonly string AccentColor = "#286840";
    private static readonly string DividerColor = Colors.Grey.Lighten1;

    /// <summary>
    /// Build a PDF receipt as byte array.
    /// </summary>
    public static byte[] BuildPdf(ReceiptData data)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(new PageSize(ReceiptWidthMm, 0, Unit.Millimetre));
                page.ContinuousSize(ReceiptWidthMm, Unit.Millimetre);
                page.Margin(MarginMm, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontColor(PrimaryColor));

                page.Content().Column(col =>
                {
                    RenderHeader(col, data);
                    RenderDivider(col);
                    RenderInvoiceInfo(col, data);
                    RenderDivider(col);

                    if (!string.IsNullOrEmpty(data.MovieTitle))
                    {
                        RenderMovieInfo(col, data);
                        RenderDivider(col);
                    }

                    if (data.Tickets.Count > 0)
                    {
                        RenderTickets(col, data);
                        RenderDivider(col);
                    }

                    if (data.Snacks.Count > 0)
                    {
                        RenderSnacks(col, data);
                        RenderDivider(col);
                    }

                    RenderTotals(col, data);
                    RenderDivider(col);

                    if (data.EarnedPoints.HasValue && data.EarnedPoints > 0)
                    {
                        RenderLoyalty(col, data);
                        RenderDivider(col);
                    }

                    RenderQrCode(col, data);
                    RenderFooter(col);
                });
            });
        });

        return document.GeneratePdf();
    }

    /// <summary>
    /// Build a preview bitmap image of the receipt.
    /// </summary>
    public static System.Drawing.Image BuildPreviewBitmap(ReceiptData data, int dpi = 200)
    {
        var pdfBytes = BuildPdf(data);
        // QuestPDF can generate images directly
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(new PageSize(ReceiptWidthMm, 0, Unit.Millimetre));
                page.ContinuousSize(ReceiptWidthMm, Unit.Millimetre);
                page.Margin(MarginMm, Unit.Millimetre);
                page.DefaultTextStyle(x => x.FontSize(8).FontColor(PrimaryColor));

                page.Content().Column(col =>
                {
                    RenderHeader(col, data);
                    RenderDivider(col);
                    RenderInvoiceInfo(col, data);
                    RenderDivider(col);

                    if (!string.IsNullOrEmpty(data.MovieTitle))
                    {
                        RenderMovieInfo(col, data);
                        RenderDivider(col);
                    }

                    if (data.Tickets.Count > 0)
                    {
                        RenderTickets(col, data);
                        RenderDivider(col);
                    }

                    if (data.Snacks.Count > 0)
                    {
                        RenderSnacks(col, data);
                        RenderDivider(col);
                    }

                    RenderTotals(col, data);
                    RenderDivider(col);

                    if (data.EarnedPoints.HasValue && data.EarnedPoints > 0)
                    {
                        RenderLoyalty(col, data);
                        RenderDivider(col);
                    }

                    RenderQrCode(col, data);
                    RenderFooter(col);
                });
            });
        });

        var images = document.GenerateImages(new ImageGenerationSettings { RasterDpi = dpi });
        var firstPage = images.FirstOrDefault();
        if (firstPage == null || firstPage.Length == 0)
        {
            // Fallback: return a placeholder
            var bmp = new Bitmap(226, 100);
            using var g = Graphics.FromImage(bmp);
            g.Clear(System.Drawing.Color.White);
            g.DrawString("Không thể tạo preview", SystemFonts.DefaultFont, Brushes.Gray, 10, 40);
            return bmp;
        }

        using var ms = new MemoryStream(firstPage);
        return new Bitmap(ms);
    }

    // ── Render sections ─────────────────────────────────────────────────────

    private static void RenderHeader(ColumnDescriptor col, ReceiptData data)
    {
        col.Item().PaddingBottom(4).AlignCenter().Text("🎬 CINEMANAGER")
            .FontSize(14).Bold().FontColor(AccentColor);
        col.Item().AlignCenter().Text("Hệ thống quản lý rạp chiếu phim")
            .FontSize(7).FontColor(MutedColor);
        col.Item().PaddingTop(2).AlignCenter().Text("────────────────────────────")
            .FontSize(6).FontColor(DividerColor);
    }

    private static void RenderInvoiceInfo(ColumnDescriptor col, ReceiptData data)
    {
        col.Item().PaddingVertical(2).Column(inner =>
        {
            InfoRow(inner, "Mã HĐ:", data.InvoiceCode);
            InfoRow(inner, "Ngày:", data.CreatedAt.ToString("dd/MM/yyyy HH:mm"));
            InfoRow(inner, "NV:", data.StaffName);

            if (!string.IsNullOrEmpty(data.CustomerName))
            {
                var customer = data.CustomerName;
                if (!string.IsNullOrEmpty(data.CustomerPhone))
                    customer += $" ({data.CustomerPhone})";
                InfoRow(inner, "KH:", customer);
            }

            if (!string.IsNullOrEmpty(data.MemberCode))
                InfoRow(inner, "Thẻ TV:", data.MemberCode);

            InfoRow(inner, "Thanh toán:", data.PaymentMethod);

            if (!string.IsNullOrEmpty(data.BookingCode))
                InfoRow(inner, "Mã đặt vé:", data.BookingCode);
        });
    }

    private static void RenderMovieInfo(ColumnDescriptor col, ReceiptData data)
    {
        col.Item().PaddingVertical(2).Column(inner =>
        {
            inner.Item().Text("THÔNG TIN PHIM").FontSize(8).Bold();
            inner.Item().PaddingTop(2).Text(data.MovieTitle ?? "").FontSize(9).Bold();

            if (!string.IsNullOrEmpty(data.ShowtimeText))
                inner.Item().Text($"Suất: {data.ShowtimeText}").FontSize(7.5f);

            if (!string.IsNullOrEmpty(data.RoomName))
            {
                var room = data.RoomName;
                if (!string.IsNullOrEmpty(data.RoomType))
                    room += $" ({data.RoomType})";
                inner.Item().Text($"Phòng: {room}").FontSize(7.5f);
            }

            if (!string.IsNullOrEmpty(data.SeatSummary))
                inner.Item().Text($"Ghế: {data.SeatSummary}").FontSize(7.5f);
        });
    }

    private static void RenderTickets(ColumnDescriptor col, ReceiptData data)
    {
        col.Item().PaddingVertical(2).Column(inner =>
        {
            inner.Item().Text("VÉ PHIM").FontSize(8).Bold();
            inner.Item().PaddingTop(2).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(3);    // Seat
                    c.RelativeColumn(2);    // Type
                    c.RelativeColumn(3);    // Price
                });

                foreach (var ticket in data.Tickets)
                {
                    table.Cell().Text(ticket.SeatLabel).FontSize(7.5f);
                    table.Cell().Text(ticket.SeatType).FontSize(7.5f).FontColor(MutedColor);
                    table.Cell().AlignRight().Text($"{ticket.Price:N0}đ").FontSize(7.5f);
                }
            });

            inner.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem().AlignRight().Text($"Tổng vé: {data.TicketTotal:N0}đ")
                    .FontSize(8).Bold();
            });
        });
    }

    private static void RenderSnacks(ColumnDescriptor col, ReceiptData data)
    {
        col.Item().PaddingVertical(2).Column(inner =>
        {
            inner.Item().Text("BẮP NƯỚC").FontSize(8).Bold();
            inner.Item().PaddingTop(2).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    c.RelativeColumn(4);    // Name
                    c.RelativeColumn(1);    // Qty
                    c.RelativeColumn(3);    // Total
                });

                foreach (var snack in data.Snacks)
                {
                    table.Cell().Text(snack.Name).FontSize(7.5f);
                    table.Cell().AlignCenter().Text($"x{snack.Quantity}").FontSize(7.5f).FontColor(MutedColor);
                    table.Cell().AlignRight().Text($"{snack.LineTotal:N0}đ").FontSize(7.5f);
                }
            });

            inner.Item().PaddingTop(2).Row(row =>
            {
                row.RelativeItem().AlignRight().Text($"Tổng bắp nước: {data.SnackTotal:N0}đ")
                    .FontSize(8).Bold();
            });
        });
    }

    private static void RenderTotals(ColumnDescriptor col, ReceiptData data)
    {
        col.Item().PaddingVertical(3).Column(inner =>
        {
            if (data.TicketTotal > 0)
                TotalRow(inner, "Tiền vé:", $"{data.TicketTotal:N0}đ", false);
            if (data.SnackTotal > 0)
                TotalRow(inner, "Bắp nước:", $"{data.SnackTotal:N0}đ", false);
            if (data.DiscountAmount > 0)
                TotalRow(inner, "Giảm giá:", $"-{data.DiscountAmount:N0}đ", false);

            inner.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(DividerColor);

            TotalRow(inner, "THANH TOÁN:", $"{data.GrandTotal:N0}đ", true, 10);

            if (data.ReceivedAmount > 0)
                TotalRow(inner, "Tiền nhận:", $"{data.ReceivedAmount:N0}đ", false);
            if (data.ChangeAmount > 0)
                TotalRow(inner, "Tiền thối:", $"{data.ChangeAmount:N0}đ", false);
        });
    }

    private static void RenderLoyalty(ColumnDescriptor col, ReceiptData data)
    {
        col.Item().PaddingVertical(2).Column(inner =>
        {
            inner.Item().Row(row =>
            {
                row.RelativeItem().Text($"🎁 Tích điểm: +{data.EarnedPoints:N0} điểm")
                    .FontSize(8).Bold().FontColor(AccentColor);
            });

            if (!string.IsNullOrEmpty(data.CustomerTier))
            {
                var icon = data.CustomerTier switch
                {
                    "Diamond" => "💎",
                    "VIP" => "⭐",
                    _ => "🎫"
                };
                inner.Item().Text($"Hạng: {data.CustomerTier} {icon}").FontSize(7.5f).FontColor(MutedColor);
            }
        });
    }

    private static void RenderQrCode(ColumnDescriptor col, ReceiptData data)
    {
        var qrContent = data.QrContent;
        if (string.IsNullOrEmpty(qrContent)) return;

        var qrBitmap = BarcodeHelper.GenerateQrCode(qrContent, 120, 120);
        if (qrBitmap == null) return;

        col.Item().PaddingVertical(4).AlignCenter().Column(inner =>
        {
            using var ms = new MemoryStream();
            qrBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            var qrBytes = ms.ToArray();

            inner.Item().AlignCenter().Width(30, Unit.Millimetre).Height(30, Unit.Millimetre)
                .Image(qrBytes);

            inner.Item().PaddingTop(2).AlignCenter().Text(qrContent)
                .FontSize(6.5f).FontColor(MutedColor);
        });

        qrBitmap.Dispose();
    }

    private static void RenderFooter(ColumnDescriptor col)
    {
        col.Item().PaddingTop(6).AlignCenter().Text("Cảm ơn quý khách!")
            .FontSize(9).Bold().FontColor(AccentColor);
        col.Item().AlignCenter().Text("Hẹn gặp lại tại CineManager")
            .FontSize(7).FontColor(MutedColor);
        col.Item().PaddingTop(4).AlignCenter().Text("────────────────────────────")
            .FontSize(6).FontColor(DividerColor);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static void RenderDivider(ColumnDescriptor col)
    {
        col.Item().PaddingVertical(2).LineHorizontal(0.5f).LineColor(DividerColor);
    }

    private static void InfoRow(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(55).Text(label).FontSize(7.5f).FontColor(MutedColor);
            row.RelativeItem().Text(value).FontSize(7.5f);
        });
    }

    private static void TotalRow(ColumnDescriptor col, string label, string value, bool bold, float fontSize = 8)
    {
        col.Item().Row(row =>
        {
            var leftText = row.RelativeItem().Text(label).FontSize(fontSize);
            var rightText = row.ConstantItem(80).AlignRight().Text(value).FontSize(fontSize);
            if (bold)
            {
                leftText.Bold();
                rightText.Bold().FontColor(AccentColor);
            }
        });
    }
}
