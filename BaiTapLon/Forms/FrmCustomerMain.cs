using BaiTapLon.Data;
using BaiTapLon.Helpers;
using BaiTapLon.Models;
using BaiTapLon.Services;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Forms;

public class FrmCustomerMain : Form
{
    private Panel pnlTitleBar = null!;
    private Panel pnlNav = null!;
    private FlowLayoutPanel flpNav = null!;
    private Panel pnlContent = null!;
    private Panel pnlAccountMenu = null!;
    private readonly List<Button> navButtons = new();
    private bool dragging;
    private Point dragStart;

    public FrmCustomerMain()
    {
        InitializeComponent();
        LoadHome();
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
        Text = "CineManager - Customer";
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(1280, 760);
        MinimumSize = new Size(1060, 640);
        FormBorderStyle = FormBorderStyle.None;
        BackColor = Customer.CustomerUi.AppBg;
        DoubleBuffered = true;
        WindowState = FormWindowState.Maximized;

        // ── Title bar ────────────────────────────────────────────────────────
        pnlTitleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = Color.FromArgb(10, 12, 20)
        };
        WireDrag(pnlTitleBar);

        var lblTitle = new Label
        {
            Text = "   CineManager Storefront",
            Dock = DockStyle.Fill,
            ForeColor = Customer.CustomerUi.Muted,
            Font = new Font("Segoe UI", 9.5f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        WireDrag(lblTitle);
        pnlTitleBar.Controls.Add(lblTitle);

        var btnClose = WindowButton("X");
        btnClose.Click += (s, e) => Application.Exit();
        btnClose.MouseEnter += (s, e) => btnClose.BackColor = Color.FromArgb(190, 45, 45);
        btnClose.MouseLeave += (s, e) => btnClose.BackColor = Color.Transparent;
        pnlTitleBar.Controls.Add(btnClose);

        var btnMax = WindowButton("□");
        btnMax.Click += (s, e) => WindowState = WindowState == FormWindowState.Maximized
            ? FormWindowState.Normal
            : FormWindowState.Maximized;
        pnlTitleBar.Controls.Add(btnMax);

        var btnMin = WindowButton("_");
        btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;
        pnlTitleBar.Controls.Add(btnMin);

        // ── Nav bar ──────────────────────────────────────────────────────────
        pnlNav = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.FromArgb(20, 23, 34),
            Padding = new Padding(24, 0, 24, 0)
        };

        var logo = new Label
        {
            Text = "CineManager",
            ForeColor = Color.FromArgb(100, 180, 120),
            Font = new Font("Segoe UI", 19, FontStyle.Bold),
            AutoSize = false,
            Size = new Size(210, 68),
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft
        };
        pnlNav.Controls.Add(logo);

        var account = Customer.CustomerUi.NavButton(BuildAccountText(false));
        account.Width = 230;
        account.Dock = DockStyle.Right;
        account.TextAlign = ContentAlignment.MiddleRight;
        account.Click += (s, e) => ToggleAccountMenu(!pnlAccountMenu.Visible);
        pnlNav.Controls.Add(account);

        flpNav = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            BackColor = Color.Transparent,
            Padding = new Padding(8, 12, 8, 0)
        };
        pnlNav.Controls.Add(flpNav);

        AddNav("Trang chủ", LoadHome);
        AddNav("Phim đang chiếu", LoadNowShowing);
        AddNav("Lịch sử vé", LoadTickets);

        // ── Account menu (floating, not docked) ──────────────────────────────
        pnlAccountMenu = new Panel
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Size = new Size(220, 0),
            BackColor = Color.FromArgb(30, 35, 52),
            Visible = false
        };
        AddAccountItem("Hồ sơ", LoadProfile);
        AddAccountItem("Đăng xuất", Logout);

        // ── Content panel ────────────────────────────────────────────────────
        pnlContent = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Customer.CustomerUi.AppBg
        };

        // Add order matters for Dock layout:
        // Fill panel must be added BEFORE Top panels so it fills the remaining space.
        Controls.Add(pnlContent);   // Fill — added first
        Controls.Add(pnlNav);       // Top  — stacks below title bar
        Controls.Add(pnlTitleBar);  // Top  — sits at very top

        // Account menu floats above everything
        Controls.Add(pnlAccountMenu);
        Resize += (s, e) => PositionAccountMenu();
        PositionAccountMenu();
        pnlAccountMenu.BringToFront();
    }

    public void ShowMovieDetail(Movie movie)
    {
        SetContent(new Customer.UcMovieDetail(movie)
        {
            BackRequested = LoadHome,
            ShowtimeSelected = showtime => _ = LoadBookingAsync(showtime)
        });
    }

    private async Task LoadBookingAsync(Showtime showtime)
    {
        var booking = new Customer.UcCustomerBooking(showtime)
        {
            BackRequested = () => ShowMovieDetail(showtime.Movie),
            PaymentRequested = state => SetContent(new Customer.UcPaymentGateway(state)
            {
                BackRequested = () => _ = LoadBookingAsync(showtime),
                PaymentCompleted = _ => LoadTickets()
            })
        };
        SetContent(booking);
        await booking.LoadAsync();
    }

    private void LoadHome()
    {
        ToggleAccountMenu(false);
        SetContent(new Customer.UcStorefront
        {
            MovieSelected = ShowMovieDetail
        });
    }

    private void LoadNowShowing()
    {
        ToggleAccountMenu(false);
        SetContent(new Customer.UcNowShowing
        {
            MovieSelected = ShowMovieDetail
        });
    }

    private void LoadTickets()
    {
        ToggleAccountMenu(false);
        SetContent(new Customer.UcMyTickets());
    }

    private void LoadProfile()
    {
        ToggleAccountMenu(false);
        SetContent(new Customer.UcMyProfile());
    }

    private void SetContent(UserControl control)
    {
        pnlContent.Controls.Clear();
        control.Dock = DockStyle.Fill;
        pnlContent.Controls.Add(control);
    }

    private void AddNav(string text, Action click)
    {
        var button = Customer.CustomerUi.NavButton(text);
        button.Click += (s, e) => click();
        flpNav.Controls.Add(button);
        navButtons.Add(button);
    }

    private void AddAccountItem(string text, Action click)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 38,
            BackColor = Color.Transparent,
            ForeColor = Customer.CustomerUi.Text,
            FlatStyle = FlatStyle.Flat,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(42, 48, 68);
        button.Click += (s, e) => click();
        pnlAccountMenu.Controls.Add(button);
        button.BringToFront();
    }

    private void ToggleAccountMenu(bool show)
    {
        pnlAccountMenu.Visible = show;
        pnlAccountMenu.Height = show ? 76 : 0;
        PositionAccountMenu();
        pnlAccountMenu.BringToFront();
    }

    private void PositionAccountMenu()
    {
        pnlAccountMenu.Location = new Point(ClientSize.Width - pnlAccountMenu.Width - 32, pnlTitleBar.Height + pnlNav.Height - 10);
    }

    private string BuildAccountText(bool expanded)
    {
        var name = SessionManager.CurrentCustomer?.FullName ?? "Khách hàng";
        if (name.Length > 22) name = name[..21] + "...";
        return $"{name} {(expanded ? "▲" : "▼")}";
    }

    private void Logout()
    {
        if (MessageBox.Show("Bạn có muốn đăng xuất?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        SessionManager.Logout();
        Hide();
        var login = new FrmLogin();
        login.FormClosed += (s, e) => Close();
        login.Show();
    }

    private void WireDrag(Control control)
    {
        control.MouseDown += (s, e) => { dragging = true; dragStart = e.Location; };
        control.MouseMove += (s, e) =>
        {
            if (dragging)
                Location = new Point(Location.X + e.X - dragStart.X, Location.Y + e.Y - dragStart.Y);
        };
        control.MouseUp += (s, e) => dragging = false;
    }

    private static Label WindowButton(string text)
    {
        var label = new Label
        {
            Text = text,
            Dock = DockStyle.Right,
            Width = 46,
            ForeColor = Color.FromArgb(165, 170, 190),
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 10),
            Cursor = Cursors.Hand
        };
        label.MouseEnter += (s, e) =>
        {
            if (label.Text != "X") label.BackColor = Color.FromArgb(40, 45, 62);
        };
        label.MouseLeave += (s, e) => label.BackColor = Color.Transparent;
        return label;
    }
}
