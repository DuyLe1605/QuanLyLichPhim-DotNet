using BaiTapLon.Services;
using BaiTapLon.Helpers;

namespace BaiTapLon.Forms.Admin;

public class UcStaffManagement : UserControl
{
    private DataGridView dgv = null!;

    public UcStaffManagement()
    {
        InitUI();
        this.Load += async (s, e) => await LoadAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        dgv = AdminControls.CreateGrid();

        var toolbar = AdminControls.CreateToolbar(
            AdminControls.CreateButton("➕ Thêm NV", AdminTheme.ButtonSuccess, 130, BtnAdd_Click),
            AdminControls.CreateButton("🔑 Reset MK", AdminTheme.ButtonWarning, 130, BtnReset_Click),
            AdminControls.CreateButton("🔄 Khóa/Mở", AdminTheme.ButtonPrimary, 130, BtnToggle_Click)
        );

        Controls.Add(AdminLayouts.CreateManagementPage("👥  Quản Lý Nhân Viên", toolbar, dgv));
    }

    private async Task LoadAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .ToListAsync(ctx.Users.OrderBy(u => u.Id));

            dgv.DataSource = users.Select(u => new
            {
                u.Id,
                HọTên = u.FullName,
                TàiKhoản = u.Username,
                VaiTrò = u.Role,
                SĐT = u.Phone ?? "",
                TrạngThái = u.IsActive ? "✅ Hoạt động" : "❌ Đã khóa",
                NgàyTạo = u.CreatedAt.ToString("dd/MM/yyyy")
            }).ToList();
            if (dgv.Columns.Contains("Id")) dgv.Columns["Id"].Visible = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
        }
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

    private async void BtnReset_Click(object? s, EventArgs e)
    {
        if (dgv.CurrentRow == null) return;
        int id = (int)dgv.CurrentRow.Cells["Id"].Value;
        string name = dgv.CurrentRow.Cells["TàiKhoản"].Value?.ToString() ?? "";

        string newPw = $"{name}123";
        if (MessageBox.Show($"Reset mật khẩu của \"{name}\" thành \"{newPw}\"?", "Xác nhận",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        using var ctx = Program.CreateDbContext();
        var user = await ctx.Users.FindAsync(id);
        if (user == null) return;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPw);
        await ctx.SaveChangesAsync();
        MessageBox.Show($"Đã reset mật khẩu thành: {newPw}", "Thành công");
    }

    private async void BtnToggle_Click(object? s, EventArgs e)
    {
        if (dgv.CurrentRow == null) return;
        int id = (int)dgv.CurrentRow.Cells["Id"].Value;
        if (id == SessionManager.CurrentUser?.Id)
        {
            MessageBox.Show("Không thể khóa tài khoản của chính bạn!", "Cảnh báo");
            return;
        }

        using var ctx = Program.CreateDbContext();
        var user = await ctx.Users.FindAsync(id);
        if (user == null) return;
        user.IsActive = !user.IsActive;
        await ctx.SaveChangesAsync();
        await LoadAsync();
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

    public DlgStaffEdit()
    {
        this.Text = "Thêm nhân viên";
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
                string.IsNullOrWhiteSpace(txtUser.Text) ||
                string.IsNullOrWhiteSpace(txtPw.Text))
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                    errorProvider.SetError(txtName, "Nhập họ tên.");
                if (string.IsNullOrWhiteSpace(txtUser.Text))
                    errorProvider.SetError(txtUser, "Nhập tên đăng nhập.");
                if (string.IsNullOrWhiteSpace(txtPw.Text))
                    errorProvider.SetError(txtPw, "Nhập mật khẩu.");

                this.DialogResult = DialogResult.None;
                return;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPw.Text))
            {
                MessageBox.Show("Điền đầy đủ thông tin bắt buộc!", "Thiếu thông tin");
                this.DialogResult = DialogResult.None;
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
