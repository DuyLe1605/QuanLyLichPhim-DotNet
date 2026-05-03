using BaiTapLon.Models;

namespace BaiTapLon.Forms.Admin;

/// <summary>
/// Dialog thêm/sửa phim.
/// </summary>
public class DlgMovieEdit : Form
{
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
    private Button btnChoosePoster = null!;
    private byte[]? _posterData;

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
        this.ClientSize = new Size(680, 600);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(24, 24, 40);
        this.ForeColor = Color.FromArgb(200, 200, 220);

        int x1 = 20, x2 = 200, w2 = 260, y = 20;

        // Poster bên phải
        picPoster = new PictureBox
        {
            Size = new Size(170, 240),
            Location = new Point(490, 20),
            BackColor = Color.FromArgb(35, 35, 55),
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle
        };
        this.Controls.Add(picPoster);

        btnChoosePoster = new Button
        {
            Text = "Chọn ảnh poster",
            Font = new Font("Segoe UI", 9),
            Size = new Size(170, 30),
            Location = new Point(490, 265),
            BackColor = Color.FromArgb(50, 50, 75),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnChoosePoster.FlatAppearance.BorderSize = 0;
        btnChoosePoster.Click += BtnChoosePoster_Click;
        this.Controls.Add(btnChoosePoster);

        // Fields bên trái
        AddLabel("Tên phim *", x1, y);
        txtTitle = AddTextBox(x2, y, w2); y += 42;

        AddLabel("Đạo diễn", x1, y);
        txtDirector = AddTextBox(x2, y, w2); y += 42;

        AddLabel("Diễn viên", x1, y);
        txtActors = AddTextBox(x2, y, w2); y += 42;

        AddLabel("Thời lượng (phút) *", x1, y);
        nudDuration = new NumericUpDown
        {
            Font = new Font("Segoe UI", 11),
            Size = new Size(120, 30),
            Location = new Point(x2, y),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            Minimum = 1, Maximum = 500, Value = 120,
            BorderStyle = BorderStyle.FixedSingle
        };
        this.Controls.Add(nudDuration);
        y += 42;

        AddLabel("Độ tuổi", x1, y);
        cboAgeRating = new ComboBox
        {
            Font = new Font("Segoe UI", 11),
            Size = new Size(120, 30),
            Location = new Point(x2, y),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboAgeRating.Items.AddRange(new[] { "P", "C13", "C16", "C18" });
        cboAgeRating.SelectedIndex = 0;
        this.Controls.Add(cboAgeRating);
        y += 42;

        AddLabel("Ngày khởi chiếu", x1, y);
        dtpRelease = new DateTimePicker
        {
            Font = new Font("Segoe UI", 11),
            Size = new Size(w2, 30),
            Location = new Point(x2, y),
            Format = DateTimePickerFormat.Short,
            CalendarForeColor = Color.Black
        };
        this.Controls.Add(dtpRelease);
        y += 42;

        AddLabel("Link trailer", x1, y);
        txtTrailer = AddTextBox(x2, y, w2); y += 42;

        // Mô tả (multiline)
        AddLabel("Mô tả", x1, y);
        txtDescription = new TextBox
        {
            Font = new Font("Segoe UI", 10),
            Size = new Size(440, 70),
            Location = new Point(x2, y),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical
        };
        this.Controls.Add(txtDescription);

        // Thể loại bên phải dưới poster
        AddLabel("Thể loại", 490, 310);
        clbGenres = new CheckedListBox
        {
            Font = new Font("Segoe UI", 10),
            Size = new Size(170, 140),
            Location = new Point(490, 335),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.FromArgb(200, 200, 220),
            BorderStyle = BorderStyle.None,
            CheckOnClick = true
        };
        foreach (var g in _genres) clbGenres.Items.Add(g.Name);
        this.Controls.Add(clbGenres);

        // Buttons
        var btnSave = new Button
        {
            Text = _editMovie == null ? "Thêm phim" : "Lưu",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Size = new Size(140, 40),
            Location = new Point(380, 548),
            BackColor = Color.FromArgb(80, 160, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.OK
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += BtnSave_Click;
        this.Controls.Add(btnSave);

        var btnCancel = new Button
        {
            Text = "Hủy",
            Font = new Font("Segoe UI", 11),
            Size = new Size(100, 40),
            Location = new Point(530, 548),
            BackColor = Color.FromArgb(50, 50, 75),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        this.Controls.Add(btnCancel);

        this.AcceptButton = btnSave;
        this.CancelButton = btnCancel;
    }

    private void LoadEditData()
    {
        if (_editMovie == null) return;
        txtTitle.Text = _editMovie.Title;
        txtDirector.Text = _editMovie.Director;
        txtActors.Text = _editMovie.Actors;
        nudDuration.Value = _editMovie.Duration;
        txtDescription.Text = _editMovie.Description;
        txtTrailer.Text = _editMovie.TrailerUrl;
        dtpRelease.Value = _editMovie.ReleaseDate ?? DateTime.Today;

        int ageIdx = cboAgeRating.Items.IndexOf(_editMovie.AgeRating ?? "P");
        cboAgeRating.SelectedIndex = ageIdx >= 0 ? ageIdx : 0;

        if (_editMovie.Poster != null)
        {
            using var ms = new MemoryStream(_editMovie.Poster);
            picPoster.Image = Image.FromStream(ms);
            _posterData = _editMovie.Poster;
        }

        // Check thể loại
        foreach (var mg in _editMovie.MovieGenres)
        {
            int idx = _genres.FindIndex(g => g.Id == mg.GenreId);
            if (idx >= 0) clbGenres.SetItemChecked(idx, true);
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtTitle.Text))
        {
            MessageBox.Show("Vui lòng nhập tên phim!", "Thiếu thông tin");
            this.DialogResult = DialogResult.None;
            return;
        }

        MovieData = new Movie
        {
            Title = txtTitle.Text.Trim(),
            Director = txtDirector.Text.Trim(),
            Actors = txtActors.Text.Trim(),
            Duration = (int)nudDuration.Value,
            AgeRating = cboAgeRating.SelectedItem?.ToString() ?? "P",
            Description = txtDescription.Text.Trim(),
            TrailerUrl = txtTrailer.Text.Trim(),
            ReleaseDate = (DateTime?)dtpRelease.Value,
            Poster = _posterData,
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
            _posterData = File.ReadAllBytes(ofd.FileName);
            using var ms = new MemoryStream(_posterData);
            picPoster.Image = Image.FromStream(ms);
        }
    }

    private Label AddLabel(string text, int x, int y)
    {
        var lbl = new Label
        {
            Text = text,
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(160, 160, 185),
            Location = new Point(x, y + 4),
            AutoSize = true
        };
        this.Controls.Add(lbl);
        return lbl;
    }

    private TextBox AddTextBox(int x, int y, int width)
    {
        var txt = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            Size = new Size(width, 30),
            Location = new Point(x, y),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        this.Controls.Add(txt);
        return txt;
    }
}
