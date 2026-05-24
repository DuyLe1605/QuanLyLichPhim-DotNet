using BaiTapLon.Helpers;
using BaiTapLon.Services;
using BaiTapLon.Models;

namespace BaiTapLon.Forms.Admin;

public class UcShiftManagement : UserControl
{
    private DataGridView dgv = null!;
    private AdminPaginationBar pagination = null!;
    private DateTimePicker dtpDate = null!;
    private ComboBox cboStaff = null!;
    private List<Shift> _shifts = new();
    private List<User> _users = new();
    private bool _isLoading;

    public UcShiftManagement()
    {
        InitUI();
        this.Load += async (s, e) => await LoadFiltersAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        dtpDate = AdminControls.CreateDatePicker();
        dtpDate.ValueChanged += async (s, e) => { if (!_isLoading) await LoadAsync(); };

        cboStaff = AdminControls.CreateComboBox(180);
        cboStaff.SelectedIndexChanged += async (s, e) => { if (!_isLoading) await LoadAsync(); };

        dgv = AdminControls.CreateGrid();
        pagination = new AdminPaginationBar();
        pagination.PaginationChanged += (s, e) => BindPage();

        var toolbar = AdminControls.CreateToolbar(
            AdminControls.CreateToolbarLabel("Ngày:", 45), dtpDate,
            AdminControls.CreateToolbarLabel("NV:", 30), cboStaff,
            AdminControls.CreateButton("🔄 Tải lại", AdminTheme.ButtonPrimary, 110, async (s, e) => await LoadAsync()));

        Controls.Add(AdminLayouts.CreateManagementPage(
            "⏰  Quản Lý Ca Làm Việc",
            toolbar,
            AdminLayouts.CreatePagedGridContent(dgv, pagination)));
    }

    private async Task LoadFiltersAsync()
    {
        _isLoading = true;
        try
        {
            using var ctx = Program.CreateDbContext();
            _users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .ToListAsync(ctx.Users.Where(u => u.IsActive).OrderBy(u => u.FullName));

            cboStaff.Items.Clear();
            cboStaff.Items.Add("-- Tất cả --");
            foreach (var u in _users) cboStaff.Items.Add(u.FullName);
            cboStaff.SelectedIndex = 0;
        }
        finally { _isLoading = false; }
        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            int? userId = cboStaff.SelectedIndex > 0 ? _users[cboStaff.SelectedIndex - 1].Id : null;
            _shifts = await new ShiftService(ctx).GetAllAsync(dtpDate.Value.Date, userId);
            pagination.SetTotalItems(_shifts.Count, resetPage: true);
            BindPage();
        }
        catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi"); }
    }

    private void BindPage()
    {
        dgv.DataSource = null;
        dgv.Columns.Clear();
        dgv.DataSource = _shifts
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(s => new
            {
                s.Id,
                NhânViên = s.User.FullName,
                MởCa = s.OpenedAt.ToString("HH:mm dd/MM"),
                ĐóngCa = s.ClosedAt?.ToString("HH:mm dd/MM") ?? "—",
                TiềnĐầuCa = $"{s.OpeningCash:N0} đ",
                TiềnCuốiCa = s.ClosingCash.HasValue ? $"{s.ClosingCash:N0} đ" : "—",
                KỳVọng = s.ExpectedCash.HasValue ? $"{s.ExpectedCash:N0} đ" : "—",
                ChênhLệch = s.CashDifference.HasValue ? $"{s.CashDifference:N0} đ" : "—",
                TrạngThái = s.Status == "Open" ? "🟢 Đang mở" : "🔴 Đã đóng"
            }).ToList();

        AdminControls.HideColumn(dgv, "Id");
        AdminControls.SetColumnWidths(dgv,
            ("NhânViên", 160), ("MởCa", 130), ("ĐóngCa", 130),
            ("TiềnĐầuCa", 120), ("TiềnCuốiCa", 120),
            ("KỳVọng", 120), ("ChênhLệch", 110), ("TrạngThái", 110));
    }
}
