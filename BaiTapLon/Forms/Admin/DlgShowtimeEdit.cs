using BaiTapLon.Models;

namespace BaiTapLon.Forms.Admin;

public class DlgShowtimeEdit : Form
{
    private ComboBox cboMovie = null!;
    private ComboBox cboRoom = null!;
    private DateTimePicker dtpDate = null!;
    private DateTimePicker dtpTime = null!;
    private NumericUpDown nudPrice = null!;

    public Showtime ShowtimeData { get; private set; } = new();
    private readonly List<Movie> _movies;
    private readonly List<Room> _rooms;
    private readonly Showtime? _edit;

    public DlgShowtimeEdit(List<Movie> movies, List<Room> rooms, Showtime? edit = null)
    {
        _movies = movies;
        _rooms = rooms;
        _edit = edit;
        InitUI();
        if (_edit != null) LoadEditData();
    }

    private void InitUI()
    {
        bool isEdit = _edit != null;
        this.Text = isEdit ? "Sửa lịch chiếu" : "Thêm lịch chiếu";
        this.ClientSize = new Size(460, 350);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(24, 24, 40);
        this.ForeColor = Color.FromArgb(200, 200, 220);
        this.Padding = new Padding(15);

        // === TableLayoutPanel ===
        var tbl = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 7,
            BackColor = Color.Transparent
        };
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30));
        tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70));
        for (int i = 0; i < 7; i++)
            tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        int row = 0;

        // Phim
        tbl.Controls.Add(MakeLbl("Phim *"), 0, row);
        cboMovie = new ComboBox
        {
            Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(35, 35, 55), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 6, 0, 6)
        };
        foreach (var m in _movies) cboMovie.Items.Add($"{m.Title} ({m.Duration}p)");
        if (cboMovie.Items.Count > 0) cboMovie.SelectedIndex = 0;
        tbl.Controls.Add(cboMovie, 1, row++);

        // Phòng
        tbl.Controls.Add(MakeLbl("Phòng *"), 0, row);
        cboRoom = new ComboBox
        {
            Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(35, 35, 55), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 6, 0, 6)
        };
        foreach (var r in _rooms) cboRoom.Items.Add($"{r.Name} ({r.Type})");
        if (cboRoom.Items.Count > 0) cboRoom.SelectedIndex = 0;
        tbl.Controls.Add(cboRoom, 1, row++);

        // Ngày chiếu
        tbl.Controls.Add(MakeLbl("Ngày chiếu"), 0, row);
        dtpDate = new DateTimePicker
        {
            Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Short,
            Margin = new Padding(0, 6, 0, 6)
        };
        tbl.Controls.Add(dtpDate, 1, row++);

        // Giờ bắt đầu
        tbl.Controls.Add(MakeLbl("Giờ bắt đầu"), 0, row);
        dtpTime = new DateTimePicker
        {
            Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Time, ShowUpDown = true,
            Value = GetDefaultStartTime(),
            Margin = new Padding(0, 6, 0, 6)
        };
        tbl.Controls.Add(dtpTime, 1, row++);

        // Giá vé
        tbl.Controls.Add(MakeLbl("Giá vé cơ bản"), 0, row);
        nudPrice = new NumericUpDown
        {
            Font = new Font("Segoe UI", 11), Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(35, 35, 55), ForeColor = Color.White,
            Minimum = 10000, Maximum = 500000, Value = 75000,
            Increment = 5000, ThousandsSeparator = true,
            Margin = new Padding(0, 6, 0, 6)
        };
        tbl.Controls.Add(nudPrice, 1, row++);

        // Empty spacer row
        row++;

        // Buttons
        var flpBtns = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var btnCancel = new Button
        {
            Text = "Hủy", Font = new Font("Segoe UI", 10),
            Size = new Size(90, 38),
            BackColor = Color.FromArgb(50, 50, 75), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel,
            Margin = new Padding(0, 4, 0, 0)
        };
        btnCancel.FlatAppearance.BorderSize = 0;

        var btnOk = new Button
        {
            Text = isEdit ? "💾 Lưu lịch chiếu" : "✅ Tạo lịch chiếu", Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Size = new Size(160, 38),
            BackColor = Color.FromArgb(80, 160, 80), ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK,
            Margin = new Padding(0, 4, 8, 0)
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) =>
        {
            if (cboMovie.SelectedIndex < 0 || cboRoom.SelectedIndex < 0)
            {
                MessageBox.Show("Chọn phim và phòng!", "Thiếu thông tin");
                this.DialogResult = DialogResult.None;
                return;
            }
            var date = dtpDate.Value.Date;
            var time = dtpTime.Value.TimeOfDay;
            ShowtimeData = new Showtime
            {
                Id = _edit?.Id ?? 0,
                MovieId = _movies[cboMovie.SelectedIndex].Id,
                RoomId = _rooms[cboRoom.SelectedIndex].Id,
                StartTime = date.Add(time),
                BasePrice = nudPrice.Value
            };
        };

        flpBtns.Controls.Add(btnCancel);
        flpBtns.Controls.Add(btnOk);
        tbl.Controls.Add(flpBtns, 0, row);
        tbl.SetColumnSpan(flpBtns, 2);

        this.Controls.Add(tbl);
        this.AcceptButton = btnOk;
        this.CancelButton = btnCancel;
    }

    private void LoadEditData()
    {
        if (_edit == null) return;

        int movieIndex = _movies.FindIndex(m => m.Id == _edit.MovieId);
        if (movieIndex >= 0) cboMovie.SelectedIndex = movieIndex;

        int roomIndex = _rooms.FindIndex(r => r.Id == _edit.RoomId);
        if (roomIndex >= 0) cboRoom.SelectedIndex = roomIndex;

        dtpDate.Value = _edit.StartTime.Date;
        dtpTime.Value = DateTime.Today.Add(_edit.StartTime.TimeOfDay);
        nudPrice.Value = Math.Min(nudPrice.Maximum, Math.Max(nudPrice.Minimum, _edit.BasePrice));
    }

    private static DateTime GetDefaultStartTime()
    {
        var now = DateTime.Now;
        int minute = now.Minute < 30 ? 30 : 0;
        int hour = minute == 30 ? now.Hour : now.Hour + 1;
        if (hour > 23) hour = 23;
        return DateTime.Today.AddHours(hour).AddMinutes(minute);
    }

    private static Label MakeLbl(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 10),
        ForeColor = Color.FromArgb(160, 160, 185),
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };
}
