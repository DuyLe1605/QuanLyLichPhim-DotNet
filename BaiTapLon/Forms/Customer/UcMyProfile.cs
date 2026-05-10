using BaiTapLon.Helpers;
using BaiTapLon.Models;
using BaiTapLon.Services;

namespace BaiTapLon.Forms.Customer;

public class UcMyProfile : UserControl
{
    private readonly TextBox txtFullName = new();
    private readonly TextBox txtPhone = new();
    private readonly TextBox txtCurrentPassword = new();
    private readonly TextBox txtNewPassword = new();
    private readonly Label lblMember = new();
    private readonly Label lblTier = new();
    private readonly Label lblPoints = new();
    private readonly ProgressBar progress = new();
    private readonly PictureBox picQr = new();
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
            RowCount = 9,
            ColumnCount = 1,
            BackColor = CustomerUi.PanelBg,
            Padding = new Padding(24)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
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

        panel.Controls.Add(Field("Họ tên", txtFullName), 0, 1);
        panel.Controls.Add(Field("Số điện thoại", txtPhone), 0, 2);

        var save = CustomerUi.PrimaryButton("LƯU THÔNG TIN");
        save.Dock = DockStyle.Left;
        save.Width = 190;
        save.Click += async (s, e) => await SaveProfileAsync();
        panel.Controls.Add(save, 0, 3);

        panel.Controls.Add(new Label
        {
            Text = "Đổi mật khẩu",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 4);

        txtCurrentPassword.UseSystemPasswordChar = true;
        txtNewPassword.UseSystemPasswordChar = true;
        panel.Controls.Add(Field("Mật khẩu hiện tại", txtCurrentPassword), 0, 5);
        panel.Controls.Add(Field("Mật khẩu mới", txtNewPassword), 0, 6);

        var change = CustomerUi.PrimaryButton("ĐỔI MẬT KHẨU");
        change.Dock = DockStyle.Left;
        change.Width = 190;
        change.BackColor = CustomerUi.AccentBlue;
        change.Click += async (s, e) => await ChangePasswordAsync();
        panel.Controls.Add(change, 0, 7);

        return panel;
    }

    private Control CreateMemberPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 7,
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
        customer = await new CustomerService(context).GetByIdAsync(current.Id);
        if (customer == null) return;

        txtFullName.Text = customer.FullName;
        txtPhone.Text = customer.Phone;
        lblMember.Text = $"Mã thành viên: {customer.MemberCode}";
        lblTier.Text = $"Hạng: {customer.Tier}";
        lblPoints.Text = $"Điểm hiện tại: {customer.TotalPoints:N0} | Tổng chi: {customer.TotalSpent:N0} đ";
        picQr.Image = BarcodeHelper.GenerateQrCode(customer.MemberCode, 220, 220);

        var next = customer.Tier switch
        {
            "Diamond" => customer.TotalSpent,
            "VIP" => 10_000_000m,
            _ => 2_000_000m
        };
        progress.Value = customer.Tier == "Diamond"
            ? 100
            : (int)Math.Clamp(customer.TotalSpent / next * 100, 0, 100);
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
}
