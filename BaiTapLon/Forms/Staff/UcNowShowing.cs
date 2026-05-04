using Microsoft.EntityFrameworkCore;
using BaiTapLon.Models;
using BaiTapLon.Services;

namespace BaiTapLon.Forms.Staff;

/// <summary>
/// Hiển thị phim đang chiếu + suất chiếu trong ngày.
/// Click suất chiếu → chuyển sang chọn ghế (UcSeatSelection).
/// </summary>
public class UcNowShowing : UserControl
{
    private FlowLayoutPanel flpMovies = null!;
    private Panel pnlShowtimes = null!;
    private FlowLayoutPanel flpShowtimes = null!;
    private Label lblSelectedMovie = null!;
    private PictureBox picSelected = null!;
    private Label lblMovieInfo = null!;
    private DateTimePicker dtpShowDate = null!;
    private Label lblShowtimeTitle = null!;

    private List<Movie> _movies = new();
    private Movie? _selectedMovie;

    /// <summary>
    /// Event khi chọn suất chiếu → chuyển sang chọn ghế.
    /// </summary>
    public event Action<Showtime>? ShowtimeSelected;

    public UcNowShowing()
    {
        InitUI();
        this.Load += async (s, e) => await LoadMoviesAsync();
    }

    private void InitUI()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(18, 18, 30);

        // === Tiêu đề ===
        var pnlTitle = new Panel { Dock = DockStyle.Top, Height = 55 };
        pnlTitle.Controls.Add(new Label
        {
            Text = "🎬  Phim Đang Chiếu",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(210, 210, 230),
            Location = new Point(5, 10),
            AutoSize = true
        });

        pnlTitle.Controls.Add(new Label
        {
            Text = "Ngày chiếu:",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(150, 150, 180),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(520, 19)
        });

        dtpShowDate = new DateTimePicker
        {
            Font = new Font("Segoe UI", 10),
            Format = DateTimePickerFormat.Short,
            Size = new Size(135, 28),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(605, 14)
        };
        dtpShowDate.ValueChanged += async (s, e) => await LoadMoviesAsync();
        pnlTitle.Controls.Add(dtpShowDate);

        pnlTitle.Resize += (s, e) =>
        {
            dtpShowDate.Location = new Point(Math.Max(260, pnlTitle.Width - 150), 14);
            foreach (Control c in pnlTitle.Controls)
            {
                if (c is Label { Text: "Ngày chiếu:" })
                    c.Location = new Point(dtpShowDate.Left - 85, 19);
            }
        };
        this.Controls.Add(pnlTitle);

        // === Panel chọn suất chiếu (bên phải) ===
        pnlShowtimes = new Panel
        {
            Dock = DockStyle.Right,
            Width = 340,
            BackColor = Color.FromArgb(22, 22, 38),
            Padding = new Padding(15),
            Visible = false
        };
        this.Controls.Add(pnlShowtimes);

        // Poster phim đã chọn
        picSelected = new PictureBox
        {
            Size = new Size(120, 170),
            Location = new Point(15, 15),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(35, 35, 55)
        };
        pnlShowtimes.Controls.Add(picSelected);

        // Tên phim + thông tin
        lblSelectedMovie = new Label
        {
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(220, 220, 240),
            Location = new Point(145, 15),
            Size = new Size(180, 50),
            MaximumSize = new Size(180, 0),
            AutoSize = true
        };
        pnlShowtimes.Controls.Add(lblSelectedMovie);

        lblMovieInfo = new Label
        {
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(140, 140, 170),
            Location = new Point(145, 70),
            Size = new Size(180, 120),
            MaximumSize = new Size(180, 0),
            AutoSize = true
        };
        pnlShowtimes.Controls.Add(lblMovieInfo);

        // Separator
        pnlShowtimes.Controls.Add(new Panel
        {
            Location = new Point(15, 195),
            Size = new Size(300, 1),
            BackColor = Color.FromArgb(50, 50, 75)
        });

        // Label ngày chiếu
        lblShowtimeTitle = new Label
        {
            Text = "📅  Suất chiếu",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(180, 180, 210),
            Location = new Point(15, 205),
            AutoSize = true
        };
        pnlShowtimes.Controls.Add(lblShowtimeTitle);

        // Flow cho các nút suất chiếu
        flpShowtimes = new FlowLayoutPanel
        {
            Location = new Point(15, 235),
            Size = new Size(310, 400),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Bottom,
            AutoScroll = true,
            BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true
        };
        pnlShowtimes.Controls.Add(flpShowtimes);

        // === Danh sách phim (FlowLayoutPanel) ===
        flpMovies = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.Transparent,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(10, 5, 10, 5)
        };
        this.Controls.Add(flpMovies);
        flpMovies.BringToFront();
    }

    private async Task LoadMoviesAsync()
    {
        try
        {
            using var ctx = Program.CreateDbContext();

            var selectedDate = dtpShowDate.Value.Date;

            // Lấy phim có suất chiếu trong ngày đang chọn.
            _movies = await ctx.Movies
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.Showtimes)
                .Where(m => m.IsActive && m.Showtimes.Any(s => s.IsActive && s.StartTime.Date == selectedDate))
                .AsNoTracking()
                .OrderBy(m => m.Title)
                .ToListAsync();

            RenderMovieCards();
            pnlShowtimes.Visible = false;
            _selectedMovie = null;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RenderMovieCards()
    {
        flpMovies.Controls.Clear();

        if (_movies.Count == 0)
        {
            flpMovies.Controls.Add(new Label
            {
                Text = $"📭  Không có phim nào có suất chiếu ngày {dtpShowDate.Value:dd/MM/yyyy}.",
                Font = new Font("Segoe UI", 14),
                ForeColor = Color.FromArgb(100, 100, 130),
                AutoSize = true,
                Padding = new Padding(30, 50, 0, 0)
            });
            return;
        }

        foreach (var movie in _movies)
        {
            var card = CreateMovieCard(movie);
            flpMovies.Controls.Add(card);
        }
    }

    private Panel CreateMovieCard(Movie movie)
    {
        var card = new Panel
        {
            Size = new Size(175, 310),
            BackColor = Color.FromArgb(28, 28, 48),
            Margin = new Padding(8),
            Cursor = Cursors.Hand,
            Tag = movie
        };

        // Poster
        var pic = new PictureBox
        {
            Size = new Size(155, 210),
            Location = new Point(10, 10),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(40, 40, 60),
            Cursor = Cursors.Hand
        };
        pic.Image = LoadPosterImage(movie);
        card.Controls.Add(pic);

        // Tên phim
        var lblTitle = new Label
        {
            Text = movie.Title,
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            ForeColor = Color.FromArgb(220, 220, 240),
            Location = new Point(10, 225),
            Size = new Size(155, 36),
            MaximumSize = new Size(155, 36),
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };
        card.Controls.Add(lblTitle);

        // Thời lượng + Độ tuổi
        var lblInfo = new Label
        {
            Text = $"⏱ {movie.Duration}p  |  {movie.AgeRating}",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(130, 130, 160),
            Location = new Point(10, 262),
            AutoSize = true,
            Cursor = Cursors.Hand
        };
        card.Controls.Add(lblInfo);

        // Thể loại
        string genres = string.Join(", ", movie.MovieGenres.Select(mg => mg.Genre.Name).Take(2));
        var lblGenre = new Label
        {
            Text = genres,
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = Color.FromArgb(100, 100, 135),
            Location = new Point(10, 280),
            Size = new Size(155, 18),
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        };
        card.Controls.Add(lblGenre);

        // Hover effect
        void OnHover(object? s, EventArgs e) => card.BackColor = Color.FromArgb(45, 40, 75);
        void OnLeave(object? s, EventArgs e) =>
            card.BackColor = _selectedMovie?.Id == movie.Id
                ? Color.FromArgb(50, 45, 85)
                : Color.FromArgb(28, 28, 48);

        void OnClick(object? s, EventArgs e) => SelectMovie(movie);

        card.MouseEnter += OnHover;
        card.MouseLeave += OnLeave;
        card.Click += OnClick;

        // Propagate events to children
        foreach (Control c in card.Controls)
        {
            c.MouseEnter += OnHover;
            c.MouseLeave += OnLeave;
            c.Click += OnClick;
        }

        // Rounded corners
        card.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(45, 45, 70), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
        };

        return card;
    }

    private async void SelectMovie(Movie movie)
    {
        _selectedMovie = movie;
        pnlShowtimes.Visible = true;

        // Highlight card đã chọn
        foreach (Control c in flpMovies.Controls)
        {
            if (c is Panel p)
                p.BackColor = (p.Tag as Movie)?.Id == movie.Id
                    ? Color.FromArgb(50, 45, 85)
                    : Color.FromArgb(28, 28, 48);
        }

        // Cập nhật panel phải
        lblSelectedMovie.Text = movie.Title;
        string genres = string.Join(", ", movie.MovieGenres.Select(mg => mg.Genre.Name));
        lblMovieInfo.Text = $"⏱  {movie.Duration} phút\n" +
                           $"🎬  {movie.Director ?? "N/A"}\n" +
                           $"📌  {movie.AgeRating}\n" +
                           $"🏷️  {genres}";

        picSelected.Image = LoadPosterImage(movie);

        // Load suất chiếu theo ngày đang chọn
        await LoadShowtimesAsync(movie.Id);
    }

    private async Task LoadShowtimesAsync(int movieId)
    {
        flpShowtimes.Controls.Clear();

        try
        {
            using var ctx = Program.CreateDbContext();
            var ticketService = new TicketService(ctx);
            var selectedDate = dtpShowDate.Value.Date;

            lblShowtimeTitle.Text = selectedDate == DateTime.Today
                ? "📅  Suất chiếu hôm nay"
                : $"📅  Suất chiếu {selectedDate:dd/MM/yyyy}";

            var showtimes = await ctx.Showtimes
                .Include(s => s.Room)
                .Where(s => s.MovieId == movieId
                         && s.IsActive
                         && s.StartTime.Date == selectedDate)
                .OrderBy(s => s.StartTime)
                .AsNoTracking()
                .ToListAsync();

            if (showtimes.Count == 0)
            {
                flpShowtimes.Controls.Add(new Label
                {
                    Text = $"Không có suất chiếu nào\nngày {selectedDate:dd/MM/yyyy}.",
                    Font = new Font("Segoe UI", 10),
                    ForeColor = Color.FromArgb(100, 100, 130),
                    AutoSize = true,
                    Padding = new Padding(5)
                });
                return;
            }

            foreach (var st in showtimes)
            {
                int sold = await ticketService.CountSoldAsync(st.Id);
                int total = st.Room.TotalSeats;
                int avail = total - sold;
                bool isPast = st.StartTime <= DateTime.Now;
                bool canSell = avail > 0 && !isPast;

                var btn = new Button
                {
                    Text = $"{st.StartTime:HH:mm}\n{st.Room.Name} ({st.Room.Type})\n" +
                           (isPast ? "Đã bắt đầu\n" : $"Còn {avail}/{total} ghế\n") +
                           $"{st.BasePrice:N0}đ",
                    Font = new Font("Segoe UI", 9),
                    Size = new Size(145, 85),
                    BackColor = canSell ? Color.FromArgb(40, 40, 65) : Color.FromArgb(35, 35, 45),
                    ForeColor = canSell ? Color.FromArgb(200, 200, 225) : Color.FromArgb(80, 80, 100),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = canSell ? Cursors.Hand : Cursors.No,
                    Enabled = canSell,
                    Margin = new Padding(4),
                    Tag = st,
                    TextAlign = ContentAlignment.MiddleCenter
                };
                btn.FlatAppearance.BorderColor = Color.FromArgb(55, 55, 80);
                btn.FlatAppearance.BorderSize = 1;
                btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 50, 110);

                btn.Click += (s, e) =>
                {
                    if (btn.Tag is Showtime showtime)
                        ShowtimeSelected?.Invoke(showtime);
                };

                flpShowtimes.Controls.Add(btn);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
        }
    }

    private static Image? LoadPosterImage(Movie movie)
    {
        if (!string.IsNullOrWhiteSpace(movie.PosterPath))
        {
            var path = Path.Combine(Application.StartupPath, "Resources", "Posters", movie.PosterPath);
            if (File.Exists(path))
            {
                try
                {
                    using var source = Image.FromFile(path);
                    return new Bitmap(source);
                }
                catch
                {
                    return null;
                }
            }
        }

        if (movie.Poster is { Length: > 0 })
        {
            try
            {
                using var ms = new MemoryStream(movie.Poster);
                using var source = Image.FromStream(ms);
                return new Bitmap(source);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }
}
