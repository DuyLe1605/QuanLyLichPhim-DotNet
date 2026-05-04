using System.ComponentModel;
using BaiTapLon.Models;

namespace BaiTapLon.Forms.Controls;

/// <summary>
/// Custom GDI+ control hiển thị sơ đồ ghế phòng chiếu.
/// Hỗ trợ số ghế khác nhau mỗi hàng (variable columns).
/// </summary>
public class SeatMapControl : Control
{
    // === Data ===
    private List<SeatInfo> _seats = new();
    private HashSet<int> _soldSeatIds = new();
    private readonly HashSet<int> _selectedSeatIds = new();
    private int _rows, _maxCols;
    private Dictionary<string, int> _seatsPerRow = new(); // RowLabel → seat count

    // === Layout constants ===
    private const int CellSize = 34;
    private const int Gap = 5;
    private const int ScreenMarginTop = 50;
    private const int RowLabelWidth = 30;
    private const int LegendHeight = 50;

    // === Colors ===
    private static readonly Color BgColor = Color.FromArgb(18, 18, 30);
    private static readonly Color ScreenColor = Color.FromArgb(60, 50, 130);
    private static readonly Color EmptyStandard = Color.FromArgb(55, 65, 85);
    private static readonly Color EmptyVip = Color.FromArgb(140, 120, 30);
    private static readonly Color EmptyCouple = Color.FromArgb(170, 55, 90);
    private static readonly Color SelectedColor = Color.FromArgb(100, 80, 255);
    private static readonly Color SoldColor = Color.FromArgb(38, 38, 48);
    private static readonly Color HoverColor = Color.FromArgb(130, 110, 255);

    // === State ===
    private int _hoveredSeatId = -1;
    private readonly ToolTip _toolTip;

    // === Properties ===
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    [Browsable(false)]
    public decimal BasePrice { get; set; }

    /// <summary>
    /// Danh sách ghế đang được chọn.
    /// </summary>
    public List<SeatInfo> SelectedSeats =>
        _seats.Where(s => _selectedSeatIds.Contains(s.Id)).ToList();

    /// <summary>
    /// Tổng tiền của các ghế đang chọn.
    /// </summary>
    public decimal TotalPrice =>
        SelectedSeats.Sum(s => BasePrice * s.PriceMultiplier);

    /// <summary>
    /// Event khi thay đổi ghế được chọn.
    /// </summary>
    public event EventHandler? SeatSelectionChanged;

    public SeatMapControl()
    {
        this.DoubleBuffered = true;
        this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
        this.BackColor = BgColor;
        this.Cursor = Cursors.Hand;

        _toolTip = new ToolTip
        {
            BackColor = Color.FromArgb(30, 30, 50),
            ForeColor = Color.FromArgb(220, 220, 240),
            OwnerDraw = false,
            InitialDelay = 200,
            ReshowDelay = 100
        };
    }

    /// <summary>
    /// Nạp dữ liệu ghế cho sơ đồ.
    /// </summary>
    public void SetData(List<Seat> seats, HashSet<int> soldIds, int rows, int cols, decimal basePrice)
    {
        _seats = seats.Select(s => new SeatInfo
        {
            Id = s.Id,
            RowLabel = s.RowLabel,
            SeatNumber = s.SeatNumber,
            GridRow = s.GridRow,
            GridColumn = s.GridColumn,
            GridSpan = Math.Max(1, s.GridSpan),
            Type = s.Type,
            PriceMultiplier = s.PriceMultiplier
        }).ToList();

        _soldSeatIds = soldIds;
        _selectedSeatIds.Clear();
        _rows = Math.Max(rows, _seats.Count == 0 ? rows : _seats.Max(s => s.GridRow) + 1);
        _maxCols = Math.Max(cols, _seats.Count == 0 ? cols : _seats.Max(s => s.GridColumn + Math.Max(1, s.GridSpan)));
        BasePrice = basePrice;

        // Build seats-per-row map
        _seatsPerRow = _seats
            .GroupBy(s => s.RowLabel)
            .ToDictionary(g => g.Key, g => g.Count());

        // Auto-size control
        int w = RowLabelWidth + _maxCols * (CellSize + Gap) + Gap + RowLabelWidth + 20;
        int h = ScreenMarginTop + _rows * (CellSize + Gap) + Gap + LegendHeight + 10;
        this.MinimumSize = new Size(w, h);
        this.Size = new Size(Math.Max(w, this.Width), h);

        Invalidate();
    }

    /// <summary>
    /// Reset ghế đã chọn.
    /// </summary>
    public void ClearSelection()
    {
        _selectedSeatIds.Clear();
        SeatSelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    // ==================== PAINTING ====================

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        if (_seats.Count == 0) return;

        // Tính offset để căn giữa toàn bộ
        int gridWidth = _maxCols * (CellSize + Gap) - Gap;
        int totalWidth = RowLabelWidth + gridWidth + RowLabelWidth;
        int offsetX = Math.Max(0, (this.Width - totalWidth) / 2);

        // === Vẽ "MÀN HÌNH" ===
        DrawScreen(g, offsetX + RowLabelWidth, gridWidth);

        // === Vẽ ghế ===
        using var fontSeat = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        using var fontRow = new Font("Segoe UI", 9f, FontStyle.Bold);

        var seatsByGridRow = _seats
            .GroupBy(s => s.GridRow)
            .ToDictionary(gp => gp.Key, gp => gp.OrderBy(s => s.GridColumn).ToList());

        for (int r = 0; r < _rows; r++)
        {
            if (!seatsByGridRow.TryGetValue(r, out var rowSeats) || rowSeats.Count == 0)
                continue;

            string rowLabel = rowSeats.First().RowLabel;

            // Offset X để căn giữa hàng ngắn hơn
            int rowOffsetX = 0;

            // Row label bên trái
            var rowRectL = new RectangleF(
                offsetX, ScreenMarginTop + r * (CellSize + Gap),
                RowLabelWidth - 5, CellSize);
            using var rowBrush = new SolidBrush(Color.FromArgb(120, 120, 150));
            g.DrawString(rowLabel, fontRow, rowBrush, rowRectL,
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

            // Row label bên phải
            var rowRectR = new RectangleF(
                offsetX + RowLabelWidth + gridWidth + 5, ScreenMarginTop + r * (CellSize + Gap),
                RowLabelWidth - 5, CellSize);
            g.DrawString(rowLabel, fontRow, rowBrush, rowRectR,
                new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });

            foreach (var seat in rowSeats)
            {
                int c = seat.SeatNumber - 1;
                int span = Math.Max(1, seat.GridSpan);
                float x = offsetX + RowLabelWidth + rowOffsetX + seat.GridColumn * (CellSize + Gap);
                float y = ScreenMarginTop + r * (CellSize + Gap);
                var rect = new RectangleF(x, y, span * CellSize + (span - 1) * Gap, CellSize);

                // Xác định màu
                Color fillColor = GetSeatColor(seat);
                float cornerRadius = seat.Type == "Couple" ? 6 : 5;

                // Vẽ ghế rounded rectangle
                using var brush = new SolidBrush(fillColor);
                using var path = RoundedRect(rect, cornerRadius);
                g.FillPath(brush, path);

                // Viền cho ghế đang hover
                if (seat.Id == _hoveredSeatId && !_soldSeatIds.Contains(seat.Id))
                {
                    using var pen = new Pen(HoverColor, 2f);
                    g.DrawPath(pen, path);
                }

                // Viền cho ghế đã chọn
                if (_selectedSeatIds.Contains(seat.Id))
                {
                    using var pen = new Pen(Color.White, 1.5f);
                    g.DrawPath(pen, path);
                }

                // Text (số ghế)
                string seatText = _soldSeatIds.Contains(seat.Id) ? "✕" : (c + 1).ToString();
                seatText = _soldSeatIds.Contains(seat.Id) ? "×" : $"{seat.RowLabel}{seat.SeatNumber}";
                Color textColor = _soldSeatIds.Contains(seat.Id)
                    ? Color.FromArgb(70, 70, 80)
                    : Color.White;

                using var textBrush = new SolidBrush(textColor);
                g.DrawString(seatText, fontSeat, textBrush, rect,
                    new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
            }
        }

        // === Vẽ Legend ===
        DrawLegend(g, offsetX + RowLabelWidth, gridWidth);
    }

    private void DrawScreen(Graphics g, float x, float width)
    {
        float screenY = 12;
        float screenH = 22;

        // Gradient screen
        var screenRect = new RectangleF(x - 10, screenY, width + 20, screenH);
        using var screenBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
            screenRect, ScreenColor, Color.FromArgb(30, 25, 70),
            System.Drawing.Drawing2D.LinearGradientMode.Vertical);
        using var screenPath = RoundedRect(screenRect, 4);
        g.FillPath(screenBrush, screenPath);

        using var screenFont = new Font("Segoe UI", 8, FontStyle.Bold);
        using var screenTextBrush = new SolidBrush(Color.FromArgb(160, 155, 220));
        g.DrawString("M À N   H Ì N H", screenFont, screenTextBrush, screenRect,
            new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
    }

    private void DrawLegend(Graphics g, float startX, float gridWidth)
    {
        float y = ScreenMarginTop + _rows * (CellSize + Gap) + 15;
        using var font = new Font("Segoe UI", 7.5f);

        var items = new[]
        {
            ("Trống", EmptyStandard),
            ("Đang chọn", SelectedColor),
            ("Đã bán", SoldColor),
            ("VIP", EmptyVip),
            ("Couple", EmptyCouple)
        };

        // Căn giữa legend
        float totalW = items.Length * 80;
        float x = startX + (gridWidth - totalW) / 2;

        foreach (var (label, color) in items)
        {
            using var brush = new SolidBrush(color);
            var seatRect = new RectangleF(x, y, 14, 14);
            using var path = RoundedRect(seatRect, 3);
            g.FillPath(brush, path);

            using var labelBrush = new SolidBrush(Color.FromArgb(150, 150, 175));
            g.DrawString(label, font, labelBrush, x + 18, y);
            x += 80;
        }
    }

    // ==================== MOUSE HANDLING ====================

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        var seat = HitTest(e.Location);
        if (seat == null) return;
        if (_soldSeatIds.Contains(seat.Id)) return;

        if (_selectedSeatIds.Contains(seat.Id))
            _selectedSeatIds.Remove(seat.Id);
        else
            _selectedSeatIds.Add(seat.Id);

        SeatSelectionChanged?.Invoke(this, EventArgs.Empty);
        Invalidate();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var seat = HitTest(e.Location);
        int newHover = seat?.Id ?? -1;

        if (newHover != _hoveredSeatId)
        {
            _hoveredSeatId = newHover;
            Invalidate();

            if (seat != null && !_soldSeatIds.Contains(seat.Id))
            {
                decimal price = BasePrice * seat.PriceMultiplier;
                string status = _selectedSeatIds.Contains(seat.Id) ? "✓ Đã chọn" : seat.Type;
                _toolTip.SetToolTip(this,
                    $"Ghế {seat.RowLabel}{seat.SeatNumber} ({status})\n" +
                    $"Giá: {price:N0} đ");
            }
            else
            {
                _toolTip.SetToolTip(this, "");
            }
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (_hoveredSeatId != -1)
        {
            _hoveredSeatId = -1;
            Invalidate();
        }
    }

    // ==================== HELPERS ====================

    private SeatInfo? HitTest(Point pt)
    {
        int gridWidth = _maxCols * (CellSize + Gap) - Gap;
        int totalWidth = RowLabelWidth + gridWidth + RowLabelWidth;
        int offsetX = Math.Max(0, (this.Width - totalWidth) / 2);

        foreach (var seat in _seats)
        {
            int span = Math.Max(1, seat.GridSpan);
            float x = offsetX + RowLabelWidth + seat.GridColumn * (CellSize + Gap);
            float y = ScreenMarginTop + seat.GridRow * (CellSize + Gap);
            var rect = new RectangleF(x, y, span * CellSize + (span - 1) * Gap, CellSize);
            if (rect.Contains(pt))
            {
                return seat;
            }
        }
        return null;
    }

    private Color GetSeatColor(SeatInfo seat)
    {
        if (_soldSeatIds.Contains(seat.Id)) return SoldColor;
        if (_selectedSeatIds.Contains(seat.Id)) return SelectedColor;

        return seat.Type switch
        {
            "VIP" => EmptyVip,
            "Couple" => EmptyCouple,
            _ => EmptyStandard
        };
    }

    /// <summary>
    /// Tạo GraphicsPath cho rounded rectangle.
    /// </summary>
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

    // ==================== DATA MODEL ====================

    public class SeatInfo
    {
        public int Id { get; set; }
        public string RowLabel { get; set; } = "";
        public int SeatNumber { get; set; }
        public int GridRow { get; set; }
        public int GridColumn { get; set; }
        public int GridSpan { get; set; } = 1;
        public string Type { get; set; } = "Standard";
        public decimal PriceMultiplier { get; set; } = 1.0m;
    }
}
