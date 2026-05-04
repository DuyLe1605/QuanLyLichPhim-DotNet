using System.ComponentModel;
using BaiTapLon.Models;

namespace BaiTapLon.Forms.Controls;

public class SeatLayoutPreviewControl : Control
{
    private readonly List<PreviewSeat> _seats = new();
    private string _title = "Sơ đồ ghế";

    private const int SeatW = 28;
    private const int SeatH = 20;
    private const int Gap = 4;
    private const int RowLabelWidth = 26;
    private const int ScreenTop = 42;
    private const int SeatsTop = 82;

    private static readonly Color BgColor = Color.FromArgb(18, 18, 30);
    private static readonly Color PanelColor = Color.FromArgb(22, 22, 38);
    private static readonly Color TextColor = Color.FromArgb(210, 210, 230);
    private static readonly Color MutedText = Color.FromArgb(150, 150, 175);
    private static readonly Color StandardColor = Color.FromArgb(105, 110, 125);
    private static readonly Color VipColor = Color.FromArgb(120, 90, 230);
    private static readonly Color CoupleColor = Color.FromArgb(220, 105, 145);

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public string Title
    {
        get => _title;
        set
        {
            _title = value;
            Invalidate();
        }
    }

    public SeatLayoutPreviewControl()
    {
        DoubleBuffered = true;
        BackColor = BgColor;
        ForeColor = TextColor;
        MinimumSize = new Size(280, 260);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    public void SetRows(IEnumerable<SeatPreviewRow> rows, string? title = null)
    {
        _seats.Clear();
        int id = 1;
        int gridRow = 0;
        foreach (var row in rows.Where(r => r.SeatCount > 0).OrderBy(r => r.RowLabel))
        {
            for (int i = 1; i <= row.SeatCount; i++)
            {
                _seats.Add(new PreviewSeat(id++, row.RowLabel, i, row.SeatType, gridRow, i - 1, 1));
            }
            gridRow++;
        }

        if (!string.IsNullOrWhiteSpace(title))
            _title = title;

        UpdateMinimumSize();
        Invalidate();
    }

    public void SetSeats(IEnumerable<Seat> seats, string? title = null)
    {
        _seats.Clear();
        _seats.AddRange(seats
            .OrderBy(s => s.RowLabel)
            .ThenBy(s => s.SeatNumber)
            .Select(s => new PreviewSeat(s.Id, s.RowLabel, s.SeatNumber, s.Type, s.GridRow, s.GridColumn, Math.Max(1, s.GridSpan))));

        if (!string.IsNullOrWhiteSpace(title))
            _title = title;

        UpdateMinimumSize();
        Invalidate();
    }

    public void ClearPreview(string title = "Chọn phòng để xem sơ đồ ghế")
    {
        _seats.Clear();
        _title = title;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var bgBrush = new SolidBrush(PanelColor);
        using var panelPath = RoundedRect(new RectangleF(0, 0, Width - 1, Height - 1), 8);
        g.FillPath(bgBrush, panelPath);

        using var borderPen = new Pen(Color.FromArgb(45, 45, 65));
        g.DrawPath(borderPen, panelPath);

        DrawTitle(g);

        if (_seats.Count == 0)
        {
            using var brush = new SolidBrush(MutedText);
            using var font = new Font("Segoe UI", 10);
            g.DrawString("Chưa có dữ liệu ghế", font, brush, ClientRectangle,
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            return;
        }

        var rows = _seats.GroupBy(s => s.GridRow).OrderBy(gp => gp.Key).ToList();
        int maxColumns = _seats.Max(s => s.GridColumn + Math.Max(1, s.GridSpan));
        int gridWidth = maxColumns * SeatW + Math.Max(0, maxColumns - 1) * Gap;
        int totalWidth = RowLabelWidth + gridWidth + RowLabelWidth;
        int startX = Math.Max(12, (Width - totalWidth) / 2);

        DrawScreen(g, startX + RowLabelWidth, gridWidth);
        DrawSeats(g, rows, startX, gridWidth);
        DrawLegend(g);
    }

    private void DrawTitle(Graphics g)
    {
        using var font = new Font("Segoe UI", 11, FontStyle.Bold);
        using var brush = new SolidBrush(TextColor);
        var rect = new RectangleF(14, 10, Width - 28, 24);
        g.DrawString(_title, font, brush, rect,
            new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
    }

    private void DrawScreen(Graphics g, int x, int width)
    {
        var screenRect = new RectangleF(x - 8, ScreenTop, width + 16, 18);
        using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
            screenRect,
            Color.FromArgb(115, 105, 180),
            Color.FromArgb(45, 42, 75),
            System.Drawing.Drawing2D.LinearGradientMode.Vertical);
        using var path = RoundedRect(screenRect, 8);
        g.FillPath(brush, path);

        using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.FromArgb(225, 222, 245));
        g.DrawString("MÀN HÌNH", font, textBrush, screenRect,
            new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
    }

    private void DrawSeats(Graphics g, List<IGrouping<int, PreviewSeat>> rows, int startX, int gridWidth)
    {
        using var rowFont = new Font("Segoe UI", 8.5f, FontStyle.Bold);
        using var seatFont = new Font("Segoe UI", 6.4f, FontStyle.Bold);
        using var rowBrush = new SolidBrush(MutedText);
        var textFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r].OrderBy(s => s.GridColumn).ToList();
            string rowLabel = row.First().RowLabel;
            int y = SeatsTop + r * (SeatH + Gap);

            var leftLabel = new RectangleF(startX, y, RowLabelWidth - 4, SeatH);
            var rightLabel = new RectangleF(startX + RowLabelWidth + gridWidth + 4, y, RowLabelWidth - 4, SeatH);
            g.DrawString(rowLabel, rowFont, rowBrush, leftLabel, textFormat);
            g.DrawString(rowLabel, rowFont, rowBrush, rightLabel, textFormat);

            foreach (var seat in row)
            {
                int span = Math.Max(1, seat.GridSpan);
                var rect = new RectangleF(
                    startX + RowLabelWidth + seat.GridColumn * (SeatW + Gap),
                    y,
                    span * SeatW + (span - 1) * Gap,
                    SeatH);
                using var seatBrush = new SolidBrush(GetSeatColor(seat.Type));
                using var path = RoundedRect(rect, 4);
                g.FillPath(seatBrush, path);

                using var highlightPen = new Pen(Color.FromArgb(255, 255, 255, 45));
                g.DrawPath(highlightPen, path);

                using var seatTextBrush = new SolidBrush(Color.White);
                g.DrawString($"{seat.RowLabel}{seat.SeatNumber}", seatFont, seatTextBrush, rect, textFormat);
            }
        }
    }

    private void DrawLegend(Graphics g)
    {
        var items = new[]
        {
            ("Ghế thường", StandardColor),
            ("VIP", VipColor),
            ("Ghế đôi", CoupleColor)
        };

        using var font = new Font("Segoe UI", 8);
        float y = Height - 34;
        float x = 14;
        foreach (var (label, color) in items)
        {
            using var brush = new SolidBrush(color);
            using var path = RoundedRect(new RectangleF(x, y + 3, 15, 13), 3);
            g.FillPath(brush, path);

            using var textBrush = new SolidBrush(MutedText);
            g.DrawString(label, font, textBrush, x + 20, y);
            x += 92;
        }
    }

    private void UpdateMinimumSize()
    {
        if (_seats.Count == 0) return;

        var rows = _seats.GroupBy(s => s.GridRow).ToList();
        int maxColumns = _seats.Max(s => s.GridColumn + Math.Max(1, s.GridSpan));
        int width = RowLabelWidth * 2 + maxColumns * (SeatW + Gap) + 40;
        int height = SeatsTop + rows.Count * (SeatH + Gap) + 54;
        MinimumSize = new Size(Math.Max(280, width), Math.Max(260, height));
    }

    private static Color GetSeatColor(string type) => type switch
    {
        "VIP" => VipColor,
        "Couple" => CoupleColor,
        _ => StandardColor
    };

    private static System.Drawing.Drawing2D.GraphicsPath RoundedRect(RectangleF rect, float radius)
    {
        var path = new System.Drawing.Drawing2D.GraphicsPath();
        float d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    public record SeatPreviewRow(string RowLabel, int SeatCount, string SeatType);

    private record PreviewSeat(int Id, string RowLabel, int SeatNumber, string Type, int GridRow, int GridColumn, int GridSpan);
}
