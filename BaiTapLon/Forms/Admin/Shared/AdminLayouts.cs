namespace BaiTapLon.Forms.Admin;

public static class AdminLayouts
{
    public static Panel CreateManagementPage(string title, Control toolbar, Control content)
    {
        var layout = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };

        var pnlHeader = CreateHeader(title, toolbar);
        content.Dock = DockStyle.Fill;

        layout.Controls.Add(content);
        layout.Controls.Add(pnlHeader);
        pnlHeader.BringToFront();
        return layout;
    }

    private static Panel CreateHeader(string title, Control toolbar)
    {
        var header = new Panel
        {
            Name = "pnlHeader",
            Dock = DockStyle.Top,
            Height = 120,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 0, 5),
            Margin = Padding.Empty
        };

        var titleLabel = new Label
        {
            Text = title,
            Font = AdminTheme.TitleFont,
            ForeColor = AdminTheme.TitleText,
            Dock = DockStyle.Top,
            Height = 50,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = Padding.Empty
        };

        toolbar.Dock = DockStyle.Fill;
        header.Controls.Add(toolbar);
        header.Controls.Add(titleLabel);
        titleLabel.BringToFront();
        return header;
    }
}
