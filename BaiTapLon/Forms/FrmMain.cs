using BaiTapLon.Data;
using BaiTapLon.Helpers;
using BaiTapLon.Services;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Forms;

public class FrmMain : Form
{
    private Panel pnlSidebar = null!;
    private Panel pnlSidebarMenu = null!;
    private Panel pnlSidebarFooter = null!;
    private Panel pnlUserDropdown = null!;
    private Button btnUserNav = null!;
    private Panel pnlHeader = null!;
    private Panel pnlContent = null!;
    private Panel pnlTitleBar = null!;
    private readonly List<Button> _menuButtons = new();
    private int? _currentShiftId;

    private bool _dragging = false;
    private Point _dragStart;

    public FrmMain()
    {
        InitializeComponent();
        SetupMenuByRole();
        _ = RunDemotionCheckAsync();
    }

    private async Task RunDemotionCheckAsync()
    {
        if (DateTime.Now.Day != 1) return;

        try
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(AppConfig.ConnectionString);
            using var context = new AppDbContext(optionsBuilder.Options);

            var service = new DemotionService(context);
            if (await service.ShouldRunAsync())
            {
                await service.ExecuteMonthlyDemotionAsync();
            }
        }
        catch
        {
            // Background check — silently ignore errors to avoid disrupting the UI
        }
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
        this.WindowState = FormWindowState.Maximized;

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
        // NOTE: sidebar is added LAST so it doesn't interfere with Fill layout        this.Controls.Add(pnlSidebar);

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

        pnlSidebarFooter = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 100,
            BackColor = Color.FromArgb(20, 20, 35),
            Padding = new Padding(0, 0, 0, 8)
        };
        pnlSidebar.Controls.Add(pnlSidebarFooter);

        pnlSidebarMenu = new Panel
        {
            Location = new Point(0, 145),
            Size = new Size(250, Math.Max(120, pnlSidebar.ClientSize.Height - 245)),
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.Transparent,
            AutoScroll = true,
            Padding = new Padding(0, 10, 0, 10)
        };
        pnlSidebar.Controls.Add(pnlSidebarMenu);
        pnlSidebar.Resize += (s, e) => ResizeSidebarMenu();

        // ===== Header =====
        pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.FromArgb(22, 22, 38),
            Padding = new Padding(20, 0, 20, 0)
        };

        var lblUserInfo = new Label
        {
            Text = $"{SessionManager.CurrentUser?.FullName ?? "User"}  ·  {SessionManager.CurrentUser?.Role ?? ""}",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(160, 160, 185),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        pnlHeader.Controls.Add(lblUserInfo);
        pnlHeader.Resize += (s, e) => lblUserInfo.Location = new Point(
            Math.Max(20, pnlHeader.ClientSize.Width - lblUserInfo.Width - 24), 18);
        lblUserInfo.Location = new Point(Math.Max(20, pnlHeader.ClientSize.Width - lblUserInfo.Width - 24), 18);

        // ===== Content =====
        pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(18, 18, 30),
            Padding = new Padding(10)
        };

        // Correct add order for Dock layout:
        // Fill must be added FIRST, then Top panels, then Left panels.
        // This ensures pnlContent fills the remaining space correctly.
        this.Controls.Add(pnlContent);   // Fill — added first
        this.Controls.Add(pnlHeader);    // Top  — stacks below title bar
        this.Controls.Add(pnlSidebar);   // Left — sidebar on the left

        ShowWelcomeScreen();
    }

    private void SetupMenuByRole()
    {
        if (SessionManager.IsAdmin)
        {
            AddMenuButton("📊  Tổng quan", "Dashboard");
            AddMenuButton("🎬  Quản lý phim", "Movies");
            AddMenuButton("🏷️  Thể loại phim", "Genres");
            AddMenuButton("🏠  Phòng chiếu", "Rooms");
            AddMenuButton("📅  Lịch chiếu", "Showtimes");
            AddMenuButton("🍿  Bắp nước", "Snacks");
            AddMenuButton("👥  Nhân viên", "Staff");
            AddMenuButton("⏰  Ca làm việc", "Shifts");
            AddMenuButton("👤  Khách hàng", "Customers");
            AddMenuButton("🎟️  Coupon", "Coupons");
            AddMenuButton("🏷️  Voucher", "Vouchers");
            AddMenuButton("📦  Đặt vé online", "Bookings");
            AddMenuButton("📄  Hóa đơn", "Invoices");
            AddMenuButton("⭐  Đánh giá", "Reviews");
            AddMenuButton("📜  Lịch sử HT", "AuditLog");
            AddMenuButton("🎬  Phim đang chiếu", "NowShowing");
            AddMenuButton("🎟️  Bán vé", "SellTicket");
        }
        else
        {
            AddMenuButton("🎬  Phim đang chiếu", "NowShowing");
            AddMenuButton("🎟️  Bán vé", "SellTicket");
            AddMenuButton("📄  Hóa đơn", "Invoices");
        }

        CreateUserNav();
    }

    private void CreateUserNav()
    {
        pnlSidebarFooter.Controls.Clear();

        pnlUserDropdown = new Panel
        {
            Dock = DockStyle.Top,
            Height = 0,
            BackColor = Color.FromArgb(25, 25, 43),
            Visible = false,
            Padding = new Padding(10, 6, 10, 6)
        };
        pnlSidebarFooter.Controls.Add(pnlUserDropdown);

        var btnProfile = CreateUserDropdownButton("Thông tin tài khoản", (s, e) => ShowCurrentUserInfo());
        var btnMyInvoices = CreateUserDropdownButton("Hóa đơn của tôi", (s, e) =>
        {
            ToggleUserDropdown(false);
            LoadModule("Invoices");
        });
        var btnShift = CreateUserDropdownButton("⏰ Ca làm việc", async (s, e) =>
        {
            ToggleUserDropdown(false);
            await ManageShiftAsync();
        });

        // Logout button inside the dropdown
        var btnDropdownLogout = new Button
        {
            Text = "🚪  Đăng xuất",
            Font = new Font("Segoe UI", 9.5f),
            Height = 30,
            Dock = DockStyle.Top,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(210, 90, 90),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };
        btnDropdownLogout.FlatAppearance.BorderSize = 0;
        btnDropdownLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 25, 25);
        btnDropdownLogout.Click += BtnLogout_Click;

        pnlUserDropdown.Controls.Add(btnDropdownLogout);
        pnlUserDropdown.Controls.Add(btnMyInvoices);
        pnlUserDropdown.Controls.Add(btnShift);
        pnlUserDropdown.Controls.Add(btnProfile);

        btnUserNav = new Button
        {
            Text = BuildUserNavText(false),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Height = 48,
            Dock = DockStyle.Top,
            BackColor = Color.FromArgb(31, 31, 50),
            ForeColor = Color.FromArgb(220, 220, 238),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(18, 0, 8, 0)
        };
        btnUserNav.FlatAppearance.BorderSize = 0;
        btnUserNav.FlatAppearance.MouseOverBackColor = Color.FromArgb(42, 42, 66);
        btnUserNav.Click += (s, e) => ToggleUserDropdown(!pnlUserDropdown.Visible);
        pnlSidebarFooter.Controls.Add(btnUserNav);

        btnUserNav.BringToFront();
        ResizeSidebarMenu();
    }

    private Button CreateUserDropdownButton(string text, EventHandler click)
    {
        var button = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 9.5f),
            Height = 30,
            Dock = DockStyle.Top,
            BackColor = Color.Transparent,
            ForeColor = Color.FromArgb(185, 185, 210),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 38, 58);
        button.Click += click;
        return button;
    }

    private void ToggleUserDropdown(bool show)
    {
        pnlUserDropdown.Visible = show;
        pnlUserDropdown.Height = show ? 132 : 0;   // 4 items × 30px + 12px padding
        pnlSidebarFooter.Height = show ? 180 : 48;
        btnUserNav.Text = BuildUserNavText(show);
        ResizeSidebarMenu();
    }

    private void ShowCurrentUserInfo()
    {
        ToggleUserDropdown(false);
        var user = SessionManager.CurrentUser;
        if (user == null) return;

        MessageBox.Show(
            $"Họ tên: {user.FullName}\nTài khoản: {user.Username}\nVai trò: {user.Role}\nSĐT: {user.Phone ?? "-"}",
            "Thông tin tài khoản",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private string BuildUserNavText(bool expanded)
    {
        var name = SessionManager.CurrentUser?.FullName ?? "User";
        if (name.Length > 19)
            name = name[..18] + "...";

        return $"{GetInitials(SessionManager.CurrentUser?.FullName)}  {name}    {(expanded ? "▴" : "▾")}";
    }

    private static string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "U";

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0][0].ToString().ToUpper();

        return (parts[0][0].ToString() + parts[^1][0]).ToUpper();
    }

    private void ResizeSidebarMenu()
    {
        if (pnlSidebarMenu == null || pnlSidebarFooter == null)
            return;

        pnlSidebarMenu.Height = Math.Max(90, pnlSidebar.ClientSize.Height - pnlSidebarMenu.Top - pnlSidebarFooter.Height);
    }

    private void AddMenuButton(string text, string tag)
    {
        var yPos = _menuButtons.Count * 48;
        var btn = new Button
        {
            Text = text,
            Tag = tag,
            Font = new Font("Segoe UI", 11),
            Size = new Size(250, 44),
            Location = new Point(0, yPos),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackColor = Color.FromArgb(20, 20, 35),
            ForeColor = Color.FromArgb(175, 175, 200),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(22, 0, 0, 0),
            Margin = Padding.Empty
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(35, 35, 55);
        btn.Click += MenuButton_Click;
        pnlSidebarMenu.Controls.Add(btn);
        _menuButtons.Add(btn);
        pnlSidebarMenu.AutoScrollMinSize = new Size(0, _menuButtons.Count * 48 + 20);
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
            "Customers" => new Admin.UcCustomerManagement(),
            "Coupons" => new Admin.Coupons.UcCouponManagement(),
            "Vouchers" => new Admin.Vouchers.UcVoucherManagement(),
            "Genres" => new Admin.UcGenreManagement(),
            "Shifts" => new Admin.UcShiftManagement(),
            "Bookings" => new Admin.UcBookingManagement(),
            "Invoices" => new Admin.UcInvoiceManagement(),
            "Reviews" => new Admin.UcReviewManagement(),
            "AuditLog" => new Admin.UcAuditLog(),
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
            if (!await EnsureShiftOpenAsync()) return;
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
        ucSeatSelection.ContinueRequested += async (state) => 
        {
            var newState = new Staff.SaleOrderState
            {
                Showtime = state.Showtime,
                Movie = state.Movie,
                Room = state.Room,
                Seats = state.Seats,
                CustomerName = state.CustomerName,
                CustomerPhone = state.CustomerPhone,
                CustomerId = state.CustomerId,
                TicketTotal = state.TicketTotal,
                ShiftId = _currentShiftId
            };
            await LoadSnackOrderAsync(newState);
        };

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

    private async Task<bool> EnsureShiftOpenAsync()
    {
        if (_currentShiftId.HasValue) return true;

        using var ctx = Program.CreateDbContext();
        var svc = new ShiftService(ctx);
        var openShift = await svc.GetOpenShiftAsync(SessionManager.CurrentUser!.Id);

        if (openShift != null)
        {
            _currentShiftId = openShift.Id;
            return true;
        }

        var result = MessageBox.Show("Bạn chưa mở ca làm việc! Bạn có muốn mở ca ngay bây giờ không?", 
            "Mở ca làm việc", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            await ManageShiftAsync();
            return _currentShiftId.HasValue;
        }

        return false;
    }

    private async Task ManageShiftAsync()
    {
        using var ctx = Program.CreateDbContext();
        var svc = new ShiftService(ctx);
        var openShift = await svc.GetOpenShiftAsync(SessionManager.CurrentUser!.Id);

        if (openShift == null)
        {
            // Open shift
            string? cashStr = Admin.UcGenreManagement.ShowInputDialog("Mở ca làm việc", "Nhập số tiền mặt đầu ca:", "0"); // Reusing input dialog
            if (cashStr != null && decimal.TryParse(cashStr, out var cash))
            {
                var (ok, msg, shift) = await svc.OpenShiftAsync(SessionManager.CurrentUser!.Id, cash);
                MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
                if (ok && shift != null) _currentShiftId = shift.Id;
            }
        }
        else
        {
            // Close shift
            string? cashStr = Admin.UcGenreManagement.ShowInputDialog("Đóng ca làm việc", "Nhập số tiền mặt cuối ca đếm được:", "0");
            if (cashStr != null && decimal.TryParse(cashStr, out var cash))
            {
                var (ok, msg) = await svc.CloseShiftAsync(openShift.Id, cash);
                MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
                if (ok) _currentShiftId = null;
            }
        }
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
