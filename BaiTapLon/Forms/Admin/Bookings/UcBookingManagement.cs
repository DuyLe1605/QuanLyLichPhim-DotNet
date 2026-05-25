using BaiTapLon.Services;
using BaiTapLon.Models;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Forms.Admin;

public class UcBookingManagement : UserControl
{
    private DataGridView dgv = null!;
    private AdminPaginationBar pagination = null!;
    private ComboBox cboStatus = null!;
    private TextBox txtSearch = null!;
    private TextBox txtQRScan = null!;
    private DateTimePicker dtpDate = null!;
    private List<Booking> _bookings = new();

    public UcBookingManagement()
    {
        InitUI();
        this.Load += async (s, e) => await LoadAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        dtpDate = AdminControls.CreateDatePicker();
        dtpDate.ValueChanged += async (s, e) => await LoadAsync();

        cboStatus = AdminControls.CreateComboBox(150);
        cboStatus.Items.AddRange(new object[] { "Tất cả", "Pending", "Paid", "CheckedIn", "Cancelled" });
        cboStatus.SelectedIndex = 0;
        cboStatus.SelectedIndexChanged += async (s, e) => await LoadAsync();

        txtSearch = AdminControls.CreateSearchBox("Tìm mã booking, KH...", 200);
        txtSearch.TextChanged += async (s, e) => await LoadAsync();

        txtQRScan = AdminControls.CreateSearchBox("📷 Quét QR Code vào đây...", 220);
        txtQRScan.BackColor = Color.FromArgb(40, 40, 60);
        txtQRScan.KeyDown += TxtQRScan_KeyDown;

        dgv = AdminControls.CreateGrid();
        pagination = new AdminPaginationBar();
        pagination.PaginationChanged += (s, e) => BindPage();

        var toolbar = AdminControls.CreateToolbar(
            AdminControls.CreateToolbarLabel("Ngày:", 45), dtpDate,
            AdminControls.CreateToolbarLabel("Trạng thái:", 80), cboStatus,
            txtSearch,
            txtQRScan,
            AdminControls.CreateButton("✅ Check-In", Color.FromArgb(65, 196, 126), 120, BtnCheckIn_Click),
            AdminControls.CreateButton("❌ Hủy", AdminTheme.ButtonDanger, 80, BtnCancel_Click));

        Controls.Add(AdminLayouts.CreateManagementPage(
            "📦  Quản Lý Đặt Vé Online",
            toolbar,
            AdminLayouts.CreatePagedGridContent(dgv, pagination)));
    }

    private async Task LoadAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var query = ctx.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Showtime).ThenInclude(s => s.Movie)
                .Include(b => b.Showtime).ThenInclude(s => s.Room)
                .Include(b => b.Tickets)
                .AsNoTracking()
                .AsQueryable();

            // Filter by date
            var date = dtpDate.Value.Date;
            query = query.Where(b => b.Showtime.StartTime.Date == date);

            // Filter by status
            if (cboStatus.SelectedIndex > 0)
            {
                string status = cboStatus.SelectedItem?.ToString() ?? "";
                query = query.Where(b => b.Status == status);
            }

            // Search
            string kw = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(kw))
                query = query.Where(b =>
                    b.BookingCode.ToLower().Contains(kw) ||
                    b.Customer.FullName.ToLower().Contains(kw) ||
                    b.Customer.Phone.Contains(kw));

            _bookings = await query.OrderByDescending(b => b.CreatedAt).ToListAsync();
            pagination.SetTotalItems(_bookings.Count, resetPage: true);
            BindPage();
        }
        catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi"); }
    }

    private void BindPage()
    {
        dgv.DataSource = null;
        dgv.Columns.Clear();
        dgv.DataSource = _bookings
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(b => new
            {
                b.Id,
                MãĐặt = b.BookingCode,
                KháchHàng = b.Customer.FullName,
                SĐT = b.Customer.Phone,
                Phim = b.Showtime.Movie.Title,
                SuấtChiếu = $"{b.Showtime.StartTime:HH:mm} - {b.Showtime.Room.Name}",
                SốVé = b.Tickets.Count,
                TổngTiền = $"{b.TotalAmount:N0} đ",
                Giảm = b.DiscountAmount > 0 ? $"{b.DiscountAmount:N0} đ" : "—",
                TT = b.PaymentMethod,
                TrạngThái = b.Status switch
                {
                    "Pending" => "🟡 Chờ",
                    "Paid" => "🔵 Đã TT",
                    "CheckedIn" => "🟢 Check-In",
                    "Cancelled" => "🔴 Đã hủy",
                    _ => b.Status
                },
                NgàyĐặt = b.CreatedAt.ToString("HH:mm dd/MM")
            }).ToList();

        AdminControls.HideColumn(dgv, "Id");
        AdminControls.SetColumnWidths(dgv,
            ("MãĐặt", 110), ("KháchHàng", 170), ("SĐT", 120),
            ("Phim", 200), ("SuấtChiếu", 150), ("SốVé", 70),
            ("TổngTiền", 120), ("Giảm", 90), ("TT", 70),
            ("TrạngThái", 140), ("NgàyĐặt", 120));

        dgv.Columns["MãĐặt"]!.HeaderText = "Mã Đặt";
        dgv.Columns["KháchHàng"]!.HeaderText = "Khách Hàng";
        dgv.Columns["SuấtChiếu"]!.HeaderText = "Suất Chiếu";
        dgv.Columns["SốVé"]!.HeaderText = "Số Vé";
        dgv.Columns["TổngTiền"]!.HeaderText = "Tổng Tiền";
        dgv.Columns["TrạngThái"]!.HeaderText = "Trạng Thái";
        dgv.Columns["NgàyĐặt"]!.HeaderText = "Ngày Đặt";

        ConfigureBookingGridColumns();
    }

    private void ConfigureBookingGridColumns()
    {
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;

        SetFill("MãĐặt", 105, 9);
        SetFill("KháchHàng", 145, 13);
        SetFill("SĐT", 110, 9);
        SetFill("Phim", 175, 16);
        SetFill("SuấtChiếu", 160, 14);
        SetFill("SốVé", 64, 5);
        SetFill("TổngTiền", 112, 9);
        SetFill("Giảm", 86, 7);
        SetFill("TT", 58, 5);
        SetFill("TrạngThái", 135, 12);
        SetFill("NgàyĐặt", 112, 9);

        AlignRight("TổngTiền");
        AlignRight("Giảm");
        AlignCenter("SốVé");
        AlignCenter("TT");
        AlignCenter("TrạngThái");
    }

    private void SetFill(string columnName, int minWidth, float fillWeight)
    {
        if (!dgv.Columns.Contains(columnName)) return;
        var column = dgv.Columns[columnName]!;
        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        column.MinimumWidth = minWidth;
        column.FillWeight = fillWeight;
    }

    private void AlignRight(string columnName)
    {
        if (dgv.Columns.Contains(columnName))
            dgv.Columns[columnName]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
    }

    private void AlignCenter(string columnName)
    {
        if (dgv.Columns.Contains(columnName))
            dgv.Columns[columnName]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
    }

    private async void BtnCheckIn_Click(object? s, EventArgs e)
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (!id.HasValue) return;

        var booking = _bookings.FirstOrDefault(b => b.Id == id.Value);
        if (booking == null) return;

        if (booking.Status != "Paid")
        {
            MessageBox.Show("Chỉ có thể check-in booking đã thanh toán (Paid)!", "Cảnh báo");
            return;
        }

        if (MessageBox.Show($"Check-in booking {booking.BookingCode}?", "Xác nhận",
            MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        try
        {
            using var ctx = Program.CreateDbContext();
            var entity = await ctx.Bookings.FindAsync(id.Value);
            if (entity != null)
            {
                entity.Status = "CheckedIn";
                await ctx.SaveChangesAsync();
                MessageBox.Show("Check-in thành công!", "Thành công");
                await LoadAsync();
            }
        }
        catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi"); }
    }

    private async void BtnCancel_Click(object? s, EventArgs e)
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (!id.HasValue) return;

        var booking = _bookings.FirstOrDefault(b => b.Id == id.Value);
        if (booking == null) return;

        if (booking.Status == "Cancelled")
        {
            MessageBox.Show("Booking đã bị hủy rồi!", "Cảnh báo");
            return;
        }

        if (booking.Status == "CheckedIn")
        {
            MessageBox.Show("Không thể hủy booking đã check-in!", "Cảnh báo");
            return;
        }

        if (MessageBox.Show($"Hủy booking {booking.BookingCode}?\nHành động này không thể hoàn tác!",
            "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        try
        {
            using var ctx = Program.CreateDbContext();
            var entity = await ctx.Bookings.FindAsync(id.Value);
            if (entity != null)
            {
                entity.Status = "Cancelled";
                await ctx.SaveChangesAsync();
                MessageBox.Show("Đã hủy booking!", "Thành công");
                await LoadAsync();
            }
        }
        catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi"); }
    }

    private async void TxtQRScan_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            string code = txtQRScan.Text.Trim();
            txtQRScan.Clear();

            if (string.IsNullOrWhiteSpace(code)) return;

            using var ctx = Program.CreateDbContext();
            var booking = await ctx.Bookings.FirstOrDefaultAsync(b => b.BookingCode == code);
            if (booking == null)
            {
                MessageBox.Show($"Không tìm thấy booking với mã: {code}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (booking.Status == "CheckedIn")
            {
                MessageBox.Show($"Booking {code} ĐÃ ĐƯỢC CHECK-IN TRƯỚC ĐÓ!", "Cảnh báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (booking.Status != "Paid")
            {
                MessageBox.Show($"Booking {code} chưa được thanh toán (Trạng thái: {booking.Status})!", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            booking.Status = "CheckedIn";
            await ctx.SaveChangesAsync();
            MessageBox.Show($"✅ Check-in thành công cho booking {code}!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadAsync();
        }
    }
}
