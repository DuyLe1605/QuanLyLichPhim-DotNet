using BaiTapLon.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.WinForms;
using SkiaSharp;

namespace BaiTapLon.Forms.Admin;

public class UcDashboard : UserControl
{
    private readonly List<Label> _sectionTitles = new();
    private readonly List<Panel> _statCards = new();

    private Panel scrollHost = null!;
    private TableLayoutPanel root = null!, bottomGrid = null!;
    private FlowLayoutPanel statsGrid = null!;
    private Label lblMovies = null!, lblShows = null!;
    private Label lblTickets = null!, lblRevenue = null!, lblInvoices = null!;
    private Label lblSnackRev = null!, lblNewCust = null!, lblAov = null!;
    private Label lblReviews = null!, lblAvgRating = null!;
    private Label lblLoading = null!;
    private DateTimePicker dtpFrom = null!, dtpTo = null!;
    private ComboBox cboRevenueMode = null!;
    private CartesianChart chartRevenue = null!, chartTopMovies = null!;
    private PieChart chartOccupancy = null!;

    public UcDashboard()
    {
        InitUI();
        Load += async (s, e) => await LoadAllAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);
        Padding = new Padding(16);
        AutoScroll = false;

        scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            AutoScroll = true
        };
        Controls.Add(scrollHost);

        root = new TableLayoutPanel
        {
            Location = new Point(0, 0),
            Width = 900,
            Height = 900,
            ColumnCount = 1,
            RowCount = 6,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 84));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 288));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 280));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 326));
        scrollHost.Controls.Add(root);

        root.Controls.Add(CreateHeader(), 0, 0);

        statsGrid = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 14),
            AutoScroll = false,
            WrapContents = true
        };
        root.Controls.Add(statsGrid, 0, 1);

        lblMovies = AddStatCard("Phim", "0", "Đang chiếu", Color.FromArgb(118, 95, 255));
        lblShows = AddStatCard("Suất hôm nay", "0", "Lịch trong ngày", Color.FromArgb(213, 159, 42));
        lblTickets = AddStatCard("Vé đã bán", "0", "Tất cả giao dịch", Color.FromArgb(65, 196, 126));
        lblRevenue = AddStatCard("Doanh thu", "0 đ", "Tổng doanh số", Color.FromArgb(226, 85, 126));
        lblInvoices = AddStatCard("Hóa đơn", "0", "Đã thanh toán", Color.FromArgb(157, 111, 234));
        lblSnackRev = AddStatCard("Doanh thu bắp nước", "0 đ", "Từ quầy", Color.FromArgb(250, 128, 114));
        lblNewCust = AddStatCard("KH mới", "0", "Tháng này", Color.FromArgb(100, 149, 237));
        lblAov = AddStatCard("AOV", "0 đ", "GTĐH trung bình", Color.FromArgb(32, 178, 170));
        lblReviews = AddStatCard("Review", "0", "Tổng đánh giá", Color.FromArgb(255, 182, 72));
        lblAvgRating = AddStatCard("Điểm phim", "0.0/5", "Trung bình sao", Color.FromArgb(72, 209, 204));

        root.Controls.Add(CreateFilterBar(), 0, 2);
        root.Controls.Add(CreateSectionTitle("Doanh thu"), 0, 3);


        chartRevenue = CreateCartesianChart();
        root.Controls.Add(chartRevenue, 0, 4);

        bottomGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 0)
        };
        bottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
        bottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        bottomGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        bottomGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(bottomGrid, 0, 5);

        bottomGrid.Controls.Add(CreateSectionTitle("Top 5 phim ăn khách"), 0, 0);
        bottomGrid.Controls.Add(CreateSectionTitle("Tỷ lệ lấp đầy phòng"), 1, 0);

        chartTopMovies = CreateCartesianChart();
        chartOccupancy = new PieChart
        {
            Dock = DockStyle.Fill,
            BackColor = AdminTheme.GridBack,
            Margin = new Padding(10, 0, 0, 0),
            LegendTextPaint = new SolidColorPaint(SKColor.Parse("#C8C8DC")),
            LegendTextSize = 12
        };
        bottomGrid.Controls.Add(chartTopMovies, 0, 1);
        bottomGrid.Controls.Add(chartOccupancy, 1, 1);

        lblLoading = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = AdminTheme.BodyBoldFont,
            ForeColor = AdminTheme.MutedText,
            Visible = false
        };
        Controls.Add(lblLoading);
        lblLoading.BringToFront();

        Resize += (s, e) => ApplyResponsiveLayout();
        scrollHost.Resize += (s, e) => ApplyResponsiveLayout();
        ApplyResponsiveLayout();
    }

    private Control CreateHeader()
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 250));

        var titleStack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 10, 0)
        };
        titleStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        titleStack.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        titleStack.Controls.Add(new Label
        {
            Text = "Tổng Quan Thống Kê",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = AdminTheme.TitleText,
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 0);
        titleStack.Controls.Add(new Label
        {
            Text = "Theo dõi doanh thu, vé bán và hiệu suất phòng chiếu",
            Dock = DockStyle.Fill,
            Font = AdminTheme.BodyFont,
            ForeColor = AdminTheme.MutedText,
            TextAlign = ContentAlignment.TopLeft
        }, 0, 1);

        header.Controls.Add(titleStack, 0, 0);
        header.Controls.Add(new Label
        {
            Text = DateTime.Today.ToString("'Hôm nay' dd/MM/yyyy"),
            Dock = DockStyle.Fill,
            Font = AdminTheme.BodyBoldFont,
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = Color.FromArgb(38, 38, 58),
            Margin = new Padding(12, 20, 0, 18)
        }, 1, 0);

        return header;
    }

    private Control CreateFilterBar()
    {
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 4, 0, 0),
            Padding = new Padding(0, 6, 0, 0)
        };

        dtpFrom = AdminControls.CreateDatePicker();
        dtpFrom.Value = DateTime.Today.AddDays(-30);
        dtpTo = AdminControls.CreateDatePicker();
        dtpTo.Value = DateTime.Today;

        cboRevenueMode = AdminControls.CreateComboBox(135);
        cboRevenueMode.Items.AddRange(new object[] { "Theo ngày", "Theo tháng" });
        cboRevenueMode.SelectedIndex = 0;

        bar.Controls.Add(AdminControls.CreateToolbarLabel("Từ:", 28));
        bar.Controls.Add(dtpFrom);
        bar.Controls.Add(AdminControls.CreateToolbarLabel("Đến:", 38));
        bar.Controls.Add(dtpTo);
        bar.Controls.Add(AdminControls.CreateToolbarLabel("Chế độ:", 64));
        bar.Controls.Add(cboRevenueMode);
        bar.Controls.Add(AdminControls.CreateButton("Cập nhật", Color.FromArgb(88, 72, 216), 108, async (s, e) => await RunWithLoadingAsync(LoadChartsAsync)));
        bar.Controls.Add(AdminControls.CreateButton("Xuất PDF", AdminTheme.ButtonDanger, 108, BtnExportPdf_Click));
        return bar;
    }

    private Label AddStatCard(string title, string value, string subtitle, Color accent)
    {
        var card = new Panel
        {
            Size = new Size(200, 100),
            BackColor = Color.FromArgb(24, 24, 40),
            Padding = new Padding(12, 10, 10, 10),
            Margin = new Padding(4, 4, 4, 4)
        };
        statsGrid.Controls.Add(card);
        _statCards.Add(card);

        card.Controls.Add(new Panel
        {
            Dock = DockStyle.Left,
            Width = 4,
            BackColor = accent
        });

        var pnlText = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = new Padding(4, 0, 0, 0)
        };
        pnlText.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        pnlText.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        pnlText.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        pnlText.Controls.Add(new Label
        {
            Text = title,
            Font = AdminTheme.BodyBoldFont,
            ForeColor = AdminTheme.MutedText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 0);

        var lblValue = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = accent,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlText.Controls.Add(lblValue, 0, 1);

        pnlText.Controls.Add(new Label
        {
            Text = subtitle,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(112, 112, 142),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.TopLeft
        }, 0, 2);

        card.Controls.Add(pnlText);
        return lblValue;
    }

    private Label CreateSectionTitle(string text)
    {
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = AdminTheme.TitleText,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };
        _sectionTitles.Add(label);
        return label;
    }

    private static CartesianChart CreateCartesianChart()
    {
        return new CartesianChart
        {
            Dock = DockStyle.Fill,
            BackColor = AdminTheme.GridBack,
            Margin = new Padding(0, 0, 0, 10),
            LegendTextPaint = new SolidColorPaint(SKColor.Parse("#C8C8DC")),
            LegendTextSize = 12
        };
    }

    private async Task LoadAllAsync()
    {
        await RunWithLoadingAsync(async () =>
        {
            await LoadStatsAsync();
            await LoadChartsAsync();
        });
    }

    private async Task LoadStatsAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var svc = new ReportService(ctx);
            var stats = await svc.GetStatsAsync();

            lblMovies.Text = stats.TotalMovies.ToString("N0");
            lblShows.Text = stats.TotalShowtimesToday.ToString("N0");
            lblTickets.Text = stats.TotalTicketsSold.ToString("N0");
            lblRevenue.Text = stats.TotalRevenue.ToString("N0") + " đ";
            lblInvoices.Text = stats.TotalInvoices.ToString("N0");
            lblSnackRev.Text = stats.TotalSnackRevenue.ToString("N0") + " đ";
            lblNewCust.Text = stats.NewCustomersMonth.ToString("N0");
            lblAov.Text = stats.AOV.ToString("N0") + " đ";
            lblReviews.Text = stats.TotalReviews.ToString("N0");
            lblAvgRating.Text = stats.AverageRating.ToString("N1") + "/5";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải thống kê: {ex.Message}", "Dashboard");
        }
    }

    private async Task LoadChartsAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var svc = new ReportService(ctx);
            var from = dtpFrom.Value.Date;
            var to = dtpTo.Value.Date;

            if (from > to)
            {
                MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc.", "Dashboard");
                return;
            }

            if (cboRevenueMode.SelectedIndex == 0)
            {
                var data = await svc.GetRevenueByDateAsync(from, to);
                chartRevenue.Series = new ISeries[]
                {
                    new ColumnSeries<decimal>
                    {
                        Values = data.Select(d => d.Revenue).ToArray(),
                        Name = "Doanh thu",

                        Fill = new SolidColorPaint(SKColor.Parse("#725DFF")),
                        MaxBarWidth = 34
                    }
                };
                chartRevenue.XAxes = new[] { CreateAxis(data.Select(d => d.Date.ToString("dd/MM")).ToArray(), 10) };
            }
            else
            {
                var data = await svc.GetRevenueByMonthAsync(to.Year);
                chartRevenue.Series = new ISeries[]
                {
                    new ColumnSeries<decimal>
                    {
                        Values = data.Select(d => d.Revenue).ToArray(),
                        Name = "Doanh thu",
                        Fill = new SolidColorPaint(SKColor.Parse("#725DFF")),
                        MaxBarWidth = 42
                    }
                };
                chartRevenue.XAxes = new[] { CreateAxis(data.Select(d => $"T{d.Month}").ToArray(), 11) };
            }

            chartRevenue.YAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#A5A5BF")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#33334D")),
                    TextSize = 10,
                    Labeler = v => v.ToString("N0")
                }
            };

            var topMovies = await svc.GetTopMoviesAsync(5, from, to);
            chartTopMovies.Series = topMovies.Count == 0
                ? Array.Empty<ISeries>()
                : new ISeries[]
                {
                    new RowSeries<int>
                    {
                        Values = topMovies.Select(m => m.TicketCount).ToArray(),
                        Name = "Số vé",
                        Fill = new SolidColorPaint(SKColor.Parse("#41C47E")),
                        MaxBarWidth = 24
                    }
                };
            chartTopMovies.YAxes = new[] { CreateAxis(topMovies.Select(m => Shorten(m.Title, 22)).ToArray(), 10) };
            chartTopMovies.XAxes = new[]
            {
                new Axis
                {
                    LabelsPaint = new SolidColorPaint(SKColor.Parse("#A5A5BF")),
                    SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#33334D")),
                    TextSize = 10
                }
            };

            var occupancy = await svc.GetRoomOccupancyAsync(from, to);
            var pieColors = new[] { "#725DFF", "#41C47E", "#E2557E", "#D59F2A", "#2CA4B8", "#A36FEA" };
            chartOccupancy.Series = occupancy.Count == 0
                ? Array.Empty<ISeries>()
                : occupancy.Select((r, i) => new PieSeries<double>
                {
                    Values = new[] { Math.Max(r.OccupancyRate, 0.1) },
                    Name = $"{r.RoomName} ({r.OccupancyRate:N1}%)",
                    Fill = new SolidColorPaint(SKColor.Parse(pieColors[i % pieColors.Length])),
                    DataLabelsPaint = new SolidColorPaint(SKColors.White),
                    DataLabelsSize = 11,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle
                } as ISeries).ToArray();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải biểu đồ: {ex.Message}", "Dashboard");
        }
    }

    private async void BtnExportPdf_Click(object? sender, EventArgs e)
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var svc = new ReportService(ctx);
            var from = dtpFrom.Value.Date;
            var to = dtpTo.Value.Date;
            var stats = await svc.GetStatsAsync();
            var topMovies = await svc.GetTopMoviesAsync(5, from, to);
            var revenueData = await svc.GetRevenueByDateAsync(from, to);

            using var sfd = new SaveFileDialog
            {
                Filter = "PDF|*.pdf",
                FileName = $"BáoCáo_{from:ddMMyyyy}_{to:ddMMyyyy}.pdf",
                Title = "Lưu báo cáo PDF"
            };

            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            Helpers.PrintHelper.ExportReport(sfd.FileName, stats, topMovies, revenueData, from, to);
            MessageBox.Show($"Đã xuất PDF: {sfd.FileName}", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi xuất PDF: {ex.Message}", "Dashboard");
        }
    }

    private async Task RunWithLoadingAsync(Func<Task> action)
    {
        lblLoading.Visible = true;
        lblLoading.Text = "Đang tải dữ liệu...";
        UseWaitCursor = true;

        try
        {
            await action();
        }
        finally
        {
            UseWaitCursor = false;
            lblLoading.Visible = false;
        }
    }

    private void ApplyResponsiveLayout()
    {
        if (scrollHost == null || root == null || statsGrid == null || bottomGrid == null)
            return;

        var width = Math.Max(560, scrollHost.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2);
        root.Width = width;

        // Keep KPI cards scan-friendly while allowing the review cards to wrap cleanly.
        var columns = width < 720 ? 2 : width < 1100 ? 3 : 4;

        statsGrid.SuspendLayout();
        
        // Calculate dynamic width for each card based on available width and columns
        // Margin is 8px horizontal per card (4 left, 4 right)
        var cardWidth = (width / columns) - 10; 
        
        foreach (var card in _statCards)
        {
            if (!statsGrid.Controls.Contains(card))
                statsGrid.Controls.Add(card);
                
            card.Size = new Size(cardWidth, 100);
        }
        
        statsGrid.ResumeLayout();
        
        int rows = (int)Math.Ceiling(_statCards.Count / (double)columns);
        root.RowStyles[1].Height = rows * 108 + 16;

        if (width < 900)
        {
            bottomGrid.ColumnCount = 1;
            bottomGrid.RowCount = 4;
            bottomGrid.ColumnStyles.Clear();
            bottomGrid.RowStyles.Clear();
            bottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottomGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            bottomGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
            bottomGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            bottomGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
            root.RowStyles[5].Height = 604;
            bottomGrid.SetCellPosition(_sectionTitles[1], new TableLayoutPanelCellPosition(0, 0));
            bottomGrid.SetCellPosition(chartTopMovies, new TableLayoutPanelCellPosition(0, 1));
            bottomGrid.SetCellPosition(_sectionTitles[2], new TableLayoutPanelCellPosition(0, 2));
            bottomGrid.SetCellPosition(chartOccupancy, new TableLayoutPanelCellPosition(0, 3));
            chartOccupancy.Margin = Padding.Empty;
        }
        else
        {
            bottomGrid.ColumnCount = 2;
            bottomGrid.RowCount = 2;
            bottomGrid.ColumnStyles.Clear();
            bottomGrid.RowStyles.Clear();
            bottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 56));
            bottomGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
            bottomGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            bottomGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles[5].Height = 326;
            bottomGrid.SetCellPosition(_sectionTitles[1], new TableLayoutPanelCellPosition(0, 0));
            bottomGrid.SetCellPosition(chartTopMovies, new TableLayoutPanelCellPosition(0, 1));
            bottomGrid.SetCellPosition(_sectionTitles[2], new TableLayoutPanelCellPosition(1, 0));
            bottomGrid.SetCellPosition(chartOccupancy, new TableLayoutPanelCellPosition(1, 1));
            chartOccupancy.Margin = new Padding(10, 0, 0, 0);
        }

        var totalHeight = 0;
        foreach (RowStyle style in root.RowStyles)
            totalHeight += (int)style.Height;

        root.Height = totalHeight;
        scrollHost.AutoScrollMinSize = new Size(0, totalHeight + 12);
    }

    private static Axis CreateAxis(string[] labels, double textSize)
    {
        return new Axis
        {
            Labels = labels,
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#C8C8DC")),
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#33334D")),
            TextSize = textSize
        };
    }

    private static string Shorten(string value, int maxLength)
    {
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
}
