namespace BaiTapLon.Forms.Admin;

public static class AdminLayouts
{
    public static Control CreateManagementPage(string title, Control toolbar, Control content)
    {
        var layout = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var pnlHeader = CreateHeader(title, toolbar);
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, pnlHeader.Height));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        pnlHeader.Dock = DockStyle.Fill;
        content.Dock = DockStyle.Fill;

        root.Controls.Add(pnlHeader, 0, 0);
        root.Controls.Add(content, 0, 1);
        layout.Controls.Add(root);
        return layout;
    }

    public static Control CreatePagedGridContent(DataGridView grid, AdminPaginationBar pagination)
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        grid.Dock = DockStyle.Fill;
        pagination.Dock = DockStyle.Bottom;
        panel.Controls.Add(grid);
        panel.Controls.Add(pagination);
        return panel;
    }

    private static Panel CreateHeader(string title, Control toolbar)
    {
        var header = new Panel
        {
            Name = "pnlHeader",
            Dock = DockStyle.Top,
            Height = 112,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 8),
            Margin = Padding.Empty
        };

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var titleLabel = new Label
        {
            Text = title,
            Font = AdminTheme.TitleFont,
            ForeColor = AdminTheme.TitleText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };

        toolbar.Dock = DockStyle.Fill;
        toolbar.Margin = Padding.Empty;
        headerLayout.Controls.Add(titleLabel, 0, 0);
        headerLayout.Controls.Add(toolbar, 0, 1);

        header.Controls.Add(headerLayout);
        return header;
    }
}
