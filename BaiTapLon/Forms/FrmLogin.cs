using BaiTapLon.Services;
using BaiTapLon.Helpers;

namespace BaiTapLon.Forms;

public class FrmLogin : Form
{
    private Panel pnlMain = null!;
    private TextBox txtUsername = null!;
    private TextBox txtPassword = null!;
    private Button btnLogin = null!;
    private Label lblError = null!;
    private CheckBox chkShowPassword = null!;

    private bool _dragging = false;
    private Point _dragStart;

    public FrmLogin()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.Text = "Đăng Nhập";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.ClientSize = new Size(500, 620);
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.FromArgb(15, 15, 28);
        this.DoubleBuffered = true;

        this.MouseDown += (s, e) => { _dragging = true; _dragStart = e.Location; };
        this.MouseMove += (s, e) => { if (_dragging) this.Location = new Point(Location.X + e.X - _dragStart.X, Location.Y + e.Y - _dragStart.Y); };
        this.MouseUp += (s, e) => { _dragging = false; };

        // Nút đóng
        var btnClose = new Label
        {
            Text = "✕",
            Font = new Font("Segoe UI", 14),
            ForeColor = Color.FromArgb(120, 120, 140),
            Size = new Size(40, 35),
            Location = new Point(455, 8),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };
        btnClose.Click += (s, e) => Application.Exit();
        btnClose.MouseEnter += (s, e) => btnClose.ForeColor = Color.FromArgb(255, 80, 80);
        btnClose.MouseLeave += (s, e) => btnClose.ForeColor = Color.FromArgb(120, 120, 140);
        this.Controls.Add(btnClose);

        // Card chính
        pnlMain = new Panel
        {
            Size = new Size(420, 530),
            Location = new Point(40, 55),
            BackColor = Color.FromArgb(24, 24, 40),
        };
        pnlMain.Paint += PnlMain_Paint;
        this.Controls.Add(pnlMain);

        int y = 30;

        // Title
        var lblTitle = new Label
        {
            Text = "🎬 CineManager",
            Font = new Font("Segoe UI", 26, FontStyle.Bold),
            ForeColor = Color.FromArgb(130, 110, 255),
            AutoSize = false,
            Size = new Size(380, 50),
            Location = new Point(20, y),
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlMain.Controls.Add(lblTitle);
        y += 55;

        // Subtitle
        var lblSubtitle = new Label
        {
            Text = "Đăng nhập để tiếp tục",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.FromArgb(130, 130, 155),
            AutoSize = false,
            Size = new Size(380, 28),
            Location = new Point(20, y),
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlMain.Controls.Add(lblSubtitle);
        y += 40;

        // Separator
        var sep = new Panel { Size = new Size(340, 1), Location = new Point(40, y), BackColor = Color.FromArgb(50, 50, 70) };
        pnlMain.Controls.Add(sep);
        y += 25;

        // Username label
        var lblUser = new Label
        {
            Text = "TÊN ĐĂNG NHẬP",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(140, 140, 165),
            Location = new Point(40, y),
            AutoSize = true
        };
        pnlMain.Controls.Add(lblUser);
        y += 25;

        // Username input
        txtUsername = new TextBox
        {
            Font = new Font("Segoe UI", 13),
            Size = new Size(340, 35),
            Location = new Point(40, y),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        pnlMain.Controls.Add(txtUsername);
        y += 50;

        // Password label
        var lblPass = new Label
        {
            Text = "MẬT KHẨU",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(140, 140, 165),
            Location = new Point(40, y),
            AutoSize = true
        };
        pnlMain.Controls.Add(lblPass);
        y += 25;

        // Password input
        txtPassword = new TextBox
        {
            Font = new Font("Segoe UI", 13),
            Size = new Size(340, 35),
            Location = new Point(40, y),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            UseSystemPasswordChar = true
        };
        pnlMain.Controls.Add(txtPassword);
        y += 42;

        // Show password
        chkShowPassword = new CheckBox
        {
            Text = "Hiển thị mật khẩu",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(130, 130, 155),
            Location = new Point(40, y),
            AutoSize = false,
            Size = new Size(340, 30),
            TextAlign = ContentAlignment.MiddleLeft
        };
        chkShowPassword.CheckedChanged += (s, e) => txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        pnlMain.Controls.Add(chkShowPassword);
        y += 42;

        // Error
        lblError = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(255, 82, 82),
            AutoSize = false,
            Size = new Size(340, 25),
            Location = new Point(40, y),
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false
        };
        pnlMain.Controls.Add(lblError);
        y += 30;

        // Login button
        btnLogin = new Button
        {
            Text = "ĐĂNG NHẬP",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Size = new Size(340, 52),
            Location = new Point(40, y),
            BackColor = Color.FromArgb(100, 80, 255),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnLogin.FlatAppearance.BorderSize = 0;
        btnLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(120, 100, 255);
        btnLogin.Click += BtnLogin_Click;
        pnlMain.Controls.Add(btnLogin);
        y += 65;

        // Hint
        // var lblHint = new Label
        // {
        //     Text = "Admin: admin / admin123\nStaff: staff / staff123",
        //     Font = new Font("Segoe UI", 9),
        //     ForeColor = Color.FromArgb(85, 85, 110),
        //     AutoSize = false,
        //     Size = new Size(340, 40),
        //     Location = new Point(40, y),
        //     TextAlign = ContentAlignment.MiddleCenter
        // };
        // pnlMain.Controls.Add(lblHint);

        this.AcceptButton = btnLogin;
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        txtUsername.Focus();
    }

    private void PnlMain_Paint(object? sender, PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var rect = new Rectangle(0, 0, pnlMain.Width - 1, pnlMain.Height - 1);
        using var path = RoundedRect(rect, 14);
        using var pen = new Pen(Color.FromArgb(45, 45, 70), 1);
        g.DrawPath(pen, path);
    }

    private async void BtnLogin_Click(object? sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
        {
            ShowError("Vui lòng nhập đầy đủ thông tin!");
            return;
        }

        btnLogin.Enabled = false;
        btnLogin.Text = "Đang xử lý...";
        lblError.Visible = false;

        try
        {
            using var context = Program.CreateDbContext();
            var authService = new AuthService(context);
            var user = await authService.LoginAsync(username, password);

            if (user != null)
            {
                SessionManager.Login(user);
                this.Hide();
                var mainForm = new FrmMain();
                mainForm.FormClosed += (s, args) => this.Close();
                mainForm.Show();
            }
            else
            {
                ShowError("Sai tên đăng nhập hoặc mật khẩu!");
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi: {ex.Message}");
        }
        finally
        {
            btnLogin.Enabled = true;
            btnLogin.Text = "ĐĂNG NHẬP";
        }
    }

    private void ShowError(string msg)
    {
        lblError.Text = msg;
        lblError.Visible = true;
    }

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(Rectangle rect, int r)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        int d = r * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }
}
