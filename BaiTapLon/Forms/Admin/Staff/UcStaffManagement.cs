using BaiTapLon.Models;
using BaiTapLon.Services;
using BaiTapLon.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Forms.Admin;

public class UcStaffManagement : UserControl
{
    private DataGridView dgv = null!;
    private AdminPaginationBar pagination = null!;
    private TextBox txtSearch = null!;
    private List<User> _users = new();

    public UcStaffManagement()
    {
        InitUI();
        this.Load += async (s, e) => await LoadAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        dgv = AdminControls.CreateGrid();
        dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) BtnEdit_Click(null, EventArgs.Empty); };
        pagination = new AdminPaginationBar();
        pagination.PaginationChanged += (s, e) => BindStaffPage();

        txtSearch = AdminControls.CreateSearchBox("Tìm nhân viên...");
        txtSearch.TextChanged += async (s, e) => await LoadAsync();

        var toolbar = AdminControls.CreateToolbar(
            txtSearch,
            AdminControls.CreateButton("➕ Thêm NV", AdminTheme.ButtonSuccess, 110, BtnAdd_Click),
            AdminControls.CreateButton("✏️ Sửa", AdminTheme.ButtonPrimary, 90, BtnEdit_Click),
            AdminControls.CreateButton("🔑 Reset MK", AdminTheme.ButtonWarning, 120, BtnReset_Click),
            AdminControls.CreateButton("🔄 Khóa/Mở", AdminTheme.ButtonNeutral, 120, BtnToggle_Click),
            AdminControls.CreateButton("📥 Xuất Excel", Color.FromArgb(40, 167, 69), 110, BtnExport_Click)
        );

        Controls.Add(AdminLayouts.CreateManagementPage(
            "👥  Quản Lý Nhân Viên",
            toolbar,
            AdminLayouts.CreatePagedGridContent(dgv, pagination)));
    }

    private async Task LoadAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var query = ctx.Users.AsNoTracking().AsQueryable();

            string kw = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(kw))
            {
                query = query.Where(u => u.FullName.ToLower().Contains(kw) || u.Username.ToLower().Contains(kw) || (u.Phone != null && u.Phone.Contains(kw)));
            }

            _users = await query.OrderBy(u => u.Id).ToListAsync();

            pagination.SetTotalItems(_users.Count, resetPage: true);
            BindStaffPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
        }
    }

    private void BindStaffPage()
    {
        dgv.DataSource = null;
        dgv.Columns.Clear();
        dgv.DataSource = _users
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(u => new
            {
                u.Id,
                HọTên = u.FullName,
                TàiKhoản = u.Username,
                VaiTrò = u.Role,
                SĐT = u.Phone ?? "",
                TrạngThái = u.IsActive ? "✅ Hoạt động" : "❌ Đã khóa",
                NgàyTạo = u.CreatedAt.ToString("dd/MM/yyyy")
            }).ToList();

        AdminControls.HideColumn(dgv, "Id");
        AdminControls.SetColumnWidths(dgv,
            ("HọTên", 220),
            ("TàiKhoản", 170),
            ("VaiTrò", 120),
            ("SĐT", 140),
            ("TrạngThái", 150),
            ("NgàyTạo", 120));
    }

    private async void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new DlgStaffEdit();
        if (dlg.ShowDialog() != DialogResult.OK) return;
        using var ctx = Program.CreateDbContext();
        var auth = new AuthService(ctx);
        var (ok, msg) = await auth.CreateUserAsync(dlg.FullName, dlg.Username, dlg.Password, dlg.Role, dlg.Phone);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadAsync();
    }

    private async void BtnEdit_Click(object? s, EventArgs e)
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (!id.HasValue) return;
        var user = _users.FirstOrDefault(u => u.Id == id.Value);
        if (user == null) return;

        using var dlg = new DlgStaffEdit(user);
        if (dlg.ShowDialog() != DialogResult.OK) return;

        using var ctx = Program.CreateDbContext();
        var auth = new AuthService(ctx);
        var (ok, msg) = await auth.UpdateUserAsync(id.Value, dlg.FullName, dlg.Role, dlg.Phone);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadAsync();
    }
    private async void BtnReset_Click(object? s, EventArgs e)
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (!id.HasValue || dgv.CurrentRow == null) return;

        string name = dgv.CurrentRow.Cells["TàiKhoản"].Value?.ToString() ?? "";

        string newPw = $"{name}123";
        if (MessageBox.Show($"Reset mật khẩu của \"{name}\" thành \"{newPw}\"?", "Xác nhận",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        using var ctx = Program.CreateDbContext();
        var user = await ctx.Users.FindAsync(id.Value);
        if (user == null) return;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPw);
        await ctx.SaveChangesAsync();
        MessageBox.Show($"Đã reset mật khẩu thành: {newPw}", "Thành công");
    }

    private async void BtnToggle_Click(object? s, EventArgs e)
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (!id.HasValue) return;

        if (id.Value == SessionManager.CurrentUser?.Id)
        {
            MessageBox.Show("Không thể khóa tài khoản của chính bạn!", "Cảnh báo");
            return;
        }

        using var ctx = Program.CreateDbContext();
        var user = await ctx.Users.FindAsync(id.Value);
        if (user == null) return;
        user.IsActive = !user.IsActive;
        await ctx.SaveChangesAsync();
        await LoadAsync();
    }

    private void BtnExport_Click(object? s, EventArgs e)
    {
        ExcelHelper.ExportDataGridViewToExcel(dgv, "NhanVien", "Danh Sách Nhân Viên");
    }

}

// ==================== DlgStaffEdit — TableLayoutPanel ====================

public class DlgStaffEdit : Form
{
    private TextBox txtName = null!, txtUser = null!, txtPw = null!, txtPhone = null!;
    private ComboBox cboRole = null!;
    private ErrorProvider errorProvider = null!;
    public string FullName => txtName.Text.Trim();
    public string Username => txtUser.Text.Trim();
    public string Password => txtPw.Text;
    public string Role => cboRole.SelectedItem?.ToString() ?? "Staff";
    public string Phone => txtPhone.Text.Trim();

    private User? _existingUser;

    public DlgStaffEdit(User? existingUser = null)
    {
        _existingUser = existingUser;
        this.Text = _existingUser == null ? "Thêm nhân viên" : "Sửa nhân viên";
        this.ClientSize = new Size(440, 340);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(24, 24, 40);
        this.ForeColor = Color.FromArgb(200, 200, 220);
        this.Padding = new Padding(15);

        errorProvider = new ErrorProvider
        {
            ContainerControl = this,
            BlinkStyle = ErrorBlinkStyle.NeverBlink
        };

        // === TableLayoutPanel (CSS Grid style) ===
        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 6,
            BackColor = Color.Transparent
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
        for (int i = 0; i < 6; i++)
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        int row = 0;
        AddFormRow(tbl, "Họ tên *", row++, out txtName);
        AddFormRow(tbl, "Tên đăng nhập *", row++, out txtUser);
        AddFormRow(tbl, "Mật khẩu *", row++, out txtPw);
        AddFormRow(tbl, "Số điện thoại", row++, out txtPhone);

        // Vai trò
        tbl.Controls.Add(MakeLabel("Vai trò"), 0, row);
        cboRole = new ComboBox
        {
            Font = new Font("Segoe UI", 11),
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 6, 0, 6)
        };
        cboRole.Items.AddRange(new[] { "Staff", "Admin" });
        cboRole.SelectedIndex = 0;
        tbl.Controls.Add(cboRole, 1, row);
        row++;

        // Edit Mode handling
        if (_existingUser != null)
        {
            txtName.Text = _existingUser.FullName;
            txtUser.Text = _existingUser.Username;
            txtUser.Enabled = false;
            txtPw.Text = "********";
            txtPw.Enabled = false;
            txtPhone.Text = _existingUser.Phone;
            cboRole.SelectedItem = _existingUser.Role;
        }

        // Buttons
        var flpBtns = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        var btnCancel = new Button
        {
            Text = "Hủy", Font = new Font("Segoe UI", 10),
            Size = new Size(90, 36),
            BackColor = Color.FromArgb(50, 50, 75), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel,
            Margin = new Padding(0, 4, 0, 0)
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        var btnOk = new Button
        {
            Text = "✅ Tạo", Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Size = new Size(100, 36),
            BackColor = Color.FromArgb(80, 160, 80), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK,
            Margin = new Padding(0, 4, 8, 0)
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) =>
        {
            errorProvider.Clear();
            if (string.IsNullOrWhiteSpace(txtName.Text) ||
                (_existingUser == null && string.IsNullOrWhiteSpace(txtUser.Text)) ||
                (_existingUser == null && string.IsNullOrWhiteSpace(txtPw.Text)))
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                    errorProvider.SetError(txtName, "Nhập họ tên.");
                if (_existingUser == null && string.IsNullOrWhiteSpace(txtUser.Text))
                    errorProvider.SetError(txtUser, "Nhập tên đăng nhập.");
                if (_existingUser == null && string.IsNullOrWhiteSpace(txtPw.Text))
                    errorProvider.SetError(txtPw, "Nhập mật khẩu.");

                MessageBox.Show("Điền đầy đủ thông tin bắt buộc!", "Thiếu thông tin");
                this.DialogResult = DialogResult.None;
                return;
            }
        };
        flpBtns.Controls.Add(btnCancel);
        flpBtns.Controls.Add(btnOk);
        tbl.Controls.Add(flpBtns, 0, row);
        tbl.SetColumnSpan(flpBtns, 2);

        this.Controls.Add(tbl);
        this.AcceptButton = btnOk;
        this.CancelButton = btnCancel;
    }

    private void AddFormRow(TableLayoutPanel tbl, string label, int row, out TextBox txt)
    {
        tbl.Controls.Add(MakeLabel(label), 0, row);
        txt = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 6, 0, 6)
        };
        tbl.Controls.Add(txt, 1, row);
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 10),
        ForeColor = Color.FromArgb(160, 160, 185),
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };
}
