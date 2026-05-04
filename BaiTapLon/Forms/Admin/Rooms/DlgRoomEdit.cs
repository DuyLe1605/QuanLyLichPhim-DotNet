using BaiTapLon.Models;
using BaiTapLon.Forms.Controls;

namespace BaiTapLon.Forms.Admin;

/// <summary>
/// DTO cho cấu hình 1 hàng ghế.
/// </summary>
public class RowConfig
{
    public string RowLabel { get; set; } = "A";
    public int SeatCount { get; set; } = 10;
    public string SeatType { get; set; } = "Standard";
    public decimal PriceMultiplier { get; set; } = 1.0m;
}

public class SeatLayoutItem
{
    public string RowLabel { get; set; } = "A";
    public int SeatNumber { get; set; }
    public int GridRow { get; set; }
    public int GridColumn { get; set; }
    public int GridSpan { get; set; } = 1;
    public string SeatType { get; set; } = "Standard";
    public decimal PriceMultiplier { get; set; } = 1.0m;
}

/// <summary>
/// Dialog tạo/sửa phòng chiếu — cấu hình ghế theo từng hàng.
/// </summary>
public class DlgRoomEdit : Form
{
    private TextBox txtName = null!;
    private ComboBox cboType = null!;
    private DataGridView dgvRows = null!;
    private Label lblSummary = null!;
    private SeatLayoutPreviewControl seatPreview = null!;
    private ErrorProvider errorProvider = null!;
    private List<SeatLayoutItem> _customSeatLayout = new();

    public Room RoomData { get; private set; } = new();
    public List<RowConfig> RowConfigs { get; private set; } = new();
    public List<SeatLayoutItem> SeatLayoutItems { get; private set; } = new();

    private readonly Room? _edit;

    public DlgRoomEdit(Room? edit)
    {
        _edit = edit;
        InitUI();
        if (_edit != null) LoadData();
    }

    private void InitUI()
    {
        bool isNew = _edit == null;
        this.Text = isNew ? "Thêm phòng chiếu" : "Sửa phòng";
        this.ClientSize = new Size(1120, 640);
        this.MinimumSize = new Size(1080, 620);
        this.StartPosition = FormStartPosition.CenterParent;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.BackColor = Color.FromArgb(24, 24, 40);
        this.ForeColor = Color.FromArgb(200, 200, 220);

        errorProvider = new ErrorProvider
        {
            ContainerControl = this,
            BlinkStyle = ErrorBlinkStyle.NeverBlink
        };

        int x1 = 20, x2 = 130, y = 20;

        // === Room info ===
        Lbl("Tên phòng *", x1, y);
        txtName = Txt(x2, y, 200);
        txtName.TextChanged += (s, e) => UpdateSeatPreview();
        y += 42;

        Lbl("Loại phòng", x1, y);
        cboType = new ComboBox
        {
            Font = new Font("Segoe UI", 11),
            Size = new Size(130, 30),
            Location = new Point(x2, y),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboType.Items.AddRange(new[] { "2D", "3D", "IMAX" });
        cboType.SelectedIndex = 0;
        this.Controls.Add(cboType);
        y += 48;

        // === Seat Configuration Section ===
        this.Controls.Add(new Label
        {
            Text = "🪑  Cấu hình ghế theo hàng",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 80, 255),
            Location = new Point(x1, y),
            AutoSize = true
        });
        y += 30;

        // Buttons: Add Row / Remove Row
        var btnAddRow = new Button
        {
            Text = "➕ Thêm hàng",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Size = new Size(120, 30),
            Location = new Point(x1, y),
            BackColor = Color.FromArgb(60, 160, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Enabled = isNew
        };
        btnAddRow.FlatAppearance.BorderSize = 0;
        btnAddRow.Click += BtnAddRow_Click;
        this.Controls.Add(btnAddRow);

        var btnRemoveRow = new Button
        {
            Text = "➖ Xóa hàng",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Size = new Size(120, 30),
            Location = new Point(155, y),
            BackColor = Color.FromArgb(200, 60, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Enabled = isNew
        };
        btnRemoveRow.FlatAppearance.BorderSize = 0;
        btnRemoveRow.Click += BtnRemoveRow_Click;
        this.Controls.Add(btnRemoveRow);

        var btnBuilder = new Button
        {
            Text = "Thiết kế sơ đồ",
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Size = new Size(140, 30),
            Location = new Point(290, y),
            BackColor = Color.FromArgb(80, 60, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Enabled = isNew
        };
        btnBuilder.FlatAppearance.BorderSize = 0;
        btnBuilder.Click += BtnSeatBuilder_Click;
        this.Controls.Add(btnBuilder);
        y += 38;

        // === DataGridView for row configs ===
        dgvRows = new DataGridView
        {
            Location = new Point(x1, y),
            Size = new Size(520, 260),
            BackgroundColor = Color.FromArgb(26, 26, 44),
            GridColor = Color.FromArgb(45, 45, 65),
            BorderStyle = BorderStyle.FixedSingle,
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            EnableHeadersVisualStyles = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 10),
            ReadOnly = !isNew,
            EditMode = isNew ? DataGridViewEditMode.EditOnEnter : DataGridViewEditMode.EditProgrammatically
        };
        dgvRows.RowTemplate.Height = 32;
        StyleGrid(dgvRows);

        // Columns
        dgvRows.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colLabel",
            HeaderText = "Hàng",
            Width = 60,
            ReadOnly = true,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 11, FontStyle.Bold) }
        });
        dgvRows.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colSeats",
            HeaderText = "Số ghế",
            Width = 80,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });

        var colType = new DataGridViewComboBoxColumn
        {
            Name = "colType",
            HeaderText = "Loại ghế",
            Width = 120,
            Items = { "Standard", "VIP", "Couple" },
            FlatStyle = FlatStyle.Flat,
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
        };
        dgvRows.Columns.Add(colType);

        dgvRows.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "colMultiplier",
            HeaderText = "Hệ số giá",
            Width = 90,
            DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter }
        });

        this.Controls.Add(dgvRows);
        y += 270;

        this.Controls.Add(new Label
        {
            Text = "Preview sơ đồ ghế",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 80, 255),
            Location = new Point(690, 20),
            AutoSize = true
        });

        seatPreview = new SeatLayoutPreviewControl
        {
            Location = new Point(690, 55),
            Size = new Size(390, 520),
            Anchor = AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom
        };
        this.Controls.Add(seatPreview);

        // Summary
        lblSummary = new Label
        {
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 200, 120),
            Location = new Point(x1, y),
            AutoSize = true,
            Text = "Tổng: 0 hàng, 0 ghế"
        };
        this.Controls.Add(lblSummary);
        y += 30;

        // === Action Buttons ===
        var btnOk = new Button
        {
            Text = isNew ? "✅ Tạo phòng" : "💾 Lưu",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            Size = new Size(140, 40),
            Location = new Point(330, y),
            BackColor = Color.FromArgb(80, 160, 80),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.OK
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += BtnOk_Click;
        this.Controls.Add(btnOk);

        var btnCancel = new Button
        {
            Text = "Hủy",
            Font = new Font("Segoe UI", 10),
            Size = new Size(100, 40),
            Location = new Point(480, y),
            BackColor = Color.FromArgb(50, 50, 75),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.Cancel
        };
        btnCancel.FlatAppearance.BorderSize = 0;
        this.Controls.Add(btnCancel);

        this.AcceptButton = btnOk;
        this.CancelButton = btnCancel;

        // Hook up change events
        dgvRows.CellValueChanged += (s, e) => UpdateSummary();
        dgvRows.CurrentCellDirtyStateChanged += (s, e) =>
        {
            if (dgvRows.IsCurrentCellDirty)
                dgvRows.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };

        // Seed default rows for new room
        if (isNew)
        {
            SeedDefaultRows();
        }
    }

    private void SeedDefaultRows()
    {
        string[] labels = { "A", "B", "C", "D", "E", "F", "G", "H" };
        foreach (var label in labels)
        {
            string type = label switch
            {
                "F" or "G" => "VIP",
                "H" => "Couple",
                _ => "Standard"
            };
            decimal mult = type switch
            {
                "VIP" => 1.5m,
                "Couple" => 2.0m,
                _ => 1.0m
            };
            int seats = type == "Couple" ? 8 : 10;

            dgvRows.Rows.Add(label, seats, type, mult);
        }
        UpdateSummary();
    }

    private void BtnAddRow_Click(object? s, EventArgs e)
    {
        int nextIdx = dgvRows.Rows.Count;
        if (nextIdx >= 26) { MessageBox.Show("Tối đa 26 hàng (A-Z)!"); return; }

        string label = ((char)('A' + nextIdx)).ToString();
        dgvRows.Rows.Add(label, 10, "Standard", 1.0m);
        UpdateSummary();
    }

    private void BtnRemoveRow_Click(object? s, EventArgs e)
    {
        if (dgvRows.Rows.Count == 0) return;
        dgvRows.Rows.RemoveAt(dgvRows.Rows.Count - 1);

        // Re-label remaining rows
        for (int i = 0; i < dgvRows.Rows.Count; i++)
            dgvRows.Rows[i].Cells["colLabel"].Value = ((char)('A' + i)).ToString();

        UpdateSummary();
    }

    private void BtnSeatBuilder_Click(object? s, EventArgs e)
    {
        using var dlg = new DlgSeatBuilder(BuildLayoutItemsFromGrid());
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _customSeatLayout = dlg.SeatLayoutItems;
        ApplyLayoutSummaryToGrid(_customSeatLayout);
        UpdateSummary();
    }

    private List<SeatLayoutItem> BuildLayoutItemsFromGrid()
    {
        var items = new List<SeatLayoutItem>();
        for (int r = 0; r < dgvRows.Rows.Count; r++)
        {
            var row = dgvRows.Rows[r];
            string label = row.Cells["colLabel"].Value?.ToString() ?? ((char)('A' + r)).ToString();
            int.TryParse(row.Cells["colSeats"].Value?.ToString(), out int seats);
            if (seats <= 0) seats = 10;

            string type = row.Cells["colType"].Value?.ToString() ?? "Standard";
            decimal.TryParse(row.Cells["colMultiplier"].Value?.ToString(), out decimal mult);
            if (mult <= 0) mult = type switch
            {
                "VIP" => 1.5m,
                "Couple" => 2.0m,
                _ => 1.0m
            };

            for (int c = 0; c < seats; c++)
            {
                items.Add(new SeatLayoutItem
                {
                    RowLabel = label,
                    SeatNumber = c + 1,
                    GridRow = r,
                    GridColumn = c,
                    GridSpan = 1,
                    SeatType = type,
                    PriceMultiplier = mult
                });
            }
        }

        return items;
    }

    private void ApplyLayoutSummaryToGrid(List<SeatLayoutItem> items)
    {
        dgvRows.Rows.Clear();
        foreach (var group in items.GroupBy(i => i.RowLabel).OrderBy(g => g.Min(i => i.GridRow)))
        {
            var first = group.OrderBy(i => i.GridColumn).First();
            dgvRows.Rows.Add(group.Key, group.Count(), first.SeatType, first.PriceMultiplier);
        }
    }

    private void UpdateSummary()
    {
        int totalSeats = 0;
        for (int i = 0; i < dgvRows.Rows.Count; i++)
        {
            if (int.TryParse(dgvRows.Rows[i].Cells["colSeats"].Value?.ToString(), out int seats))
                totalSeats += seats;
        }
        lblSummary.Text = $"Tổng: {dgvRows.Rows.Count} hàng, {totalSeats} ghế";
        UpdateSeatPreview();
    }

    private void UpdateSeatPreview()
    {
        if (seatPreview == null) return;

        if (_customSeatLayout.Count > 0)
        {
            var previewSeats = _customSeatLayout.Select((item, index) => new Seat
            {
                Id = index + 1,
                RowLabel = item.RowLabel,
                SeatNumber = item.SeatNumber,
                Type = item.SeatType,
                GridRow = item.GridRow,
                GridColumn = item.GridColumn,
                GridSpan = item.GridSpan
            });
            seatPreview.SetSeats(previewSeats, $"Sơ đồ {txtName.Text.Trim()}");
            return;
        }

        var rows = new List<SeatLayoutPreviewControl.SeatPreviewRow>();
        for (int i = 0; i < dgvRows.Rows.Count; i++)
        {
            var row = dgvRows.Rows[i];
            string label = row.Cells["colLabel"].Value?.ToString() ?? ((char)('A' + i)).ToString();
            int.TryParse(row.Cells["colSeats"].Value?.ToString(), out int seats);
            string type = row.Cells["colType"].Value?.ToString() ?? "Standard";
            rows.Add(new SeatLayoutPreviewControl.SeatPreviewRow(label, Math.Max(0, seats), type));
        }

        seatPreview.SetRows(rows, $"Sơ đồ {txtName.Text.Trim()}");
    }

    private void BtnOk_Click(object? s, EventArgs e)
    {
        errorProvider.Clear();
        if (string.IsNullOrWhiteSpace(txtName.Text) || dgvRows.Rows.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
                errorProvider.SetError(txtName, "Nhập tên phòng.");
            if (dgvRows.Rows.Count == 0)
                errorProvider.SetError(dgvRows, "Chưa có hàng ghế nào.");

            this.DialogResult = DialogResult.None;
            return;
        }

        if (string.IsNullOrWhiteSpace(txtName.Text))
        {
            MessageBox.Show("Nhập tên phòng!", "Thiếu thông tin");
            this.DialogResult = DialogResult.None;
            return;
        }
        if (dgvRows.Rows.Count == 0)
        {
            MessageBox.Show("Chưa có hàng ghế nào!", "Thiếu thông tin");
            this.DialogResult = DialogResult.None;
            return;
        }

        // Build RowConfigs
        RowConfigs.Clear();
        SeatLayoutItems.Clear();
        int totalSeats = 0;
        int maxCols = 0;

        if (_customSeatLayout.Count > 0)
        {
            SeatLayoutItems = _customSeatLayout
                .OrderBy(i => i.GridRow)
                .ThenBy(i => i.GridColumn)
                .ToList();

            totalSeats = SeatLayoutItems.Count;
            maxCols = SeatLayoutItems.Max(i => i.GridColumn + Math.Max(1, i.GridSpan));
            RowConfigs = SeatLayoutItems
                .GroupBy(i => i.RowLabel)
                .OrderBy(g => g.Min(i => i.GridRow))
                .Select(g => new RowConfig
                {
                    RowLabel = g.Key,
                    SeatCount = g.Count(),
                    SeatType = g.First().SeatType,
                    PriceMultiplier = g.First().PriceMultiplier
                })
                .ToList();
        }
        else
        {
            for (int i = 0; i < dgvRows.Rows.Count; i++)
            {
                var row = dgvRows.Rows[i];
                string label = row.Cells["colLabel"].Value?.ToString() ?? ((char)('A' + i)).ToString();
                int seats = 10;
                int.TryParse(row.Cells["colSeats"].Value?.ToString(), out seats);
                string type = row.Cells["colType"].Value?.ToString() ?? "Standard";
                decimal mult = 1.0m;
                decimal.TryParse(row.Cells["colMultiplier"].Value?.ToString(), out mult);

                if (seats < 1) seats = 1;
                if (seats > 30) seats = 30;
                if (mult <= 0) mult = 1.0m;

                RowConfigs.Add(new RowConfig
                {
                    RowLabel = label,
                    SeatCount = seats,
                    SeatType = type,
                    PriceMultiplier = mult
                });

                totalSeats += seats;
                if (seats > maxCols) maxCols = seats;
            }
        }

        RoomData = new Room
        {
            Name = txtName.Text.Trim(),
            Type = cboType.SelectedItem?.ToString() ?? "2D",
            Rows = SeatLayoutItems.Count > 0 ? SeatLayoutItems.Max(i => i.GridRow) + 1 : RowConfigs.Count,
            Columns = maxCols,
            TotalSeats = totalSeats,
            IsActive = true
        };
    }

    private void LoadData()
    {
        txtName.Text = _edit!.Name;
        var i = cboType.Items.IndexOf(_edit.Type);
        cboType.SelectedIndex = i >= 0 ? i : 0;

        // Load existing seats into the grid (read-only)
        if (_edit.Seats.Count > 0)
        {
            var rowGroups = _edit.Seats
                .GroupBy(s => s.RowLabel)
                .OrderBy(g => g.Key)
                .ToList();

            foreach (var group in rowGroups)
            {
                dgvRows.Rows.Add(
                    group.Key,
                    group.Count(),
                    group.First().Type,
                    group.First().PriceMultiplier
                );
            }
        }
        UpdateSummary();
    }

    // === Helpers ===

    private void Lbl(string t, int x, int y)
    {
        this.Controls.Add(new Label
        {
            Text = t,
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(160, 160, 185),
            Location = new Point(x, y + 4),
            AutoSize = true
        });
    }

    private TextBox Txt(int x, int y, int w)
    {
        var t = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            Size = new Size(w, 30),
            Location = new Point(x, y),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle
        };
        this.Controls.Add(t);
        return t;
    }

    private static void StyleGrid(DataGridView dgv)
    {
        dgv.DefaultCellStyle.BackColor = Color.FromArgb(26, 26, 44);
        dgv.DefaultCellStyle.ForeColor = Color.FromArgb(200, 200, 220);
        dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(60, 50, 120);
        dgv.DefaultCellStyle.SelectionForeColor = Color.White;
        dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 52);
        dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(160, 160, 190);
        dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        dgv.ColumnHeadersHeight = 36;
        dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(30, 30, 48);
    }
}
