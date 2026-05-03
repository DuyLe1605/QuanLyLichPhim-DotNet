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
    private Button btnExportPdf = null!;

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

        var mainPanel = new Panel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowOnly,
            Width = 1200,
            Dock = DockStyle.Top,
            Padding = new Padding(5)
        };
        this.Controls.Add(mainPanel);

        int y = 5;

        // === Title ===
        mainPanel.Controls.Add(new Label
        {
            Text = "📊  Tổng Quan & Thống Kê",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(210, 210, 230),
            Location = new Point(5, y),
            AutoSize = true
        });
        y += 45;

        // === Stat Cards (6 cards) ===
        var statsPanel = new FlowLayoutPanel
        {
            Location = new Point(5, y),
            Size = new Size(1150, 85),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        mainPanel.Controls.Add(statsPanel);

        (lblMovies, _) = AddStatCard(statsPanel, "🎬 Phim", "0", Color.FromArgb(100, 80, 255));
        (lblRooms, _) = AddStatCard(statsPanel, "🏠 Phòng", "0", Color.FromArgb(60, 160, 200));
        (lblShows, _) = AddStatCard(statsPanel, "📅 Suất hôm nay", "0", Color.FromArgb(200, 150, 40));
        (lblTickets, _) = AddStatCard(statsPanel, "🎟️ Vé đã bán", "0", Color.FromArgb(80, 200, 120));
        (lblRevenue, _) = AddStatCard(statsPanel, "💰 Doanh thu", "0 đ", Color.FromArgb(220, 80, 120));
        (lblInvoices, _) = AddStatCard(statsPanel, "📄 Hóa đơn", "0", Color.FromArgb(160, 100, 220));

        y += 95;

        // === Filter Row ===
        var pnlFilter = new Panel
        {
            Location = new Point(5, y),
            Size = new Size(1150, 38),
            BackColor = Color.Transparent
        };
        mainPanel.Controls.Add(pnlFilter);

        pnlFilter.Controls.Add(new Label { Text = "Từ:", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(150, 150, 180), Location = new Point(0, 8), AutoSize = true });
        dtpFrom = new DateTimePicker { Font = new Font("Segoe UI", 10), Size = new Size(140, 28), Location = new Point(35, 5), Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(-30) };
        pnlFilter.Controls.Add(dtpFrom);

        pnlFilter.Controls.Add(new Label { Text = "Đến:", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(150, 150, 180), Location = new Point(190, 8), AutoSize = true });
        dtpTo = new DateTimePicker { Font = new Font("Segoe UI", 10), Size = new Size(140, 28), Location = new Point(235, 5), Format = DateTimePickerFormat.Short };
        pnlFilter.Controls.Add(dtpTo);

        pnlFilter.Controls.Add(new Label { Text = "Doanh thu:", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(150, 150, 180), Location = new Point(400, 8), AutoSize = true });
        cboRevenueMode = new ComboBox
        {
            Font = new Font("Segoe UI", 10),
            Size = new Size(140, 28),
            Location = new Point(490, 5),
            BackColor = Color.FromArgb(30, 30, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboRevenueMode.Items.AddRange(new[] { "Theo ngày", "Theo tháng" });
        cboRevenueMode.SelectedIndex = 0;
        pnlFilter.Controls.Add(cboRevenueMode);

        var btnRefresh = new Button
        {
            Text = "🔄 Cập nhật",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(120, 30),
            Location = new Point(650, 4),
            BackColor = Color.FromArgb(80, 60, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnRefresh.FlatAppearance.BorderSize = 0;
        btnRefresh.Click += async (s, e) => await LoadChartsAsync();
        pnlFilter.Controls.Add(btnRefresh);

        btnExportPdf = new Button
        {
            Text = "📄 Xuất PDF",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(120, 30),
            Location = new Point(785, 4),
            BackColor = Color.FromArgb(200, 60, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnExportPdf.FlatAppearance.BorderSize = 0;
        btnExportPdf.Click += BtnExportPdf_Click;
        pnlFilter.Controls.Add(btnExportPdf);

        y += 48;

        // === Revenue Chart ===
        mainPanel.Controls.Add(new Label
        {
            Text = "📈  Doanh thu",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(190, 190, 215),
            Location = new Point(5, y),
            AutoSize = true
        });
        y += 30;

        chartRevenue = new CartesianChart
        {
            Location = new Point(5, y),
            Size = new Size(1140, 260),
            BackColor = Color.FromArgb(22, 22, 38)
        };
        mainPanel.Controls.Add(chartRevenue);
        y += 270;

        // === Bottom row: Top Movies (left) + Occupancy (right) ===
        // Top Movies
        mainPanel.Controls.Add(new Label
        {
            Text = "🏆  Top 5 phim ăn khách",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(190, 190, 215),
            Location = new Point(5, y),
            AutoSize = true
        });

        mainPanel.Controls.Add(new Label
        {
            Text = "🥧  Tỷ lệ lấp đầy phòng",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(190, 190, 215),
            Location = new Point(600, y),
            AutoSize = true
        });
        y += 30;

        chartTopMovies = new CartesianChart
        {
            Location = new Point(5, y),
            Size = new Size(570, 280),
            BackColor = Color.FromArgb(22, 22, 38)
        };
        mainPanel.Controls.Add(chartTopMovies);

        chartOccupancy = new PieChart
        {
            Location = new Point(600, y),
            Size = new Size(540, 280),
            BackColor = Color.FromArgb(22, 22, 38)
        };
        mainPanel.Controls.Add(chartOccupancy);

        y += 290;
        mainPanel.Height = y + 10;
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
            if (cboRevenueMode.SelectedIndex == 0) // Theo ngày
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
            else // Theo tháng
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
            chartOccupancy.Series = occupancy.Select((r, i) => new PieSeries<double>
            {
                Values = new[] { r.OccupancyRate },
                Name = $"{r.RoomName} ({r.OccupancyRate}%)",
                Fill = new SolidColorPaint(SKColor.Parse(pieColors[i % pieColors.Length])),
                DataLabelsSize = 11,
                DataLabelsPaint = new SolidColorPaint(SKColors.White),
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle
            } as ISeries).ToArray();
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
            Size = new Size(180, 75),
            BackColor = Color.FromArgb(26, 26, 44),
            Margin = new Padding(4)
        };

        // Accent bar (top)
        card.Controls.Add(new Panel
        {
            Size = new Size(180, 3),
            BackColor = accentColor,
            Dock = DockStyle.Top
        });

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(130, 130, 160),
            Location = new Point(12, 12),
            AutoSize = true
        };
        card.Controls.Add(lblTitle);

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
