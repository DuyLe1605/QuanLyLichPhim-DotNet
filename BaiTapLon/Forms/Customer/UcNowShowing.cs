using BaiTapLon.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace BaiTapLon.Forms.Customer;

/// <summary>
/// Customer-facing "Phim đang chiếu" page.
/// Shows a searchable, genre-filterable grid of movies that have at least one
/// active future showtime. Clicking a card invokes <see cref="MovieSelected"/>.
/// Distinct from <c>Forms/Staff/UcNowShowing.cs</c>.
/// </summary>
public class UcNowShowing : UserControl
{
    // ── Fields ──────────────────────────────────────────────────────────────
    private readonly TextBox txtSearch = new();
    private readonly FlowLayoutPanel flpGenres = new() { WrapContents = true };
    private readonly FlowLayoutPanel flpMovies = new() { WrapContents = true };

    private List<Movie> allMovies = new();
    private List<string> selectedGenres = new();

    // ── Public API ───────────────────────────────────────────────────────────
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<Movie>? MovieSelected { get; set; }

    // ── Constructor ──────────────────────────────────────────────────────────
    public UcNowShowing()
    {
        InitializeComponent();
        Load += async (s, e) => await LoadDataAsync();
    }

    // ── Layout ───────────────────────────────────────────────────────────────
    private void InitializeComponent()
    {
        BackColor = CustomerUi.AppBg;

        // Outer scrollable panel
        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = CustomerUi.AppBg,
            Padding = new Padding(24, 16, 24, 24)
        };
        Controls.Add(scroll);

        // Content stack (grows downward)
        var content = new Panel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            BackColor = Color.Transparent
        };
        scroll.Controls.Add(content);

        // ── Header panel ────────────────────────────────────────────────────
        var pnlHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = Color.Transparent
        };

        var lblTitle = new Label
        {
            Text = "Phim đang chiếu",
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = CustomerUi.Text,
            AutoSize = true,
            Location = new Point(0, 14)
        };
        pnlHeader.Controls.Add(lblTitle);

        // Search box — right-aligned inside the header
        txtSearch.PlaceholderText = "Tìm kiếm phim...";
        txtSearch.Font = new Font("Segoe UI", 10.5f);
        txtSearch.BackColor = CustomerUi.CardBg;
        txtSearch.ForeColor = CustomerUi.Text;
        txtSearch.BorderStyle = BorderStyle.FixedSingle;
        txtSearch.Size = new Size(240, 32);
        txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        txtSearch.TextChanged += (s, e) => ApplyFilters();

        // Position search box on the right; adjust on resize
        pnlHeader.Resize += (s, e) =>
        {
            txtSearch.Location = new Point(pnlHeader.Width - txtSearch.Width, 18);
        };
        pnlHeader.Controls.Add(txtSearch);

        content.Controls.Add(pnlHeader);

        // ── Genre filter bar ─────────────────────────────────────────────────
        flpGenres.Dock = DockStyle.Top;
        flpGenres.AutoSize = true;
        flpGenres.BackColor = Color.Transparent;
        flpGenres.Padding = new Padding(0, 4, 0, 8);
        content.Controls.Add(flpGenres);

        // ── Movie cards panel ────────────────────────────────────────────────
        flpMovies.Dock = DockStyle.Top;
        flpMovies.AutoSize = true;
        flpMovies.BackColor = Color.Transparent;
        flpMovies.Padding = new Padding(0, 4, 0, 0);
        content.Controls.Add(flpMovies);

        // Stack panels top-to-bottom (Controls are added in reverse for Dock=Top)
        // Re-order so header is on top, then genres, then movies
        content.Controls.SetChildIndex(pnlHeader, 0);
        content.Controls.SetChildIndex(flpGenres, 1);
        content.Controls.SetChildIndex(flpMovies, 2);
    }

    // ── Data loading ─────────────────────────────────────────────────────────
    private async Task LoadDataAsync()
    {
        try
        {
            using var context = Program.CreateDbContext();
            var now = DateTime.Now;

            allMovies = await context.Movies
                .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
                .Include(m => m.Showtimes)
                .Where(m => m.IsActive && m.Showtimes.Any(s => s.IsActive && s.StartTime > now))
                .AsNoTracking()
                .OrderBy(m => m.Title)
                .ToListAsync();

            BuildGenreButtons();
            ApplyFilters();
        }
        catch
        {
            flpMovies.Controls.Clear();
            flpMovies.Controls.Add(new Label
            {
                Text = "Không thể tải danh sách phim. Vui lòng thử lại.",
                ForeColor = CustomerUi.Muted,
                Font = new Font("Segoe UI", 11),
                AutoSize = true,
                Padding = new Padding(0, 20, 0, 0)
            });
        }
    }

    private void BuildGenreButtons()
    {
        flpGenres.Controls.Clear();

        // "Tất cả" button
        var btnAll = CustomerUi.NavButton("Tất cả");
        btnAll.Width = 90;
        btnAll.Height = 34;
        btnAll.Margin = new Padding(0, 0, 8, 4);
        btnAll.BackColor = CustomerUi.Accent;
        btnAll.ForeColor = Color.White;
        btnAll.Click += (s, e) =>
        {
            selectedGenres.Clear();
            RefreshGenreButtonStates();
            ApplyFilters();
        };
        flpGenres.Controls.Add(btnAll);

        // One button per distinct genre
        var genres = allMovies
            .SelectMany(m => m.MovieGenres.Select(mg => mg.Genre.Name))
            .Distinct()
            .OrderBy(g => g)
            .ToList();

        foreach (var genre in genres)
        {
            var g = genre; // capture
            var btn = CustomerUi.NavButton(g);
            btn.Width = Math.Max(90, TextRenderer.MeasureText(g, btn.Font).Width + 24);
            btn.Height = 34;
            btn.Margin = new Padding(0, 0, 8, 4);
            btn.BackColor = Color.Transparent;
            btn.Click += (s, e) =>
            {
                if (selectedGenres.Contains(g))
                    selectedGenres.Remove(g);
                else
                    selectedGenres.Add(g);

                RefreshGenreButtonStates();
                ApplyFilters();
            };
            flpGenres.Controls.Add(btn);
        }
    }

    private void RefreshGenreButtonStates()
    {
        bool allSelected = selectedGenres.Count == 0;

        foreach (Control ctrl in flpGenres.Controls)
        {
            if (ctrl is not Button btn) continue;

            if (btn.Text == "Tất cả")
            {
                btn.BackColor = allSelected ? CustomerUi.Accent : Color.Transparent;
                btn.ForeColor = allSelected ? Color.White : CustomerUi.Text;
            }
            else
            {
                bool active = selectedGenres.Contains(btn.Text);
                btn.BackColor = active ? CustomerUi.Accent : Color.Transparent;
                btn.ForeColor = active ? Color.White : CustomerUi.Text;
            }
        }
    }

    // ── Filtering ────────────────────────────────────────────────────────────

    /// <summary>
    /// Filters <paramref name="movies"/> to those whose title contains
    /// <paramref name="query"/> (case-insensitive). An empty or whitespace
    /// query returns all movies unchanged.
    /// </summary>
    public static IEnumerable<Movie> FilterByTitle(IEnumerable<Movie> movies, string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return movies;

        return movies.Where(m =>
            m.Title.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Filters <paramref name="movies"/> to those that have at least one genre
    /// present in <paramref name="genres"/>. An empty genre set returns all
    /// movies unchanged.
    /// </summary>
    public static IEnumerable<Movie> FilterByGenres(IEnumerable<Movie> movies, IEnumerable<string> genres)
    {
        var genreList = genres.ToList();
        if (genreList.Count == 0)
            return movies;

        return movies.Where(m =>
            m.MovieGenres.Any(mg => genreList.Contains(mg.Genre.Name)));
    }

    /// <summary>
    /// Applies the current search text and selected genres to <see cref="allMovies"/>
    /// and renders the result into <see cref="flpMovies"/>.
    /// </summary>
    public void ApplyFilters()
    {
        var filtered = FilterByTitle(allMovies, txtSearch.Text);
        filtered = FilterByGenres(filtered, selectedGenres);
        RenderMovies(filtered);
    }

    // ── Rendering ────────────────────────────────────────────────────────────
    private void RenderMovies(IEnumerable<Movie> movies)
    {
        flpMovies.Controls.Clear();
        var list = movies.ToList();

        if (list.Count == 0)
        {
            flpMovies.Controls.Add(new Label
            {
                Text = "Không có phim nào phù hợp.",
                ForeColor = CustomerUi.Muted,
                Font = new Font("Segoe UI", 12),
                AutoSize = true,
                Padding = new Padding(0, 20, 0, 20)
            });
            return;
        }

        foreach (var movie in list)
            flpMovies.Controls.Add(CreateMovieCard(movie));
    }

    private Control CreateMovieCard(Movie movie)
    {
        var card = new Panel
        {
            Width = 185,
            Height = 310,
            BackColor = CustomerUi.CardBg,
            Margin = new Padding(0, 0, 16, 16),
            Cursor = Cursors.Hand,
            Tag = movie
        };

        var pic = new PictureBox
        {
            Size = new Size(card.Width - 20, 220),
            Location = new Point(10, 10),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(38, 42, 58),
            Image = CustomerUi.LoadPosterImage(movie),
            Cursor = Cursors.Hand
        };
        card.Controls.Add(pic);

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
            Text = $"{movie.Duration} phút | {movie.AgeRating}",
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
}
