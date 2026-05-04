namespace BaiTapLon.Forms.Admin;

public static class AdminControls
{
    public static void ConfigurePage(UserControl page)
    {
        page.Dock = DockStyle.Fill;
        page.BackColor = AdminTheme.PageBack;
        page.Padding = new Padding(10);
    }

    public static FlowLayoutPanel CreateToolbar(params Control[] controls)
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 5, 0, 0),
            Margin = Padding.Empty
        };

        toolbar.Controls.AddRange(controls);
        return toolbar;
    }

    public static TextBox CreateSearchBox(string placeholder, int width = 270)
    {
        return new TextBox
        {
            PlaceholderText = placeholder,
            Font = AdminTheme.BodyFont,
            Size = new Size(width, 30),
            BackColor = AdminTheme.InputBack,
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 8, 0)
        };
    }

    public static ComboBox CreateComboBox(int width = 160)
    {
        return new ComboBox
        {
            Font = AdminTheme.BodyFont,
            Size = new Size(width, 30),
            BackColor = AdminTheme.InputBack,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 0, 8, 0)
        };
    }

    public static Label CreateToolbarLabel(string text, int width)
    {
        return new Label
        {
            Text = text,
            Font = AdminTheme.BodyFont,
            ForeColor = AdminTheme.MutedText,
            Size = new Size(width, 28),
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 2, 0, 0)
        };
    }

    public static DateTimePicker CreateDatePicker(int width = 135)
    {
        return new DateTimePicker
        {
            Font = AdminTheme.BodyFont,
            Size = new Size(width, 28),
            Format = DateTimePickerFormat.Short,
            Margin = new Padding(0, 0, 10, 0)
        };
    }

    public static Button CreateButton(string text, Color backColor, int width, EventHandler click)
    {
        var button = new Button
        {
            Text = text,
            Font = AdminTheme.BodyBoldFont,
            Size = new Size(width, 32),
            BackColor = backColor,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 0, 6, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += click;
        return button;
    }

    public static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = AdminTheme.GridBack,
            GridColor = AdminTheme.GridLine,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = true,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
            ScrollBars = ScrollBars.Both,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            RowTemplate = { Height = 40 },
            Font = AdminTheme.BodyFont
        };

        grid.DefaultCellStyle.BackColor = AdminTheme.GridBack;
        grid.DefaultCellStyle.ForeColor = AdminTheme.Text;
        grid.DefaultCellStyle.SelectionBackColor = AdminTheme.GridSelection;
        grid.DefaultCellStyle.SelectionForeColor = Color.White;
        grid.DefaultCellStyle.Padding = new Padding(5, 0, 5, 0);
        grid.AlternatingRowsDefaultCellStyle.BackColor = AdminTheme.GridAltBack;
        grid.AlternatingRowsDefaultCellStyle.ForeColor = AdminTheme.Text;
        grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = AdminTheme.GridSelection;
        grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;
        grid.ColumnHeadersDefaultCellStyle.BackColor = AdminTheme.GridHeaderBack;
        grid.ColumnHeadersDefaultCellStyle.ForeColor = AdminTheme.MutedText;
        grid.ColumnHeadersDefaultCellStyle.Font = AdminTheme.BodyBoldFont;
        grid.ColumnHeadersDefaultCellStyle.Padding = new Padding(5, 0, 5, 0);
        grid.ColumnHeadersHeight = 42;
        return grid;
    }

    public static void SetColumnWidths(DataGridView grid, params (string ColumnName, int Width)[] columns)
    {
        foreach (var (columnName, width) in columns)
        {
            var column = grid.Columns[columnName];
            if (column == null) continue;

            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            column.Width = width;
        }
    }

    public static void HideColumn(DataGridView grid, string columnName)
    {
        var column = grid.Columns[columnName];
        if (column != null)
            column.Visible = false;
    }

    public static int? GetCurrentIntValue(DataGridView grid, string columnName)
    {
        if (grid.CurrentRow == null || !grid.Columns.Contains(columnName))
            return null;

        var cell = grid.CurrentRow.Cells[columnName];
        return cell?.Value is int value ? value : null;
    }
}
