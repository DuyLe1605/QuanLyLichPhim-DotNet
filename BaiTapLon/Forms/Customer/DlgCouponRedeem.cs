using BaiTapLon.Services;

namespace BaiTapLon.Forms.Customer;

public class DlgCouponRedeem : Form
{
    private readonly int _customerId;
    private readonly TextBox txtCode = new();
    private readonly Label lblError = new();
    private readonly Button btnConfirm = new();
    private readonly Button btnCancel = new();

    public DlgCouponRedeem(int customerId)
    {
        _customerId = customerId;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        Text = "Nhập mã thưởng";
        Size = new Size(420, 280);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        BackColor = CustomerUi.PanelBg;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 6,
            ColumnCount = 1,
            Padding = new Padding(28, 20, 28, 20),
            BackColor = Color.Transparent
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42)); // title
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36)); // subtitle
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44)); // textbox
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28)); // error label
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48)); // buttons
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // spacer
        Controls.Add(layout);

        // Title
        var lblTitle = new Label
        {
            Text = "🎁 Nhập mã thưởng",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(lblTitle, 0, 0);

        // Subtitle
        var lblSubtitle = new Label
        {
            Text = "Nhập mã để nhận điểm thưởng vào tài khoản của bạn",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Muted,
            Font = new Font("Segoe UI", 9.5f),
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(lblSubtitle, 0, 1);

        // Code TextBox
        txtCode.Dock = DockStyle.Fill;
        txtCode.BackColor = Color.FromArgb(35, 40, 56);
        txtCode.ForeColor = CustomerUi.Text;
        txtCode.BorderStyle = BorderStyle.FixedSingle;
        txtCode.Font = new Font("Segoe UI", 12);
        txtCode.CharacterCasing = CharacterCasing.Upper;
        txtCode.PlaceholderText = "Nhập mã coupon...";
        layout.Controls.Add(txtCode, 0, 2);

        // Error label
        lblError.Dock = DockStyle.Fill;
        lblError.ForeColor = Color.FromArgb(255, 80, 80);
        lblError.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        lblError.TextAlign = ContentAlignment.MiddleLeft;
        lblError.Visible = false;
        layout.Controls.Add(lblError, 0, 3);

        // Buttons row
        var btnPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = Color.Transparent,
            WrapContents = false
        };

        btnCancel.Text = "Hủy";
        btnCancel.Width = 90;
        btnCancel.Height = 40;
        btnCancel.BackColor = Color.FromArgb(55, 60, 82);
        btnCancel.ForeColor = CustomerUi.Text;
        btnCancel.FlatStyle = FlatStyle.Flat;
        btnCancel.FlatAppearance.BorderSize = 0;
        btnCancel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        btnCancel.Cursor = Cursors.Hand;
        btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        btnPanel.Controls.Add(btnCancel);

        btnConfirm.Text = "Xác nhận";
        btnConfirm.Width = 110;
        btnConfirm.Height = 40;
        btnConfirm.BackColor = CustomerUi.Accent;
        btnConfirm.ForeColor = Color.White;
        btnConfirm.FlatStyle = FlatStyle.Flat;
        btnConfirm.FlatAppearance.BorderSize = 0;
        btnConfirm.FlatAppearance.MouseOverBackColor = Color.FromArgb(75, 200, 125);
        btnConfirm.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        btnConfirm.Cursor = Cursors.Hand;
        btnConfirm.Click += async (s, e) => await OnConfirmAsync();
        btnPanel.Controls.Add(btnConfirm);

        layout.Controls.Add(btnPanel, 0, 4);

        // Allow Enter key to confirm
        AcceptButton = btnConfirm;
        CancelButton = btnCancel;
    }

    private async Task OnConfirmAsync()
    {
        var code = txtCode.Text.Trim();
        if (string.IsNullOrEmpty(code))
        {
            ShowError("Vui lòng nhập mã coupon.");
            return;
        }

        btnConfirm.Enabled = false;
        lblError.Visible = false;

        try
        {
            using var context = Program.CreateDbContext();
            var result = await new CouponService(context).RedeemAsync(_customerId, code);

            if (result.Success)
            {
                MessageBox.Show(
                    $"Thành công! Bạn nhận được {result.PointsAwarded:N0} điểm.",
                    "Thành công",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                ShowError(result.Message);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Lỗi: {ex.Message}");
        }
        finally
        {
            btnConfirm.Enabled = true;
        }
    }

    private void ShowError(string message)
    {
        lblError.Text = message;
        lblError.Visible = true;
    }
}
