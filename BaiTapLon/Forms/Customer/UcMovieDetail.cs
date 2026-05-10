using BaiTapLon.Models;
using BaiTapLon.Services;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;
using System.Diagnostics;

namespace BaiTapLon.Forms.Customer;

public class UcMovieDetail : UserControl
{
    private readonly Movie movie;
    private readonly FlowLayoutPanel flpShowtimes = new();
    private readonly DateTimePicker dtpDate = new();
    private readonly Label lblShowtimes = new();

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action? BackRequested { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<Showtime>? ShowtimeSelected { get; set; }

    public UcMovieDetail(Movie movie)
    {
        this.movie = movie;
        InitializeComponent();
        Load += async (s, e) => await LoadShowtimesAsync();
    }

    private void InitializeComponent()
    {
        BackColor = CustomerUi.AppBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(28),
            BackColor = Color.Transparent
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var left = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent
        };
        root.Controls.Add(left, 0, 0);

        var back = CustomerUi.PrimaryButton("← Quay lại");
        back.Width = 125;
        back.Location = new Point(0, 0);
        back.BackColor = Color.FromArgb(52, 58, 76);
        back.Click += (s, e) => BackRequested?.Invoke();
        left.Controls.Add(back);

        var poster = new PictureBox
        {
            Location = new Point(0, 58),
            Size = new Size(290, 410),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(35, 40, 56),
            Image = CustomerUi.LoadPosterImage(movie)
        };
        left.Controls.Add(poster);

        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(right, 1, 0);

        var info = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 11),
            Text =
                $"{movie.Title}\n\n" +
                $"Đạo diễn: {movie.Director ?? "-"}\n" +
                $"Diễn viên: {movie.Actors ?? "-"}\n" +
                $"Thể loại: {CustomerUi.FormatGenres(movie)}\n" +
                $"Thời lượng: {movie.Duration} phút | Phân loại: {movie.AgeRating}",
            Padding = new Padding(0, 0, 20, 0)
        };
        info.Font = new Font("Segoe UI", 11);
        right.Controls.Add(info, 0, 0);

        var desc = new Label
        {
            Text = string.IsNullOrWhiteSpace(movie.Description) ? "Chưa có mô tả." : movie.Description,
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Muted,
            Font = new Font("Segoe UI", 10),
            AutoEllipsis = true
        };
        right.Controls.Add(desc, 0, 1);

        var trailer = CustomerUi.PrimaryButton("▶ Xem trailer");
        trailer.Dock = DockStyle.Left;
        trailer.Width = 150;
        trailer.BackColor = CustomerUi.AccentBlue;
        trailer.Enabled = !string.IsNullOrWhiteSpace(movie.TrailerUrl);
        trailer.Click += (s, e) => OpenTrailer();
        right.Controls.Add(trailer, 0, 2);

        var datePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        datePanel.Controls.Add(new Label
        {
            Text = "Ngày chiếu:",
            ForeColor = CustomerUi.Muted,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            AutoSize = false,
            Size = new Size(90, 34),
            TextAlign = ContentAlignment.MiddleLeft
        });
        dtpDate.Format = DateTimePickerFormat.Short;
        dtpDate.Width = 145;
        dtpDate.Value = DateTime.Today;
        dtpDate.ValueChanged += async (s, e) => await LoadShowtimesAsync();
        datePanel.Controls.Add(dtpDate);
        right.Controls.Add(datePanel, 0, 3);

        var showtimeHost = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };
        showtimeHost.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        showtimeHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        lblShowtimes.Text = "Suất chiếu";
        lblShowtimes.ForeColor = CustomerUi.Text;
        lblShowtimes.Font = new Font("Segoe UI", 15, FontStyle.Bold);
        lblShowtimes.Dock = DockStyle.Fill;
        showtimeHost.Controls.Add(lblShowtimes, 0, 0);

        flpShowtimes.Dock = DockStyle.Fill;
        flpShowtimes.AutoScroll = true;
        flpShowtimes.WrapContents = true;
        flpShowtimes.BackColor = Color.Transparent;
        showtimeHost.Controls.Add(flpShowtimes, 0, 1);
        right.Controls.Add(showtimeHost, 0, 4);
    }

    private async Task LoadShowtimesAsync()
    {
        flpShowtimes.Controls.Clear();

        using var context = Program.CreateDbContext();
        var ticketService = new TicketService(context);
        var selectedDate = dtpDate.Value.Date;

        var showtimes = await context.Showtimes
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .Where(s => s.MovieId == movie.Id && s.IsActive && s.StartTime.Date == selectedDate)
            .OrderBy(s => s.StartTime)
            .AsNoTracking()
            .ToListAsync();

        lblShowtimes.Text = $"Suất chiếu {selectedDate:dd/MM/yyyy}";

        if (showtimes.Count == 0)
        {
            flpShowtimes.Controls.Add(new Label
            {
                Text = "Không có suất chiếu trong ngày này.",
                ForeColor = CustomerUi.Muted,
                Font = new Font("Segoe UI", 11),
                AutoSize = true,
                Padding = new Padding(4, 12, 0, 0)
            });
            return;
        }

        foreach (var showtime in showtimes)
        {
            var sold = await ticketService.CountSoldAsync(showtime.Id);
            var available = showtime.Room.TotalSeats - sold;
            var canBook = available > 0 && showtime.StartTime > DateTime.Now;

            var button = new Button
            {
                Text = $"{showtime.StartTime:HH:mm}\n{showtime.Room.Name} ({showtime.Room.Type})\nCòn {available}/{showtime.Room.TotalSeats} ghế\n{showtime.BasePrice:N0} đ",
                Size = new Size(170, 92),
                Margin = new Padding(0, 0, 12, 12),
                BackColor = canBook ? CustomerUi.CardBg : Color.FromArgb(35, 38, 48),
                ForeColor = canBook ? CustomerUi.Text : Color.FromArgb(100, 105, 120),
                FlatStyle = FlatStyle.Flat,
                Enabled = canBook,
                Cursor = canBook ? Cursors.Hand : Cursors.No,
                TextAlign = ContentAlignment.MiddleCenter,
                Tag = showtime
            };
            button.FlatAppearance.BorderColor = CustomerUi.Border;
            button.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 56, 70);
            button.Click += (s, e) =>
            {
                if (button.Tag is Showtime selected)
                    ShowtimeSelected?.Invoke(selected);
            };
            flpShowtimes.Controls.Add(button);
        }
    }

    private void OpenTrailer()
    {
        if (string.IsNullOrWhiteSpace(movie.TrailerUrl)) return;

        try
        {
            Process.Start(new ProcessStartInfo(movie.TrailerUrl) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không mở được trailer: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
