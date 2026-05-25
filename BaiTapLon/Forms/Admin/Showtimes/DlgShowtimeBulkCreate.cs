using BaiTapLon.Models;

namespace BaiTapLon.Forms.Admin;

/// <summary>
/// Dialog tạo nhiều lịch chiếu trong một lần: chọn 1 phim, chọn nhiều phòng, thêm nhiều giờ.
/// (Dùng cho luồng tạo lịch chiếu linh hoạt hơn.)
/// </summary>
public class DlgShowtimeBulkCreate : Form
{
    private ComboBox cboMovie = null!;
    private DateTimePicker dtpDate = null!;
    private ListBox lstRooms = null!;

    private DateTimePicker dtpTime = null!;
    private ListBox lstTimes = null!;

    private NumericUpDown nudPrice = null!;
    private Label lblCount = null!;
    private ErrorProvider errorProvider = null!;

    private readonly List<Movie> _allMovies;
    private readonly List<Room> _rooms;
    private List<Movie> _filteredMovies = new();
    private readonly SortedSet<TimeSpan> _times = new();

    public DateTime SelectedDate => dtpDate.Value.Date;

    /// <summary>
    /// Danh sách lịch chiếu sẽ tạo (MovieId/RoomId/StartTime/BasePrice).
    /// EndTime sẽ được ShowtimeService tự tính.
    /// </summary>
    public List<Showtime> ShowtimesToCreate { get; private set; } = new();

    public DlgShowtimeBulkCreate(List<Movie> movies, List<Room> rooms, DateTime? defaultDate = null)
    {
        _allMovies = movies;
        _rooms = rooms;

        InitUI(defaultDate);
        RefreshRoomList();
        RefreshMovieList();
        RefreshTimesList();
        UpdateCount();
    }

    private void InitUI(DateTime? defaultDate)
    {
        Text = "Tạo nhiều lịch chiếu";
        ClientSize = new Size(740, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = Color.FromArgb(24, 24, 40);
        ForeColor = Color.FromArgb(200, 200, 220);
        Padding = new Padding(14);

        errorProvider = new ErrorProvider
        {
            ContainerControl = this,
            BlinkStyle = ErrorBlinkStyle.NeverBlink
        };

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            BackColor = Color.Transparent
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 52));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 60));
        Controls.Add(root);

        // ===== Left column =====
        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 12, 0)
        };
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        left.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(left, 0, 0);

        left.Controls.Add(MakeLbl("Phim *"), 0, 0);
        cboMovie = MakeComboBox();
        left.Controls.Add(cboMovie, 1, 0);

        left.Controls.Add(MakeLbl("Ngày *"), 0, 1);
        dtpDate = new DateTimePicker
        {
            Font = new Font("Segoe UI", 11),
            Dock = DockStyle.Fill,
            Format = DateTimePickerFormat.Short,
            Margin = new Padding(0, 6, 0, 6),
            Value = (defaultDate ?? DateTime.Today).Date
        };
        dtpDate.ValueChanged += (s, e) =>
        {
            RefreshMovieList();
            UpdateCount();
        };
        left.Controls.Add(dtpDate, 1, 1);

        left.Controls.Add(MakeLbl("Phòng *"), 0, 2);
        lstRooms = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            SelectionMode = SelectionMode.MultiExtended,
            IntegralHeight = false,
            Margin = new Padding(0, 6, 0, 6)
        };
        lstRooms.SelectedIndexChanged += (s, e) => UpdateCount();
        left.Controls.Add(lstRooms, 1, 2);

        // ===== Right column =====
        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            BackColor = Color.Transparent,
            Padding = new Padding(12, 0, 0, 0)
        };
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        root.Controls.Add(right, 1, 0);

        right.Controls.Add(MakeLbl("Giờ bắt đầu"), 0, 0);
        var pnlAddTime = new Panel { Dock = DockStyle.Fill, BackColor = Color.Transparent };
        dtpTime = new DateTimePicker
        {
            Font = new Font("Segoe UI", 11),
            Size = new Size(140, 30),
            Location = new Point(0, 6),
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = GetDefaultStartTime()
        };
        var btnAddTime = new Button
        {
            Text = "➕",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Size = new Size(44, 30),
            Location = new Point(150, 6),
            BackColor = Color.FromArgb(80, 160, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnAddTime.FlatAppearance.BorderSize = 0;
        btnAddTime.Click += (s, e) =>
        {
            _times.Add(dtpTime.Value.TimeOfDay);
            RefreshTimesList();
            UpdateCount();
        };
        pnlAddTime.Controls.Add(dtpTime);
        pnlAddTime.Controls.Add(btnAddTime);
        right.Controls.Add(pnlAddTime, 1, 0);

        right.Controls.Add(MakeLbl("Giờ đã chọn *"), 0, 1);
        var pnlTimeBtns = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 6, 0, 6)
        };

        var btnRemove = new Button
        {
            Text = "🗑 Xóa",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            AutoSize = true,
            BackColor = Color.FromArgb(200, 60, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnRemove.FlatAppearance.BorderSize = 0;
        btnRemove.Click += (s, e) =>
        {
            var selected = lstTimes.SelectedItems.Cast<string>().ToList();
            foreach (var t in selected)
            {
                if (TimeSpan.TryParse(t, out var ts)) _times.Remove(ts);
            }
            RefreshTimesList();
            UpdateCount();
        };

        var btnClear = new Button
        {
            Text = "✖ Xóa hết",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            AutoSize = true,
            BackColor = Color.FromArgb(50, 50, 75),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnClear.FlatAppearance.BorderSize = 0;
        btnClear.Click += (s, e) =>
        {
            _times.Clear();
            RefreshTimesList();
            UpdateCount();
        };

        pnlTimeBtns.Controls.Add(btnRemove);
        pnlTimeBtns.Controls.Add(btnClear);
        right.Controls.Add(pnlTimeBtns, 1, 1);

        right.Controls.Add(MakeLbl("Giá vé *"), 0, 2);
        nudPrice = new NumericUpDown
        {
            Font = new Font("Segoe UI", 11),
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            Minimum = 10000,
            Maximum = 500000,
            Value = 75000,
            Increment = 5000,
            ThousandsSeparator = true,
            Margin = new Padding(0, 6, 0, 6)
        };
        right.Controls.Add(nudPrice, 1, 2);

        right.Controls.Add(MakeLbl("Danh sách giờ"), 0, 3);
        lstTimes = new ListBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            SelectionMode = SelectionMode.MultiExtended,
            IntegralHeight = false,
            Margin = new Padding(0, 6, 0, 6)
        };
        right.Controls.Add(lstTimes, 1, 3);

        lblCount = new Label
        {
            Text = "Sẽ tạo: 0 lịch chiếu",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(180, 180, 210),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        right.Controls.Add(lblCount, 1, 4);

        // ===== Bottom buttons =====
        var flpBtns = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent
        };

        var btnCancel = new Button
        {
            Text = "Hủy",
            Font = new Font("Segoe UI", 10),
            Size = new Size(100, 40),
            BackColor = Color.FromArgb(50, 50, 75),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel,
            Margin = new Padding(0, 10, 0, 0)
        };
        btnCancel.FlatAppearance.BorderSize = 0;

        var btnOk = new Button
        {
            Text = "✅ Tạo lịch",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Size = new Size(140, 40),
            BackColor = Color.FromArgb(80, 160, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.OK,
            Margin = new Padding(8, 10, 0, 0)
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += BtnOk_Click;

        flpBtns.Controls.Add(btnCancel);
        flpBtns.Controls.Add(btnOk);

        root.Controls.Add(flpBtns, 0, 1);
        root.SetColumnSpan(flpBtns, 2);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        errorProvider.Clear();

        if (cboMovie.SelectedIndex < 0)
        {
            errorProvider.SetError(cboMovie, "Chọn phim.");
            DialogResult = DialogResult.None;
            return;
        }

        if (lstRooms.SelectedItems.Count == 0)
        {
            errorProvider.SetError(lstRooms, "Chọn ít nhất 1 phòng.");
            DialogResult = DialogResult.None;
            return;
        }

        if (_times.Count == 0)
        {
            errorProvider.SetError(lstTimes, "Thêm ít nhất 1 giờ bắt đầu.");
            DialogResult = DialogResult.None;
            return;
        }

        if (nudPrice.Value <= 0)
        {
            errorProvider.SetError(nudPrice, "Giá vé phải lớn hơn 0.");
            DialogResult = DialogResult.None;
            return;
        }

        var movieId = _filteredMovies[cboMovie.SelectedIndex].Id;
        var date = dtpDate.Value.Date;
        var basePrice = nudPrice.Value;

        var roomIds = lstRooms.SelectedItems
            .Cast<RoomListItem>()
            .Select(x => x.Room.Id)
            .Distinct()
            .ToList();

        ShowtimesToCreate = new List<Showtime>();
        foreach (var roomId in roomIds)
        {
            foreach (var time in _times)
            {
                ShowtimesToCreate.Add(new Showtime
                {
                    MovieId = movieId,
                    RoomId = roomId,
                    StartTime = date.Add(time),
                    BasePrice = basePrice
                });
            }
        }

        if (ShowtimesToCreate.Count == 0)
        {
            DialogResult = DialogResult.None;
            return;
        }
    }

    private void RefreshMovieList()
    {
        var date = dtpDate.Value.Date;

        // Hide movies whose EndDate < selected date
        _filteredMovies = _allMovies
            .Where(m => m.IsActive && (!m.EndDate.HasValue || m.EndDate.Value.Date >= date))
            .OrderBy(m => m.Title)
            .ToList();

        cboMovie.Items.Clear();
        foreach (var m in _filteredMovies)
            cboMovie.Items.Add($"{m.Title} ({m.Duration}p)");

        cboMovie.SelectedIndex = _filteredMovies.Count > 0 ? 0 : -1;
    }

    private void RefreshRoomList()
    {
        lstRooms.Items.Clear();
        foreach (var r in _rooms)
            lstRooms.Items.Add(new RoomListItem(r));
    }

    private void RefreshTimesList()
    {
        lstTimes.Items.Clear();
        foreach (var t in _times.OrderBy(t => t))
            lstTimes.Items.Add(t.ToString(@"hh\:mm"));
    }

    private void UpdateCount()
    {
        int roomCount = lstRooms.SelectedItems.Count;
        int timeCount = _times.Count;
        lblCount.Text = $"Sẽ tạo: {roomCount * timeCount} lịch chiếu";
    }

    private static DateTime GetDefaultStartTime()
    {
        var now = DateTime.Now;
        int minute = now.Minute < 30 ? 30 : 0;
        int hour = minute == 30 ? now.Hour : now.Hour + 1;
        if (hour > 23) hour = 23;
        return DateTime.Today.AddHours(hour).AddMinutes(minute);
    }

    private static ComboBox MakeComboBox() => new()
    {
        Font = new Font("Segoe UI", 11),
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(35, 35, 55),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        DropDownStyle = ComboBoxStyle.DropDownList,
        Margin = new Padding(0, 6, 0, 6)
    };

    private static Label MakeLbl(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 10),
        ForeColor = Color.FromArgb(160, 160, 185),
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private sealed class RoomListItem
    {
        public Room Room { get; }
        public RoomListItem(Room room) { Room = room; }
        public override string ToString() => $"{Room.Name} ({Room.Type})";
    }
}
