using BaiTapLon.Models;
using BaiTapLon.Services;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Forms.Admin;

public class UcReviewManagement : UserControl
{
    private DataGridView dgv = null!;
    private AdminPaginationBar pagination = null!;
    private List<MovieReview> _reviews = new();
    private TextBox txtSearch = null!;

    public UcReviewManagement()
    {
        InitUI();
        this.Load += async (s, e) => await LoadAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        txtSearch = AdminControls.CreateSearchBox("Tìm theo phim, khách hàng...", 250);
        txtSearch.TextChanged += async (s, e) => await LoadAsync();

        dgv = AdminControls.CreateGrid();
        pagination = new AdminPaginationBar();
        pagination.PaginationChanged += (s, e) => BindPage();

        var toolbar = AdminControls.CreateToolbar(
            txtSearch,
            AdminControls.CreateButton("🗑️ Xóa", AdminTheme.ButtonDanger, 100, BtnDelete_Click)
        );

        Controls.Add(AdminLayouts.CreateManagementPage(
            "⭐  Đánh Giá Phim (Reviews)",
            toolbar,
            AdminLayouts.CreatePagedGridContent(dgv, pagination)));
    }

    private async Task LoadAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var query = ctx.MovieReviews
                .Include(r => r.Movie)
                .Include(r => r.Customer)
                .AsQueryable();

            string kw = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(kw))
            {
                query = query.Where(r => 
                    r.Movie.Title.ToLower().Contains(kw) || 
                    r.Customer.FullName.ToLower().Contains(kw) ||
                    (r.Comment != null && r.Comment.ToLower().Contains(kw)));
            }

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
                ĐánhGiá = new string('⭐', r.Rating),
                BìnhLuận = r.Comment ?? "-",
                ThờiGian = r.CreatedAt.ToString("dd/MM/yyyy HH:mm")
            }).ToList();

        AdminControls.HideColumn(dgv, "Id");
        AdminControls.SetColumnWidths(dgv,
            ("Phim", 250),
            ("KháchHàng", 200),
            ("ĐánhGiá", 120),
            ("BìnhLuận", 400),
            ("ThờiGian", 150)
        );

        dgv.Columns["KháchHàng"]!.HeaderText = "Khách Hàng";
        dgv.Columns["ĐánhGiá"]!.HeaderText = "Đánh Giá";
        dgv.Columns["BìnhLuận"]!.HeaderText = "Bình Luận";
        dgv.Columns["ThờiGian"]!.HeaderText = "Thời Gian";
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
        catch (Exception ex) { MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi"); }
    }
}
