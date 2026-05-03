using BaiTapLon.Models;
using BaiTapLon.Services;

namespace BaiTapLon.Forms.Admin;

public class UcSnackManagement : UserControl
{
    private DataGridView dgvSnacks = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboCategory = null!;
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

        var toolbar = AdminControls.CreateToolbar(
            txtSearch,
            cboCategory,
            AdminControls.CreateButton("↻", AdminTheme.ButtonNeutral, 36, async (s, e) => await LoadDataAsync()),
            AdminControls.CreateButton("+ Thêm", AdminTheme.ButtonSuccess, 110, BtnAdd_Click),
            AdminControls.CreateButton("Sửa", AdminTheme.ButtonPrimary, 95, BtnEdit_Click),
            AdminControls.CreateButton("Ẩn", AdminTheme.ButtonDanger, 85, BtnDelete_Click)
        );

        Controls.Add(AdminLayouts.CreateManagementPage("Quản Lý Bắp Nước", toolbar, dgvSnacks));
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
            var snacks = await service.SearchAsync(txtSearch.Text, category, includeInactive: true);

            dgvSnacks.DataSource = null;
            dgvSnacks.Columns.Clear();
            dgvSnacks.DataSource = snacks.Select(s => new
            {
                s.Id,
                Loại = GetCategoryDisplay(s.Category),
                TênMón = s.Name,
                Giá = $"{s.Price:N0} đ",
                TrạngThái = s.IsActive ? "Đang bán" : "Đã ẩn"
            }).ToList();

            if (dgvSnacks.Columns.Contains("Id"))
                dgvSnacks.Columns["Id"].Visible = false;
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
        if (dgvSnacks.CurrentRow == null || !dgvSnacks.Columns.Contains("Id")) return null;
        return dgvSnacks.CurrentRow.Cells["Id"].Value is int id ? id : null;
    }

    private static string GetCategoryDisplay(string category) => category switch
    {
        "Food" => "Đồ ăn",
        "Drink" => "Thức uống",
        "Combo" => "Combo",
        _ => category
    };
}
