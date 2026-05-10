using BaiTapLon.Services;
using BaiTapLon.Models;

namespace BaiTapLon.Forms.Admin.Coupons;

public class UcCouponManagement : UserControl
{
    private DataGridView dgv = null!;
    private AdminPaginationBar pagination = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboStatus = null!;
    private List<Coupon> _coupons = new();

    public UcCouponManagement()
    {
        InitUI();
        this.Load += async (s, e) => await LoadAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        txtSearch = AdminControls.CreateSearchBox("Tìm mã coupon...", 270);
        txtSearch.TextChanged += async (s, e) => await LoadAsync();

        cboStatus = AdminControls.CreateComboBox(160);
        cboStatus.Items.AddRange(new object[] { "Tất cả", "Đang hoạt động", "Đã tắt" });
        cboStatus.SelectedIndex = 0;
        cboStatus.SelectedIndexChanged += async (s, e) => await LoadAsync();

        var btnCreate = AdminControls.CreateButton("➕ Tạo mới", AdminTheme.ButtonSuccess, 120, BtnCreate_Click);

        var toolbar = AdminControls.CreateToolbar(txtSearch, cboStatus, btnCreate);

        dgv = AdminControls.CreateGrid();
        dgv.CellDoubleClick += DgvCellDoubleClick;

        // Right-click context menu
        var ctxMenu = new ContextMenuStrip();
        var menuViewRedemptions = new ToolStripMenuItem("👁 Xem lượt đổi");
        menuViewRedemptions.Click += MenuViewRedemptions_Click;
        ctxMenu.Items.Add(menuViewRedemptions);
        dgv.ContextMenuStrip = ctxMenu;

        pagination = new AdminPaginationBar();
        pagination.SetTotalItems(0, resetPage: true);
        // Override default page size to 15
        pagination.PaginationChanged += (s, e) => BindPage();

        var content = AdminLayouts.CreatePagedGridContent(dgv, pagination);
        var page = AdminLayouts.CreateManagementPage("🎟️  Quản Lý Coupon", toolbar, content);
        Controls.Add(page);
    }

    private async Task LoadAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var svc = new CouponService(ctx);

            string keyword = txtSearch.Text.Trim();
            bool? activeFilter = cboStatus.SelectedIndex switch
            {
                1 => true,
                2 => false,
                _ => null
            };

            _coupons = await svc.GetAllAsync(activeFilter, string.IsNullOrWhiteSpace(keyword) ? null : keyword);
            pagination.SetTotalItems(_coupons.Count, resetPage: true);
            BindPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindPage()
    {
        dgv.DataSource = null;
        dgv.Columns.Clear();
        dgv.DataSource = _coupons
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(c => new
            {
                c.Id,
                Mã = c.Code,
                Điểm = c.PointsAwarded,
                HếtHạn = c.ExpirationDate.ToString("dd/MM/yyyy"),
                TốiĐa = c.MaxRedemptions,
                ĐãDùng = c.UsedCount,
                TrạngThái = c.IsActive ? "✅ Hoạt động" : "❌ Tắt"
            }).ToList();

        AdminControls.HideColumn(dgv, "Id");
        AdminControls.SetColumnWidths(dgv,
            ("Mã", 160),
            ("Điểm", 100),
            ("HếtHạn", 130),
            ("TốiĐa", 100),
            ("ĐãDùng", 100),
            ("TrạngThái", 140));
    }

    private void DgvCellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        OpenEditDialog();
    }

    private void BtnCreate_Click(object? sender, EventArgs e)
    {
        using var dlg = new DlgCouponEdit(null);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _ = LoadAsync();
    }

    private void OpenEditDialog()
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (!id.HasValue) return;

        var coupon = _coupons.FirstOrDefault(c => c.Id == id.Value);
        if (coupon == null) return;

        using var dlg = new DlgCouponEdit(coupon);
        if (dlg.ShowDialog(this) == DialogResult.OK)
            _ = LoadAsync();
    }

    private void MenuViewRedemptions_Click(object? sender, EventArgs e)
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (!id.HasValue || dgv.CurrentRow == null) return;

        string code = dgv.CurrentRow.Cells["Mã"].Value?.ToString() ?? "";
        using var dlg = new DlgCouponRedemptions(id.Value, code);
        dlg.ShowDialog(this);
    }
}
