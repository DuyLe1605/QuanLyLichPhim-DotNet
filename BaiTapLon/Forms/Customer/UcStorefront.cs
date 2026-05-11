using BaiTapLon.Data;
using BaiTapLon.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace BaiTapLon.Forms.Customer;

public class UcStorefront : UserControl
{
    private readonly Panel hero = new();
    private readonly FlowLayoutPanel flpHot = new();
    private readonly FlowLayoutPanel flpComingSoon = new();
    private readonly System.Windows.Forms.Timer sliderTimer = new();
    private List<Movie> hotMovies = new();
    private int heroIndex;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<Movie>? MovieSelected { get; set; }

    public UcStorefront()
    {
        InitializeComponent();
        Load += async (s, e) => await LoadDataAsync();
    }

    private void InitializeComponent()
    {
        BackColor = CustomerUi.AppBg;

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = CustomerUi.AppBg,
            Padding = new Padding(24)
        };
        Controls.Add(scroll);

        var content = new TableLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 5,
            Dock = DockStyle.Top,
            BackColor = Color.Transparent
        };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 265));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        content.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        scroll.Controls.Add(content);

        hero.Height = 250;
        hero.Dock = DockStyle.Fill;
        hero.BackColor = CustomerUi.CardBg;
        hero.Paint += PaintHero;
        hero.Resize += (s, e) => hero.Invalidate();
        content.Controls.Add(hero, 0, 0);

        content.Controls.Add(CustomerUi.SectionTitle("Phim đang hot"), 0, 1);
        flpHot.Dock = DockStyle.Top;
        flpHot.AutoSize = true;
        flpHot.WrapContents = true;
        flpHot.BackColor = Color.Transparent;
        flpHot.Padding = new Padding(0, 0, 0, 20);
        content.Controls.Add(flpHot, 0, 2);

        content.Controls.Add(CustomerUi.SectionTitle("Phim sắp chiếu"), 0, 3);
        flpComingSoon.Dock = DockStyle.Top;
        flpComingSoon.AutoSize = true;
        flpComingSoon.WrapContents = true;
        flpComingSoon.BackColor = Color.Transparent;
        content.Controls.Add(flpComingSoon, 0, 4);

        sliderTimer.Interval = 5000;
        sliderTimer.Tick += (s, e) =>
        {
            if (hotMovies.Count <= 1) return;
            heroIndex = (heroIndex + 1) % hotMovies.Count;
            hero.Invalidate();
        };
    }

    /// <summary>
    /// Returns up to 6 upcoming movies (ReleaseDate > today, IsActive = true),
    /// ordered by ReleaseDate ascending. Extracted for testability.
    /// </summary>
    public static async Task<List<Movie>> GetUpcomingMoviesAsync(
        Data.AppDbContext context, DateTime today)
    {
        return await context.Movies
            .Where(m => m.IsActive && m.ReleaseDate.HasValue && m.ReleaseDate.Value.Date > today.Date)
            .AsNoTracking()
            .OrderBy(m => m.ReleaseDate)
            .Take(6)
            .ToListAsync();
    }

    private async Task LoadDataAsync()
    {
        using var context = Program.CreateDbContext();
        var now = DateTime.Now;

        hotMovies = await GetHotMoviesAsync(context, now);

        var upcoming = await GetUpcomingMoviesAsync(context, now);

        RenderCards(flpHot, hotMovies, false);
        RenderCards(flpComingSoon, upcoming, true);
        sliderTimer.Enabled = hotMovies.Count > 1;
        hero.Invalidate();
    }

    private void RenderCards(FlowLayoutPanel host, IEnumerable<Movie> movies, bool comingSoon)
    {
        host.Controls.Clear();
        var movieList = movies.ToList();

        if (movieList.Count == 0)
        {
            host.Controls.Add(new Label
            {
                Text = comingSoon ? "Chưa có phim sắp chiếu." : "Chưa có suất chiếu khả dụng.",
                ForeColor = CustomerUi.Muted,
                Font = new Font("Segoe UI", 12),
                AutoSize = true,
                Padding = new Padding(0, 20, 0, 20)
            });
            return;
        }

        foreach (var movie in movieList)
            host.Controls.Add(CreateMovieCard(movie, comingSoon));
    }

    private Control CreateMovieCard(Movie movie, bool comingSoon)
    {
        var card = new Panel
        {
            Width = comingSoon ? 160 : 185,
            Height = comingSoon ? 260 : 310,
            BackColor = CustomerUi.CardBg,
            Margin = new Padding(0, 0, 16, 16),
            Cursor = Cursors.Hand,
            Tag = movie
        };

        var pic = new PictureBox
        {
            Size = new Size(card.Width - 20, comingSoon ? 172 : 220),
            Location = new Point(10, 10),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(38, 42, 58),
            Image = CustomerUi.LoadPosterImage(movie),
            Cursor = Cursors.Hand
        };
        card.Controls.Add(pic);

        if (comingSoon)
        {
            var badge = new Label
            {
                Text = "Coming Soon",
                BackColor = CustomerUi.AccentBlue,
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(10, 10),
                Size = new Size(96, 24)
            };
            card.Controls.Add(badge);
            badge.BringToFront();
        }

        card.Controls.Add(new Label
        {
            Text = movie.Title,
            Location = new Point(10, pic.Bottom + 10),
            Size = new Size(card.Width - 20, 38),
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        });

        card.Controls.Add(new Label
        {
            Text = comingSoon
                ? $"{movie.ReleaseDate:dd/MM/yyyy} | {movie.AgeRating}"
                : $"{movie.Duration} phút | {movie.AgeRating}",
            Location = new Point(10, pic.Bottom + 50),
            Size = new Size(card.Width - 20, 22),
            ForeColor = CustomerUi.Muted,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand
        });

        card.Controls.Add(new Label
        {
            Text = CustomerUi.FormatGenres(movie),
            Location = new Point(10, pic.Bottom + 72),
            Size = new Size(card.Width - 20, 22),
            ForeColor = Color.FromArgb(120, 128, 150),
            Font = new Font("Segoe UI", 8),
            AutoEllipsis = true,
            Cursor = Cursors.Hand
        });

        void Click(object? s, EventArgs e) => MovieSelected?.Invoke(movie);
        void Hover(object? s, EventArgs e) => card.BackColor = Color.FromArgb(40, 48, 64);
        void Leave(object? s, EventArgs e) => card.BackColor = CustomerUi.CardBg;

        card.Click += Click;
        card.MouseEnter += Hover;
        card.MouseLeave += Leave;
        foreach (Control child in card.Controls)
        {
            child.Click += Click;
            child.MouseEnter += Hover;
            child.MouseLeave += Leave;
        }

        return card;
    }

    /// <summary>
    /// Returns the "hot movies" list: active movies with at least one active future showtime,
    /// ordered by nearest showtime ascending, capped at 6.
    /// Extracted as a static method to enable property-based testing without UI.
    /// </summary>
    public static async Task<List<Movie>> GetHotMoviesAsync(
        BaiTapLon.Data.AppDbContext context, DateTime now)
    {
        return await context.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .Include(m => m.Showtimes)
            .Where(m => m.IsActive && m.Showtimes.Any(s => s.IsActive && s.StartTime > now))
            .AsNoTracking()
            .OrderBy(m => m.Showtimes.Min(s => s.StartTime))
            .Take(6)
            .ToListAsync();
    }

    public static float ComputeTextAreaWidth(int heroPanelWidth)
    {
        var posterLeft = heroPanelWidth - 220;
        return Math.Max(200f, posterLeft - 50f);
    }

    private void PaintHero(object? sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        var rect = hero.ClientRectangle;
        if (rect.Width <= 0 || rect.Height <= 0) return;

        using var bg = new System.Drawing.Drawing2D.LinearGradientBrush(
            rect,
            Color.FromArgb(42, 66, 52),
            Color.FromArgb(24, 28, 42),
            System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
        e.Graphics.FillRectangle(bg, rect);

        var movie = hotMovies.Count == 0 ? null : hotMovies[Math.Clamp(heroIndex, 0, hotMovies.Count - 1)];
        var textWidth = ComputeTextAreaWidth(rect.Width);
        if (movie == null)
        {
            DrawHeroText(e.Graphics, "CineManager", "Chọn phim, đặt ghế và thanh toán QR trong vài bước.", null, textWidth);
            return;
        }

        var poster = CustomerUi.LoadPosterImage(movie);
        if (poster != null)
        {
            var posterRect = new Rectangle(rect.Right - 220, 22, 160, 210);
            e.Graphics.DrawImage(poster, posterRect);
            poster.Dispose();
        }

        DrawHeroText(
            e.Graphics,
            movie.Title,
            $"{movie.Duration} phút | {movie.AgeRating} | {CustomerUi.FormatGenres(movie)}",
            movie.Description,
            textWidth);
    }

    private static void DrawHeroText(Graphics graphics, string title, string line, string? description, float textWidth)
    {
        using var titleFont = new Font("Segoe UI", 26, FontStyle.Bold);
        using var lineFont = new Font("Segoe UI", 12, FontStyle.Bold);
        using var descFont = new Font("Segoe UI", 10);
        using var titleBrush = new SolidBrush(Color.White);
        using var mutedBrush = new SolidBrush(Color.FromArgb(210, 220, 230));
        using var descBrush = new SolidBrush(Color.FromArgb(165, 175, 190));

        graphics.DrawString(title, titleFont, titleBrush, new RectangleF(34, 38, textWidth, 62));
        graphics.DrawString(line, lineFont, mutedBrush, new RectangleF(38, 104, textWidth - 4, 28));

        if (!string.IsNullOrWhiteSpace(description))
        {
            graphics.DrawString(description, descFont, descBrush, new RectangleF(38, 144, textWidth - 4, 70));
        }
    }
}
