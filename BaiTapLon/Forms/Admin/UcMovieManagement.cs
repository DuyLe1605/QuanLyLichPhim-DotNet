using BaiTapLon.Models;
using BaiTapLon.Services;

namespace BaiTapLon.Forms.Admin;

public class UcMovieManagement : UserControl
{
    private DataGridView dgvMovies = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboGenre = null!;

    private List<Genre> _genres = new();
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

        var toolbar = AdminControls.CreateToolbar(
            txtSearch,
            cboGenre,
            AdminControls.CreateButton("🔄", AdminTheme.ButtonNeutral, 36, async (s, e) => await LoadDataAsync()),
            AdminControls.CreateButton("➕ Thêm", AdminTheme.ButtonSuccess, 110, BtnAdd_Click),
            AdminControls.CreateButton("✏️ Sửa", AdminTheme.ButtonPrimary, 100, BtnEdit_Click),
            AdminControls.CreateButton("🗑️ Xóa", AdminTheme.ButtonDanger, 100, BtnDelete_Click)
        );

        Controls.Add(AdminLayouts.CreateManagementPage("🎬  Quản Lý Phim", toolbar, dgvMovies));
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

            var movies = await service.SearchAsync(txtSearch.Text, genreId);

            dgvMovies.DataSource = null;
            dgvMovies.Columns.Clear();
            dgvMovies.DataSource = movies.Select(m => new
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

            if (dgvMovies.Columns.Contains("Id"))
                dgvMovies.Columns["Id"].Visible = false;
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

}
