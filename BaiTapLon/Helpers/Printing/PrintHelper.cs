using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using BaiTapLon.Services;

namespace BaiTapLon.Helpers;

/// <summary>
/// Xuất báo cáo PDF bằng QuestPDF.
/// </summary>
public static class PrintHelper
{
    static PrintHelper()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Xuất báo cáo tổng hợp ra file PDF.
    /// </summary>
    public static void ExportReport(
        string filePath,
        ReportService.DashboardStats stats,
        List<ReportService.TopMovie> topMovies,
        List<ReportService.RevenueByDate> revenueData,
        DateTime from, DateTime to)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(30);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Grey.Darken3));

                // === HEADER ===
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("🎬 CineManager").FontSize(22).Bold().FontColor(Colors.Blue.Darken2);
                            c.Item().Text("BÁO CÁO THỐNG KÊ").FontSize(14).SemiBold().FontColor(Colors.Grey.Darken1);
                        });
                        row.ConstantItem(200).AlignRight().Column(c =>
                        {
                            c.Item().Text($"Từ: {from:dd/MM/yyyy}").FontSize(10);
                            c.Item().Text($"Đến: {to:dd/MM/yyyy}").FontSize(10);
                            c.Item().Text($"Ngày xuất: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(9).FontColor(Colors.Grey.Medium);
                        });
                    });

                    col.Item().PaddingVertical(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                // === CONTENT ===
                page.Content().Column(col =>
                {
                    // --- Tổng quan ---
                    col.Item().PaddingTop(10).Text("1. TỔNG QUAN").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);
                    col.Item().PaddingTop(5).Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn();
                        });

                        AddStatsCell(table, "Phim đang chiếu", stats.TotalMovies.ToString());
                        AddStatsCell(table, "Phòng chiếu", stats.TotalRooms.ToString());
                        AddStatsCell(table, "Suất hôm nay", stats.TotalShowtimesToday.ToString());
                        AddStatsCell(table, "Vé đã bán", stats.TotalTicketsSold.ToString("N0"));
                        AddStatsCell(table, "Tổng doanh thu", stats.TotalRevenue.ToString("N0") + " đ");
                        AddStatsCell(table, "Hóa đơn", stats.TotalInvoices.ToString("N0"));
                    });

                    // --- Top 5 phim ---
                    col.Item().PaddingTop(20).Text("2. TOP 5 PHIM ĂN KHÁCH").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                    if (topMovies.Count > 0)
                    {
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(30);
                                c.RelativeColumn(3);
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                            });

                            // Header
                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("#").Bold();
                                header.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("Tên phim").Bold();
                                header.Cell().Background(Colors.Blue.Lighten4).Padding(5).Text("Vé").Bold();
                                header.Cell().Background(Colors.Blue.Lighten4).Padding(5).AlignRight().Text("Doanh thu").Bold();
                            });

                            for (int i = 0; i < topMovies.Count; i++)
                            {
                                var m = topMovies[i];
                                var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;
                                table.Cell().Background(bg).Padding(5).Text((i + 1).ToString());
                                table.Cell().Background(bg).Padding(5).Text(m.Title);
                                table.Cell().Background(bg).Padding(5).Text(m.TicketCount.ToString());
                                table.Cell().Background(bg).Padding(5).AlignRight().Text(m.Revenue.ToString("N0") + " đ");
                            }
                        });
                    }
                    else
                    {
                        col.Item().PaddingTop(5).Text("Chưa có dữ liệu.").Italic().FontColor(Colors.Grey.Medium);
                    }

                    // --- Doanh thu theo ngày ---
                    col.Item().PaddingTop(20).Text("3. DOANH THU THEO NGÀY").FontSize(13).Bold().FontColor(Colors.Blue.Darken2);

                    if (revenueData.Count > 0)
                    {
                        col.Item().PaddingTop(5).Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(2);
                                c.RelativeColumn(1);
                                c.RelativeColumn(2);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Background(Colors.Green.Lighten4).Padding(5).Text("Ngày").Bold();
                                header.Cell().Background(Colors.Green.Lighten4).Padding(5).Text("Vé bán").Bold();
                                header.Cell().Background(Colors.Green.Lighten4).Padding(5).AlignRight().Text("Doanh thu").Bold();
                            });

                            decimal totalPeriod = 0;
                            int totalTickets = 0;

                            foreach (var d in revenueData)
                            {
                                table.Cell().Padding(4).Text(d.Date.ToString("dd/MM/yyyy"));
                                table.Cell().Padding(4).Text(d.TicketCount.ToString());
                                table.Cell().Padding(4).AlignRight().Text(d.Revenue.ToString("N0") + " đ");
                                totalPeriod += d.Revenue;
                                totalTickets += d.TicketCount;
                            }

                            // Total row
                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text("TỔNG CỘNG").Bold();
                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5).Text(totalTickets.ToString()).Bold();
                            table.Cell().Background(Colors.Grey.Lighten3).Padding(5).AlignRight().Text(totalPeriod.ToString("N0") + " đ").Bold();
                        });
                    }
                    else
                    {
                        col.Item().PaddingTop(5).Text("Chưa có dữ liệu doanh thu trong khoảng thời gian này.").Italic().FontColor(Colors.Grey.Medium);
                    }
                });

                // === FOOTER ===
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("CineManager — Báo cáo tự động | Trang ");
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf(filePath);
    }

    private static void AddStatsCell(TableDescriptor table, string label, string value)
    {
        table.Cell().Padding(6).Column(col =>
        {
            col.Item().Text(label).FontSize(9).FontColor(Colors.Grey.Medium);
            col.Item().Text(value).FontSize(14).Bold().FontColor(Colors.Blue.Darken2);
        });
    }
}
