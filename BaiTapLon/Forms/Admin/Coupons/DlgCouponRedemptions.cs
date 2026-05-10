using BaiTapLon.Services;

namespace BaiTapLon.Forms.Admin.Coupons;

public class DlgCouponRedemptions : Form
{
    private readonly int _couponId;
    private readonly string _couponCode;
    private DataGridView dgv = null!;

    public DlgCouponRedemptions(int couponId, string couponCode)
    {
        _couponId = couponId;
        _couponCode = couponCode;
        InitializeComponent();
        this.Load += async (s, e) => await LoadAsync();
    }

    private void InitializeComponent()
    {
        this.Text = $"Lượt đổi coupon: {_couponCode}";
        this.ClientSize = new Size(600, 450);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = AdminTheme.PageBack;
        this.ForeColor = AdminTheme.Text;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(16, 14, 16, 12),
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        this.Controls.Add(root);

        // Title label
        var lblTitle = new Label
        {
            Text = $"📋 Lịch sử đổi mã: {_couponCode}",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = AdminTheme.TitleText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };
        root.Controls.Add(lblTitle, 0, 0);

        // DataGridView
        dgv = AdminControls.CreateGrid();
        root.Controls.Add(dgv, 0, 1);

        // Close button
        var buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };
        root.Controls.Add(buttonBar, 0, 2);

        var btnClose = AdminControls.CreateButton("✕ Đóng", AdminTheme.ButtonNeutral, 100, (s, e) => this.Close());
        btnClose.Height = 36;
        buttonBar.Controls.Add(btnClose);

        this.CancelButton = btnClose;
    }

    private async Task LoadAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var svc = new CouponService(ctx);
            var redemptions = await svc.GetRedemptionsAsync(_couponId);

            dgv.DataSource = null;
            dgv.Columns.Clear();
            dgv.DataSource = redemptions.Select(r => new
            {
                KhácHàng = r.Customer?.FullName ?? "(Không rõ)",
                Email = r.Customer?.Email ?? "",
                ThờiGian = r.RedeemedAt.ToString("dd/MM/yyyy HH:mm")
            }).ToList();

            AdminControls.SetColumnWidths(dgv,
                ("KhácHàng", 200),
                ("Email", 220),
                ("ThờiGian", 140));
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
