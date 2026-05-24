using BaiTapLon.Services;
using BaiTapLon.Models;

namespace BaiTapLon.Forms.Admin.Vouchers;

public class UcVoucherManagement : UserControl
{
    private DataGridView dgv = null!;
    private AdminPaginationBar pagination = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboStatus = null!;
    private List<Voucher> _vouchers = new();

    public UcVoucherManagement()
    {
        InitUI();
        this.Load += async (s, e) => await LoadAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        txtSearch = AdminControls.CreateSearchBox("Tìm mã voucher...", 220);
        txtSearch.TextChanged += async (s, e) => await LoadAsync();

        cboStatus = AdminControls.CreateComboBox(160);
        cboStatus.Items.AddRange(new object[] { "Tất cả", "Đang hoạt động", "Đã tắt" });
        cboStatus.SelectedIndex = 0;
        cboStatus.SelectedIndexChanged += async (s, e) => await LoadAsync();

        dgv = AdminControls.CreateGrid();
        dgv.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) OpenEdit(); };

        pagination = new AdminPaginationBar();
        pagination.PaginationChanged += (s, e) => BindPage();

        var toolbar = AdminControls.CreateToolbar(
            txtSearch, cboStatus,
            AdminControls.CreateButton("➕ Tạo mới", AdminTheme.ButtonSuccess, 120, BtnCreate_Click),
            AdminControls.CreateButton("✏️ Sửa", AdminTheme.ButtonPrimary, 100, (s, e) => OpenEdit()),
            AdminControls.CreateButton("🗑️ Xóa", AdminTheme.ButtonDanger, 100, BtnDelete_Click));

        Controls.Add(AdminLayouts.CreateManagementPage(
            "🏷️  Quản Lý Voucher",
            toolbar,
            AdminLayouts.CreatePagedGridContent(dgv, pagination)));
    }

    private async Task LoadAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var svc = new VoucherService(ctx);
            bool? active = cboStatus.SelectedIndex switch { 1 => true, 2 => false, _ => null };
            string? kw = string.IsNullOrWhiteSpace(txtSearch.Text) ? null : txtSearch.Text.Trim();
            _vouchers = await svc.GetAllAsync(active, kw);
            pagination.SetTotalItems(_vouchers.Count, resetPage: true);
            BindPage();
        }
        catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi"); }
    }

    private void BindPage()
    {
        dgv.DataSource = null;
        dgv.Columns.Clear();
        dgv.DataSource = _vouchers
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(v => new
            {
                v.Id,
                Mã = v.Code,
                Loại = v.Type switch { "Percent" => "Phần trăm", "Fixed" => "Cố định", "FreeTicket" => "Miễn phí vé", _ => v.Type },
                GiáTrị = v.Type == "Percent" ? $"{v.Value}%" : $"{v.Value:N0} đ",
                GiảmTốiĐa = v.MaxDiscount.HasValue ? $"{v.MaxDiscount:N0} đ" : "—",
                BắtĐầu = v.StartDate.ToString("dd/MM/yyyy"),
                KếtThúc = v.EndDate.ToString("dd/MM/yyyy"),
                ĐãDùng = $"{v.UsedCount}/{v.MaxUses}",
                TrạngThái = v.IsActive ? (v.EndDate < DateTime.Now ? "⏰ Hết hạn" : "✅ Hoạt động") : "❌ Tắt"
            }).ToList();

        AdminControls.HideColumn(dgv, "Id");
        AdminControls.SetColumnWidths(dgv,
            ("Mã", 130), ("Loại", 110), ("GiáTrị", 100),
            ("GiảmTốiĐa", 110), ("BắtĐầu", 110), ("KếtThúc", 110),
            ("ĐãDùng", 90), ("TrạngThái", 120));
    }

    private void BtnCreate_Click(object? s, EventArgs e)
    {
        using var dlg = new DlgVoucherEdit();
        if (dlg.ShowDialog(this) == DialogResult.OK) _ = LoadAsync();
    }

    private void OpenEdit()
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (!id.HasValue) return;
        var v = _vouchers.FirstOrDefault(x => x.Id == id.Value);
        if (v == null) return;
        using var dlg = new DlgVoucherEdit(v);
        if (dlg.ShowDialog(this) == DialogResult.OK) _ = LoadAsync();
    }

    private async void BtnDelete_Click(object? s, EventArgs e)
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (!id.HasValue) return;
        if (MessageBox.Show("Xóa voucher này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        using var ctx = Program.CreateDbContext();
        var (ok, msg) = await new VoucherService(ctx).DeleteAsync(id.Value);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadAsync();
    }
}

// ==================== DlgVoucherEdit ====================
public class DlgVoucherEdit : Form
{
    private readonly Voucher? _existing;
    private TextBox txtCode = null!;
    private ComboBox cboType = null!;
    private NumericUpDown nudValue = null!, nudMaxDiscount = null!, nudMaxUses = null!;
    private DateTimePicker dtpStart = null!, dtpEnd = null!;
    private CheckBox chkActive = null!;

    public DlgVoucherEdit(Voucher? existing = null)
    {
        _existing = existing;
        InitializeComponent();
        if (existing != null) LoadData();
    }

    private void InitializeComponent()
    {
        Text = _existing != null ? "Sửa Voucher" : "Tạo Voucher Mới";
        ClientSize = new Size(480, 480);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false; MinimizeBox = false;
        BackColor = AdminTheme.PageBack;
        ForeColor = AdminTheme.Text;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2,
            BackColor = Color.Transparent, Padding = new Padding(24, 20, 24, 12)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));
        Controls.Add(root);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 8,
            BackColor = Color.Transparent
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 8; i++) grid.RowStyles.Add(new RowStyle(SizeType.Absolute, 46F));
        root.Controls.Add(grid, 0, 0);

        int row = 0;
        txtCode = MakeTxt(); txtCode.CharacterCasing = CharacterCasing.Upper;
        txtCode.Enabled = _existing == null;
        AddRow(grid, "Mã voucher *", txtCode, row++);

        cboType = new ComboBox
        {
            Font = AdminTheme.BodyFont, Dock = DockStyle.Fill,
            BackColor = AdminTheme.InputBack, ForeColor = Color.White,
            DropDownStyle = ComboBoxStyle.DropDownList, FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 6, 0, 6)
        };
        cboType.Items.AddRange(new[] { "Percent", "Fixed", "FreeTicket" });
        cboType.SelectedIndex = 0;
        AddRow(grid, "Loại *", cboType, row++);

        nudValue = MakeNud(0, 100000000, 10); AddRow(grid, "Giá trị *", nudValue, row++);
        nudMaxDiscount = MakeNud(0, 100000000, 0); AddRow(grid, "Giảm tối đa", nudMaxDiscount, row++);
        nudMaxUses = MakeNud(1, 100000, 100); AddRow(grid, "Lượt tối đa *", nudMaxUses, row++);

        dtpStart = new DateTimePicker
        {
            Font = AdminTheme.BodyFont, Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Short, Value = DateTime.Today,
            Margin = new Padding(0, 8, 0, 8)
        };
        AddRow(grid, "Ngày bắt đầu *", dtpStart, row++);

        dtpEnd = new DateTimePicker
        {
            Font = AdminTheme.BodyFont, Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Short, Value = DateTime.Today.AddDays(30),
            Margin = new Padding(0, 8, 0, 8)
        };
        AddRow(grid, "Ngày kết thúc *", dtpEnd, row++);

        chkActive = new CheckBox
        {
            Text = "Đang hoạt động", Font = AdminTheme.BodyFont,
            ForeColor = AdminTheme.Text, Dock = DockStyle.Fill, Checked = true,
            Margin = new Padding(0, 12, 0, 0)
        };
        AddRow(grid, "Trạng thái", chkActive, row++);

        // Buttons
        var bar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false, BackColor = Color.Transparent, Padding = new Padding(0, 8, 0, 0)
        };
        root.Controls.Add(bar, 0, 1);

        var btnCancel = AdminControls.CreateButton("✕ Hủy", AdminTheme.ButtonNeutral, 100, (s, e) => { DialogResult = DialogResult.Cancel; Close(); });
        btnCancel.Height = 38;
        var btnSave = AdminControls.CreateButton("💾 Lưu", AdminTheme.ButtonSuccess, 110, BtnSave_Click);
        btnSave.Height = 38;
        bar.Controls.Add(btnCancel);
        bar.Controls.Add(btnSave);
        AcceptButton = btnSave; CancelButton = btnCancel;
    }

    private void LoadData()
    {
        if (_existing == null) return;
        txtCode.Text = _existing.Code;
        cboType.SelectedItem = _existing.Type;
        nudValue.Value = Math.Clamp(_existing.Value, 0, 100000000);
        nudMaxDiscount.Value = _existing.MaxDiscount.HasValue ? Math.Clamp(_existing.MaxDiscount.Value, 0, 100000000) : 0;
        nudMaxUses.Value = Math.Clamp(_existing.MaxUses, 1, 100000);
        dtpStart.Value = _existing.StartDate;
        dtpEnd.Value = _existing.EndDate;
        chkActive.Checked = _existing.IsActive;
    }

    private async void BtnSave_Click(object? s, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtCode.Text)) { MessageBox.Show("Nhập mã voucher!"); return; }
        if (dtpEnd.Value <= dtpStart.Value) { MessageBox.Show("Ngày kết thúc phải sau ngày bắt đầu!"); return; }

        try
        {
            using var ctx = Program.CreateDbContext();
            var svc = new VoucherService(ctx);

            var voucher = _existing != null
                ? (await svc.GetByIdAsync(_existing.Id))!
                : new Voucher();

            voucher.Code = txtCode.Text.Trim().ToUpper();
            voucher.Type = cboType.SelectedItem?.ToString() ?? "Percent";
            voucher.Value = nudValue.Value;
            voucher.MaxDiscount = nudMaxDiscount.Value > 0 ? nudMaxDiscount.Value : null;
            voucher.MaxUses = (int)nudMaxUses.Value;
            voucher.StartDate = dtpStart.Value.Date;
            voucher.EndDate = dtpEnd.Value.Date;
            voucher.IsActive = chkActive.Checked;

            var (ok, msg) = _existing != null
                ? await svc.UpdateAsync(voucher)
                : (await svc.CreateAsync(voucher)).Let(r => (r.Ok, r.Msg));

            if (!ok) { MessageBox.Show(msg, "Lỗi"); return; }
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}"); }
    }

    private static TextBox MakeTxt() => new()
    {
        Font = AdminTheme.BodyFont, Dock = DockStyle.Fill,
        BackColor = AdminTheme.InputBack, ForeColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle, Margin = new Padding(0, 6, 0, 6)
    };

    private static NumericUpDown MakeNud(decimal min, decimal max, decimal val) => new()
    {
        Font = AdminTheme.BodyFont, Dock = DockStyle.Fill,
        BackColor = AdminTheme.InputBack, ForeColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle, Minimum = min, Maximum = max,
        Value = val, DecimalPlaces = 0, ThousandsSeparator = true,
        Margin = new Padding(0, 6, 0, 6)
    };

    private static void AddRow(TableLayoutPanel t, string label, Control c, int row)
    {
        t.Controls.Add(new Label
        {
            Text = label, Font = AdminTheme.BodyFont, ForeColor = AdminTheme.MutedText,
            Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Margin = new Padding(0, 0, 10, 0)
        }, 0, row);
        t.Controls.Add(c, 1, row);
    }
}

// Extension helper
internal static class TupleExtensions
{
    public static TResult Let<T, TResult>(this T value, Func<T, TResult> func) => func(value);
}
