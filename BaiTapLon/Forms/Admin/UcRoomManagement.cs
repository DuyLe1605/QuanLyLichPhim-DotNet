using BaiTapLon.Models;
using BaiTapLon.Services;

namespace BaiTapLon.Forms.Admin;

public class UcRoomManagement : UserControl
{
    private DataGridView dgvRooms = null!;

    public UcRoomManagement()
    {
        InitUI();
        this.Load += async (s, e) => await LoadDataAsync();
    }

    private void InitUI()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(18, 18, 30);
        this.Padding = new Padding(5);

        // === Top Panel ===
        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 100 };
        this.Controls.Add(pnlTop);

        pnlTop.Controls.Add(new Label
        {
            Text = "🏠  Quản Lý Phòng Chiếu",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(210, 210, 230),
            Location = new Point(5, 5),
            AutoSize = true
        });

        // Buttons — row 2, below the title
        int btnY = 55;
        var btnAdd = Btn("➕ Thêm phòng", Color.FromArgb(60, 160, 60), new Point(5, btnY));
        btnAdd.Click += BtnAdd_Click;
        pnlTop.Controls.Add(btnAdd);

        var btnEdit = Btn("✏️ Sửa", Color.FromArgb(60, 120, 200), new Point(160, btnY));
        btnEdit.Click += BtnEdit_Click;
        pnlTop.Controls.Add(btnEdit);

        var btnDel = Btn("🗑️ Xóa", Color.FromArgb(200, 60, 60), new Point(285, btnY));
        btnDel.Click += BtnDel_Click;
        pnlTop.Controls.Add(btnDel);

        // === DataGridView ===
        dgvRooms = StyledGrid();
        this.Controls.Add(dgvRooms);
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            var rooms = await new RoomService(ctx).GetAllAsync();
            dgvRooms.DataSource = rooms.Select(r => new
            {
                r.Id,
                Tên = r.Name,
                Loại = r.Type,
                Hàng = r.Rows,
                Cột = r.Columns,
                TổngGhế = r.TotalSeats,
                TrạngThái = r.IsActive ? "Hoạt động" : "Ẩn"
            }).ToList();
            if (dgvRooms.Columns.Contains("Id")) dgvRooms.Columns["Id"].Visible = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
        }
    }

    private async void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new DlgRoomEdit(null);
        if (dlg.ShowDialog() != DialogResult.OK) return;
        using var ctx = Program.CreateDbContext();
        var (ok, msg) = await new RoomService(ctx).CreateAsync(dlg.RoomData, dlg.VipFromRow, dlg.CoupleLastRow ? 1 : 0);
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

    private static Button Btn(string t, Color c, Point loc)
    {
        var b = new Button
        {
            Text = t,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(140, 34),
            Location = loc,
            BackColor = c,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    private static DataGridView StyledGrid()
    {
        var d = new DataGridView
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
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            Font = new Font("Segoe UI", 10)
        };
        d.RowTemplate.Height = 40;
        d.DefaultCellStyle.BackColor = Color.FromArgb(22, 22, 38);
        d.DefaultCellStyle.ForeColor = Color.FromArgb(200, 200, 220);
        d.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 50, 120);
        d.DefaultCellStyle.SelectionForeColor = Color.White;
        d.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(28, 28, 48);
        d.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(160, 160, 190);
        d.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        d.ColumnHeadersHeight = 42;
        d.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(26, 26, 42);
        return d;
    }
}
