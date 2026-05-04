using BaiTapLon.Models;
using BaiTapLon.Services;

namespace BaiTapLon.Forms.Admin;

public class UcSnackManagement : UserControl
{
    private DataGridView dgvSnacks = null!;
    private AdminPaginationBar pagination = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboCategory = null!;
    private List<Snack> _snacks = new();
    private bool _isLoading;

    public UcSnackManagement()
    {
        InitializeComponent();
        Load += async (s, e) => await LoadDataAsync();
    }

    private void InitializeComponent()
    {
        AdminControls.ConfigurePage(this);

        txtSearch = AdminControls.CreateSearchBox("Tìm tên món...");
        txtSearch.TextChanged += async (s, e) => await LoadDataAsync();

        cboCategory = AdminControls.CreateComboBox(150);
        cboCategory.Items.AddRange(["All", "Food", "Drink", "Combo"]);
        cboCategory.SelectedIndex = 0;
        cboCategory.SelectedIndexChanged += async (s, e) =>
        {
            if (!_isLoading) await LoadDataAsync();
        };

        dgvSnacks = AdminControls.CreateGrid();
        dgvSnacks.DoubleClick += BtnEdit_Click;
        pagination = new AdminPaginationBar();
        pagination.PaginationChanged += (s, e) => BindSnackPage();

        var toolbar = AdminControls.CreateToolbar(
            txtSearch,
            cboCategory,
            AdminControls.CreateButton("↻", AdminTheme.ButtonNeutral, 36, async (s, e) => await LoadDataAsync()),
            AdminControls.CreateButton("+ Thêm", AdminTheme.ButtonSuccess, 110, BtnAdd_Click),
            AdminControls.CreateButton("Sửa", AdminTheme.ButtonPrimary, 95, BtnEdit_Click),
            AdminControls.CreateButton("Ẩn", AdminTheme.ButtonDanger, 85, BtnDelete_Click)
        );

        Controls.Add(AdminLayouts.CreateManagementPage(
            "Quản Lý Bắp Nước",
            toolbar,
            AdminLayouts.CreatePagedGridContent(dgvSnacks, pagination)));
    }

    private async Task LoadDataAsync()
    {
        if (_isLoading) return;
        _isLoading = true;

        try
        {
            using var context = Program.CreateDbContext();
            var service = new SnackService(context);
            var category = cboCategory.SelectedItem?.ToString();
            _snacks = await service.SearchAsync(txtSearch.Text, category, includeInactive: true);
            pagination.SetTotalItems(_snacks.Count, resetPage: true);
            BindSnackPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void BindSnackPage()
    {
        dgvSnacks.DataSource = null;
        dgvSnacks.Columns.Clear();
        dgvSnacks.DataSource = _snacks
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(s => new
            {
                s.Id,
                Loại = GetCategoryDisplay(s.Category),
                TênMón = s.Name,
                Giá = $"{s.Price:N0} đ",
                TrạngThái = s.IsActive ? "Đang bán" : "Đã ẩn"
            }).ToList();

        AdminControls.HideColumn(dgvSnacks, "Id");
        AdminControls.SetColumnWidths(dgvSnacks,
            ("Loại", 150),
            ("TênMón", 260),
            ("Giá", 130),
            ("TrạngThái", 140));
    }

    private async void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var dlg = new DlgSnackEdit(null);
        if (dlg.ShowDialog() != DialogResult.OK) return;

        using var context = Program.CreateDbContext();
        var (ok, msg) = await new SnackService(context).CreateAsync(dlg.SnackData);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadDataAsync();
    }

    private async void BtnEdit_Click(object? sender, EventArgs e)
    {
        var id = GetCurrentSnackId();
        if (!id.HasValue) return;

        using var context = Program.CreateDbContext();
        var service = new SnackService(context);
        var snack = await service.GetByIdAsync(id.Value);
        if (snack == null) return;

        using var dlg = new DlgSnackEdit(snack);
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var (ok, msg) = await service.UpdateAsync(dlg.SnackData);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadDataAsync();
    }

    private async void BtnDelete_Click(object? sender, EventArgs e)
    {
        var id = GetCurrentSnackId();
        if (!id.HasValue) return;

        var name = dgvSnacks.CurrentRow?.Cells["TênMón"].Value?.ToString() ?? "món này";
        if (MessageBox.Show($"Ẩn \"{name}\" khỏi menu bán hàng?", "Xác nhận",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        using var context = Program.CreateDbContext();
        var (ok, msg) = await new SnackService(context).SoftDeleteAsync(id.Value);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadDataAsync();
    }

    private int? GetCurrentSnackId()
    {
        return AdminControls.GetCurrentIntValue(dgvSnacks, "Id");
    }

    private static string GetCategoryDisplay(string category) => category switch
    {
        "Food" => "Đồ ăn",
        "Drink" => "Thức uống",
        "Combo" => "Combo",
        _ => category
    };
}
