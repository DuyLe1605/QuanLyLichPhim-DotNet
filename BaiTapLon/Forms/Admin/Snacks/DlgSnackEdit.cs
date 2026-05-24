using BaiTapLon.Models;

namespace BaiTapLon.Forms.Admin;

public class DlgSnackEdit : Form
{
    private TextBox txtName = null!;
    private NumericUpDown nudPrice = null!;
    private ComboBox cboCategory = null!;
    private CheckBox chkActive = null!;
    private PictureBox picImage = null!;
    private ErrorProvider errorProvider = null!;

    private readonly Snack? _editSnack;
    private string? _imagePath;

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
        ClientSize = new Size(520, 420);
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

        // Content: left form + right image
        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58F));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42F));
        root.Controls.Add(content, 0, 0);

        // Left: form fields
        var form = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 14, 0)
        };
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F));
        form.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        for (int i = 0; i < 4; i++) form.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
        content.Controls.Add(form, 0, 0);

        txtName = MakeTextBox();
        AddFormRow(form, "Tên món *", txtName, 0);

        nudPrice = new NumericUpDown
        {
            Dock = DockStyle.Fill,
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
            Dock = DockStyle.Fill,
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
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 0)
        };
        AddFormRow(form, "Trạng thái", chkActive, 3);

        // Right: image panel
        var imagePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        imagePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28F));
        imagePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        imagePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        content.Controls.Add(imagePanel, 1, 0);

        imagePanel.Controls.Add(new Label
        {
            Text = "Ảnh món",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 160, 185),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        picImage = new PictureBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(35, 35, 55),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 4, 0, 4)
        };
        imagePanel.Controls.Add(picImage, 0, 1);

        var btnChooseImage = new Button
        {
            Text = "📷 Chọn ảnh",
            Font = new Font("Segoe UI", 9),
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(50, 50, 75),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 0, 0)
        };
        btnChooseImage.FlatAppearance.BorderSize = 0;
        btnChooseImage.Click += BtnChooseImage_Click;
        imagePanel.Controls.Add(btnChooseImage, 0, 2);

        // Button bar
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

        _imagePath = _editSnack.ImagePath;
        if (!string.IsNullOrWhiteSpace(_imagePath))
        {
            var img = LoadSnackImage(_imagePath);
            if (img != null)
                picImage.Image = img;
        }
    }

    private void BtnChooseImage_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Ảnh|*.jpg;*.jpeg;*.png;*.bmp;*.webp",
            Title = "Chọn ảnh món bắp nước"
        };

        if (ofd.ShowDialog() != DialogResult.OK) return;

        _imagePath = CopySnackImageToResources(ofd.FileName);
        picImage.Image?.Dispose();
        picImage.Image = LoadSnackImage(_imagePath);
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
            IsActive = chkActive.Checked,
            ImagePath = _imagePath
        };
    }

    private static string CopySnackImageToResources(string sourcePath)
    {
        var targetDir = Path.Combine(Application.StartupPath, "Resources", "Snacks");
        Directory.CreateDirectory(targetDir);

        var originalName = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var safeName = string.Join("_", originalName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "snack";

        var fileName = $"{safeName}_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
        var targetPath = Path.Combine(targetDir, fileName);
        File.Copy(sourcePath, targetPath, overwrite: true);
        return fileName;
    }

    private static Image? LoadSnackImage(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath)) return null;

        var path = Path.Combine(Application.StartupPath, "Resources", "Snacks", imagePath);
        if (!File.Exists(path)) return null;

        try
        {
            using var source = Image.FromFile(path);
            return new Bitmap(source);
        }
        catch
        {
            return null;
        }
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
