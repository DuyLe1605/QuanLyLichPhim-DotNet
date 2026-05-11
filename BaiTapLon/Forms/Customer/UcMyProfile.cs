using BaiTapLon.Helpers;
using BaiTapLon.Models;
using BaiTapLon.Services;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Forms.Customer;

public class UcMyProfile : UserControl
{
    private readonly TextBox txtUsername = new() { ReadOnly = true };
    private readonly TextBox txtFullName = new();
    private readonly TextBox txtPhone = new();
    private readonly TextBox txtCurrentPassword = new();
    private readonly TextBox txtNewPassword = new();
    private readonly Label lblMember = new();
    private readonly Label lblTier = new();
    private readonly Label lblPoints = new();
    private readonly ProgressBar progress = new();
    private readonly PictureBox picQr = new();
    private readonly FlowLayoutPanel flpTransactions = new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        MaximumSize = new Size(0, 280)
    };
    private BaiTapLon.Models.Customer? customer;

    public UcMyProfile()
    {
        InitializeComponent();
        Load += async (s, e) => await LoadProfileAsync();
    }

    private void InitializeComponent()
    {
        BackColor = CustomerUi.AppBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(28),
            BackColor = Color.Transparent
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        Controls.Add(root);

        root.Controls.Add(CreateFormPanel(), 0, 0);
        root.Controls.Add(CreateMemberPanel(), 1, 0);
    }

    private Control CreateFormPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 10,
            ColumnCount = 1,
            BackColor = CustomerUi.PanelBg,
            Padding = new Padding(24),
            MinimumSize = new Size(280, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(new Label
        {
            Text = "Hồ sơ cá nhân",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 21, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        panel.Controls.Add(Field("Tên đăng nhập", txtUsername), 0, 1);
        panel.Controls.Add(Field("Họ tên", txtFullName), 0, 2);
        panel.Controls.Add(Field("Số điện thoại", txtPhone), 0, 3);

        var save = CustomerUi.PrimaryButton("LƯU THÔNG TIN");
        save.Dock = DockStyle.Left;
        save.Width = 190;
        save.Click += async (s, e) => await SaveProfileAsync();
        panel.Controls.Add(save, 0, 4);

        panel.Controls.Add(new Label
        {
            Text = "Đổi mật khẩu",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 5);

        txtCurrentPassword.UseSystemPasswordChar = true;
        txtNewPassword.UseSystemPasswordChar = true;
        panel.Controls.Add(Field("Mật khẩu hiện tại", txtCurrentPassword), 0, 6);
        panel.Controls.Add(Field("Mật khẩu mới", txtNewPassword), 0, 7);

        var change = CustomerUi.PrimaryButton("ĐỔI MẬT KHẨU");
        change.Dock = DockStyle.Left;
        change.Width = 190;
        change.BackColor = CustomerUi.AccentBlue;
        change.Click += async (s, e) => await ChangePasswordAsync();
        panel.Controls.Add(change, 0, 8);

        return panel;
    }

    private Control CreateMemberPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 10,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(28, 0, 0, 0)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 250));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 45));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(new Label
        {
            Text = "Thẻ thành viên",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 21, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        picQr.Dock = DockStyle.Fill;
        picQr.BackColor = Color.White;
        picQr.SizeMode = PictureBoxSizeMode.CenterImage;
        panel.Controls.Add(picQr, 0, 1);

        foreach (var label in new[] { lblMember, lblTier, lblPoints })
        {
            label.Dock = DockStyle.Fill;
            label.ForeColor = CustomerUi.Text;
            label.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            label.TextAlign = ContentAlignment.MiddleLeft;
        }
        lblTier.ForeColor = CustomerUi.Accent;
        panel.Controls.Add(lblMember, 0, 2);
        panel.Controls.Add(lblTier, 0, 3);
        panel.Controls.Add(lblPoints, 0, 4);

        progress.Dock = DockStyle.Fill;
        progress.Maximum = 100;
        panel.Controls.Add(progress, 0, 5);

        var btnRedeem = CustomerUi.PrimaryButton("🎁 Nhập mã thưởng");
        btnRedeem.Dock = DockStyle.Fill;
        btnRedeem.BackColor = Color.FromArgb(130, 110, 255);
        btnRedeem.FlatAppearance.MouseOverBackColor = Color.FromArgb(150, 130, 255);
        btnRedeem.Click += async (s, e) =>
        {
            if (customer == null) return;
            using var dlg = new DlgCouponRedeem(customer.Id);
            if (dlg.ShowDialog(this) == DialogResult.OK)
                await LoadProfileAsync();
        };
        panel.Controls.Add(btnRedeem, 0, 6);

        panel.Controls.Add(new Label
        {
            Text = "Lịch sử điểm thưởng",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 7);

        panel.Controls.Add(flpTransactions, 0, 8);

        return panel;
    }

    private static Control Field(string label, TextBox textBox)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            Text = label,
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Muted,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 0);
        textBox.Dock = DockStyle.Top;
        textBox.MinimumSize = new Size(280, 0);
        textBox.BackColor = Color.FromArgb(35, 40, 56);
        textBox.ForeColor = CustomerUi.Text;
        textBox.BorderStyle = BorderStyle.FixedSingle;
        textBox.Font = new Font("Segoe UI", 12);
        panel.Controls.Add(textBox, 0, 1);
        return panel;
    }

    private async Task LoadProfileAsync()
    {
        var current = SessionManager.CurrentCustomer;
        if (current == null) return;

        using var context = Program.CreateDbContext();
        try
        {
            customer = await new CustomerService(context).GetByIdAsync(current.Id);
            if (customer == null) return;

            ApplyCustomerToUi(customer);

            var transactions = await context.PointTransactions
                .Where(pt => pt.CustomerId == customer.Id)
                .OrderByDescending(pt => pt.CreatedAt)
                .Take(10)
                .AsNoTracking()
                .ToListAsync();

            RenderTransactions(transactions);
        }
        catch
        {
            flpTransactions.Controls.Clear();
            flpTransactions.Controls.Add(new Label
            {
                Text = "Không thể tải lịch sử điểm thưởng.",
                ForeColor = CustomerUi.Muted,
                Font = new Font("Segoe UI", 10),
                AutoSize = true,
                Padding = new Padding(4)
            });
        }
    }

    /// <summary>
    /// Applies customer data to the UI fields. Extracted for testability.
    /// </summary>
    internal void ApplyCustomerToUi(BaiTapLon.Models.Customer c)
    {
        txtFullName.Text = c.FullName;
        txtPhone.Text = c.Phone;
        txtUsername.Text = c.Username;
        lblMember.Text = $"Mã thành viên: {c.MemberCode}";
        lblTier.Text = $"{TierIcon(c.Tier)} Hạng: {c.Tier}";
        lblPoints.Text = $"Điểm thưởng: {c.LoyaltyPoints:N0} | Điểm TV: {c.MembershipPoints:N0}";
        picQr.Image = BarcodeHelper.GenerateQrCode(c.MemberCode, 220, 220);

        var next = c.Tier switch
        {
            "Diamond" => c.TotalSpent,
            "VIP" => 10_000_000m,
            _ => 2_000_000m
        };
        progress.Value = c.Tier == "Diamond"
            ? 100
            : (int)Math.Clamp(c.TotalSpent / next * 100, 0, 100);
    }

    private async Task SaveProfileAsync()
    {
        if (customer == null) return;
        using var context = Program.CreateDbContext();
        var result = await new CustomerService(context).UpdateProfileAsync(
            customer.Id,
            txtFullName.Text.Trim(),
            txtPhone.Text.Trim());

        MessageBox.Show(result.Message, result.Success ? "Thành công" : "Lỗi",
            MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);

        if (result.Success)
            await LoadProfileAsync();
    }

    private async Task ChangePasswordAsync()
    {
        if (customer == null) return;
        if (txtNewPassword.Text.Length < 6)
        {
            MessageBox.Show("Mật khẩu mới tối thiểu 6 ký tự.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var context = Program.CreateDbContext();
        var result = await new CustomerService(context).ChangePasswordAsync(
            customer.Id,
            txtCurrentPassword.Text,
            txtNewPassword.Text);

        MessageBox.Show(result.Message, result.Success ? "Thành công" : "Lỗi",
            MessageBoxButtons.OK,
            result.Success ? MessageBoxIcon.Information : MessageBoxIcon.Error);

        if (result.Success)
        {
            txtCurrentPassword.Clear();
            txtNewPassword.Clear();
        }
    }

    public static (string text, Color color) FormatPointChange(int points)
    {
        return points >= 0
            ? ($"+{points:N0}", Color.FromArgb(80, 200, 120))
            : ($"-{Math.Abs(points):N0}", Color.FromArgb(220, 80, 80));
    }

    private void RenderTransactions(IEnumerable<PointTransaction> transactions)
    {
        flpTransactions.Controls.Clear();

        foreach (var tx in transactions)
        {
            var row = new Panel
            {
                Height = 28,
                Dock = DockStyle.Top,
                BackColor = Color.Transparent
            };

            var (pointText, pointColor) = FormatPointChange(tx.Points);

            var lblLeft = new Label
            {
                Text = $"{tx.CreatedAt:dd/MM/yy} — {tx.Description}",
                ForeColor = CustomerUi.Muted,
                Font = new Font("Segoe UI", 9),
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var lblRight = new Label
            {
                Text = pointText,
                ForeColor = pointColor,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = false,
                Width = 70,
                Dock = DockStyle.Right,
                TextAlign = ContentAlignment.MiddleRight
            };

            row.Controls.Add(lblLeft);
            row.Controls.Add(lblRight);
            flpTransactions.Controls.Add(row);
        }
    }

    private static string TierIcon(string tier) => tier switch
    {
        "Diamond" => "💎",
        "VIP" => "⭐",
        _ => "🎫"
    };
}
