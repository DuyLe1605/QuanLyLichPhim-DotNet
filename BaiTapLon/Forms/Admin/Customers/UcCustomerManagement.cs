using BaiTapLon.Services;
using BaiTapLon.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Forms.Admin;

public class UcCustomerManagement : UserControl
{
    private DataGridView dgv = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboTier = null!;

    // Panel chi tiết bên phải
    private Panel pnlDetail = null!;
    private Label lblCustName = null!, lblCustInfo = null!, lblCustStats = null!;
    private PictureBox picQr = null!;
    private DataGridView dgvHistory = null!;

    public UcCustomerManagement()
    {
        InitUI();
        this.Load += async (s, e) => await LoadAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        // Search
        txtSearch = AdminControls.CreateSearchBox("🔍 Tìm tên, SĐT, email, mã TV...", 300);
        txtSearch.TextChanged += async (s, e) => await LoadAsync();

        // Filter tier
        cboTier = AdminControls.CreateComboBox(150);
        cboTier.Items.AddRange(new object[] { "Tất cả hạng", "Standard", "VIP", "Diamond" });
        cboTier.SelectedIndex = 0;
        cboTier.SelectedIndexChanged += async (s, e) => await LoadAsync();

        var toolbar = AdminControls.CreateToolbar(
            txtSearch,
            cboTier,
            AdminControls.CreateButton("🔄 Khóa/Mở", AdminTheme.ButtonWarning, 120, BtnToggle_Click),
            AdminControls.CreateButton("🔃 Làm mới", AdminTheme.ButtonNeutral, 110, async (s, e) => await LoadAsync())
        );

        dgv = AdminControls.CreateGrid();
        dgv.SelectionChanged += DgvSelectionChanged;

        // === Layout: trái = grid, phải = detail ===
        var splitContainer = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor = AdminTheme.PageBack
        };
        splitContainer.HandleCreated += (s, e) => ApplyCustomerSplitLayout(splitContainer);
        splitContainer.Resize += (s, e) => ApplyCustomerSplitLayout(splitContainer);

        dgv.Dock = DockStyle.Fill;
        splitContainer.Panel1.Controls.Add(dgv);

        pnlDetail = CreateDetailPanel();
        splitContainer.Panel2.Controls.Add(pnlDetail);

        var page = AdminLayouts.CreateManagementPage("👤  Quản Lý Khách Hàng", toolbar, splitContainer);
        Controls.Add(page);
    }

    private Panel CreateDetailPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(22, 22, 38),
            Padding = new Padding(15),
            AutoScroll = true
        };

        int y = 10;

        // QR Code
        picQr = new PictureBox
        {
            Size = new Size(120, 120),
            Location = new Point(15, y),
            BackColor = Color.White,
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle
        };
        panel.Controls.Add(picQr);

        // Tên KH
        lblCustName = new Label
        {
            Text = "Chọn khách hàng",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(210, 210, 230),
            Location = new Point(145, y),
            Size = new Size(200, 30)
        };
        panel.Controls.Add(lblCustName);

        // Thông tin
        lblCustInfo = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(150, 150, 180),
            Location = new Point(145, y + 32),
            Size = new Size(200, 88),
            MaximumSize = new Size(200, 0),
            AutoSize = true
        };
        panel.Controls.Add(lblCustInfo);

        y += 135;

        // Separator
        panel.Controls.Add(new Panel { Location = new Point(15, y), Size = new Size(320, 1), BackColor = Color.FromArgb(50, 50, 75) });
        y += 10;

        // Thống kê
        lblCustStats = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(180, 180, 210),
            Location = new Point(15, y),
            Size = new Size(320, 80),
            MaximumSize = new Size(320, 0),
            AutoSize = true
        };
        panel.Controls.Add(lblCustStats);
        y += 90;

        // Separator
        panel.Controls.Add(new Panel { Location = new Point(15, y), Size = new Size(320, 1), BackColor = Color.FromArgb(50, 50, 75) });
        y += 10;

        // Label lịch sử
        panel.Controls.Add(new Label
        {
            Text = "📋 Lịch sử điểm thưởng",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 160, 190),
            Location = new Point(15, y),
            AutoSize = true
        });
        y += 25;

        // Grid lịch sử điểm
        dgvHistory = new DataGridView
        {
            Location = new Point(15, y),
            Size = new Size(320, 200),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
            BackgroundColor = Color.FromArgb(26, 26, 42),
            GridColor = Color.FromArgb(40, 40, 60),
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 30 },
            Font = new Font("Segoe UI", 9)
        };
        dgvHistory.DefaultCellStyle.BackColor = Color.FromArgb(26, 26, 42);
        dgvHistory.DefaultCellStyle.ForeColor = AdminTheme.Text;
        dgvHistory.DefaultCellStyle.SelectionBackColor = AdminTheme.GridSelection;
        dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = AdminTheme.GridHeaderBack;
        dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = AdminTheme.MutedText;
        dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        dgvHistory.ColumnHeadersHeight = 32;
        panel.Controls.Add(dgvHistory);

        return panel;
    }

    private async Task LoadAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var svc = new CustomerService(ctx);

            string keyword = txtSearch.Text.Trim();
            string tier = cboTier.SelectedItem?.ToString() ?? "";

            List<Models.Customer> customers;
            if (tier != "Tất cả hạng" && !string.IsNullOrEmpty(tier))
                customers = await svc.GetByTierAsync(tier);
            else
                customers = await svc.SearchAsync(keyword);

            // Nếu đang lọc tier mà có keyword thì lọc tiếp phía client
            if (tier != "Tất cả hạng" && !string.IsNullOrEmpty(tier) && !string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.ToLower();
                customers = customers.Where(c =>
                    c.FullName.ToLower().Contains(keyword) ||
                    c.Phone.Contains(keyword) ||
                    c.Email.ToLower().Contains(keyword) ||
                    c.MemberCode.ToLower().Contains(keyword)
                ).ToList();
            }

            dgv.DataSource = customers.Select(c => new
            {
                c.Id,
                MãTV = c.MemberCode,
                HọTên = c.FullName,
                SĐT = c.Phone,
                Email = c.Email,
                Hạng = FormatTier(c.Tier),
                Điểm = c.TotalPoints.ToString("N0"),
                TổngChi = c.TotalSpent.ToString("N0") + "đ",
                TrạngThái = c.IsActive ? "✅ Active" : "❌ Khóa",
                NgàyĐK = c.CreatedAt.ToString("dd/MM/yyyy")
            }).ToList();

            AdminControls.HideColumn(dgv, "Id");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
        }
    }

    private async void DgvSelectionChanged(object? sender, EventArgs e)
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (id.HasValue)
            await LoadDetailAsync(id.Value);
    }

    private async Task LoadDetailAsync(int customerId)
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var customer = await ctx.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId);
            if (customer == null) return;

            // QR
            picQr.Image?.Dispose();
            picQr.Image = BarcodeHelper.GenerateQrCode(customer.MemberCode, 120, 120);

            // Tên
            lblCustName.Text = customer.FullName;

            // Info
            lblCustInfo.Text = $"📧  {customer.Email}\n" +
                               $"📱  {customer.Phone}\n" +
                               $"🏷️  Mã TV: {customer.MemberCode}\n" +
                               $"📅  Đăng ký: {customer.CreatedAt:dd/MM/yyyy}";

            // Stats
            var pointSvc = new PointService(ctx);
            var (earned, redeemed, balance) = await pointSvc.GetSummaryAsync(customerId);

            string tierIcon = customer.Tier switch
            {
                "Diamond" => "💎",
                "VIP" => "⭐",
                _ => "🎫"
            };

            // Tính tiến trình lên hạng tiếp theo
            string progress = customer.Tier switch
            {
                "Standard" => $"📈 Còn {Math.Max(0, 2_000_000m - customer.TotalSpent):N0}đ để lên VIP",
                "VIP" => $"📈 Còn {Math.Max(0, 10_000_000m - customer.TotalSpent):N0}đ để lên Diamond",
                _ => "🏆 Hạng cao nhất!"
            };

            lblCustStats.Text = $"{tierIcon} Hạng: {customer.Tier}\n" +
                                $"💰 Tổng chi tiêu: {customer.TotalSpent:N0}đ\n" +
                                $"✨ Điểm hiện tại: {balance:N0}\n" +
                                $"📊 Đã tích: {earned:N0} | Đã đổi: {redeemed:N0}\n" +
                                progress;

            // Lịch sử điểm
            var history = await pointSvc.GetHistoryAsync(customerId);
            dgvHistory.DataSource = history.Take(20).Select(h => new
            {
                Ngày = h.CreatedAt.ToString("dd/MM HH:mm"),
                Loại = h.Type == "Earn" ? "✅ Tích" : "🔄 Đổi",
                Điểm = h.Points > 0 ? $"+{h.Points:N0}" : h.Points.ToString("N0"),
                MôTả = h.Description.Length > 30 ? h.Description[..30] + "..." : h.Description
            }).ToList();
        }
        catch (Exception ex)
        {
            lblCustStats.Text = $"Lỗi: {ex.Message}";
        }
    }

    private async void BtnToggle_Click(object? s, EventArgs e)
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (!id.HasValue || dgv.CurrentRow == null) return;

        string name = dgv.CurrentRow.Cells["HọTên"].Value?.ToString() ?? "";

        if (MessageBox.Show($"Khóa/Mở tài khoản \"{name}\"?", "Xác nhận",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        using var ctx = Program.CreateDbContext();
        var svc = new CustomerService(ctx);
        await svc.ToggleActiveAsync(id.Value);
        await LoadAsync();
    }

    private static void ApplyCustomerSplitLayout(SplitContainer splitContainer)
    {
        const int preferredLeftWidth = 650;
        const int desiredLeftMin = 360;
        const int desiredRightMin = 260;

        var availableWidth = splitContainer.ClientSize.Width - splitContainer.SplitterWidth;
        if (availableWidth <= 50)
            return;

        splitContainer.Panel1MinSize = 25;
        splitContainer.Panel2MinSize = 25;

        var leftMin = desiredLeftMin;
        var rightMin = desiredRightMin;
        if (availableWidth < leftMin + rightMin)
        {
            leftMin = 25;
            rightMin = 25;
        }

        var maxDistance = splitContainer.ClientSize.Width - splitContainer.SplitterWidth - rightMin;
        if (maxDistance < leftMin)
            return;

        splitContainer.SplitterDistance = Math.Clamp(preferredLeftWidth, leftMin, maxDistance);
        splitContainer.Panel1MinSize = leftMin;
        splitContainer.Panel2MinSize = rightMin;
    }

    private static string FormatTier(string tier) => tier switch
    {
        "Diamond" => "💎 Diamond",
        "VIP" => "⭐ VIP",
        _ => "🎫 Standard"
    };
}
