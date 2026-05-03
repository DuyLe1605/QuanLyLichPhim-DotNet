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
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(18, 18, 30);
        this.Padding = new Padding(5);

        var pnlTop = new Panel { Dock = DockStyle.Top, Height = 130 };
        this.Controls.Add(pnlTop);

        // Title
        pnlTop.Controls.Add(new Label
        {
            Text = "📅  Quản Lý Lịch Chiếu",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(210, 210, 230),
            Location = new Point(5, 5),
            AutoSize = true
        });

        // === Row 2: Filters ===
        int filterY = 50;

        pnlTop.Controls.Add(new Label { Text = "Ngày:", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(150, 150, 180), Location = new Point(5, filterY + 4), AutoSize = true });
        dtpDate = new DateTimePicker
        {
            Font = new Font("Segoe UI", 10),
            Size = new Size(140, 28),
            Location = new Point(55, filterY),
            Format = DateTimePickerFormat.Short
        };
        dtpDate.ValueChanged += async (s, e) => { if (!_isLoading) await LoadDataAsync(); };
        pnlTop.Controls.Add(dtpDate);

        pnlTop.Controls.Add(new Label { Text = "Phim:", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(150, 150, 180), Location = new Point(210, filterY + 4), AutoSize = true });
        cboMovie = new ComboBox
        {
            Font = new Font("Segoe UI", 10),
            Size = new Size(200, 28),
            Location = new Point(260, filterY),
            BackColor = Color.FromArgb(30, 30, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboMovie.SelectedIndexChanged += async (s, e) => { if (!_isLoading) await LoadDataAsync(); };
        pnlTop.Controls.Add(cboMovie);

        pnlTop.Controls.Add(new Label { Text = "Phòng:", Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(150, 150, 180), Location = new Point(480, filterY + 4), AutoSize = true });
        cboRoom = new ComboBox
        {
            Font = new Font("Segoe UI", 10),
            Size = new Size(130, 28),
            Location = new Point(535, filterY),
            BackColor = Color.FromArgb(30, 30, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboRoom.SelectedIndexChanged += async (s, e) => { if (!_isLoading) await LoadDataAsync(); };
        pnlTop.Controls.Add(cboRoom);

        // === Row 3: Buttons ===
        int btnY = 90;
        var btnAdd = Btn("➕ Thêm lịch", Color.FromArgb(60, 160, 60), new Point(5, btnY));
        btnAdd.Size = new Size(145, 32);
        btnAdd.Click += BtnAdd_Click;
        pnlTop.Controls.Add(btnAdd);

        var btnDel = Btn("🗑️ Xóa", Color.FromArgb(200, 60, 60), new Point(160, btnY));
        btnDel.Click += BtnDel_Click;
        pnlTop.Controls.Add(btnDel);

        // === DataGridView ===
        dgv = StyledGrid();
        this.Controls.Add(dgv);
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
        if (ok) await LoadDataAsync();
    }

    private async void BtnDel_Click(object? s, EventArgs e)
    {
        if (dgv.CurrentRow == null) return;
        int id = (int)dgv.CurrentRow.Cells["Id"].Value;
        if (MessageBox.Show("Xóa lịch chiếu này?", "Xác nhận", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        using var ctx = Program.CreateDbContext();
        var (ok, msg) = await new ShowtimeService(ctx).DeleteAsync(id);
        MessageBox.Show(msg, ok ? "Thành công" : "Lỗi");
        if (ok) await LoadDataAsync();
    }

    private static Button Btn(string t, Color c, Point loc)
    {
        var b = new Button
        {
            Text = t,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Size = new Size(120, 32),
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
