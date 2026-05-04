using System.ComponentModel;
using BaiTapLon.Models;

namespace BaiTapLon.Forms.Controls;

public class SeatLayoutPreviewControl : Control
{
    private readonly List<PreviewSeat> _seats = new();
    private readonly HashSet<int> _highlightSeatIds = new();
    private string _title = "So do ghe";

    private const float BaseSeatW = 28;
    private const float BaseSeatH = 20;
    private const float BaseGap = 4;
    private const float BaseRowLabelWidth = 26;
    private const float BaseScreenTop = 42;
    private const float BaseSeatsTop = 82;

    private static readonly Color BgColor = Color.FromArgb(18, 18, 30);
    private static readonly Color PanelColor = Color.FromArgb(22, 22, 38);
    private static readonly Color TextColor = Color.FromArgb(210, 210, 230);
    private static readonly Color MutedText = Color.FromArgb(150, 150, 175);
    private static readonly Color StandardColor = Color.FromArgb(105, 110, 125);
    private static readonly Color VipColor = Color.FromArgb(120, 90, 230);
    private static readonly Color CoupleColor = Color.FromArgb(220, 105, 145);
    private static readonly Color HighlightColor = Color.FromArgb(100, 80, 255);

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

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public bool ShowLegend { get; set; } = true;

    public SeatLayoutPreviewControl()
    {
        DoubleBuffered = true;
        BackColor = BgColor;
        ForeColor = TextColor;
        MinimumSize = new Size(220, 170);
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
    }

    public void SetRows(IEnumerable<SeatPreviewRow> rows, string? title = null)
    {
        _seats.Clear();
        _highlightSeatIds.Clear();

        int id = 1;
        int gridRow = 0;
        foreach (var row in rows.Where(r => r.SeatCount > 0).OrderBy(r => r.RowLabel))
        {
            for (int i = 1; i <= row.SeatCount; i++)
                _seats.Add(new PreviewSeat(id++, row.RowLabel, i, row.SeatType, gridRow, i - 1, 1));

            gridRow++;
        }

        if (!string.IsNullOrWhiteSpace(title))
            _title = title;

        Invalidate();
    }

    public void SetSeats(IEnumerable<Seat> seats, string? title = null)
    {
        _seats.Clear();
        _highlightSeatIds.Clear();
        _seats.AddRange(seats
            .OrderBy(s => s.GridRow)
            .ThenBy(s => s.GridColumn)
            .Select(s => new PreviewSeat(s.Id, s.RowLabel, s.SeatNumber, s.Type, s.GridRow, s.GridColumn, Math.Max(1, s.GridSpan))));

        if (!string.IsNullOrWhiteSpace(title))
            _title = title;

        Invalidate();
    }

    public void SetHighlightedSeats(IEnumerable<int> seatIds)
    {
        _highlightSeatIds.Clear();
        foreach (var seatId in seatIds)
            _highlightSeatIds.Add(seatId);

        Invalidate();
    }

    public void ClearPreview(string title = "Chon phong de xem so do ghe")
    {
        _seats.Clear();
        _highlightSeatIds.Clear();
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
            g.DrawString("Chua co du lieu ghe", font, brush, ClientRectangle,
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            return;
        }

        var rows = _seats.GroupBy(s => s.GridRow).OrderBy(gp => gp.Key).ToList();
        int maxColumns = _seats.Max(s => s.GridColumn + Math.Max(1, s.GridSpan));
        var layout = CreateLayout(rows.Count, maxColumns);

        float gridWidth = maxColumns * layout.SeatW + Math.Max(0, maxColumns - 1) * layout.Gap;
        float totalWidth = layout.RowLabelWidth + gridWidth + layout.RowLabelWidth;
        float startX = Math.Max(8, (Width - totalWidth) / 2f);

        DrawScreen(g, startX + layout.RowLabelWidth, gridWidth, layout);
        DrawSeats(g, rows, startX, gridWidth, layout);
        if (ShowLegend)
            DrawLegend(g, layout);
    }

    private PreviewLayout CreateLayout(int rowCount, int maxColumns)
    {
        float naturalGridWidth = maxColumns * BaseSeatW + Math.Max(0, maxColumns - 1) * BaseGap;
        float naturalWidth = BaseRowLabelWidth * 2 + naturalGridWidth + 24;
        float naturalHeight = BaseSeatsTop + rowCount * (BaseSeatH + BaseGap) + (ShowLegend ? 50 : 14);

        float widthScale = Width > 0 ? (Width - 18f) / naturalWidth : 1f;
        float heightScale = Height > 0 ? (Height - 10f) / naturalHeight : 1f;
        float scale = Math.Min(1f, Math.Min(widthScale, heightScale));
        scale = Math.Max(0.52f, scale);

        return new PreviewLayout(
            BaseSeatW * scale,
            BaseSeatH * scale,
            Math.Max(2f, BaseGap * scale),
            BaseRowLabelWidth * scale,
            BaseScreenTop * scale,
            BaseSeatsTop * scale,
            scale);
    }

    private void DrawTitle(Graphics g)
    {
        using var font = new Font("Segoe UI", 11, FontStyle.Bold);
        using var brush = new SolidBrush(TextColor);
        var rect = new RectangleF(14, 10, Width - 28, 24);
        g.DrawString(_title, font, brush, rect,
            new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center });
    }

    private static void DrawScreen(Graphics g, float x, float width, PreviewLayout layout)
    {
        var screenRect = new RectangleF(x - 8 * layout.Scale, layout.ScreenTop, width + 16 * layout.Scale, 18 * layout.Scale);
        using var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
            screenRect,
            Color.FromArgb(115, 105, 180),
            Color.FromArgb(45, 42, 75),
            System.Drawing.Drawing2D.LinearGradientMode.Vertical);
        using var path = RoundedRect(screenRect, 8 * layout.Scale);
        g.FillPath(brush, path);

        using var font = new Font("Segoe UI", Math.Max(5.5f, 7.5f * layout.Scale), FontStyle.Bold);
        using var textBrush = new SolidBrush(Color.FromArgb(225, 222, 245));
        g.DrawString("MAN HINH", font, textBrush, screenRect,
            new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
    }

    private void DrawSeats(Graphics g, List<IGrouping<int, PreviewSeat>> rows, float startX, float gridWidth, PreviewLayout layout)
    {
        using var rowFont = new Font("Segoe UI", Math.Max(5.5f, 8.5f * layout.Scale), FontStyle.Bold);
        using var seatFont = new Font("Segoe UI", Math.Max(5f, 7f * layout.Scale), FontStyle.Bold);
        using var rowBrush = new SolidBrush(MutedText);
        var textFormat = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };

        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r].OrderBy(s => s.GridColumn).ToList();
            string rowLabel = row.First().RowLabel;
            float y = layout.SeatsTop + r * (layout.SeatH + layout.Gap);

            var leftLabel = new RectangleF(startX, y, layout.RowLabelWidth - 4 * layout.Scale, layout.SeatH);
            var rightLabel = new RectangleF(startX + layout.RowLabelWidth + gridWidth + 4 * layout.Scale, y, layout.RowLabelWidth - 4 * layout.Scale, layout.SeatH);
            g.DrawString(rowLabel, rowFont, rowBrush, leftLabel, textFormat);
            g.DrawString(rowLabel, rowFont, rowBrush, rightLabel, textFormat);

            foreach (var seat in row)
            {
                int span = Math.Max(1, seat.GridSpan);
                var rect = new RectangleF(
                    startX + layout.RowLabelWidth + seat.GridColumn * (layout.SeatW + layout.Gap),
                    y,
                    span * layout.SeatW + (span - 1) * layout.Gap,
                    layout.SeatH);

                bool isHighlighted = _highlightSeatIds.Contains(seat.Id);
                using var seatBrush = new SolidBrush(isHighlighted ? HighlightColor : GetSeatColor(seat.Type));
                using var path = RoundedRect(rect, Math.Max(2.5f, 4 * layout.Scale));
                g.FillPath(seatBrush, path);

                using var highlightPen = new Pen(isHighlighted ? Color.White : Color.FromArgb(255, 255, 255, 45), isHighlighted ? 2f : 1f);
                g.DrawPath(highlightPen, path);

                using var seatTextBrush = new SolidBrush(Color.White);
                g.DrawString(seat.SeatNumber.ToString(), seatFont, seatTextBrush, rect, textFormat);
            }
        }
    }

    private void DrawLegend(Graphics g, PreviewLayout layout)
    {
        var items = new[]
        {
            ("Thuong", StandardColor),
            ("VIP", VipColor),
            ("Doi", CoupleColor),
            ("Da mua", HighlightColor)
        };

        using var font = new Font("Segoe UI", Math.Max(6f, 8f * layout.Scale));
        float y = Height - 28;
        float x = 14;
        foreach (var (label, color) in items)
        {
            using var brush = new SolidBrush(color);
            using var path = RoundedRect(new RectangleF(x, y + 3, 13, 11), 3);
            g.FillPath(brush, path);

            using var textBrush = new SolidBrush(MutedText);
            g.DrawString(label, font, textBrush, x + 18, y);
            x += Math.Max(54, g.MeasureString(label, font).Width + 30);
        }
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

    private record PreviewLayout(float SeatW, float SeatH, float Gap, float RowLabelWidth, float ScreenTop, float SeatsTop, float Scale);
}
