using BaiTapLon.Models;
using BaiTapLon.Data;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Forms.Admin;

public class UcReviewManagement : UserControl
{
    private const string ReviewRewardSettingKey = "ReviewRewardPoints";
    private const int DefaultReviewRewardPoints = 5;

    private DataGridView dgv = null!;
    private AdminPaginationBar pagination = null!;
    private TextBox txtSearch = null!;
    private ComboBox cboMovie = null!, cboRating = null!;
    private DateTimePicker dtpFrom = null!, dtpTo = null!;
    private NumericUpDown nudRewardPoints = null!;
    private Label lblRewardStatus = null!;
    private List<MovieReview> _reviews = new();
    private List<Movie> _movies = new();
    private bool _loadingFilters;

    public UcReviewManagement()
    {
        InitUI();
        Load += async (s, e) => await LoadInitialAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        txtSearch = AdminControls.CreateSearchBox("Tìm phim, khách hàng, bình luận...", 250);
        txtSearch.TextChanged += async (s, e) => await LoadAsync();

        cboMovie = AdminControls.CreateComboBox(210);
        cboMovie.SelectedIndexChanged += async (s, e) => { if (!_loadingFilters) await LoadAsync(); };

        cboRating = AdminControls.CreateComboBox(120);
        cboRating.Items.AddRange(new object[] { "Tất cả sao", "5 sao", "4 sao", "3 sao", "2 sao", "1 sao" });
        cboRating.SelectedIndex = 0;
        cboRating.SelectedIndexChanged += async (s, e) => { if (!_loadingFilters) await LoadAsync(); };

        dtpFrom = AdminControls.CreateDatePicker();
        dtpFrom.Value = DateTime.Today.AddMonths(-1);
        dtpTo = AdminControls.CreateDatePicker();
        dtpTo.Value = DateTime.Today;

        nudRewardPoints = new NumericUpDown
        {
            Width = 82,
            Height = 30,
            Minimum = 0,
            Maximum = 1000,
            Value = DefaultReviewRewardPoints,
            BackColor = AdminTheme.InputBack,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Font = AdminTheme.BodyFont,
            Margin = new Padding(0, 0, 8, 0)
        };

        lblRewardStatus = new Label
        {
            AutoSize = false,
            Size = new Size(170, 30),
            ForeColor = AdminTheme.MutedText,
            Font = AdminTheme.BodyFont,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 0, 8, 0)
        };

        dgv = AdminControls.CreateGrid();
        pagination = new AdminPaginationBar();
        pagination.PaginationChanged += (s, e) => BindPage();

        var toolbar = AdminControls.CreateToolbar(
            txtSearch,
            AdminControls.CreateToolbarLabel("Phim:", 42), cboMovie,
            AdminControls.CreateToolbarLabel("Sao:", 38), cboRating,
            AdminControls.CreateToolbarLabel("Từ:", 28), dtpFrom,
            AdminControls.CreateToolbarLabel("Đến:", 38), dtpTo,
            AdminControls.CreateButton("Lọc", AdminTheme.ButtonPrimary, 78, async (s, e) => await LoadAsync()),
            AdminControls.CreateToolbarLabel("Điểm:", 48), nudRewardPoints,
            AdminControls.CreateButton("Lưu điểm", AdminTheme.ButtonSuccess, 105, async (s, e) => await SaveRewardPointsAsync()),
            lblRewardStatus,
            AdminControls.CreateButton("Xóa", AdminTheme.ButtonDanger, 82, BtnDelete_Click)
        );

        Controls.Add(AdminLayouts.CreateManagementPage(
            "Đánh Giá Phim",
            toolbar,
            AdminLayouts.CreatePagedGridContent(dgv, pagination)));
    }

    private async Task LoadInitialAsync()
    {
        _loadingFilters = true;
        try
        {
            using var ctx = Program.CreateDbContext();
            _movies = await ctx.Movies.AsNoTracking().OrderBy(m => m.Title).ToListAsync();

            cboMovie.Items.Clear();
            cboMovie.Items.Add(new MovieFilterItem(null, "Tất cả phim"));
            foreach (var movie in _movies)
                cboMovie.Items.Add(new MovieFilterItem(movie.Id, movie.Title));
            cboMovie.SelectedIndex = 0;

            nudRewardPoints.Value = Math.Clamp(await GetReviewRewardPointsAsync(ctx), 0, 1000);
            lblRewardStatus.Text = $"Đang áp dụng: {nudRewardPoints.Value:N0} điểm";
        }
        finally
        {
            _loadingFilters = false;
        }

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (_loadingFilters) return;

        try
        {
            using var ctx = Program.CreateDbContext();
            var query = ctx.MovieReviews
                .Include(r => r.Movie)
                .Include(r => r.Customer)
                .AsQueryable();

            var kw = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(kw))
            {
                query = query.Where(r =>
                    r.Movie.Title.ToLower().Contains(kw) ||
                    r.Customer.FullName.ToLower().Contains(kw) ||
                    (r.Comment != null && r.Comment.ToLower().Contains(kw)));
            }

            if (cboMovie.SelectedItem is MovieFilterItem { MovieId: int movieId })
                query = query.Where(r => r.MovieId == movieId);

            var rating = cboRating.SelectedIndex switch
            {
                1 => 5,
                2 => 4,
                3 => 3,
                4 => 2,
                5 => 1,
                _ => 0
            };
            if (rating > 0)
                query = query.Where(r => r.Rating == rating);

            var from = dtpFrom.Value.Date;
            var to = dtpTo.Value.Date.AddDays(1).AddTicks(-1);
            if (from > to)
            {
                MessageBox.Show("Ngày bắt đầu không được lớn hơn ngày kết thúc.", "Lọc đánh giá");
                return;
            }
            query = query.Where(r => r.CreatedAt >= from && r.CreatedAt <= to);

            _reviews = await query.OrderByDescending(r => r.CreatedAt).ToListAsync();
            pagination.SetTotalItems(_reviews.Count, resetPage: true);
            BindPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
        }
    }

    private void BindPage()
    {
        dgv.DataSource = null;
        dgv.Columns.Clear();
        dgv.DataSource = _reviews
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(r => new
            {
                r.Id,
                Phim = r.Movie.Title,
                KháchHàng = r.Customer.FullName,
                SốSao = r.Rating,
                ĐánhGiá = new string('★', r.Rating),
                BìnhLuận = r.Comment ?? "-",
                ThờiGian = r.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            }).ToList();

        AdminControls.HideColumn(dgv, "Id");
        ConfigureReviewGridColumns();
    }

    private void ConfigureReviewGridColumns()
    {
        dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgv.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
        dgv.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;

        SetFill("Phim", 200, 18);
        SetFill("KháchHàng", 170, 15);
        SetFill("SốSao", 70, 6);
        SetFill("ĐánhGiá", 110, 9);
        SetFill("BìnhLuận", 320, 30);
        SetFill("ThờiGian", 145, 12);

        dgv.Columns["KháchHàng"]!.HeaderText = "Khách Hàng";
        dgv.Columns["SốSao"]!.HeaderText = "Số Sao";
        dgv.Columns["ĐánhGiá"]!.HeaderText = "Đánh Giá";
        dgv.Columns["BìnhLuận"]!.HeaderText = "Bình Luận";
        dgv.Columns["ThờiGian"]!.HeaderText = "Thời Gian";

        AlignCenter("SốSao");
        AlignCenter("ĐánhGiá");
        AlignCenter("ThờiGian");
    }

    private async Task SaveRewardPointsAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var setting = await ctx.SystemSettings.FirstOrDefaultAsync(s => s.Key == ReviewRewardSettingKey);
            if (setting is null)
            {
                setting = new SystemSetting
                {
                    Key = ReviewRewardSettingKey,
                    Description = "Số điểm thưởng cho lần đầu khách đánh giá một phim."
                };
                ctx.SystemSettings.Add(setting);
            }

            setting.Value = ((int)nudRewardPoints.Value).ToString();
            setting.UpdatedAt = DateTime.Now;
            await ctx.SaveChangesAsync();
            lblRewardStatus.Text = $"Đã lưu: {nudRewardPoints.Value:N0} điểm";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể lưu điểm thưởng: {ex.Message}", "Lỗi");
        }
    }

    private static async Task<int> GetReviewRewardPointsAsync(AppDbContext ctx)
    {
        var raw = await ctx.SystemSettings
            .AsNoTracking()
            .Where(s => s.Key == ReviewRewardSettingKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        return int.TryParse(raw, out var points) && points >= 0
            ? points
            : DefaultReviewRewardPoints;
    }

    private void SetFill(string columnName, int minWidth, float fillWeight)
    {
        if (!dgv.Columns.Contains(columnName)) return;
        var column = dgv.Columns[columnName]!;
        column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        column.MinimumWidth = minWidth;
        column.FillWeight = fillWeight;
    }

    private void AlignCenter(string columnName)
    {
        if (dgv.Columns.Contains(columnName))
            dgv.Columns[columnName]!.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
    }

    private async void BtnDelete_Click(object? s, EventArgs e)
    {
        var id = AdminControls.GetCurrentIntValue(dgv, "Id");
        if (!id.HasValue) return;

        if (MessageBox.Show("Bạn có chắc muốn xóa đánh giá này?", "Xác nhận", MessageBoxButtons.YesNo) != DialogResult.Yes) return;

        try
        {
            using var ctx = Program.CreateDbContext();
            var review = await ctx.MovieReviews.FindAsync(id.Value);
            if (review != null)
            {
                ctx.MovieReviews.Remove(review);
                await ctx.SaveChangesAsync();
                MessageBox.Show("Đã xóa đánh giá!", "Thành công");
                await LoadAsync();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
        }
    }

    private sealed record MovieFilterItem(int? MovieId, string Text)
    {
        public override string ToString() => Text;
    }
}
