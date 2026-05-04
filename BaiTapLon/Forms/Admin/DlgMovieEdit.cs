using BaiTapLon.Models;

namespace BaiTapLon.Forms.Admin;

/// <summary>
/// Dialog thêm/sửa phim — layout 2 cột, có Mã phim.
/// </summary>
public class DlgMovieEdit : Form
{
    private TextBox txtCode = null!;
    private TextBox txtTitle = null!;
    private TextBox txtDirector = null!;
    private TextBox txtActors = null!;
    private NumericUpDown nudDuration = null!;
    private ComboBox cboAgeRating = null!;
    private TextBox txtDescription = null!;
    private TextBox txtTrailer = null!;
    private DateTimePicker dtpRelease = null!;
    private CheckedListBox clbGenres = null!;
    private PictureBox picPoster = null!;
    private ErrorProvider errorProvider = null!;
    private byte[]? _posterData;
    private string? _posterPath;

    public Movie MovieData { get; private set; } = new();
    public List<int> SelectedGenreIds { get; private set; } = new();

    private readonly Movie? _editMovie;
    private readonly List<Genre> _genres;

    public DlgMovieEdit(Movie? editMovie, List<Genre> genres)
    {
        _editMovie = editMovie;
        _genres = genres;
        InitializeComponent();
        if (_editMovie != null) LoadEditData();
    }

    private void InitializeComponent()
    {
        this.Text = _editMovie == null ? "Thêm phim mới" : "Sửa thông tin phim";
        this.ClientSize = new Size(760, 620);
        this.MinimumSize = new Size(720, 560);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(24, 24, 40);
        this.ForeColor = Color.FromArgb(200, 200, 220);

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
            BackColor = Color.Transparent,
            Padding = new Padding(18),
            Margin = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
        this.Controls.Add(root);

        var content = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
        root.Controls.Add(content, 0, 0);

        var formGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 9,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 18, 0),
            Padding = Padding.Empty
        };
        formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 34F));
        formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 66F));
        for (int i = 0; i < 8; i++)
            formGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        formGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        content.Controls.Add(formGrid, 0, 0);

        txtCode = MakeTextBox();
        txtCode.CharacterCasing = CharacterCasing.Upper;
        AddFormRow(formGrid, "Mã phim *", txtCode, 0);

        txtTitle = MakeTextBox();
        AddFormRow(formGrid, "Tên phim *", txtTitle, 1);

        txtDirector = MakeTextBox();
        AddFormRow(formGrid, "Đạo diễn", txtDirector, 2);

        txtActors = MakeTextBox();
        AddFormRow(formGrid, "Diễn viên", txtActors, 3);

        nudDuration = new NumericUpDown
        {
            Font = new Font("Segoe UI", 10),
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            Minimum = 1,
            Maximum = 500,
            Value = 120,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 6, 0, 6)
        };
        AddFormRow(formGrid, "Thời lượng (phút) *", nudDuration, 4);

        cboAgeRating = new ComboBox
        {
            Font = new Font("Segoe UI", 10),
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 6, 0, 6)
        };
        cboAgeRating.Items.AddRange(new[] { "P", "C13", "C16", "C18" });
        cboAgeRating.SelectedIndex = 0;
        AddFormRow(formGrid, "Độ tuổi", cboAgeRating, 5);

        dtpRelease = new DateTimePicker
        {
            Font = new Font("Segoe UI", 10),
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Short,
            CalendarForeColor = Color.Black,
            Margin = new Padding(0, 6, 0, 6)
        };
        AddFormRow(formGrid, "Ngày khởi chiếu", dtpRelease, 6);

        txtTrailer = MakeTextBox();
        AddFormRow(formGrid, "Link trailer", txtTrailer, 7);

        txtDescription = MakeTextBox();
        txtDescription.Multiline = true;
        txtDescription.ScrollBars = ScrollBars.Vertical;
        AddFormRow(formGrid, "Mô tả", txtDescription, 8);

        var sidePanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 235F));
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 32F));
        sidePanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        content.Controls.Add(sidePanel, 1, 0);

        picPoster = new PictureBox
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(35, 35, 55),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 0, 8)
        };
        sidePanel.Controls.Add(picPoster, 0, 0);

        var btnChoose = new Button
        {
            Text = "Chọn ảnh poster",
            Font = new Font("Segoe UI", 9),
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(50, 50, 75),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 0, 8)
        };
        btnChoose.FlatAppearance.BorderSize = 0;
        btnChoose.Click += BtnChoosePoster_Click;
        sidePanel.Controls.Add(btnChoose, 0, 1);

        sidePanel.Controls.Add(MakeLabel("Thể loại"), 0, 2);
        clbGenres = new CheckedListBox
        {
            Font = new Font("Segoe UI", 9.5f),
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.FromArgb(200, 200, 220),
            BorderStyle = BorderStyle.FixedSingle,
            CheckOnClick = true,
            Margin = Padding.Empty
        };
        foreach (var g in _genres) clbGenres.Items.Add(g.Name);
        sidePanel.Controls.Add(clbGenres, 0, 3);

        var buttonBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };
        root.Controls.Add(buttonBar, 0, 1);

        var btnSave = new Button
        {
            Text = _editMovie == null ? "✅ Thêm phim" : "💾 Lưu",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Size = new Size(140, 40),
            BackColor = Color.FromArgb(80, 160, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.OK,
            Margin = new Padding(8, 0, 0, 0)
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += BtnSave_Click;

        var btnCancel = new Button
        {
            Text = "Hủy",
            Font = new Font("Segoe UI", 10),
            Size = new Size(100, 40),
            BackColor = Color.FromArgb(50, 50, 75),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel,
            Margin = Padding.Empty
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        buttonBar.Controls.Add(btnCancel);
        buttonBar.Controls.Add(btnSave);

        this.AcceptButton = btnSave;
        this.CancelButton = btnCancel;
    }

    private void LoadEditData()
    {
        if (_editMovie == null) return;
        txtCode.Text = _editMovie.Code;
        txtTitle.Text = _editMovie.Title;
        txtDirector.Text = _editMovie.Director;
        txtActors.Text = _editMovie.Actors;
        nudDuration.Value = _editMovie.Duration;
        txtDescription.Text = _editMovie.Description;
        txtTrailer.Text = _editMovie.TrailerUrl;
        dtpRelease.Value = _editMovie.ReleaseDate ?? DateTime.Today;

        int ageIdx = cboAgeRating.Items.IndexOf(_editMovie.AgeRating ?? "P");
        cboAgeRating.SelectedIndex = ageIdx >= 0 ? ageIdx : 0;

        _posterPath = _editMovie.PosterPath;
        var posterImage = LoadPosterImage(_editMovie);
        if (posterImage != null)
        {
            picPoster.Image = posterImage;
        }
        else if (_editMovie.Poster != null)
        {
            using var ms = new MemoryStream(_editMovie.Poster);
            picPoster.Image = Image.FromStream(ms);
            _posterData = _editMovie.Poster;
        }

        foreach (var mg in _editMovie.MovieGenres)
        {
            int idx = _genres.FindIndex(g => g.Id == mg.GenreId);
            if (idx >= 0) clbGenres.SetItemChecked(idx, true);
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        errorProvider.Clear();
        bool isValid = true;

        if (string.IsNullOrWhiteSpace(txtCode.Text))
        {
            errorProvider.SetError(txtCode, "Vui lòng nhập mã phim.");
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(txtTitle.Text))
        {
            errorProvider.SetError(txtTitle, "Vui lòng nhập tên phim.");
            isValid = false;
        }

        if (!isValid)
        {
            this.DialogResult = DialogResult.None;
            return;
        }

        MovieData = new Movie
        {
            Code = txtCode.Text.Trim().ToUpper(),
            Title = txtTitle.Text.Trim(),
            Director = txtDirector.Text.Trim(),
            Actors = txtActors.Text.Trim(),
            Duration = (int)nudDuration.Value,
            AgeRating = cboAgeRating.SelectedItem?.ToString() ?? "P",
            Description = txtDescription.Text.Trim(),
            TrailerUrl = txtTrailer.Text.Trim(),
            ReleaseDate = (DateTime?)dtpRelease.Value,
            Poster = _posterData,
            PosterPath = _posterPath,
            IsActive = true
        };

        SelectedGenreIds.Clear();
        for (int i = 0; i < clbGenres.Items.Count; i++)
        {
            if (clbGenres.GetItemChecked(i))
                SelectedGenreIds.Add(_genres[i].Id);
        }
    }

    private void BtnChoosePoster_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog
        {
            Filter = "Ảnh|*.jpg;*.jpeg;*.png;*.bmp",
            Title = "Chọn ảnh poster"
        };
        if (ofd.ShowDialog() == DialogResult.OK)
        {
            _posterPath = CopyPosterToResources(ofd.FileName);
            _posterData = null;
            picPoster.Image = LoadPosterImage(new Movie { PosterPath = _posterPath });
        }
    }

    private static string CopyPosterToResources(string sourcePath)
    {
        var targetDir = Path.Combine(Application.StartupPath, "Resources", "Posters");
        Directory.CreateDirectory(targetDir);

        var originalName = Path.GetFileNameWithoutExtension(sourcePath);
        var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
        var safeName = string.Join("_", originalName.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "poster";

        var fileName = $"{safeName}_{DateTime.Now:yyyyMMddHHmmssfff}{extension}";
        var targetPath = Path.Combine(targetDir, fileName);
        File.Copy(sourcePath, targetPath, overwrite: true);
        return fileName;
    }

    private static Image? LoadPosterImage(Movie movie)
    {
        if (string.IsNullOrWhiteSpace(movie.PosterPath)) return null;

        var path = Path.Combine(Application.StartupPath, "Resources", "Posters", movie.PosterPath);
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
        table.Controls.Add(MakeLabel(label), 0, row);
        table.Controls.Add(editor, 1, row);
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 10),
        ForeColor = Color.FromArgb(160, 160, 185),
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = new Padding(0, 0, 10, 0)
    };

    private static TextBox MakeTextBox() => new()
    {
        Font = new Font("Segoe UI", 10),
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(35, 35, 55),
        ForeColor = Color.White,
        BorderStyle = BorderStyle.FixedSingle,
        Margin = new Padding(0, 6, 0, 6)
    };
}
