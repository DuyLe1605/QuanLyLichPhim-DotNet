using BaiTapLon.Models;
using BaiTapLon.Services;

namespace BaiTapLon.Forms.Admin;

public class UcShowtimeManagement : UserControl
{
    private DataGridView dgv = null!;
    private DateTimePicker dtpDate = null!;
    private ComboBox cboMovie = null!;
    private ComboBox cboRoom = null!;

    private List<Movie> _movies = new();
    private List<Room> _rooms = new();
    private bool _isLoading = false;

    public UcShowtimeManagement()
    {
        InitUI();
        this.Load += async (s, e) => await LoadFiltersAndDataAsync();
    }

    private void InitUI()
    {
        AdminControls.ConfigurePage(this);

        dtpDate = AdminControls.CreateDatePicker();
        dtpDate.ValueChanged += async (s, e) => { if (!_isLoading) await LoadDataAsync(); };

        cboMovie = AdminControls.CreateComboBox(200);
        cboMovie.SelectedIndexChanged += async (s, e) => { if (!_isLoading) await LoadDataAsync(); };

        cboRoom = AdminControls.CreateComboBox(130);
        cboRoom.SelectedIndexChanged += async (s, e) => { if (!_isLoading) await LoadDataAsync(); };

        dgv = AdminControls.CreateGrid();
        dgv.DoubleClick += BtnEdit_Click;

        var toolbar = AdminControls.CreateToolbar(
            AdminControls.CreateToolbarLabel("Ngày:", 48),
            dtpDate,
            AdminControls.CreateToolbarLabel("Phim:", 45),
            cboMovie,
            AdminControls.CreateToolbarLabel("Phòng:", 55),
            cboRoom,
            AdminControls.CreateButton("➕ Thêm lịch", AdminTheme.ButtonSuccess, 135, BtnAdd_Click),
            AdminControls.CreateButton("✏️ Sửa", AdminTheme.ButtonPrimary, 100, BtnEdit_Click),
            AdminControls.CreateButton("🗑️ Xóa", AdminTheme.ButtonDanger, 100, BtnDel_Click)
        );

        Controls.Add(AdminLayouts.CreateManagementPage("📅  Quản Lý Lịch Chiếu", toolbar, dgv));
    }

    private async Task LoadFiltersAndDataAsync()
    {
        _isLoading = true;
        try
        {
            using var ctx = Program.CreateDbContext();
            _movies = await new MovieService(ctx).GetAllAsync();
            _rooms = await new RoomService(ctx).GetAllAsync();

            cboMovie.Items.Clear();
            cboMovie.Items.Add("-- Tất cả --");
            foreach (var m in _movies) cboMovie.Items.Add(m.Title);
            cboMovie.SelectedIndex = 0;

            cboRoom.Items.Clear();
            cboRoom.Items.Add("-- Tất cả --");
            foreach (var r in _rooms) cboRoom.Items.Add(r.Name);
            cboRoom.SelectedIndex = 0;
        }
        finally
        {
            _isLoading = false;
        }

        await LoadDataAsync();
    }

    private async Task LoadDataAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();
            int? mId = cboMovie.SelectedIndex > 0 ? _movies[cboMovie.SelectedIndex - 1].Id : null;
            int? rId = cboRoom.SelectedIndex > 0 ? _rooms[cboRoom.SelectedIndex - 1].Id : null;

            var list = await new ShowtimeService(ctx).GetAllAsync(dtpDate.Value.Date, mId, rId);
            dgv.DataSource = list.Select(s => new
            {
                s.Id,
                Phim = s.Movie.Title,
                Phòng = s.Room.Name,
                BắtĐầu = s.StartTime.ToString("HH:mm"),
                KếtThúc = s.EndTime.ToString("HH:mm"),
                Ngày = s.StartTime.ToString("dd/MM/yyyy"),
                GiáVé = s.BasePrice.ToString("N0") + " đ"
            }).ToList();
            if (dgv.Columns.Contains("Id")) dgv.Columns["Id"].Visible = false;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
        }
    }

    private async void BtnAdd_Click(object? s, EventArgs e)
    {
        using var dlg = new DlgShowtimeEdit(_movies, _rooms);
        if (dlg.ShowDialog() != DialogResult.OK) return;
        using var ctx = Program.CreateDbContext();
        var (ok, msg) = await new ShowtimeService(ctx).CreateAsync(dlg.ShowtimeData);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok)
        {
            dtpDate.Value = dlg.ShowtimeData.StartTime.Date;
            await LoadDataAsync();
        }
    }

    private async void BtnEdit_Click(object? s, EventArgs e)
    {
        int? id = GetCurrentShowtimeId();
        if (!id.HasValue) return;

        using var ctx = Program.CreateDbContext();
        var service = new ShowtimeService(ctx);
        var showtime = await service.GetByIdAsync(id.Value);
        if (showtime == null) return;

        using var dlg = new DlgShowtimeEdit(_movies, _rooms, showtime);
        if (dlg.ShowDialog() != DialogResult.OK) return;

        var (ok, msg) = await service.UpdateAsync(dlg.ShowtimeData);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok)
        {
            dtpDate.Value = dlg.ShowtimeData.StartTime.Date;
            await LoadDataAsync();
        }
    }

    private async void BtnDel_Click(object? s, EventArgs e)
    {
        int? id = GetCurrentShowtimeId();
        if (!id.HasValue) return;
        if (MessageBox.Show("Xóa lịch chiếu này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        using var ctx = Program.CreateDbContext();
        var (ok, msg) = await new ShowtimeService(ctx).DeleteAsync(id.Value);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadDataAsync();
    }

    private int? GetCurrentShowtimeId()
    {
        if (dgv.CurrentRow == null) return null;
        if (!dgv.Columns.Contains("Id")) return null;
        return dgv.CurrentRow.Cells["Id"].Value is int id ? id : null;
    }

}
