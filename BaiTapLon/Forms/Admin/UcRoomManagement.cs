using BaiTapLon.Models;
using BaiTapLon.Services;
using BaiTapLon.Forms.Controls;

namespace BaiTapLon.Forms.Admin;

public class UcRoomManagement : UserControl
{
    private DataGridView dgvRooms = null!;
    private SeatLayoutPreviewControl seatPreview = null!;
    private List<Room> _rooms = new();

    public UcRoomManagement()
    {
        InitUI();
        this.Load += async (s, e) => await LoadDataAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        dgvRooms = AdminControls.CreateGrid();
        dgvRooms.SelectionChanged += (s, e) => UpdateSeatPreview();

        seatPreview = new SeatLayoutPreviewControl
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty
        };
        seatPreview.ClearPreview();

        var previewHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = AdminTheme.PageBack,
            Padding = new Padding(10, 0, 0, 0)
        };
        previewHost.Controls.Add(seatPreview);

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
        content.Controls.Add(dgvRooms, 0, 0);
        content.Controls.Add(previewHost, 1, 0);

        var toolbar = AdminControls.CreateToolbar(
            AdminControls.CreateButton("➕ Thêm phòng", AdminTheme.ButtonSuccess, 145, BtnAdd_Click),
            AdminControls.CreateButton("✏️ Sửa", AdminTheme.ButtonPrimary, 100, BtnEdit_Click),
            AdminControls.CreateButton("🗑️ Xóa", AdminTheme.ButtonDanger, 100, BtnDel_Click)
        );

        Controls.Add(AdminLayouts.CreateManagementPage("🏠  Quản Lý Phòng Chiếu", toolbar, content));
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            _rooms = await new RoomService(ctx).GetAllAsync();
            dgvRooms.DataSource = _rooms.Select(r => new
            {
                r.Id,
                Tên = r.Name,
                Loại = r.Type,
                Hàng = r.Rows,
                CộtMax = r.Columns,
                TổngGhế = r.TotalSeats,
                TrạngThái = r.IsActive ? "Hoạt động" : "Ẩn"
            }).ToList();
            if (dgvRooms.Columns.Contains("Id")) dgvRooms.Columns["Id"].Visible = false;
            UpdateSeatPreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
        }
    }

    private void UpdateSeatPreview()
    {
        if (seatPreview == null) return;

        int? id = GetCurrentRoomId();
        var room = id.HasValue ? _rooms.FirstOrDefault(r => r.Id == id.Value) : null;
        if (room == null)
        {
            seatPreview.ClearPreview();
            return;
        }

        seatPreview.SetSeats(room.Seats, $"{room.Name} - {room.TotalSeats} ghế");
    }

    private int? GetCurrentRoomId()
    {
        if (dgvRooms.CurrentRow == null) return null;
        if (!dgvRooms.Columns.Contains("Id")) return null;
        return dgvRooms.CurrentRow.Cells["Id"].Value is int id ? id : null;
    }

    private async void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new DlgRoomEdit(null);
        if (dlg.ShowDialog() != DialogResult.OK) return;
        using var ctx = Program.CreateDbContext();
        var (ok, msg) = await new RoomService(ctx).CreateAsync(dlg.RoomData, dlg.RowConfigs);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadDataAsync();
    }

    private async void BtnEdit_Click(object? s, EventArgs e)
    {
        if (dgvRooms.CurrentRow == null) return;
        int id = (int)dgvRooms.CurrentRow.Cells["Id"].Value;
        using var ctx = Program.CreateDbContext();
        var svc = new RoomService(ctx);
        var room = await svc.GetByIdAsync(id);
        if (room == null) return;
        using var dlg = new DlgRoomEdit(room);
        if (dlg.ShowDialog() != DialogResult.OK) return;
        dlg.RoomData.Id = id;
        var (ok, msg) = await svc.UpdateAsync(dlg.RoomData);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadDataAsync();
    }

    private async void BtnDel_Click(object? s, EventArgs e)
    {
        if (dgvRooms.CurrentRow == null) return;
        int id = (int)dgvRooms.CurrentRow.Cells["Id"].Value;
        if (MessageBox.Show("Xóa phòng này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        using var ctx = Program.CreateDbContext();
        var (ok, msg) = await new RoomService(ctx).SoftDeleteAsync(id);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadDataAsync();
    }

}
