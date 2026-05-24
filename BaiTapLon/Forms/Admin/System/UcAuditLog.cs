using BaiTapLon.Models;
using BaiTapLon.Services;

namespace BaiTapLon.Forms.Admin;

public class UcAuditLog : UserControl
{
    private DataGridView dgv = null!;
    private AdminPaginationBar pagination = null!;
    private List<AuditLog> _logs = new();
    private TextBox txtSearch = null!;

    public UcAuditLog()
    {
        InitUI();
        this.Load += async (s, e) => await LoadAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        txtSearch = AdminControls.CreateSearchBox("Tìm theo action, entity, user...", 250);
        txtSearch.TextChanged += async (s, e) => await LoadAsync();

        dgv = AdminControls.CreateGrid();
        pagination = new AdminPaginationBar();
        pagination.PaginationChanged += (s, e) => BindPage();

        var toolbar = AdminControls.CreateToolbar(
            txtSearch,
            AdminControls.CreateButton("🔄 Làm mới", AdminTheme.ButtonNeutral, 100, async (s, e) => await LoadAsync())
        );

        Controls.Add(AdminLayouts.CreateManagementPage(
            "📜  Lịch Sử Hệ Thống (Audit Log)",
            toolbar,
            AdminLayouts.CreatePagedGridContent(dgv, pagination)));
    }

    private async Task LoadAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var service = new AuditService(ctx);
            var allLogs = await service.GetLogsAsync(500);

            string kw = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(kw))
            {
                allLogs = allLogs.Where(l => 
                    l.Action.ToLower().Contains(kw) || 
                    l.EntityType.ToLower().Contains(kw) || 
                    (l.User?.Username?.ToLower().Contains(kw) == true)).ToList();
            }

            _logs = allLogs;
            pagination.SetTotalItems(_logs.Count, resetPage: true);
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
        dgv.DataSource = _logs
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(l => new
            {
                l.Id,
                ThờiGian = l.Timestamp.ToString("dd/MM/yyyy HH:mm:ss"),
                User = l.User?.Username ?? "Hệ thống",
                HànhĐộng = l.Action,
                ĐốiTượng = l.EntityType,
                IdĐốiTượng = l.EntityId,
                Cũ = l.OldValue ?? "-",
                Mới = l.NewValue ?? "-"
            }).ToList();

        AdminControls.HideColumn(dgv, "Id");
        AdminControls.SetColumnWidths(dgv,
            ("ThờiGian", 140),
            ("User", 100),
            ("HànhĐộng", 100),
            ("ĐốiTượng", 100),
            ("IdĐốiTượng", 80),
            ("Cũ", 200),
            ("Mới", 200)
        );
    }
}
