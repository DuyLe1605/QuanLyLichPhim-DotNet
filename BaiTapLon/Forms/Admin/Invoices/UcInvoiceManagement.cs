using BaiTapLon.Helpers;
using BaiTapLon.Forms.Controls;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Forms.Admin;

public class UcInvoiceManagement : UserControl
{
    private readonly bool _adminMode;
    private DataGridView dgvInvoices = null!;
    private DataGridView dgvTickets = null!;
    private DataGridView dgvSnacks = null!;
    private SeatLayoutPreviewControl seatPreview = null!;
    private SplitContainer split = null!;
    private AdminPaginationBar pagination = null!;
    private TextBox txtSearch = null!;
    private DateTimePicker dtpFrom = null!, dtpTo = null!;
    private ComboBox cboPayment = null!;
    private Label lblSummary = null!, lblDetailTitle = null!, lblDetailInfo = null!;
    private List<InvoiceRow> _rows = new();

    public UcInvoiceManagement()
    {
        _adminMode = SessionManager.IsAdmin;
        InitUI();
        Load += async (s, e) => await LoadInvoicesAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        txtSearch = AdminControls.CreateSearchBox("Tìm mã HĐ, khách hàng, SĐT...", 270);
        txtSearch.TextChanged += async (s, e) => await LoadInvoicesAsync();

        dtpFrom = AdminControls.CreateDatePicker();
        dtpFrom.Value = DateTime.Today.AddDays(-30);
        dtpTo = AdminControls.CreateDatePicker();
        dtpTo.Value = DateTime.Today;

        cboPayment = AdminControls.CreateComboBox(130);
        cboPayment.Items.AddRange(new object[] { "Tất cả", "Cash", "Card", "Transfer", "QR" });
        cboPayment.SelectedIndex = 0;
        cboPayment.SelectedIndexChanged += async (s, e) => await LoadInvoicesAsync();

        var toolbar = AdminControls.CreateToolbar(
            txtSearch,
            AdminControls.CreateToolbarLabel("Tu:", 28),
            dtpFrom,
            AdminControls.CreateToolbarLabel("Den:", 38),
            dtpTo,
            cboPayment,
            AdminControls.CreateButton("Lọc", AdminTheme.ButtonPrimary, 74, async (s, e) => await LoadInvoicesAsync()),
            AdminControls.CreateButton("Làm mới", AdminTheme.ButtonNeutral, 98, async (s, e) => await LoadInvoicesAsync())
        );

        dgvInvoices = AdminControls.CreateGrid();
        dgvInvoices.SelectionChanged += async (s, e) => await LoadSelectedDetailAsync();
        dgvInvoices.CellDoubleClick += async (s, e) => await LoadSelectedDetailAsync();

        pagination = new AdminPaginationBar();
        pagination.PaginationChanged += (s, e) => BindInvoicePage();

        split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor = AdminTheme.PageBack
        };
        split.HandleCreated += (s, e) => ApplyInvoiceLayout();
        split.Resize += (s, e) => ApplyInvoiceLayout();

        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        lblSummary = new Label
        {
            Dock = DockStyle.Fill,
            Font = AdminTheme.BodyBoldFont,
            ForeColor = AdminTheme.MutedText,
            TextAlign = ContentAlignment.MiddleLeft
        };
        left.Controls.Add(lblSummary, 0, 0);
        left.Controls.Add(AdminLayouts.CreatePagedGridContent(dgvInvoices, pagination), 0, 1);
        split.Panel1.Controls.Add(left);
        split.Panel2.Controls.Add(CreateDetailPanel());

        var title = _adminMode ? "Hóa Đơn" : "Hóa Đơn Của Tôi";
        Controls.Add(CreateInvoicePage(title, toolbar, split));
        Resize += (s, e) => ApplyInvoiceLayout();
    }

    private Control CreateInvoicePage(string title, Control toolbar, Control content)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        layout.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            Font = AdminTheme.TitleFont,
            ForeColor = AdminTheme.TitleText,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        }, 0, 0);

        toolbar.Dock = DockStyle.Fill;
        toolbar.Margin = Padding.Empty;
        layout.Controls.Add(toolbar, 0, 1);

        content.Dock = DockStyle.Fill;
        layout.Controls.Add(content, 0, 2);
        return layout;
    }

    private Control CreateDetailPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            BackColor = Color.FromArgb(22, 22, 38),
            Padding = new Padding(14)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 128));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 240));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 54));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 46));

        lblDetailTitle = new Label
        {
            Text = "Chọn hóa đơn",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = AdminTheme.TitleText,
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(lblDetailTitle, 0, 0);

        lblDetailInfo = new Label
        {
            Text = "Chi tiết vé và bắp nước sẽ hiển thị tại đây.",
            Dock = DockStyle.Fill,
            Font = AdminTheme.BodyFont,
            ForeColor = AdminTheme.MutedText,
            TextAlign = ContentAlignment.TopLeft
        };
        panel.Controls.Add(lblDetailInfo, 0, 1);

        panel.Controls.Add(new Label
        {
            Text = "So do ghe da mua",
            Dock = DockStyle.Fill,
            Font = AdminTheme.BodyBoldFont,
            ForeColor = AdminTheme.TitleText,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 2);

        seatPreview = new SeatLayoutPreviewControl
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 8),
            ShowLegend = true
        };
        seatPreview.ClearPreview("Chon hoa don de xem ghe");
        panel.Controls.Add(seatPreview, 0, 3);

        panel.Controls.Add(new Label
        {
            Text = "Vé trong hóa đơn",
            Dock = DockStyle.Fill,
            Font = AdminTheme.BodyBoldFont,
            ForeColor = AdminTheme.TitleText,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 4);

        dgvTickets = AdminControls.CreateGrid();
        dgvTickets.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        panel.Controls.Add(dgvTickets, 0, 5);

        dgvSnacks = AdminControls.CreateGrid();
        dgvSnacks.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvSnacks.Margin = new Padding(0, 10, 0, 0);
        panel.Controls.Add(dgvSnacks, 0, 6);

        return panel;
    }

    private async Task LoadInvoicesAsync()
    {
        try
        {
            var from = dtpFrom.Value.Date;
            var toExclusive = dtpTo.Value.Date.AddDays(1);
            if (from >= toExclusive)
            {
                MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc.", "Hóa đơn");
                return;
            }

            using var ctx = Program.CreateDbContext();
            var query = ctx.Invoices
                .AsNoTracking()
                .Include(i => i.User)
                .Include(i => i.Customer)
                .Include(i => i.Tickets)
                .Include(i => i.InvoiceSnacks)
                .Where(i => i.CreatedAt >= from && i.CreatedAt < toExclusive);

            if (!_adminMode && SessionManager.CurrentUser != null)
            {
                var userId = SessionManager.CurrentUser.Id;
                query = query.Where(i => i.UserId == userId);
            }

            var payment = cboPayment.SelectedItem?.ToString();
            if (!string.IsNullOrWhiteSpace(payment) && payment != "Tất cả")
                query = query.Where(i => i.PaymentMethod == payment);

            var keyword = txtSearch.Text.Trim();
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(i =>
                    i.Id.ToString().Contains(keyword) ||
                    (i.CustomerName != null && i.CustomerName.Contains(keyword)) ||
                    (i.CustomerPhone != null && i.CustomerPhone.Contains(keyword)) ||
                    (i.Customer != null && i.Customer.FullName.Contains(keyword)) ||
                    (i.Customer != null && i.Customer.Phone.Contains(keyword)));
            }

            _rows = await query
                .OrderByDescending(i => i.CreatedAt)
                .Select(i => new InvoiceRow(
                    i.Id,
                    i.CreatedAt,
                    i.User.FullName,
                    i.Customer != null ? i.Customer.FullName : (i.CustomerName ?? "Khách lẻ"),
                    i.Customer != null ? i.Customer.Phone : (i.CustomerPhone ?? ""),
                    i.PaymentMethod,
                    i.Tickets.Count,
                    i.InvoiceSnacks.Sum(s => s.Quantity),
                    i.DiscountAmount,
                    i.TotalAmount))
                .ToListAsync();

            pagination.SetTotalItems(_rows.Count, resetPage: true);
            BindInvoicePage();
            await LoadSelectedDetailAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải hóa đơn: {ex.Message}", "Hóa đơn");
        }
    }

    private void BindInvoicePage()
    {
        dgvInvoices.DataSource = null;
        dgvInvoices.Columns.Clear();
        dgvInvoices.DataSource = _rows
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(i => new
            {
                i.Id,
                MaHD = $"HD{i.Id:000000}",
                ThoiGian = i.CreatedAt.ToString("dd/MM/yyyy HH:mm"),
                NhanVien = i.StaffName,
                KhachHang = i.CustomerName,
                SDT = i.CustomerPhone,
                ThanhToan = i.PaymentMethod,
                Ve = i.TicketCount,
                BapNuoc = i.SnackCount,
                GiamGia = i.DiscountAmount.ToString("N0"),
                TongTien = i.TotalAmount.ToString("N0") + " đ"
            })
            .ToList();

        AdminControls.HideColumn(dgvInvoices, "Id");
        AdminControls.SetColumnWidths(dgvInvoices,
            ("MaHD", 98),
            ("ThoiGian", 140),
            ("NhanVien", 150),
            ("KhachHang", 170),
            ("SDT", 112),
            ("ThanhToan", 100),
            ("Ve", 60),
            ("BapNuoc", 78),
            ("GiamGia", 100),
            ("TongTien", 120));

        var total = _rows.Sum(i => i.TotalAmount);
        lblSummary.Text = $"  {_rows.Count:N0} hóa đơn | Tổng doanh thu: {total:N0} đ";
    }

    private async Task LoadSelectedDetailAsync()
    {
        var id = AdminControls.GetCurrentIntValue(dgvInvoices, "Id");
        if (!id.HasValue)
        {
            ClearDetail();
            return;
        }

        try
        {
            using var ctx = Program.CreateDbContext();
            var invoice = await ctx.Invoices
                .AsNoTracking()
                .Include(i => i.User)
                .Include(i => i.Customer)
                .Include(i => i.Tickets)
                    .ThenInclude(t => t.Seat)
                .Include(i => i.Tickets)
                    .ThenInclude(t => t.Showtime)
                    .ThenInclude(s => s.Movie)
                .Include(i => i.Tickets)
                    .ThenInclude(t => t.Showtime)
                    .ThenInclude(s => s.Room)
                .Include(i => i.InvoiceSnacks)
                    .ThenInclude(s => s.Snack)
                .FirstOrDefaultAsync(i => i.Id == id.Value);

            if (invoice == null)
            {
                ClearDetail();
                return;
            }

            if (!_adminMode && invoice.UserId != SessionManager.CurrentUser?.Id)
            {
                ClearDetail();
                return;
            }

            var customerName = invoice.Customer?.FullName ?? invoice.CustomerName ?? "Khách lẻ";
            var customerPhone = invoice.Customer?.Phone ?? invoice.CustomerPhone ?? "";
            lblDetailTitle.Text = $"HD{invoice.Id:000000} - {invoice.TotalAmount:N0} đ";
            lblDetailInfo.Text =
                $"Thời gian: {invoice.CreatedAt:dd/MM/yyyy HH:mm}\n" +
                $"Nhân viên: {invoice.User.FullName}\n" +
                $"Khách hàng: {customerName}" + (string.IsNullOrWhiteSpace(customerPhone) ? "" : $" - {customerPhone}") + "\n" +
                $"Thanh toán: {invoice.PaymentMethod} | Nhận: {invoice.ReceivedAmount:N0} đ | Thối lại: {invoice.ChangeAmount:N0} đ\n" +
                $"Giảm giá: {invoice.DiscountAmount:N0} đ";

            var firstTicket = invoice.Tickets
                .OrderBy(t => t.Showtime.StartTime)
                .ThenBy(t => t.Seat.RowLabel)
                .ThenBy(t => t.Seat.SeatNumber)
                .FirstOrDefault();

            if (firstTicket == null)
            {
                seatPreview.ClearPreview("Hoa don khong co ve");
            }
            else
            {
                var sameShowtimeTickets = invoice.Tickets
                    .Where(t => t.ShowtimeId == firstTicket.ShowtimeId)
                    .ToList();

                var roomSeats = await ctx.Seats
                    .AsNoTracking()
                    .Where(s => s.RoomId == firstTicket.Showtime.RoomId)
                    .OrderBy(s => s.GridRow)
                    .ThenBy(s => s.GridColumn)
                    .ToListAsync();

                var extraShowtimeCount = invoice.Tickets
                    .Select(t => t.ShowtimeId)
                    .Distinct()
                    .Count() - 1;
                var extraText = extraShowtimeCount > 0 ? $" (+{extraShowtimeCount} suat khac)" : "";

                seatPreview.SetSeats(roomSeats, $"{firstTicket.Showtime.Room.Name} - {firstTicket.Showtime.StartTime:dd/MM HH:mm}{extraText}");
                seatPreview.SetHighlightedSeats(sameShowtimeTickets.Select(t => t.SeatId));
            }

            dgvTickets.DataSource = invoice.Tickets
                .OrderBy(t => t.Showtime.StartTime)
                .ThenBy(t => t.Seat.RowLabel)
                .ThenBy(t => t.Seat.SeatNumber)
                .Select(t => new
                {
                    Phim = t.Showtime.Movie.Title,
                    Phong = t.Showtime.Room.Name,
                    Suat = t.Showtime.StartTime.ToString("dd/MM HH:mm"),
                    Ghe = t.Seat.Label,
                    Gia = t.Price.ToString("N0") + " đ"
                })
                .ToList();

            dgvSnacks.DataSource = invoice.InvoiceSnacks
                .OrderBy(s => s.Snack.Name)
                .Select(s => new
                {
                    Mon = s.Snack.Name,
                    SL = s.Quantity,
                    DonGia = s.UnitPrice.ToString("N0") + " đ",
                    ThanhTien = (s.UnitPrice * s.Quantity).ToString("N0") + " đ"
                })
                .ToList();
        }
        catch (Exception ex)
        {
            lblDetailInfo.Text = $"Lỗi tải chi tiết: {ex.Message}";
        }
    }

    private void ClearDetail()
    {
        lblDetailTitle.Text = "Chọn hóa đơn";
        lblDetailInfo.Text = "Chi tiết vé và bắp nước sẽ hiển thị tại đây.";
        seatPreview.ClearPreview("Chon hoa don de xem ghe");
        dgvTickets.DataSource = null;
        dgvSnacks.DataSource = null;
    }

    private void ApplyInvoiceLayout()
    {
        if (split == null || split.ClientSize.Width <= 80 || split.ClientSize.Height <= 80)
            return;

        split.Panel1MinSize = 25;
        split.Panel2MinSize = 25;

        if (ClientSize.Width < 1180)
        {
            if (split.Orientation != Orientation.Horizontal)
                split.Orientation = Orientation.Horizontal;

            var maxDistance = split.ClientSize.Height - split.SplitterWidth - 210;
            if (maxDistance <= 240)
                return;

            split.SplitterDistance = Math.Clamp(330, 240, maxDistance);
            split.Panel1MinSize = 220;
            split.Panel2MinSize = 190;
            return;
        }

        if (split.Orientation != Orientation.Vertical)
            split.Orientation = Orientation.Vertical;

        const int leftMin = 560;
        const int rightMin = 500;
        const int preferredRightWidth = 560;
        var maxVerticalDistance = split.ClientSize.Width - split.SplitterWidth - rightMin;
        if (maxVerticalDistance < leftMin)
            return;

        var preferredLeftWidth = split.ClientSize.Width - split.SplitterWidth - preferredRightWidth;
        split.SplitterDistance = Math.Clamp(preferredLeftWidth, leftMin, maxVerticalDistance);
        split.Panel1MinSize = leftMin;
        split.Panel2MinSize = rightMin;
    }

    private record InvoiceRow(
        int Id,
        DateTime CreatedAt,
        string StaffName,
        string CustomerName,
        string CustomerPhone,
        string PaymentMethod,
        int TicketCount,
        int SnackCount,
        decimal DiscountAmount,
        decimal TotalAmount);
}
