using BaiTapLon.Models;
using BaiTapLon.Services;

namespace BaiTapLon.Forms.Admin;

public class UcMovieManagement : UserControl
{
    private DataGridView dgvMovies = null!;
    private AdminPaginationBar pagination = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboGenre = null!;

    private List<Genre> _genres = new();
    private List<Movie> _movies = new();
    private bool _isLoading = false;

    public UcMovieManagement()
    {
        InitializeComponent();
        this.Load += async (s, e) => await LoadDataAsync();
    }

    private void InitializeComponent()
    {
        AdminControls.ConfigurePage(this);

        txtSearch = AdminControls.CreateSearchBox("🔍 Tìm mã, tên phim, đạo diễn...");
        txtSearch.TextChanged += async (s, e) => await LoadDataAsync();

        cboGenre = AdminControls.CreateComboBox();
        cboGenre.SelectedIndexChanged += async (s, e) =>
        {
            if (!_isLoading) await LoadDataAsync();
        };

        dgvMovies = AdminControls.CreateGrid();
        dgvMovies.DoubleClick += BtnEdit_Click;
        pagination = new AdminPaginationBar();
        pagination.PaginationChanged += (s, e) => BindMoviePage();

        var toolbar = AdminControls.CreateToolbar(
            txtSearch,
            cboGenre,
            AdminControls.CreateButton("🔄", AdminTheme.ButtonNeutral, 36, async (s, e) => await LoadDataAsync()),
            AdminControls.CreateButton("➕ Thêm", AdminTheme.ButtonSuccess, 110, BtnAdd_Click),
            AdminControls.CreateButton("✏️ Sửa", AdminTheme.ButtonPrimary, 100, BtnEdit_Click),
            AdminControls.CreateButton("🗑️ Xóa", AdminTheme.ButtonDanger, 100, BtnDelete_Click)
        );

        Controls.Add(AdminLayouts.CreateManagementPage(
            "🎬  Quản Lý Phim",
            toolbar,
            AdminLayouts.CreatePagedGridContent(dgvMovies, pagination)));
    }

    private async Task LoadDataAsync()
    {
        if (_isLoading) return;
        _isLoading = true;

        try
        {
            using var context = Program.CreateDbContext();
            var service = new MovieService(context);

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

            _movies = await service.SearchAsync(txtSearch.Text, genreId);
            pagination.SetTotalItems(_movies.Count, resetPage: true);
            BindMoviePage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void BindMoviePage()
    {
        dgvMovies.DataSource = null;
        dgvMovies.Columns.Clear();
        dgvMovies.DataSource = _movies
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(m => new
            {
                m.Id,
                MãPhim = m.Code,
                TênPhim = m.Title,
                ĐạoDiễn = m.Director ?? "",
                ThờiLượng = $"{m.Duration} phút",
                ĐộTuổi = m.AgeRating ?? "P",
                ThểLoại = string.Join(", ", m.MovieGenres.Select(mg => mg.Genre.Name)),
                NgàyKhởiChiếu = m.ReleaseDate.HasValue ? m.ReleaseDate.Value.ToString("dd/MM/yyyy") : "",
                TrạngThái = m.IsActive ? "Đang chiếu" : "Đã ẩn"
            }).ToList();

        AdminControls.HideColumn(dgvMovies, "Id");
        AdminControls.SetColumnWidths(dgvMovies,
            ("MãPhim", 100),
            ("TênPhim", 260),
            ("ĐạoDiễn", 180),
            ("ThờiLượng", 110),
            ("ĐộTuổi", 90),
            ("ThểLoại", 220),
            ("NgàyKhởiChiếu", 140),
            ("TrạngThái", 120));
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
        var id = GetCurrentMovieId();
        if (!id.HasValue) return;

        using var context = Program.CreateDbContext();
        var service = new MovieService(context);
        var movie = await service.GetByIdAsync(id.Value);
        if (movie == null) return;

        using var dlg = new DlgMovieEdit(movie, _genres);
        if (dlg.ShowDialog() == DialogResult.OK)
        {
            dlg.MovieData.Id = id.Value;
            var (ok, msg) = await service.UpdateAsync(dlg.MovieData, dlg.SelectedGenreIds);
            MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
            if (ok) await LoadDataAsync();
        }
    }

    private async void BtnDelete_Click(object? sender, EventArgs e)
    {
        var id = GetCurrentMovieId();
        if (!id.HasValue || dgvMovies.CurrentRow == null) return;

        string title = dgvMovies.CurrentRow.Cells["TênPhim"].Value?.ToString() ?? "";

        if (MessageBox.Show($"Xóa phim \"{title}\"?", "Xác nhận",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
        {
            using var context = Program.CreateDbContext();
            var service = new MovieService(context);
            var (ok, msg) = await service.SoftDeleteAsync(id.Value);
            MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
            if (ok) await LoadDataAsync();
        }
    }

    private int? GetCurrentMovieId()
    {
        return AdminControls.GetCurrentIntValue(dgvMovies, "Id");
    }

}
