using BaiTapLon.Helpers;

namespace BaiTapLon.Forms;

public class FrmMain : Form
{
    private Panel pnlSidebar = null!;
    private Panel pnlHeader = null!;
    private Panel pnlContent = null!;
    private Panel pnlTitleBar = null!;
    private readonly List<Button> _menuButtons = new();

    private bool _dragging = false;
    private Point _dragStart;

    public FrmMain()
    {
        InitializeComponent();
        SetupMenuByRole();
    }

    private void InitializeComponent()
    {
        this.Text = "CineManager";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.ClientSize = new Size(1300, 760);
        this.MinimumSize = new Size(1100, 650);
        this.FormBorderStyle = FormBorderStyle.None;
        this.BackColor = Color.FromArgb(18, 18, 30);
        this.DoubleBuffered = true;

        // ===== Title Bar =====
        pnlTitleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 35,
            BackColor = Color.FromArgb(12, 12, 22)
        };
        pnlTitleBar.MouseDown += (s, e) => { _dragging = true; _dragStart = e.Location; };
        pnlTitleBar.MouseMove += (s, e) => { if (_dragging) this.Location = new Point(Location.X + e.X - _dragStart.X, Location.Y + e.Y - _dragStart.Y); };
        pnlTitleBar.MouseUp += (s, e) => { _dragging = false; };

        var lblWinTitle = new Label
        {
            Text = "   🎬 CineManager",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(130, 130, 155),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        // Title label cũng cho drag
        lblWinTitle.MouseDown += (s, e) => { _dragging = true; _dragStart = e.Location; };
        lblWinTitle.MouseMove += (s, e) => { if (_dragging) this.Location = new Point(Location.X + e.X - _dragStart.X, Location.Y + e.Y - _dragStart.Y); };
        lblWinTitle.MouseUp += (s, e) => { _dragging = false; };
        pnlTitleBar.Controls.Add(lblWinTitle);

        // Window buttons (phải → trái: close, max, min)
        var btnClose = MakeWinBtn("✕", 46);
        btnClose.Click += (s, e) => Application.Exit();
        btnClose.MouseEnter += (s, e) => btnClose.BackColor = Color.FromArgb(200, 40, 40);
        btnClose.MouseLeave += (s, e) => btnClose.BackColor = Color.Transparent;
        pnlTitleBar.Controls.Add(btnClose);

        var btnMax = MakeWinBtn("□", 46);
        btnMax.Click += (s, e) => this.WindowState = this.WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized;
        pnlTitleBar.Controls.Add(btnMax);

        var btnMin = MakeWinBtn("─", 46);
        btnMin.Click += (s, e) => this.WindowState = FormWindowState.Minimized;
        pnlTitleBar.Controls.Add(btnMin);

        this.Controls.Add(pnlTitleBar);

        // ===== Sidebar =====
        pnlSidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 250,
            BackColor = Color.FromArgb(20, 20, 35),
        };
        this.Controls.Add(pnlSidebar);
        pnlSidebar.BringToFront();

        // Logo icon
        var lblLogo = new Label
        {
            Text = "🎬",
            Font = new Font("Segoe UI", 30),
            AutoSize = false,
            Size = new Size(250, 58),
            Location = new Point(0, 8),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.FromArgb(130, 110, 255)
        };
        pnlSidebar.Controls.Add(lblLogo);

        // App name
        var lblAppName = new Label
        {
            Text = "CineManager",
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            ForeColor = Color.FromArgb(130, 110, 255),
            AutoSize = false,
            Size = new Size(250, 36),
            Location = new Point(0, 68),
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlSidebar.Controls.Add(lblAppName);

        // Role label
        var lblRole = new Label
        {
            Text = SessionManager.CurrentUser?.Role == "Admin" ? "Quản trị viên" : "Nhân viên bán vé",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(90, 90, 115),
            AutoSize = false,
            Size = new Size(250, 22),
            Location = new Point(0, 102),
            TextAlign = ContentAlignment.MiddleCenter
        };
        pnlSidebar.Controls.Add(lblRole);

        // Separator
        var sep = new Panel { Size = new Size(200, 1), Location = new Point(25, 132), BackColor = Color.FromArgb(40, 40, 60) };
        pnlSidebar.Controls.Add(sep);

        // ===== Header =====
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.FromArgb(22, 22, 38),
            Padding = new Padding(20, 0, 20, 0)
        };
        this.Controls.Add(pnlHeader);
        pnlHeader.BringToFront();

        var lblUserInfo = new Label
        {
            Text = $"👤  {SessionManager.CurrentUser?.FullName ?? "User"}",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.FromArgb(190, 190, 210),
            AutoSize = true,
            Location = new Point(20, 17)
        };
        pnlHeader.Controls.Add(lblUserInfo);

        var btnLogout = new Button
        {
            Text = "🚪 Đăng xuất",
            Font = new Font("Segoe UI", 10),
            Size = new Size(140, 36),
            BackColor = Color.FromArgb(50, 50, 70),
            ForeColor = Color.FromArgb(210, 210, 230),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnLogout.FlatAppearance.BorderColor = Color.FromArgb(65, 65, 90);
        btnLogout.FlatAppearance.BorderSize = 1;
        btnLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(180, 50, 50);
        btnLogout.Click += BtnLogout_Click;
        pnlHeader.Controls.Add(btnLogout);
        pnlHeader.Resize += (s, e) => btnLogout.Location = new Point(pnlHeader.Width - 165, 10);
        btnLogout.Location = new Point(pnlHeader.Width - 165, 10);

        // ===== Content =====
        pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(18, 18, 30),
            Padding = new Padding(10)
        };
        this.Controls.Add(pnlContent);
        pnlContent.BringToFront();

        ShowWelcomeScreen();
    }

    private void SetupMenuByRole()
    {
        int yPos = 155;

        if (SessionManager.IsAdmin)
        {
            AddMenuButton("📊  Tổng quan", yPos, "Dashboard"); yPos += 48;
            AddMenuButton("🎬  Quản lý phim", yPos, "Movies"); yPos += 48;
            AddMenuButton("🏠  Phòng chiếu", yPos, "Rooms"); yPos += 48;
            AddMenuButton("📅  Lịch chiếu", yPos, "Showtimes"); yPos += 48;
            AddMenuButton("🍿  Bắp nước", yPos, "Snacks"); yPos += 48;
            AddMenuButton("👥  Nhân viên", yPos, "Staff"); yPos += 48;
        }
        else
        {
            AddMenuButton("🎬  Phim đang chiếu", yPos, "NowShowing"); yPos += 48;
            AddMenuButton("🎟️  Bán vé", yPos, "SellTicket"); yPos += 48;
        }

        // Logout ở cuối sidebar
        var btnSideLogout = new Button
        {
            Text = "🚪  Đăng xuất",
            Font = new Font("Segoe UI", 10),
            Size = new Size(250, 44),
            BackColor = Color.FromArgb(20, 20, 35),
            ForeColor = Color.FromArgb(210, 90, 90),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(22, 0, 0, 0),
            Dock = DockStyle.Bottom
        };
        btnSideLogout.FlatAppearance.BorderSize = 0;
        btnSideLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 25, 25);
        btnSideLogout.Click += BtnLogout_Click;
        pnlSidebar.Controls.Add(btnSideLogout);
    }

    private void AddMenuButton(string text, int yPos, string tag)
    {
        var btn = new Button
        {
            Text = text,
            Tag = tag,
            Font = new Font("Segoe UI", 11),
            Size = new Size(250, 44),
            Location = new Point(0, yPos),
            BackColor = Color.FromArgb(20, 20, 35),
            ForeColor = Color.FromArgb(175, 175, 200),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(22, 0, 0, 0)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 35, 55);
        btn.Click += MenuButton_Click;
        pnlSidebar.Controls.Add(btn);
        _menuButtons.Add(btn);
    }

    private void MenuButton_Click(object? sender, EventArgs e)
    {
        if (sender is not Button btn) return;

        foreach (var b in _menuButtons)
        {
            b.BackColor = Color.FromArgb(20, 20, 35);
            b.ForeColor = Color.FromArgb(175, 175, 200);
            b.Font = new Font("Segoe UI", 11);
        }
        btn.BackColor = Color.FromArgb(100, 80, 255);
        btn.ForeColor = Color.White;
        btn.Font = new Font("Segoe UI", 11, FontStyle.Bold);

        LoadModule(btn.Tag?.ToString() ?? "");
    }

    private void LoadModule(string module)
    {
        pnlContent.Controls.Clear();

        UserControl? uc = module switch
        {
            "Dashboard" => new Admin.UcDashboard(),
            "Movies" => new Admin.UcMovieManagement(),
            "Rooms" => new Admin.UcRoomManagement(),
            "Showtimes" => new Admin.UcShowtimeManagement(),
            "Snacks" => new Admin.UcSnackManagement(),
            "Staff" => new Admin.UcStaffManagement(),
            "NowShowing" or "SellTicket" => CreateNowShowingModule(),
            _ => null
        };

        if (uc != null)
        {
            uc.Dock = DockStyle.Fill;
            pnlContent.Controls.Add(uc);
        }
        else
        {
            var lbl = new Label
            {
                Text = $"📌 {module}\n\nĐang phát triển...",
                Font = new Font("Segoe UI", 18),
                ForeColor = Color.FromArgb(100, 100, 130),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            pnlContent.Controls.Add(lbl);
        }
    }

    /// <summary>
    /// Tạo module bán vé với luồng: NowShowing → SeatSelection → SnackOrder → Checkout → NowShowing.
    /// </summary>
    private Staff.UcNowShowing CreateNowShowingModule()
    {
        var ucNowShowing = new Staff.UcNowShowing();

        ucNowShowing.ShowtimeSelected += async (showtime) =>
        {
            await LoadSeatSelectionAsync(showtime);
        };

        return ucNowShowing;
    }

    private async Task LoadSeatSelectionAsync(Models.Showtime showtime)
    {
        pnlContent.Controls.Clear();

        var ucSeatSelection = new Staff.UcSeatSelection { Dock = DockStyle.Fill };
        pnlContent.Controls.Add(ucSeatSelection);

        ucSeatSelection.BackRequested += () => LoadModule("NowShowing");
        ucSeatSelection.ContinueRequested += async (state) => await LoadSnackOrderAsync(state);

        await ucSeatSelection.LoadShowtimeAsync(showtime);
    }

    private async Task LoadSnackOrderAsync(Staff.SaleOrderState state)
    {
        pnlContent.Controls.Clear();

        var ucSnackOrder = new Staff.UcSnackOrder { Dock = DockStyle.Fill };
        pnlContent.Controls.Add(ucSnackOrder);

        ucSnackOrder.BackRequested += async () => await LoadSeatSelectionAsync(state.Showtime);
        ucSnackOrder.CheckoutCompleted += () => LoadModule("NowShowing");

        await ucSnackOrder.LoadOrderAsync(state);
    }

    private void ShowWelcomeScreen()
    {
        pnlContent.Controls.Clear();

        var pnl = new Panel { Dock = DockStyle.Fill };

        var lblHello = new Label
        {
            Text = $"Xin chào, {SessionManager.CurrentUser?.FullName}! 👋",
            Font = new Font("Segoe UI", 24, FontStyle.Bold),
            ForeColor = Color.FromArgb(210, 210, 230),
            AutoSize = true
        };

        var lblDesc = new Label
        {
            Text = "Chọn một mục từ menu bên trái để bắt đầu.",
            Font = new Font("Segoe UI", 13),
            ForeColor = Color.FromArgb(110, 110, 140),
            AutoSize = true
        };

        pnl.Controls.Add(lblHello);
        pnl.Controls.Add(lblDesc);
        pnl.Resize += (s, e) =>
        {
            lblHello.Location = new Point((pnl.Width - lblHello.Width) / 2, pnl.Height / 2 - 40);
            lblDesc.Location = new Point((pnl.Width - lblDesc.Width) / 2, pnl.Height / 2 + 20);
        };

        pnlContent.Controls.Add(pnl);
    }

    private void BtnLogout_Click(object? sender, EventArgs e)
    {
        var result = MessageBox.Show("Bạn có chắc muốn đăng xuất?", "Xác nhận",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            SessionManager.Logout();
            this.Hide();
            var login = new FrmLogin();
            login.FormClosed += (s, args) => this.Close();
            login.Show();
        }
    }

    private static Label MakeWinBtn(string text, int width)
    {
        var btn = new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(150, 150, 170),
            Size = new Size(width, 35),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand,
            Dock = DockStyle.Right
        };
        btn.MouseEnter += (s, e) => { if (btn.Text != "✕") btn.BackColor = Color.FromArgb(45, 45, 65); };
        btn.MouseLeave += (s, e) => btn.BackColor = Color.Transparent;
        return btn;
    }
}
