namespace BaiTapLon.Forms.Admin;

public class DlgSeatBuilder : Form
{
    private readonly NumericUpDown nudRows;
    private readonly NumericUpDown nudCols;
    private readonly SeatBuilderCanvas canvas;
    private string currentTool = "Standard";

    public List<SeatLayoutItem> SeatLayoutItems { get; private set; } = new();

    public DlgSeatBuilder(List<SeatLayoutItem> initialLayout)
    {
        Text = "Thiet ke so do ghe";
        ClientSize = new Size(1040, 680);
        MinimumSize = new Size(880, 560);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.FromArgb(18, 18, 30);
        ForeColor = Color.FromArgb(220, 220, 240);
        Padding = new Padding(12);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        Controls.Add(root);

        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent
        };
        root.Controls.Add(toolbar, 0, 0);

        toolbar.Controls.Add(MakeLabel("Hang", 45));
        nudRows = MakeNumber(8, 1, 20);
        toolbar.Controls.Add(nudRows);
        toolbar.Controls.Add(MakeLabel("Cot", 35));
        nudCols = MakeNumber(10, 1, 30);
        toolbar.Controls.Add(nudCols);
        toolbar.Controls.Add(MakeButton("Ap dung luoi", 115, Color.FromArgb(50, 50, 75), (s, e) => ApplyGridSize()));

        toolbar.Controls.Add(MakeButton("Ghe thuong", 105, Color.FromArgb(70, 80, 105), (s, e) => SetTool("Standard")));
        toolbar.Controls.Add(MakeButton("VIP", 70, Color.FromArgb(120, 90, 230), (s, e) => SetTool("VIP")));
        toolbar.Controls.Add(MakeButton("Ghe doi", 90, Color.FromArgb(220, 105, 145), (s, e) => SetTool("Couple")));
        toolbar.Controls.Add(MakeButton("Xoa/loi di", 100, Color.FromArgb(80, 80, 95), (s, e) => SetTool("Empty")));

        var scroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(22, 22, 38),
            Padding = new Padding(14)
        };
        root.Controls.Add(scroll, 0, 1);

        canvas = new SeatBuilderCanvas(initialLayout)
        {
            Location = new Point(14, 14)
        };
        canvas.Tool = currentTool;
        scroll.Controls.Add(canvas);
        nudRows.Value = Math.Min(nudRows.Maximum, Math.Max(nudRows.Minimum, canvas.GridRows));
        nudCols.Value = Math.Min(nudCols.Maximum, Math.Max(nudCols.Minimum, canvas.GridColumns));

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 8, 0, 0)
        };
        root.Controls.Add(buttons, 0, 2);

        var btnCancel = MakeButton("Huy", 90, Color.FromArgb(50, 50, 75), (s, e) => DialogResult = DialogResult.Cancel);
        var btnSave = MakeButton("Luu so do", 120, Color.FromArgb(60, 160, 80), BtnSave_Click);
        btnCancel.DialogResult = DialogResult.Cancel;
        btnSave.DialogResult = DialogResult.OK;
        buttons.Controls.Add(btnCancel);
        buttons.Controls.Add(btnSave);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
        SetTool("Standard");
    }

    private void ApplyGridSize()
    {
        canvas.ResizeGrid((int)nudRows.Value, (int)nudCols.Value);
    }

    private void SetTool(string tool)
    {
        currentTool = tool;
        canvas.Tool = tool;
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        SeatLayoutItems = canvas.BuildSeatLayoutItems();
        if (SeatLayoutItems.Count == 0)
        {
            MessageBox.Show("So do phai co it nhat 1 ghe.", "Thieu du lieu", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
        }
    }

    private static Label MakeLabel(string text, int width) => new()
    {
        Text = text,
        Size = new Size(width, 30),
        Font = new Font("Segoe UI", 10),
        ForeColor = Color.FromArgb(170, 170, 195),
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = new Padding(0, 4, 4, 0)
    };

    private static NumericUpDown MakeNumber(int value, int min, int max) => new()
    {
        Value = value,
        Minimum = min,
        Maximum = max,
        Size = new Size(58, 30),
        Font = new Font("Segoe UI", 10),
        BackColor = Color.FromArgb(35, 35, 55),
        ForeColor = Color.White,
        Margin = new Padding(0, 4, 10, 0)
    };

    private static Button MakeButton(string text, int width, Color color, EventHandler click)
    {
        var button = new Button
        {
            Text = text,
            Size = new Size(width, 32),
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            BackColor = color,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 4, 8, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += click;
        return button;
    }
}

internal class SeatBuilderCanvas : Control
{
    private enum CellKind { Empty, Standard, VIP, Couple, Blocked }

    private const int Cell = 34;
    private const int Gap = 5;
    private const int Pad = 24;
    private CellKind[,] cells;

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public int GridRows { get; private set; }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public int GridColumns { get; private set; }

    [System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
    [System.ComponentModel.Browsable(false)]
    public string Tool { get; set; } = "Standard";

    public SeatBuilderCanvas(List<SeatLayoutItem> initialLayout)
    {
        DoubleBuffered = true;
        BackColor = Color.FromArgb(22, 22, 38);
        Cursor = Cursors.Cross;

        GridRows = Math.Max(1, initialLayout.Count == 0 ? 8 : initialLayout.Max(i => i.GridRow) + 1);
        GridColumns = Math.Max(1, initialLayout.Count == 0 ? 10 : initialLayout.Max(i => i.GridColumn + Math.Max(1, i.GridSpan)));
        cells = new CellKind[GridRows, GridColumns];

        if (initialLayout.Count == 0)
        {
            Fill(CellKind.Standard);
        }
        else
        {
            Fill(CellKind.Empty);
            foreach (var item in initialLayout)
            {
                var kind = item.SeatType switch
                {
                    "VIP" => CellKind.VIP,
                    "Couple" => CellKind.Couple,
                    _ => CellKind.Standard
                };

                cells[item.GridRow, item.GridColumn] = kind;
                for (int offset = 1; offset < Math.Max(1, item.GridSpan); offset++)
                {
                    if (item.GridColumn + offset < GridColumns)
                        cells[item.GridRow, item.GridColumn + offset] = CellKind.Blocked;
                }
            }
        }

        ResizeSurface();
    }

    public void ResizeGrid(int rows, int columns)
    {
        rows = Math.Max(1, rows);
        columns = Math.Max(1, columns);
        var next = new CellKind[rows, columns];
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < columns; c++)
            {
                next[r, c] = r < GridRows && c < GridColumns ? cells[r, c] : CellKind.Standard;
            }
        }

        GridRows = rows;
        GridColumns = columns;
        cells = next;
        NormalizeBlockedCells();
        ResizeSurface();
        Invalidate();
    }

    public List<SeatLayoutItem> BuildSeatLayoutItems()
    {
        var result = new List<SeatLayoutItem>();
        int labelIndex = 0;

        for (int r = 0; r < GridRows; r++)
        {
            var rowSeats = new List<(int Col, CellKind Kind)>();
            for (int c = 0; c < GridColumns; c++)
            {
                if (cells[r, c] is CellKind.Standard or CellKind.VIP or CellKind.Couple)
                    rowSeats.Add((c, cells[r, c]));
            }

            if (rowSeats.Count == 0) continue;

            string rowLabel = ((char)('A' + labelIndex++)).ToString();
            int seatNumber = 1;
            foreach (var (col, kind) in rowSeats.OrderBy(s => s.Col))
            {
                string type = kind switch
                {
                    CellKind.VIP => "VIP",
                    CellKind.Couple => "Couple",
                    _ => "Standard"
                };

                result.Add(new SeatLayoutItem
                {
                    RowLabel = rowLabel,
                    SeatNumber = seatNumber++,
                    GridRow = r,
                    GridColumn = col,
                    GridSpan = kind == CellKind.Couple ? 2 : 1,
                    SeatType = type,
                    PriceMultiplier = type switch
                    {
                        "VIP" => 1.5m,
                        "Couple" => 2.0m,
                        _ => 1.0m
                    }
                });
            }
        }

        return result;
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        int col = (e.X - Pad) / (Cell + Gap);
        int row = (e.Y - Pad) / (Cell + Gap);
        if (row < 0 || row >= GridRows || col < 0 || col >= GridColumns) return;

        SetCell(row, col, Tool);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        using var font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
        var labels = BuildSeatLayoutItems().ToDictionary(i => (i.GridRow, i.GridColumn), i => $"{i.RowLabel}{i.SeatNumber}");

        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridColumns; c++)
            {
                var kind = cells[r, c];
                if (kind == CellKind.Blocked) continue;

                var rect = new RectangleF(Pad + c * (Cell + Gap), Pad + r * (Cell + Gap), Cell, Cell);
                if (kind == CellKind.Couple && c + 1 < GridColumns)
                    rect.Width = Cell * 2 + Gap;

                using var brush = new SolidBrush(GetCellColor(kind));
                using var path = RoundedRect(rect, 5);
                g.FillPath(brush, path);

                using var pen = new Pen(Color.FromArgb(255, 255, 255, kind == CellKind.Empty ? 28 : 55));
                g.DrawPath(pen, path);

                if (kind != CellKind.Empty && labels.TryGetValue((r, c), out var label))
                {
                    using var textBrush = new SolidBrush(Color.White);
                    g.DrawString(label, font, textBrush, rect,
                        new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
                }
            }
        }
    }

    private void SetCell(int row, int col, string tool)
    {
        ClearSpanAt(row, col);

        if (tool == "Empty")
        {
            cells[row, col] = CellKind.Empty;
            return;
        }

        if (tool == "Couple")
        {
            if (col >= GridColumns - 1)
            {
                cells[row, col] = CellKind.Empty;
                return;
            }

            ClearSpanAt(row, col + 1);
            cells[row, col] = CellKind.Couple;
            cells[row, col + 1] = CellKind.Blocked;
            return;
        }

        cells[row, col] = tool == "VIP" ? CellKind.VIP : CellKind.Standard;
    }

    private void ClearSpanAt(int row, int col)
    {
        if (cells[row, col] == CellKind.Blocked)
        {
            for (int c = col - 1; c >= 0; c--)
            {
                if (cells[row, c] == CellKind.Couple)
                {
                    cells[row, c] = CellKind.Empty;
                    break;
                }
                if (cells[row, c] != CellKind.Blocked) break;
            }
        }

        if (cells[row, col] == CellKind.Couple && col + 1 < GridColumns && cells[row, col + 1] == CellKind.Blocked)
            cells[row, col + 1] = CellKind.Empty;

        cells[row, col] = CellKind.Empty;
    }

    private void NormalizeBlockedCells()
    {
        for (int r = 0; r < GridRows; r++)
        {
            for (int c = 0; c < GridColumns; c++)
            {
                if (cells[r, c] == CellKind.Blocked && (c == 0 || cells[r, c - 1] != CellKind.Couple))
                    cells[r, c] = CellKind.Empty;
                if (cells[r, c] == CellKind.Couple && c + 1 >= GridColumns)
                    cells[r, c] = CellKind.Empty;
            }
        }
    }

    private void Fill(CellKind kind)
    {
        for (int r = 0; r < GridRows; r++)
            for (int c = 0; c < GridColumns; c++)
                cells[r, c] = kind;
    }

    private void ResizeSurface()
    {
        Size = new Size(Pad * 2 + GridColumns * (Cell + Gap), Pad * 2 + GridRows * (Cell + Gap));
    }

    private static Color GetCellColor(CellKind kind) => kind switch
    {
        CellKind.Standard => Color.FromArgb(70, 80, 105),
        CellKind.VIP => Color.FromArgb(120, 90, 230),
        CellKind.Couple => Color.FromArgb(220, 105, 145),
        _ => Color.FromArgb(35, 35, 50)
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
}
