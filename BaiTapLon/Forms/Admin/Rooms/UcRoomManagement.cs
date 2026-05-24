using BaiTapLon.Forms.Controls;
using BaiTapLon.Models;
using BaiTapLon.Services;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Forms.Admin;

public class UcRoomManagement : UserControl
{
    private DataGridView dgvRooms = null!;
    private AdminPaginationBar pagination = null!;
    private TextBox txtSearch = null!;
    private List<Room> _rooms = new();

    public UcRoomManagement()
    {
        InitUI();
        Load += async (s, e) => await LoadDataAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        dgvRooms = AdminControls.CreateGrid();
        dgvRooms.CellContentClick += DgvRooms_CellContentClick;
        dgvRooms.DoubleClick += (s, e) => ShowSeatPreview();
        pagination = new AdminPaginationBar();
        pagination.PaginationChanged += (s, e) => BindRoomPage();

        txtSearch = AdminControls.CreateSearchBox("Tìm tên phòng...");
        txtSearch.TextChanged += async (s, e) => await LoadDataAsync();

        var toolbar = AdminControls.CreateToolbar(
            txtSearch,
            AdminControls.CreateButton("+ Thêm phòng", AdminTheme.ButtonSuccess, 145, BtnAdd_Click),
            AdminControls.CreateButton("Xem sơ đồ", AdminTheme.ButtonNeutral, 140, (s, e) => ShowSeatPreview()),
            AdminControls.CreateButton("Sửa", AdminTheme.ButtonPrimary, 100, BtnEdit_Click),
            AdminControls.CreateButton("Xóa", AdminTheme.ButtonDanger, 100, BtnDel_Click),
            AdminControls.CreateButton("📥 Xuất Excel", Color.FromArgb(40, 167, 69), 110, BtnExport_Click)
        );

        Controls.Add(AdminLayouts.CreateManagementPage(
            "Quản Lý Phòng Chiếu",
            toolbar,
            AdminLayouts.CreatePagedGridContent(dgvRooms, pagination)));
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var query = ctx.Rooms.Include(r => r.Seats).AsNoTracking().AsQueryable();

            string kw = txtSearch.Text.Trim().ToLower();
            if (!string.IsNullOrWhiteSpace(kw))
            {
                query = query.Where(r => r.Name.ToLower().Contains(kw) || r.Type.ToLower().Contains(kw));
            }

            _rooms = await query.OrderBy(r => r.Name).ToListAsync();

            pagination.SetTotalItems(_rooms.Count, resetPage: true);
            BindRoomPage();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BindRoomPage()
    {
        dgvRooms.DataSource = null;
        dgvRooms.Columns.Clear();
        dgvRooms.DataSource = _rooms
            .Skip(pagination.Skip)
            .Take(pagination.PageSize)
            .Select(r => new
            {
                r.Id,
                Ten = r.Name,
                Loai = r.Type,
                Hang = r.Rows,
                CotMax = r.Columns,
                TongGhe = r.TotalSeats,
                TrangThai = r.IsActive ? "Hoạt động" : "Ẩn"
            }).ToList();

        AdminControls.HideColumn(dgvRooms, "Id");
        AdminControls.SetColumnWidths(dgvRooms,
            ("Ten", 180),
            ("Loai", 140),
            ("Hang", 90),
            ("CotMax", 100),
            ("TongGhe", 110),
            ("TrangThai", 130));

        // Rename column headers to proper display names
        RenameColumns(dgvRooms,
            ("Ten", "Tên"),
            ("Loai", "Loại"),
            ("Hang", "Hàng"),
            ("CotMax", "Cột Max"),
            ("TongGhe", "Tổng Ghế"),
            ("TrangThai", "Trạng Thái"));

        AddViewColumn();
    }

    private void AddViewColumn()
    {
        if (dgvRooms.Columns.Contains("ViewLayout")) return;

        var viewColumn = new DataGridViewButtonColumn
        {
            Name = "ViewLayout",
            HeaderText = "Xem",
            Text = "Xem",
            UseColumnTextForButtonValue = true,
            Width = 72,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
            FlatStyle = FlatStyle.Flat
        };

        dgvRooms.Columns.Add(viewColumn);
    }

    private void DgvRooms_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
        if (dgvRooms.Columns[e.ColumnIndex].Name != "ViewLayout") return;

        dgvRooms.CurrentCell = dgvRooms.Rows[e.RowIndex].Cells[e.ColumnIndex];
        ShowSeatPreview();
    }

    private void ShowSeatPreview()
    {
        int? id = GetCurrentRoomId();
        var room = id.HasValue ? _rooms.FirstOrDefault(r => r.Id == id.Value) : null;
        if (room == null) return;

        using var dlg = new DlgRoomSeatPreview(room);
        dlg.ShowDialog(this);
    }

    private int? GetCurrentRoomId()
    {
        return AdminControls.GetCurrentIntValue(dgvRooms, "Id");
    }

    private async void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new DlgRoomEdit(null);
        if (dlg.ShowDialog() != DialogResult.OK) return;

        using var ctx = Program.CreateDbContext();
        var (ok, msg) = await new RoomService(ctx).CreateAsync(dlg.RoomData, dlg.RowConfigs, dlg.SeatLayoutItems);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadDataAsync();
    }

    private async void BtnEdit_Click(object? s, EventArgs e)
    {
        int? id = GetCurrentRoomId();
        if (!id.HasValue) return;

        using var ctx = Program.CreateDbContext();
        var svc = new RoomService(ctx);
        var room = await svc.GetByIdAsync(id.Value);
        if (room == null) return;

        using var dlg = new DlgRoomEdit(room);
        if (dlg.ShowDialog() != DialogResult.OK) return;

        dlg.RoomData.Id = id.Value;
        var (ok, msg) = await svc.UpdateAsync(dlg.RoomData, dlg.RowConfigs, dlg.SeatLayoutItems);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadDataAsync();
    }

    private async void BtnDel_Click(object? s, EventArgs e)
    {
        int? id = GetCurrentRoomId();
        if (!id.HasValue) return;

        if (MessageBox.Show("Xóa phòng này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            return;

        using var ctx = Program.CreateDbContext();
        var service = new RoomService(ctx);
        var (ok, msg) = await service.SoftDeleteAsync(id.Value);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadDataAsync();
    }

    private void BtnExport_Click(object? s, EventArgs e)
    {
        BaiTapLon.Helpers.ExcelHelper.ExportDataGridViewToExcel(dgvRooms, "Phong", "Danh Sách Phòng Chiếu");
    }

    private static void RenameColumns(DataGridView grid, params (string Name, string Header)[] mappings)
    {
        foreach (var (name, header) in mappings)
        {
            var col = grid.Columns[name];
            if (col != null) col.HeaderText = header;
        }
    }
}

internal class DlgRoomSeatPreview : Form
{
    public DlgRoomSeatPreview(Room room)
    {
        Text = $"Sơ đồ ghế - {room.Name}";
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(760, 560);
        MinimumSize = new Size(640, 440);
        BackColor = AdminTheme.PageBack;
        ForeColor = AdminTheme.Text;
        Padding = new Padding(14);

        var preview = new SeatLayoutPreviewControl
        {
            Location = Point.Empty,
            Margin = Padding.Empty
        };
        preview.SetSeats(room.Seats, $"{room.Name} - {room.Type} - {room.TotalSeats} ghế");

        var scrollHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = AdminTheme.PageBack,
            Padding = Padding.Empty
        };
        scrollHost.Controls.Add(preview);
        scrollHost.Resize += (s, e) =>
        {
            preview.Size = new Size(
                Math.Max(scrollHost.ClientSize.Width, preview.MinimumSize.Width),
                Math.Max(scrollHost.ClientSize.Height, preview.MinimumSize.Height));
        };
        preview.Size = preview.MinimumSize;

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 48,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 10, 0, 0)
        };
        footer.Controls.Add(AdminControls.CreateButton("Đóng", AdminTheme.ButtonNeutral, 100, (s, e) => Close()));

        Controls.Add(scrollHost);
        Controls.Add(footer);
    }
}
