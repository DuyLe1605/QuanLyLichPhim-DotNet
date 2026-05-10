using BaiTapLon.Services;
using BaiTapLon.Models;

namespace BaiTapLon.Forms.Admin.Coupons;

public class DlgCouponEdit : Form
{
    private readonly Coupon? _coupon;
    private readonly bool _isEditMode;

    private TextBox txtCode = null!;
    private NumericUpDown nudPoints = null!;
    private DateTimePicker dtpExpiration = null!;
    private NumericUpDown nudMaxRedemptions = null!;
    private CheckBox chkActive = null!;

    public DlgCouponEdit(Coupon? coupon = null)
    {
        _coupon = coupon;
        _isEditMode = coupon != null;
        InitializeComponent();
        if (_isEditMode) LoadEditData();
    }

    private void InitializeComponent()
    {
        this.Text = _isEditMode ? "Sửa Coupon" : "Tạo Coupon Mới";
        this.ClientSize = new Size(450, 380);
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
            RowCount = 2,
            BackColor = Color.Transparent,
            Padding = new Padding(24, 20, 24, 12),
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        this.Controls.Add(root);

        // Form fields grid
        var formGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = _isEditMode ? 5 : 4,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160F));
        formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < (_isEditMode ? 5 : 4); i++)
            formGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));
        root.Controls.Add(formGrid, 0, 0);

        // Code
        txtCode = new TextBox
        {
            Font = AdminTheme.BodyFont,
            Dock = DockStyle.Fill,
            BackColor = AdminTheme.InputBack,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            CharacterCasing = CharacterCasing.Upper,
            Margin = new Padding(0, 8, 0, 8),
            Enabled = !_isEditMode
        };
        AddFormRow(formGrid, "Mã coupon *", txtCode, 0);

        // Points Awarded
        nudPoints = new NumericUpDown
        {
            Font = AdminTheme.BodyFont,
            Dock = DockStyle.Fill,
            BackColor = AdminTheme.InputBack,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Minimum = 1,
            Maximum = 100000,
            Value = 100,
            Margin = new Padding(0, 8, 0, 8)
        };
        AddFormRow(formGrid, "Điểm thưởng *", nudPoints, 1);

        // Expiration Date
        dtpExpiration = new DateTimePicker
        {
            Font = AdminTheme.BodyFont,
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Short,
            MinDate = DateTime.Today.AddDays(1),
            Value = DateTime.Today.AddDays(30),
            Margin = new Padding(0, 8, 0, 8)
        };
        AddFormRow(formGrid, "Ngày hết hạn *", dtpExpiration, 2);

        // Max Redemptions
        nudMaxRedemptions = new NumericUpDown
        {
            Font = AdminTheme.BodyFont,
            Dock = DockStyle.Fill,
            BackColor = AdminTheme.InputBack,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Minimum = 1,
            Maximum = 10000,
            Value = 100,
            Margin = new Padding(0, 8, 0, 8)
        };
        AddFormRow(formGrid, "Lượt tối đa *", nudMaxRedemptions, 3);

        // Active checkbox (edit mode only)
        if (_isEditMode)
        {
            chkActive = new CheckBox
            {
                Text = "Đang hoạt động",
                Font = AdminTheme.BodyFont,
                ForeColor = AdminTheme.Text,
                Dock = DockStyle.Fill,
                Checked = true,
                Margin = new Padding(0, 12, 0, 0)
            };
            AddFormRow(formGrid, "Trạng thái", chkActive, 4);
        }

        // Button bar
        var buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };
        root.Controls.Add(buttonBar, 0, 1);

        var btnCancel = AdminControls.CreateButton("✕ Hủy", AdminTheme.ButtonNeutral, 100, (s, e) =>
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        });
        btnCancel.Height = 38;

        var btnSave = AdminControls.CreateButton("💾 Lưu", AdminTheme.ButtonSuccess, 110, BtnSave_Click);
        btnSave.Height = 38;

        buttonBar.Controls.Add(btnCancel);
        buttonBar.Controls.Add(btnSave);

        this.AcceptButton = btnSave;
        this.CancelButton = btnCancel;
    }

    private void LoadEditData()
    {
        if (_coupon == null) return;
        txtCode.Text = _coupon.Code;
        nudPoints.Value = Math.Clamp(_coupon.PointsAwarded, 1, 100000);
        dtpExpiration.Value = _coupon.ExpirationDate > DateTime.Today.AddDays(1)
            ? _coupon.ExpirationDate
            : DateTime.Today.AddDays(1);
        nudMaxRedemptions.Value = Math.Clamp(_coupon.MaxRedemptions, 1, 10000);
        if (chkActive != null)
            chkActive.Checked = _coupon.IsActive;
    }

    private async void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtCode.Text))
        {
            MessageBox.Show("Vui lòng nhập mã coupon.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using var ctx = Program.CreateDbContext();
            var svc = new CouponService(ctx);

            if (_isEditMode && _coupon != null)
            {
                bool? isActive = chkActive?.Checked;
                var (success, message) = await svc.UpdateAsync(
                    _coupon.Id,
                    dtpExpiration.Value.Date,
                    (int)nudMaxRedemptions.Value,
                    isActive);

                if (!success)
                {
                    MessageBox.Show(message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                var (success, message, _) = await svc.CreateAsync(
                    txtCode.Text.Trim().ToUpper(),
                    (int)nudPoints.Value,
                    dtpExpiration.Value.Date,
                    (int)nudMaxRedemptions.Value);

                if (!success)
                {
                    MessageBox.Show(message, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static void AddFormRow(TableLayoutPanel table, string labelText, Control editor, int row)
    {
        var lbl = new Label
        {
            Text = labelText,
            Font = AdminTheme.BodyFont,
            ForeColor = AdminTheme.MutedText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 10, 0)
        };
        table.Controls.Add(lbl, 0, row);
        table.Controls.Add(editor, 1, row);
    }
}
