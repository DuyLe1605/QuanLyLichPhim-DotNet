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
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(18, 18, 30);

        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 70 };
        this.Controls.Add(pnlTop);

        pnlTop.Controls.Add(new Label { Text = "👥  Quản Lý Nhân Viên", Font = new Font("Segoe UI", 18, FontStyle.Bold), ForeColor = Color.FromArgb(210, 210, 230), Location = new Point(5, 5), AutoSize = true });

        var btnAdd = Btn("➕ Thêm NV", Color.FromArgb(60, 160, 60), 5);
        btnAdd.Click += BtnAdd_Click; pnlTop.Controls.Add(btnAdd);
        var btnReset = Btn("🔑 Reset MK", Color.FromArgb(200, 150, 30), 155);
        btnReset.Click += BtnReset_Click; pnlTop.Controls.Add(btnReset);
        var btnToggle = Btn("🔄 Khóa/Mở", Color.FromArgb(60, 120, 200), 305);
        btnToggle.Click += BtnToggle_Click; pnlTop.Controls.Add(btnToggle);

        dgv = Grid(); this.Controls.Add(dgv);
    }

    private async Task LoadAsync()
    {
        using var ctx = Program.CreateDbContext();
        var users = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(ctx.Users.OrderBy(u => u.Id));

        dgv.DataSource = users.Select(u => new
        {
            u.Id, Họ_Tên = u.FullName, Tài_Khoản = u.Username,
            Vai_Trò = u.Role, SĐT = u.Phone ?? "",
            Trạng_Thái = u.IsActive ? "✅ Hoạt động" : "❌ Đã khóa",
            Ngày_Tạo = u.CreatedAt.ToString("dd/MM/yyyy")
        }).ToList();
        if (dgv.Columns.Contains("Id")) dgv.Columns["Id"].Visible = false;
    }

    private async void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new DlgStaffEdit();
        if (dlg.ShowDialog() != DialogResult.OK) return;
        using var ctx = Program.CreateDbContext();
        var auth = new AuthService(ctx);
        var (ok, msg) = await auth.CreateUserAsync(dlg.FullName, dlg.Username, dlg.Password, dlg.Role, dlg.Phone);
        MessageBox.Show(msg); if (ok) await LoadAsync();
    }

    private async void BtnReset_Click(object? s, EventArgs e)
    {
        if (dgv.CurrentRow == null) return;
        int id = (int)dgv.CurrentRow.Cells["Id"].Value;
        string name = dgv.CurrentRow.Cells["Tài_Khoản"].Value?.ToString() ?? "";

        string newPw = $"{name}123";
        if (MessageBox.Show($"Reset mật khẩu của \"{name}\" thành \"{newPw}\"?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        using var ctx = Program.CreateDbContext();
        var user = await ctx.Users.FindAsync(id);
        if (user == null) return;
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPw);
        await ctx.SaveChangesAsync();
        MessageBox.Show($"Đã reset mật khẩu thành: {newPw}");
    }

    private async void BtnToggle_Click(object? s, EventArgs e)
    {
        if (dgv.CurrentRow == null) return;
        int id = (int)dgv.CurrentRow.Cells["Id"].Value;
        if (id == SessionManager.CurrentUser?.Id) { MessageBox.Show("Không thể khóa tài khoản của chính bạn!"); return; }

        using var ctx = Program.CreateDbContext();
        var user = await ctx.Users.FindAsync(id);
        if (user == null) return;
        user.IsActive = !user.IsActive;
        await ctx.SaveChangesAsync();
        await LoadAsync();
    }

    private Button Btn(string t, Color c, int x) { var b = new Button { Text = t, Font = new Font("Segoe UI", 10, FontStyle.Bold), Size = new Size(140, 32), Location = new Point(x, 42), BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand }; b.FlatAppearance.BorderSize = 0; return b; }
    private static DataGridView Grid() { var d = new DataGridView { Dock = DockStyle.Fill, BackgroundColor = Color.FromArgb(22, 22, 38), GridColor = Color.FromArgb(40, 40, 60), BorderStyle = BorderStyle.None, CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal, SelectionMode = DataGridViewSelectionMode.FullRowSelect, MultiSelect = false, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill, RowHeadersVisible = false, EnableHeadersVisualStyles = false, Font = new Font("Segoe UI", 10) }; d.RowTemplate.Height = 40; d.DefaultCellStyle.BackColor = Color.FromArgb(22, 22, 38); d.DefaultCellStyle.ForeColor = Color.FromArgb(200, 200, 220); d.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 50, 120); d.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 28, 48); d.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(160, 160, 190); d.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold); d.ColumnHeadersHeight = 42; d.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(26, 26, 42); return d; }
}

public class DlgStaffEdit : Form
{
    private TextBox txtName = null!, txtUser = null!, txtPw = null!, txtPhone = null!;
    private ComboBox cboRole = null!;
    public string FullName => txtName.Text.Trim();
    public string Username => txtUser.Text.Trim();
    public string Password => txtPw.Text;
    public string Role => cboRole.SelectedItem?.ToString() ?? "Staff";
    public string Phone => txtPhone.Text.Trim();

    public DlgStaffEdit()
    {
        this.Text = "Thêm nhân viên";
        this.ClientSize = new Size(400, 310);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false; this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(24, 24, 40);
        this.ForeColor = Color.FromArgb(200, 200, 220);

        int x1 = 20, x2 = 160, y = 20;
        Lbl("Họ tên *", x1, y); txtName = Txt(x2, y); y += 45;
        Lbl("Tên đăng nhập *", x1, y); txtUser = Txt(x2, y); y += 45;
        Lbl("Mật khẩu *", x1, y); txtPw = Txt(x2, y); y += 45;
        Lbl("Số điện thoại", x1, y); txtPhone = Txt(x2, y); y += 45;
        Lbl("Vai trò", x1, y);
        cboRole = new ComboBox { Font = new Font("Segoe UI", 11), Size = new Size(130, 30), Location = new Point(x2, y), BackColor = Color.FromArgb(35, 35, 55), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList };
        cboRole.Items.AddRange(new[] { "Staff", "Admin" }); cboRole.SelectedIndex = 0;
        this.Controls.Add(cboRole);

        var btnOk = new Button { Text = "Tạo", Font = new Font("Segoe UI", 11, FontStyle.Bold), Size = new Size(100, 36), Location = new Point(160, 265), BackColor = Color.FromArgb(80, 160, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) => { if (string.IsNullOrWhiteSpace(txtName.Text) || string.IsNullOrWhiteSpace(txtUser.Text) || string.IsNullOrWhiteSpace(txtPw.Text)) { MessageBox.Show("Điền đầy đủ thông tin!"); this.DialogResult = DialogResult.None; } };
        this.Controls.Add(btnOk);
        var btnC = new Button { Text = "Hủy", Font = new Font("Segoe UI", 10), Size = new Size(80, 36), Location = new Point(270, 265), BackColor = Color.FromArgb(50, 50, 75), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
        btnC.FlatAppearance.BorderSize = 0; this.Controls.Add(btnC);
        this.AcceptButton = btnOk; this.CancelButton = btnC;
    }

    private void Lbl(string t, int x, int y) { this.Controls.Add(new Label { Text = t, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(160, 160, 185), Location = new Point(x, y + 4), AutoSize = true }); }
    private TextBox Txt(int x, int y) { var t = new TextBox { Font = new Font("Segoe UI", 11), Size = new Size(210, 30), Location = new Point(x, y), BackColor = Color.FromArgb(35, 35, 55), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle }; this.Controls.Add(t); return t; }
}
