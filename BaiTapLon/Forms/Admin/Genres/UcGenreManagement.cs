using BaiTapLon.Services;
using BaiTapLon.Models;

namespace BaiTapLon.Forms.Admin;

public class UcGenreManagement : UserControl
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private List<Genre> _genres = new();

    public UcGenreManagement()
    {
        InitUI();
        this.Load += async (s, e) => await LoadAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        dgv = AdminControls.CreateGrid();
        dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnEdit_Click(null, EventArgs.Empty); };

        txtSearch = AdminControls.CreateSearchBox("Tìm tên thể loại...");
        txtSearch.TextChanged += (s, e) => FilterData();

        var toolbar = AdminControls.CreateToolbar(
            txtSearch,
            AdminControls.CreateButton("➕ Thêm thể loại", AdminTheme.ButtonSuccess, 150, BtnAdd_Click),
            AdminControls.CreateButton("✏️ Sửa", AdminTheme.ButtonPrimary, 100, BtnEdit_Click),
            AdminControls.CreateButton("🗑️ Xóa", AdminTheme.ButtonDanger, 100, BtnDelete_Click),
            AdminControls.CreateButton("📥 Xuất Excel", Color.FromArgb(40, 167, 69), 110, BtnExport_Click)
        );

        var content = new Panel { Dock = DockStyle.Fill };
        dgv.Dock = DockStyle.Fill;
        content.Controls.Add(dgv);

        Controls.Add(AdminLayouts.CreateManagementPage("🏷️  Quản Lý Thể Loại Phim", toolbar, content));
    }

    private async Task LoadAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            _genres = await new GenreService(ctx).GetAllAsync();
            FilterData();
        }
        catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi"); }
    }

    private void FilterData()
    {
        string kw = txtSearch.Text.Trim().ToLower();
        var filtered = string.IsNullOrWhiteSpace(kw) ? _genres : _genres.Where(g => g.Name.ToLower().Contains(kw)).ToList();

        dgv.DataSource = null;
        dgv.Columns.Clear();
        dgv.DataSource = filtered.Select(g => new
        {
            g.Id,
            TêThểLoại = g.Name,
            SốPhim = g.MovieGenres?.Count ?? 0
        }).ToList();
        AdminControls.HideColumn(dgv, "Id");
        AdminControls.SetColumnWidths(dgv, ("TêThểLoại", 300), ("SốPhim", 120));
    }

    private async void BtnAdd_Click(object? s, EventArgs e)
    {
        string? name = ShowInputDialog("Thêm thể loại mới", "Tên thể loại:", "");
        if (name == null) return;
        using var ctx = Program.CreateDbContext();
        var (ok, msg) = await new GenreService(ctx).CreateAsync(name);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadAsync();
    }

    private async void BtnEdit_Click(object? s, EventArgs e)
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (!id.HasValue) return;
        var genre = _genres.FirstOrDefault(g => g.Id == id.Value);
        if (genre == null) return;

        string? newName = ShowInputDialog("Sửa thể loại", "Tên thể loại:", genre.Name);
        if (newName == null) return;
        using var ctx = Program.CreateDbContext();
        var (ok, msg) = await new GenreService(ctx).UpdateAsync(id.Value, newName);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadAsync();
    }

    private async void BtnDelete_Click(object? s, EventArgs e)
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (!id.HasValue) return;
        if (MessageBox.Show("Xóa thể loại này?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        using var ctx = Program.CreateDbContext();
        var (ok, msg) = await new GenreService(ctx).DeleteAsync(id.Value);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadAsync();
    }

    private void BtnExport_Click(object? s, EventArgs e)
    {
        BaiTapLon.Helpers.ExcelHelper.ExportDataGridViewToExcel(dgv, "TheLoai", "Danh Sách Thể Loại Phim");
    }

    public static string? ShowInputDialog(string title, string prompt, string defaultVal)
    {
        var dlg = new Form
        {
            Text = title, ClientSize = new Size(380, 160),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            MaximizeBox = false, MinimizeBox = false,
            BackColor = AdminTheme.PageBack, ForeColor = AdminTheme.Text
        };

        var lbl = new Label { Text = prompt, Location = new Point(20, 20), AutoSize = true, Font = AdminTheme.BodyFont, ForeColor = AdminTheme.MutedText };
        var txt = new TextBox
        {
            Text = defaultVal, Location = new Point(20, 48), Size = new Size(340, 30),
            Font = AdminTheme.BodyFont, BackColor = AdminTheme.InputBack,
            ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle
        };
        var btnOk = AdminControls.CreateButton("✅ Lưu", AdminTheme.ButtonSuccess, 90, null!);
        btnOk.Location = new Point(180, 100); btnOk.Height = 36;
        btnOk.Click += (s, e) => { dlg.DialogResult = DialogResult.OK; dlg.Close(); };

        var btnCancel = AdminControls.CreateButton("Hủy", AdminTheme.ButtonNeutral, 80, null!);
        btnCancel.Location = new Point(280, 100); btnCancel.Height = 36;
        btnCancel.Click += (s, e) => { dlg.DialogResult = DialogResult.Cancel; dlg.Close(); };

        dlg.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
        dlg.AcceptButton = btnOk; dlg.CancelButton = btnCancel;

        return dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(txt.Text)
            ? txt.Text.Trim() : null;
    }
}
