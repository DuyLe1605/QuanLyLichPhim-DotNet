$filePath = "d:\Study\.Net\BaiTapLon\BaiTapLon\Forms\Admin\Customers\UcCustomerManagement.cs"
$content = Get-Content $filePath -Raw

$startIndex = $content.IndexOf("    private Control CreateDetailPanel()")
$endIndex = $content.IndexOf("        return panel;", $startIndex)

if ($startIndex -ge 0 -and $endIndex -ge 0) {
    # Find the closing brace after return panel;
    $braceIndex = $content.IndexOf("    }", $endIndex) + 5
    $oldBlock = $content.Substring($startIndex, $braceIndex - $startIndex)
    
    $newBlock = @"
    private Control CreateDetailPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            BackColor = Color.FromArgb(22, 22, 38),
            Padding = new Padding(15)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
        panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var topInfo = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };
        topInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        topInfo.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        picQr = new PictureBox
        {
            Size = new Size(120, 120),
            BackColor = Color.White,
            SizeMode = PictureBoxSizeMode.Zoom,
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 0, 10, 0)
        };
        topInfo.Controls.Add(picQr, 0, 0);

        var infoText = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            Margin = Padding.Empty
        };
        infoText.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        infoText.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        lblCustName = new Label
        {
            Text = "Chọn khách hàng",
            Font = new Font("Segoe UI", 13, FontStyle.Bold),
            ForeColor = Color.FromArgb(210, 210, 230),
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 5)
        };
        infoText.Controls.Add(lblCustName, 0, 0);

        lblCustInfo = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(150, 150, 180),
            Dock = DockStyle.Fill,
            AutoSize = true
        };
        infoText.Controls.Add(lblCustInfo, 0, 1);
        topInfo.Controls.Add(infoText, 1, 0);

        panel.Controls.Add(topInfo, 0, 0);

        lblCustStats = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(180, 180, 210),
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 10)
        };
        panel.Controls.Add(lblCustStats, 0, 1);

        panel.Controls.Add(new Panel { Dock = DockStyle.Fill, Height = 1, BackColor = Color.FromArgb(50, 50, 75), Margin = new Padding(0, 10, 0, 10) }, 0, 2);

        panel.Controls.Add(new Label
        {
            Text = "📋 Lịch sử điểm thưởng",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 160, 190),
            Dock = DockStyle.Fill,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 5)
        }, 0, 3);

        dgvHistory = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.FromArgb(26, 26, 42),
            GridColor = Color.FromArgb(40, 40, 60),
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            RowHeadersVisible = false,
            EnableHeadersVisualStyles = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            RowTemplate = { Height = 30 },
            Font = new Font("Segoe UI", 9)
        };
        dgvHistory.DefaultCellStyle.BackColor = Color.FromArgb(26, 26, 42);
        dgvHistory.DefaultCellStyle.ForeColor = AdminTheme.Text;
        dgvHistory.DefaultCellStyle.SelectionBackColor = AdminTheme.GridSelection;
        dgvHistory.ColumnHeadersDefaultCellStyle.BackColor = AdminTheme.GridHeaderBack;
        dgvHistory.ColumnHeadersDefaultCellStyle.ForeColor = AdminTheme.MutedText;
        dgvHistory.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        dgvHistory.ColumnHeadersHeight = 32;
        panel.Controls.Add(dgvHistory, 0, 4);

        return panel;
    }
"@
    
    $content = $content.Replace($oldBlock, $newBlock)
    Set-Content -Path $filePath -Value $content -Encoding UTF8
    Write-Host "Replaced successfully"
} else {
    Write-Host "Could not find CreateDetailPanel"
}
