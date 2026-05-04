namespace BaiTapLon.Forms.Admin;

public class AdminPaginationBar : UserControl
{
    private readonly Button _btnFirst;
    private readonly Button _btnPrev;
    private readonly Button _btnNext;
    private readonly Button _btnLast;
    private readonly Label _lblPage;
    private readonly Label _lblRange;
    private readonly ComboBox _cboPageSize;
    private bool _updating;

    public event EventHandler? PaginationChanged;

    public int PageIndex { get; private set; } = 1;
    public int PageSize { get; private set; } = 20;
    public int TotalItems { get; private set; }
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));
    public int Skip => (PageIndex - 1) * PageSize;

    public AdminPaginationBar()
    {
        Dock = DockStyle.Bottom;
        Height = 48;
        BackColor = AdminTheme.PageBack;
        Padding = new Padding(0, 8, 0, 0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 205));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 290));
        Controls.Add(layout);

        _lblRange = new Label
        {
            Dock = DockStyle.Fill,
            ForeColor = AdminTheme.MutedText,
            Font = AdminTheme.BodyFont,
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(_lblRange, 0, 0);

        var pageSizePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };
        pageSizePanel.Controls.Add(new Label
        {
            Text = "Dòng/trang",
            ForeColor = AdminTheme.MutedText,
            Font = AdminTheme.BodyFont,
            Size = new Size(112, 30),
            TextAlign = ContentAlignment.MiddleLeft
        });
        _cboPageSize = AdminControls.CreateComboBox(78);
        _cboPageSize.Items.AddRange(new object[] { 10, 20, 50, 100 });
        _cboPageSize.SelectedItem = PageSize;
        _cboPageSize.SelectedIndexChanged += (s, e) =>
        {
            if (_updating || _cboPageSize.SelectedItem is not int pageSize || pageSize == PageSize)
                return;

            PageSize = pageSize;
            PageIndex = 1;
            RefreshState();
            PaginationChanged?.Invoke(this, EventArgs.Empty);
        };
        pageSizePanel.Controls.Add(_cboPageSize);
        layout.Controls.Add(pageSizePanel, 1, 0);

        var nav = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = Padding.Empty
        };

        _btnLast = CreatePagerButton(">|", () => GoToPage(TotalPages));
        _btnNext = CreatePagerButton(">", () => GoToPage(PageIndex + 1));
        _lblPage = new Label
        {
            ForeColor = AdminTheme.Text,
            Font = AdminTheme.BodyBoldFont,
            Size = new Size(96, 30),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(4, 0, 4, 0)
        };
        _btnPrev = CreatePagerButton("<", () => GoToPage(PageIndex - 1));
        _btnFirst = CreatePagerButton("|<", () => GoToPage(1));

        nav.Controls.Add(_btnLast);
        nav.Controls.Add(_btnNext);
        nav.Controls.Add(_lblPage);
        nav.Controls.Add(_btnPrev);
        nav.Controls.Add(_btnFirst);
        layout.Controls.Add(nav, 2, 0);

        RefreshState();
    }

    public void SetTotalItems(int totalItems, bool resetPage)
    {
        TotalItems = Math.Max(0, totalItems);
        if (resetPage)
            PageIndex = 1;
        else
            PageIndex = Math.Clamp(PageIndex, 1, TotalPages);

        RefreshState();
    }

    private Button CreatePagerButton(string text, Action click)
    {
        var button = AdminControls.CreateButton(text, AdminTheme.ButtonNeutral, 38, (s, e) => click());
        button.Height = 30;
        button.Margin = new Padding(2, 0, 0, 0);
        return button;
    }

    private void GoToPage(int page)
    {
        var nextPage = Math.Clamp(page, 1, TotalPages);
        if (nextPage == PageIndex)
            return;

        PageIndex = nextPage;
        RefreshState();
        PaginationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshState()
    {
        _updating = true;
        _cboPageSize.SelectedItem = PageSize;
        _updating = false;

        var start = TotalItems == 0 ? 0 : Skip + 1;
        var end = Math.Min(Skip + PageSize, TotalItems);

        _lblRange.Text = TotalItems == 0
            ? "Không có dữ liệu"
            : $"Hiển thị {start:N0}-{end:N0} / {TotalItems:N0}";
        _lblPage.Text = $"{PageIndex:N0} / {TotalPages:N0}";

        _btnFirst.Enabled = PageIndex > 1;
        _btnPrev.Enabled = PageIndex > 1;
        _btnNext.Enabled = PageIndex < TotalPages;
        _btnLast.Enabled = PageIndex < TotalPages;
    }
}
