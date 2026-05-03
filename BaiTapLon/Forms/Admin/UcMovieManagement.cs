using BaiTapLon.Models;
using BaiTapLon.Services;

namespace BaiTapLon.Forms.Admin;

public class UcMovieManagement : UserControl
{
    private DataGridView dgvMovies = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboGenre = null!;
    private Button btnAdd = null!;
    private Button btnEdit = null!;
    private Button btnDelete = null!;
    private Button btnRefresh = null!;
    private Label lblTitle = null!;

    private List<Genre> _genres = new();

    public UcMovieManagement()
    {
        InitializeComponent();
        this.Load += async (s, e) => await LoadDataAsync();
    }

    private void InitializeComponent()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(18, 18, 30);
        this.Padding = new Padding(5);

        // === Header ===
        var pnlTop = new Panel
        {
            Dock = DockStyle.Top,
            Height = 110,
            BackColor = Color.Transparent
        };
        this.Controls.Add(pnlTop);

        lblTitle = new Label
        {
            Text = "🎬  Quản Lý Phim",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(210, 210, 230),
            Location = new Point(5, 5),
            AutoSize = true
        };
        pnlTop.Controls.Add(lblTitle);

        // Search + Filter row
        txtSearch = new TextBox
        {
            PlaceholderText = "🔍 Tìm kiếm theo tên phim, đạo diễn...",
            Font = new Font("Segoe UI", 11),
            Size = new Size(350, 30),
            Location = new Point(5, 55),
            BackColor = Color.FromArgb(30, 30, 50),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        txtSearch.TextChanged += async (s, e) => await LoadDataAsync();
        pnlTop.Controls.Add(txtSearch);

        cboGenre = new ComboBox
        {
            Font = new Font("Segoe UI", 11),
            Size = new Size(180, 30),
            Location = new Point(370, 55),
            BackColor = Color.FromArgb(30, 30, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboGenre.SelectedIndexChanged += async (s, e) => await LoadDataAsync();
        pnlTop.Controls.Add(cboGenre);

        // Action buttons
        btnAdd = MakeButton("➕ Thêm phim", Color.FromArgb(60, 160, 60), new Point(600, 52));
        btnAdd.Click += BtnAdd_Click;
        pnlTop.Controls.Add(btnAdd);

        btnEdit = MakeButton("✏️ Sửa", Color.FromArgb(60, 120, 200), new Point(745, 52));
        btnEdit.Click += BtnEdit_Click;
        pnlTop.Controls.Add(btnEdit);

        btnDelete = MakeButton("🗑️ Xóa", Color.FromArgb(200, 60, 60), new Point(860, 52));
        btnDelete.Click += BtnDelete_Click;
        pnlTop.Controls.Add(btnDelete);

        btnRefresh = MakeButton("🔄", Color.FromArgb(50, 50, 75), new Point(560, 52));
        btnRefresh.Size = new Size(30, 34);
        btnRefresh.Click += async (s, e) => await LoadDataAsync();
        pnlTop.Controls.Add(btnRefresh);

        // === DataGridView ===
        dgvMovies = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.FromArgb(22, 22, 38),
            GridColor = Color.FromArgb(40, 40, 60),
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 40 },
            Font = new Font("Segoe UI", 10)
        };
        StyleGrid(dgvMovies);
        dgvMovies.DoubleClick += BtnEdit_Click;
        this.Controls.Add(dgvMovies);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var context = Program.CreateDbContext();
            var service = new MovieService(context);

            // Load genres cho filter
            if (_genres.Count == 0)
            {
                _genres = await service.GetAllGenresAsync();
                cboGenre.Items.Clear();
                cboGenre.Items.Add("-- Tất cả thể loại --");
                foreach (var g in _genres) cboGenre.Items.Add(g.Name);
                cboGenre.SelectedIndex = 0;
            }

            int? genreId = null;
            if (cboGenre.SelectedIndex > 0)
                genreId = _genres[cboGenre.SelectedIndex - 1].Id;

            var movies = await service.SearchAsync(txtSearch.Text, genreId);

            dgvMovies.DataSource = null;
            dgvMovies.Columns.Clear();
            dgvMovies.DataSource = movies.Select(m => new
            {
                m.Id,
                TênPhim = m.Title,
                ĐạoDiễn = m.Director ?? "",
                ThờiLượng = $"{m.Duration} phút",
                ĐộTuổi = m.AgeRating ?? "P",
                ThểLoại = string.Join(", ", m.MovieGenres.Select(mg => mg.Genre.Name)),
                NgàyKhởiChiếu = m.ReleaseDate.HasValue ? m.ReleaseDate.Value.ToString("dd/MM/yyyy") : "",
                TrạngThái = m.IsActive ? "Đang chiếu" : "Đã ẩn"
            }).ToList();

            if (dgvMovies.Columns.Contains("Id"))
                dgvMovies.Columns["Id"].Visible = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void BtnAdd_Click(object? sender, EventArgs e)
    {
        using var dlg = new DlgMovieEdit(null, _genres);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            using var context = Program.CreateDbContext();
            var service = new MovieService(context);
            var (ok, msg) = await service.CreateAsync(dlg.MovieData, dlg.SelectedGenreIds);
            MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
            if (ok) await LoadDataAsync();
        }
    }

    private async void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (dgvMovies.CurrentRow == null) return;
        int id = (int)dgvMovies.CurrentRow.Cells["Id"].Value;

        using var context = Program.CreateDbContext();
        var service = new MovieService(context);
        var movie = await service.GetByIdAsync(id);
        if (movie == null) return;

        using var dlg = new DlgMovieEdit(movie, _genres);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            dlg.MovieData.Id = id;
            var (ok, msg) = await service.UpdateAsync(dlg.MovieData, dlg.SelectedGenreIds);
            MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
            if (ok) await LoadDataAsync();
        }
    }

    private async void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (dgvMovies.CurrentRow == null) return;
        int id = (int)dgvMovies.CurrentRow.Cells["Id"].Value;
        string title = dgvMovies.CurrentRow.Cells["TênPhim"].Value?.ToString() ?? "";

        if (MessageBox.Show($"Xóa phim \"{title}\"?", "Xác nhận",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            using var context = Program.CreateDbContext();
            var service = new MovieService(context);
            var (ok, msg) = await service.SoftDeleteAsync(id);
            MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
            if (ok) await LoadDataAsync();
        }
    }

    private static Button MakeButton(string text, Color bg, Point loc)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(110, 34),
            Location = loc,
            BackColor = bg,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private static void StyleGrid(DataGridView dgv)
    {
        dgv.DefaultCellStyle.BackColor = Color.FromArgb(22, 22, 38);
        dgv.DefaultCellStyle.ForeColor = Color.FromArgb(200, 200, 220);
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 50, 120);
        dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        dgv.DefaultCellStyle.Padding = new Padding(5, 0, 5, 0);

        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 28, 48);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(160, 160, 190);
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(5, 0, 5, 0);
        dgv.ColumnHeadersHeight = 42;

        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(26, 26, 42);
    }
}
