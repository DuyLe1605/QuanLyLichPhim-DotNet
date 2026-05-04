using BaiTapLon.Forms.Controls;
using BaiTapLon.Models;
using BaiTapLon.Services;

namespace BaiTapLon.Forms.Admin;

public class UcRoomManagement : UserControl
{
    private DataGridView dgvRooms = null!;
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

        var toolbar = AdminControls.CreateToolbar(
            AdminControls.CreateButton("+ Thêm phòng", AdminTheme.ButtonSuccess, 145, BtnAdd_Click),
            AdminControls.CreateButton("Xem sơ đồ", AdminTheme.ButtonNeutral, 140, (s, e) => ShowSeatPreview()),
            AdminControls.CreateButton("Sửa", AdminTheme.ButtonPrimary, 100, BtnEdit_Click),
            AdminControls.CreateButton("Xóa", AdminTheme.ButtonDanger, 100, BtnDel_Click)
        );

        Controls.Add(AdminLayouts.CreateManagementPage("Quản Lý Phòng Chiếu", toolbar, dgvRooms));
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            _rooms = await new RoomService(ctx).GetAllAsync();

            dgvRooms.DataSource = null;
            dgvRooms.Columns.Clear();
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

            var idColumn = dgvRooms.Columns["Id"];
            if (idColumn != null)
                idColumn.Visible = false;

            AddViewColumn();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
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
        if (e.RowIndex < 0) return;
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
        if (dgvRooms.CurrentRow == null) return null;
        if (!dgvRooms.Columns.Contains("Id")) return null;
        return dgvRooms.CurrentRow.Cells["Id"].Value is int id ? id : null;
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
        var (ok, msg) = await svc.UpdateAsync(dlg.RoomData);
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
        var (ok, msg) = await new RoomService(ctx).SoftDeleteAsync(id.Value);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadDataAsync();
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
