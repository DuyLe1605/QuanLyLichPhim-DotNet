using BaiTapLon.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;

namespace BaiTapLon.Forms.Admin;

/// <summary>
/// Dashboard thống kê: tổng quan + biểu đồ doanh thu, top phim, tỷ lệ lấp đầy.
/// </summary>
public class UcDashboard : UserControl
{
    // === Stats cards ===
    private Label lblMovies = null!, lblRooms = null!, lblShows = null!;
    private Label lblTickets = null!, lblRevenue = null!, lblInvoices = null!;

    // === Charts ===
    private CartesianChart chartRevenue = null!;
    private CartesianChart chartTopMovies = null!;
    private PieChart chartOccupancy = null!;

    // === Filters ===
    private DateTimePicker dtpFrom = null!, dtpTo = null!;
    private ComboBox cboRevenueMode = null!;

    public UcDashboard()
    {
        InitUI();
        this.Load += async (s, e) => await LoadAllAsync();
    }

    private void InitUI()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(18, 18, 30);
        this.AutoScroll = true;
        this.Padding = new Padding(5);

        // ========= Outer container: Flow cho responsive =========
        var mainFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0)
        };
        this.Controls.Add(mainFlow);

        // === Title ===
        mainFlow.Controls.Add(new Label
        {
            Text = "📊  Tổng Quan & Thống Kê",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(210, 210, 230),
            AutoSize = true,
            Margin = new Padding(5, 5, 5, 10)
        });

        // === Stat Cards (6 cards) ===
        var statsPanel = new FlowLayoutPanel
        {
            Size = new Size(1100, 85),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 5)
        };
        mainFlow.Controls.Add(statsPanel);

        (lblMovies, _) = AddStatCard(statsPanel, "🎬 Phim", "0", Color.FromArgb(100, 80, 255));
        (lblRooms, _) = AddStatCard(statsPanel, "🏠 Phòng", "0", Color.FromArgb(60, 160, 200));
        (lblShows, _) = AddStatCard(statsPanel, "📅 Suất hôm nay", "0", Color.FromArgb(200, 150, 40));
        (lblTickets, _) = AddStatCard(statsPanel, "🎟️ Vé đã bán", "0", Color.FromArgb(80, 200, 120));
        (lblRevenue, _) = AddStatCard(statsPanel, "💰 Doanh thu", "0 đ", Color.FromArgb(220, 80, 120));
        (lblInvoices, _) = AddStatCard(statsPanel, "📄 Hóa đơn", "0", Color.FromArgb(160, 100, 220));

        // === Filter Row ===
        var pnlFilter = new Panel
        {
            Size = new Size(1100, 38),
            BackColor = Color.Transparent,
            Margin = new Padding(0, 5, 0, 8)
        };
        mainFlow.Controls.Add(pnlFilter);

        int fx = 0;
        pnlFilter.Controls.Add(new Label { Text = "Từ:", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(150, 150, 180), Location = new Point(fx, 8), AutoSize = true });
        fx += 30;
        dtpFrom = new DateTimePicker { Font = new Font("Segoe UI", 10), Size = new Size(135, 28), Location = new Point(fx, 5), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
        pnlFilter.Controls.Add(dtpFrom);
        fx += 150;

        pnlFilter.Controls.Add(new Label { Text = "Đến:", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(150, 150, 180), Location = new Point(fx, 8), AutoSize = true });
        fx += 38;
        dtpTo = new DateTimePicker { Font = new Font("Segoe UI", 10), Size = new Size(135, 28), Location = new Point(fx, 5), Format = DateTimePickerFormat.Short };
        pnlFilter.Controls.Add(dtpTo);
        fx += 150;

        pnlFilter.Controls.Add(new Label { Text = "Doanh thu:", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(150, 150, 180), Location = new Point(fx, 8), AutoSize = true });
        fx += 85;
        cboRevenueMode = new ComboBox
        {
            Font = new Font("Segoe UI", 10), Size = new Size(120, 28),
            Location = new Point(fx, 5),
            BackColor = Color.FromArgb(30, 30, 50), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboRevenueMode.Items.AddRange(new[] { "Theo ngày", "Theo tháng" });
        cboRevenueMode.SelectedIndex = 0;
        pnlFilter.Controls.Add(cboRevenueMode);
        fx += 135;

        var btnRefresh = new Button
        {
            Text = "🔄 Cập nhật", Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(110, 30), Location = new Point(fx, 4),
            BackColor = Color.FromArgb(80, 60, 200), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += async (s, e) => await LoadChartsAsync();
        pnlFilter.Controls.Add(btnRefresh);
        fx += 120;

        var btnPdf = new Button
        {
            Text = "📄 Xuất PDF", Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(110, 30), Location = new Point(fx, 4),
            BackColor = Color.FromArgb(200, 60, 60), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand
        };
        btnPdf.FlatAppearance.BorderSize = 0;
        btnPdf.Click += BtnExportPdf_Click;
        pnlFilter.Controls.Add(btnPdf);

        // === Revenue Chart ===
        mainFlow.Controls.Add(new Label
        {
            Text = "📈  Doanh thu",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(190, 190, 215),
            AutoSize = true,
            Margin = new Padding(5, 5, 5, 3)
        });

        chartRevenue = new CartesianChart
        {
            Size = new Size(1060, 240),
            BackColor = Color.FromArgb(22, 22, 38),
            Margin = new Padding(5, 0, 5, 10)
        };
        mainFlow.Controls.Add(chartRevenue);

        // === Bottom row container ===
        var pnlBottom = new Panel
        {
            Size = new Size(1100, 310),
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 10)
        };
        mainFlow.Controls.Add(pnlBottom);

        // Top Movies label
        pnlBottom.Controls.Add(new Label
        {
            Text = "🏆  Top 5 phim ăn khách",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(190, 190, 215),
            Location = new Point(5, 0),
            AutoSize = true
        });

        // Occupancy label
        pnlBottom.Controls.Add(new Label
        {
            Text = "🥧  Tỷ lệ lấp đầy phòng",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(190, 190, 215),
            Location = new Point(550, 0),
            AutoSize = true
        });

        chartTopMovies = new CartesianChart
        {
            Location = new Point(5, 28),
            Size = new Size(520, 270),
            BackColor = Color.FromArgb(22, 22, 38)
        };
        pnlBottom.Controls.Add(chartTopMovies);

        chartOccupancy = new PieChart
        {
            Location = new Point(550, 28),
            Size = new Size(500, 270),
            BackColor = Color.FromArgb(22, 22, 38)
        };
        pnlBottom.Controls.Add(chartOccupancy);
    }

    // ==================== DATA LOADING ====================

    private async Task LoadAllAsync()
    {
        await LoadStatsAsync();
        await LoadChartsAsync();
    }

    private async Task LoadStatsAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var svc = new ReportService(ctx);
            var stats = await svc.GetStatsAsync();

            lblMovies.Text = stats.TotalMovies.ToString();
            lblRooms.Text = stats.TotalRooms.ToString();
            lblShows.Text = stats.TotalShowtimesToday.ToString();
            lblTickets.Text = stats.TotalTicketsSold.ToString("N0");
            lblRevenue.Text = stats.TotalRevenue.ToString("N0") + " đ";
            lblInvoices.Text = stats.TotalInvoices.ToString("N0");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải thống kê: {ex.Message}");
        }
    }

    private async Task LoadChartsAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var svc = new ReportService(ctx);

            DateTime from = dtpFrom.Value.Date;
            DateTime to = dtpTo.Value.Date;

            // === Revenue Chart ===
            if (cboRevenueMode.SelectedIndex == 0)
            {
                var data = await svc.GetRevenueByDateAsync(from, to);
                chartRevenue.Series = new ISeries[]
                {
                    new ColumnSeries<decimal>
                    {
                        Values = data.Select(d => d.Revenue).ToArray(),
                        Name = "Doanh thu (đ)",
                        Fill = new SolidColorPaint(SKColor.Parse("#6450FF")),
                        MaxBarWidth = 25
                    }
                };
                chartRevenue.XAxes = new[]
                {
                    new Axis
                    {
                        Labels = data.Select(d => d.Date.ToString("dd/MM")).ToArray(),
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#9999B0")),
                        TextSize = 10
                    }
                };
            }
            else
            {
                var data = await svc.GetRevenueByMonthAsync(DateTime.Now.Year);
                chartRevenue.Series = new ISeries[]
                {
                    new ColumnSeries<decimal>
                    {
                        Values = data.Select(d => d.Revenue).ToArray(),
                        Name = "Doanh thu (đ)",
                        Fill = new SolidColorPaint(SKColor.Parse("#6450FF")),
                        MaxBarWidth = 40
                    }
                };
                chartRevenue.XAxes = new[]
                {
                    new Axis
                    {
                        Labels = data.Select(d => $"T{d.Month}").ToArray(),
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#9999B0")),
                        TextSize = 11
                    }
                };
            }

            chartRevenue.YAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#9999B0")),
                    TextSize = 10,
                    Labeler = v => v.ToString("N0")
                }
            };

            // === Top Movies Chart ===
            var topMovies = await svc.GetTopMoviesAsync(5, from, to);
            if (topMovies.Count > 0)
            {
                chartTopMovies.Series = new ISeries[]
                {
                    new RowSeries<int>
                    {
                        Values = topMovies.Select(m => m.TicketCount).ToArray(),
                        Name = "Số vé",
                        Fill = new SolidColorPaint(SKColor.Parse("#50C878")),
                        MaxBarWidth = 20
                    }
                };
                chartTopMovies.YAxes = new[]
                {
                    new Axis
                    {
                        Labels = topMovies.Select(m => m.Title.Length > 18 ? m.Title[..18] + "..." : m.Title).ToArray(),
                        LabelsPaint = new SolidColorPaint(SKColor.Parse("#CCCCDD")),
                        TextSize = 10
                    }
                };
            }
            else
            {
                chartTopMovies.Series = Array.Empty<ISeries>();
            }
            chartTopMovies.XAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#9999B0")),
                    TextSize = 10
                }
            };

            // === Occupancy Pie Chart ===
            var occupancy = await svc.GetRoomOccupancyAsync(from, to);
            var pieColors = new[] { "#6450FF", "#50C878", "#FF6B8A", "#FFB84D", "#40C4E0", "#C878FF" };
            if (occupancy.Count > 0)
            {
                chartOccupancy.Series = occupancy.Select((r, i) => new PieSeries<double>
                {
                    Values = new[] { Math.Max(r.OccupancyRate, 0.1) }, // Avoid zero-size slices
                    Name = $"{r.RoomName} ({r.OccupancyRate}%)",
                    Fill = new SolidColorPaint(SKColor.Parse(pieColors[i % pieColors.Length])),
                    DataLabelsSize = 11,
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle
                } as ISeries).ToArray();
            }
            else
            {
                chartOccupancy.Series = Array.Empty<ISeries>();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải biểu đồ: {ex.Message}");
        }
    }

    // ==================== PDF EXPORT ====================

    private async void BtnExportPdf_Click(object? sender, EventArgs e)
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var svc = new ReportService(ctx);
            DateTime from = dtpFrom.Value.Date;
            DateTime to = dtpTo.Value.Date;

            var stats = await svc.GetStatsAsync();
            var topMovies = await svc.GetTopMoviesAsync(5, from, to);
            var revenueData = await svc.GetRevenueByDateAsync(from, to);

            using var sfd = new SaveFileDialog
            {
                Filter = "PDF|*.pdf",
                FileName = $"BaoCao_{from:ddMMyyyy}_{to:ddMMyyyy}.pdf",
                Title = "Lưu báo cáo PDF"
            };

            if (sfd.ShowDialog() != DialogResult.OK) return;

            Helpers.PrintHelper.ExportReport(sfd.FileName, stats, topMovies, revenueData, from, to);
            MessageBox.Show($"Đã xuất PDF: {sfd.FileName}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi xuất PDF: {ex.Message}", "Lỗi");
        }
    }

    // ==================== HELPERS ====================

    private (Label valueLabel, Panel card) AddStatCard(FlowLayoutPanel parent, string title, string value, Color accentColor)
    {
        var card = new Panel
        {
            Size = new Size(170, 75),
            BackColor = Color.FromArgb(26, 26, 44),
            Margin = new Padding(4)
        };

        // Accent bar (top)
        card.Controls.Add(new Panel
        {
            Size = new Size(170, 3),
            BackColor = accentColor,
            Dock = DockStyle.Top
        });

        card.Controls.Add(new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(130, 130, 160),
            Location = new Point(12, 12),
            AutoSize = true
        });

        var lblValue = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = accentColor,
            Location = new Point(12, 35),
            AutoSize = true
        };
        card.Controls.Add(lblValue);

        parent.Controls.Add(card);
        return (lblValue, card);
    }
}
