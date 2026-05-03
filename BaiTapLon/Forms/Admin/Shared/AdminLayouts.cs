namespace BaiTapLon.Forms.Admin;

public static class AdminLayouts
{
    public static TableLayoutPanel CreateManagementPage(string title, Control toolbar, Control content)
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 120F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        layout.Controls.Add(CreateHeader(title, toolbar), 0, 0);
        layout.Controls.Add(content, 0, 1);
        return layout;
    }

    private static TableLayoutPanel CreateHeader(string title, Control toolbar)
    {
        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 5),
            Margin = Padding.Empty
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F));
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        header.Controls.Add(new Label
        {
            Text = title,
            Font = AdminTheme.TitleFont,
            ForeColor = AdminTheme.TitleText,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        }, 0, 0);

        header.Controls.Add(toolbar, 0, 1);
        return header;
    }
}
