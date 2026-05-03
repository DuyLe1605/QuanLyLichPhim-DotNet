using BaiTapLon.Models;

namespace BaiTapLon.Forms.Admin;

public class DlgSnackEdit : Form
{
    private TextBox txtName = null!;
    private NumericUpDown nudPrice = null!;
    private ComboBox cboCategory = null!;
    private CheckBox chkActive = null!;
    private ErrorProvider errorProvider = null!;

    private readonly Snack? _editSnack;

    public Snack SnackData { get; private set; } = new();

    public DlgSnackEdit(Snack? editSnack)
    {
        _editSnack = editSnack;
        InitializeComponent();
        if (_editSnack != null) LoadEditData();
    }

    private void InitializeComponent()
    {
        Text = _editSnack == null ? "Thêm món bắp nước" : "Sửa món bắp nước";
        ClientSize = new Size(460, 300);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(24, 24, 40);
        ForeColor = Color.FromArgb(220, 220, 240);

        errorProvider = new ErrorProvider
        {
            ContainerControl = this,
            BlinkStyle = ErrorBlinkStyle.NeverBlink
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(18),
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
        Controls.Add(root);

        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 4,
            Height = 190,
            BackColor = Color.Transparent
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 4; i++) form.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        root.Controls.Add(form, 0, 0);

        txtName = MakeTextBox();
        AddFormRow(form, "Tên món *", txtName, 0);

        nudPrice = new NumericUpDown
        {
            Dock = DockStyle.Left,
            Width = 160,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Minimum = 1000,
            Maximum = 5000000,
            Increment = 1000,
            Value = 29000,
            ThousandsSeparator = true,
            Margin = new Padding(0, 6, 0, 6)
        };
        AddFormRow(form, "Giá *", nudPrice, 1);

        cboCategory = new ComboBox
        {
            Dock = DockStyle.Left,
            Width = 170,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 6, 0, 6)
        };
        cboCategory.Items.AddRange(["Food", "Drink", "Combo"]);
        cboCategory.SelectedIndex = 0;
        AddFormRow(form, "Phân loại *", cboCategory, 2);

        chkActive = new CheckBox
        {
            Text = "Đang bán",
            Checked = true,
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(220, 220, 240),
            Dock = DockStyle.Left,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };
        AddFormRow(form, "Trạng thái", chkActive, 3);

        var buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 10, 0, 0),
            BackColor = Color.Transparent
        };
        root.Controls.Add(buttonBar, 0, 1);

        var btnSave = MakeButton(_editSnack == null ? "Thêm món" : "Lưu", Color.FromArgb(80, 160, 80));
        btnSave.DialogResult = DialogResult.OK;
        btnSave.Click += BtnSave_Click;

        var btnCancel = MakeButton("Hủy", Color.FromArgb(50, 50, 75));
        btnCancel.DialogResult = DialogResult.Cancel;

        buttonBar.Controls.Add(btnCancel);
        buttonBar.Controls.Add(btnSave);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private void LoadEditData()
    {
        if (_editSnack == null) return;

        txtName.Text = _editSnack.Name;
        nudPrice.Value = Math.Max(nudPrice.Minimum, Math.Min(nudPrice.Maximum, _editSnack.Price));
        cboCategory.SelectedItem = _editSnack.Category;
        chkActive.Checked = _editSnack.IsActive;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        errorProvider.Clear();
        var hasError = false;

        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            errorProvider.SetError(txtName, "Nhập tên món");
            hasError = true;
        }

        if (nudPrice.Value <= 0)
        {
            errorProvider.SetError(nudPrice, "Giá phải lớn hơn 0");
            hasError = true;
        }

        if (hasError)
        {
            DialogResult = DialogResult.None;
            return;
        }

        SnackData = new Snack
        {
            Id = _editSnack?.Id ?? 0,
            Name = txtName.Text.Trim(),
            Price = nudPrice.Value,
            Category = cboCategory.SelectedItem?.ToString() ?? "Food",
            IsActive = chkActive.Checked
        };
    }

    private static void AddFormRow(TableLayoutPanel table, string label, Control editor, int row)
    {
        table.Controls.Add(new Label
        {
            Text = label,
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(160, 160, 185),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 10, 0)
        }, 0, row);
        table.Controls.Add(editor, 1, row);
    }

    private static TextBox MakeTextBox() => new()
    {
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 10),
        BackColor = Color.FromArgb(35, 35, 55),
        ForeColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(0, 6, 0, 6)
    };

    private static Button MakeButton(string text, Color backColor)
    {
        var button = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(110, 38),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(8, 0, 0, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        return button;
    }
}
