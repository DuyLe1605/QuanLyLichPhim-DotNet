using BaiTapLon.Models;

namespace BaiTapLon.Forms.Customer;

internal static class CustomerUi
{
    public static readonly Color AppBg = Color.FromArgb(16, 18, 28);
    public static readonly Color PanelBg = Color.FromArgb(24, 27, 40);
    public static readonly Color CardBg = Color.FromArgb(31, 35, 50);
    public static readonly Color Border = Color.FromArgb(55, 60, 82);
    public static readonly Color Text = Color.FromArgb(235, 238, 245);
    public static readonly Color Muted = Color.FromArgb(150, 156, 176);
    public static readonly Color Accent = Color.FromArgb(100, 180, 120);
    public static readonly Color AccentBlue = Color.FromArgb(70, 145, 230);

    public static Button NavButton(string text)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = false,
            Width = 150,
            Height = 44,
            BackColor = Color.Transparent,
            ForeColor = Text,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(38, 44, 62);
        return button;
    }

    public static Button PrimaryButton(string text)
    {
        var button = new Button
        {
            Text = text,
            Height = 42,
            BackColor = Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10.5f, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(75, 200, 125);
        return button;
    }

    public static Label SectionTitle(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            Height = 42,
            ForeColor = Text,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    public static string FormatGenres(Movie movie)
    {
        return string.Join(", ", movie.MovieGenres.Select(mg => mg.Genre.Name));
    }

    public static Image? LoadPosterImage(Movie movie)
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

    public static string SeatLabel(Forms.Controls.SeatMapControl.SeatInfo seat)
    {
        return $"{seat.RowLabel}{seat.SeatNumber}";
    }
}
