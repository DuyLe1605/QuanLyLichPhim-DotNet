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

    public DlgShowtimeEdit(List<Movie> movies, List<Room> rooms)
    {
        _movies = movies; _rooms = rooms; Init();
    }

    private void Init()
    {
        this.Text = "Thêm lịch chiếu";
        this.ClientSize = new Size(420, 330);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false; this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(24, 24, 40);
        this.ForeColor = Color.FromArgb(200, 200, 220);

        int x1 = 20, x2 = 160, y = 20;

        Lbl("Phim *", x1, y);
        cboMovie = new ComboBox { Font = new Font("Segoe UI", 11), Size = new Size(230, 30), Location = new Point(x2, y), BackColor = Color.FromArgb(35, 35, 55), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var m in _movies) cboMovie.Items.Add($"{m.Title} ({m.Duration}p)");
        if (cboMovie.Items.Count > 0) cboMovie.SelectedIndex = 0;
        this.Controls.Add(cboMovie); y += 45;

        Lbl("Phòng *", x1, y);
        cboRoom = new ComboBox { Font = new Font("Segoe UI", 11), Size = new Size(230, 30), Location = new Point(x2, y), BackColor = Color.FromArgb(35, 35, 55), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DropDownStyle = ComboBoxStyle.DropDownList };
        foreach (var r in _rooms) cboRoom.Items.Add($"{r.Name} ({r.Type})");
        if (cboRoom.Items.Count > 0) cboRoom.SelectedIndex = 0;
        this.Controls.Add(cboRoom); y += 45;

        Lbl("Ngày chiếu", x1, y);
        dtpDate = new DateTimePicker { Font = new Font("Segoe UI", 11), Size = new Size(150, 30), Location = new Point(x2, y), Format = DateTimePickerFormat.Short };
        this.Controls.Add(dtpDate); y += 45;

        Lbl("Giờ bắt đầu", x1, y);
        dtpTime = new DateTimePicker { Font = new Font("Segoe UI", 11), Size = new Size(120, 30), Location = new Point(x2, y), Format = DateTimePickerFormat.Time, ShowUpDown = true, Value = DateTime.Today.AddHours(9) };
        this.Controls.Add(dtpTime); y += 45;

        Lbl("Giá vé cơ bản", x1, y);
        nudPrice = new NumericUpDown { Font = new Font("Segoe UI", 11), Size = new Size(150, 30), Location = new Point(x2, y), BackColor = Color.FromArgb(35, 35, 55), ForeColor = Color.White, Minimum = 10000, Maximum = 500000, Value = 75000, Increment = 5000, ThousandsSeparator = true };
        this.Controls.Add(nudPrice);

        var btnOk = new Button { Text = "Tạo lịch chiếu", Font = new Font("Segoe UI", 11, FontStyle.Bold), Size = new Size(150, 38), Location = new Point(130, 280), BackColor = Color.FromArgb(80, 160, 80), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.OK };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (s, e) =>
        {
            if (cboMovie.SelectedIndex < 0 || cboRoom.SelectedIndex < 0) { MessageBox.Show("Chọn phim và phòng!"); this.DialogResult = DialogResult.None; return; }
            var date = dtpDate.Value.Date;
            var time = dtpTime.Value.TimeOfDay;
            ShowtimeData = new Showtime
            {
                MovieId = _movies[cboMovie.SelectedIndex].Id,
                RoomId = _rooms[cboRoom.SelectedIndex].Id,
                StartTime = date.Add(time),
                BasePrice = nudPrice.Value
            };
        };
        this.Controls.Add(btnOk);

        var btnC = new Button { Text = "Hủy", Font = new Font("Segoe UI", 10), Size = new Size(80, 38), Location = new Point(290, 280), BackColor = Color.FromArgb(50, 50, 75), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, DialogResult = DialogResult.Cancel };
        btnC.FlatAppearance.BorderSize = 0; this.Controls.Add(btnC);
        this.AcceptButton = btnOk; this.CancelButton = btnC;
    }

    private void Lbl(string t, int x, int y) { this.Controls.Add(new Label { Text = t, Font = new Font("Segoe UI", 10), ForeColor = Color.FromArgb(160, 160, 185), Location = new Point(x, y + 4), AutoSize = true }); }
}
