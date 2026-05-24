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

        dgv = AdminControls.CreateGrid();
        pagination = new AdminPaginationBar();
        pagination.PaginationChanged += (s, e) => BindPage();

        var toolbar = AdminControls.CreateToolbar(
            AdminControls.CreateToolbarLabel("Ngày:", 45), dtpDate,
            AdminControls.CreateToolbarLabel("Trạng thái:", 80), cboStatus,
            txtSearch,
            AdminControls.CreateButton("✅ Check-In", Color.FromArgb(65, 196, 126), 120, BtnCheckIn_Click),
            AdminControls.CreateButton("❌ Hủy booking", AdminTheme.ButtonDanger, 130, BtnCancel_Click));

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
            ("MãĐặt", 110), ("KháchHàng", 150), ("SĐT", 110),
            ("Phim", 180), ("SuấtChiếu", 140), ("SốVé", 60),
            ("TổngTiền", 110), ("Giảm", 90), ("TT", 70),
            ("TrạngThái", 100), ("NgàyĐặt", 110));
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
}
