using BaiTapLon.Helpers;
using BaiTapLon.Models;
using BaiTapLon.Services;
using System.Drawing.Drawing2D;

namespace BaiTapLon.Forms.Staff;

public class UcSnackOrder : UserControl
{
    private FlowLayoutPanel flpMenu = null!;
    private DataGridView dgvCart = null!;
    private ComboBox cboCategory = null!;
    private TextBox txtReceived = null!;
    private Label lblTicketTotal = null!;
    private Label lblSnackTotal = null!;
    private Label lblGrandTotal = null!;
    private Label lblChange = null!;
    private Button btnCheckout = null!;

    private readonly Dictionary<int, SnackCartItem> _cart = new();
    private List<Snack> _snacks = new();
    private SaleOrderState _state = null!;

    public event Action? BackRequested;
    public event Action? CheckoutCompleted;

    public UcSnackOrder()
    {
        InitializeComponent();
    }

    public async Task LoadOrderAsync(SaleOrderState state)
    {
        _state = state;
        lblTicketTotal.Text = $"Tiền vé: {state.TicketTotal:N0} đ";
        await LoadSnacksAsync();
        RefreshCart();
    }

    private void InitializeComponent()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.FromArgb(18, 18, 30);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62F));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38F));
        Controls.Add(root);

        root.Controls.Add(CreateMenuSide(), 0, 0);
        root.Controls.Add(CreateCartSide(), 1, 0);
    }

    private Control CreateMenuSide()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(12)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 70F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170F));

        header.Controls.Add(new Label
        {
            Text = "Chọn bắp nước",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            ForeColor = Color.FromArgb(230, 230, 245),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        cboCategory = new ComboBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 20, 0, 14)
        };
        cboCategory.Items.AddRange(["Tất cả", "Food", "Drink", "Combo"]);
        cboCategory.SelectedIndex = 0;
        cboCategory.SelectedIndexChanged += (s, e) => RenderSnackCards();
        header.Controls.Add(cboCategory, 1, 0);

        layout.Controls.Add(header, 0, 0);

        flpMenu = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            WrapContents = true,
            BackColor = Color.FromArgb(18, 18, 30),
            Padding = new Padding(0, 0, 8, 0)
        };
        layout.Controls.Add(flpMenu, 0, 1);
        return layout;
    }

    private Control CreateCartSide()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 9,
            ColumnCount = 1,
            BackColor = Color.FromArgb(22, 22, 38),
            Padding = new Padding(14)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36F));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 54F));

        var btnBack = new Button
        {
            Text = "← Quay lại ghế",
            Dock = DockStyle.Left,
            Width = 135,
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(50, 50, 75),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.Click += (s, e) => BackRequested?.Invoke();
        panel.Controls.Add(btnBack, 0, 0);

        var summary = new Label
        {
            Text = "Giỏ hàng",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            ForeColor = Color.FromArgb(230, 230, 245),
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(summary, 0, 1);

        dgvCart = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = Color.FromArgb(28, 28, 45),
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            GridColor = Color.FromArgb(45, 45, 65),
            RowHeadersVisible = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            MultiSelect = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            EnableHeadersVisualStyles = false,
            Font = new Font("Segoe UI", 9),
            RowTemplate = { Height = 36 }
        };
        dgvCart.DefaultCellStyle.BackColor = Color.FromArgb(28, 28, 45);
        dgvCart.DefaultCellStyle.ForeColor = Color.FromArgb(220, 220, 235);
        dgvCart.DefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 60, 110);
        dgvCart.DefaultCellStyle.SelectionForeColor = Color.White;
        dgvCart.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(32, 32, 50);
        dgvCart.AlternatingRowsDefaultCellStyle.ForeColor = Color.FromArgb(220, 220, 235);
        dgvCart.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(70, 60, 110);
        dgvCart.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.White;
        dgvCart.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 35, 55);
        dgvCart.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(180, 180, 205);
        dgvCart.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        dgvCart.CellContentClick += DgvCart_CellContentClick;
        panel.Controls.Add(dgvCart, 0, 2);

        lblTicketTotal = MakeTotalLabel("Tiền vé: 0 đ", Color.FromArgb(190, 190, 215));
        lblSnackTotal = MakeTotalLabel("Bắp nước: 0 đ", Color.FromArgb(190, 190, 215));
        lblGrandTotal = MakeTotalLabel("Tổng bill: 0 đ", Color.FromArgb(80, 220, 120), 16);
        panel.Controls.Add(lblTicketTotal, 0, 3);
        panel.Controls.Add(lblSnackTotal, 0, 4);
        panel.Controls.Add(lblGrandTotal, 0, 5);

        txtReceived = new TextBox
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 11),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.FromArgb(80, 220, 120),
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Right,
            PlaceholderText = "Tiền nhận"
        };
        txtReceived.TextChanged += (s, e) => CalculateChange();
        panel.Controls.Add(txtReceived, 0, 6);

        lblChange = MakeTotalLabel("Tiền thối: 0 đ", Color.FromArgb(255, 200, 60));
        panel.Controls.Add(lblChange, 0, 7);

        btnCheckout = new Button
        {
            Text = "THANH TOÁN",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            BackColor = Color.FromArgb(60, 170, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCheckout.FlatAppearance.BorderSize = 0;
        btnCheckout.Click += BtnCheckout_Click;
        panel.Controls.Add(btnCheckout, 0, 8);

        return panel;
    }

    private async Task LoadSnacksAsync()
    {
        using var context = Program.CreateDbContext();
        _snacks = await new SnackService(context).GetAllActiveAsync();
        RenderSnackCards();
    }

    private void RenderSnackCards()
    {
        flpMenu.SuspendLayout();
        flpMenu.Controls.Clear();

        var category = cboCategory.SelectedItem?.ToString();
        var items = _snacks.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(category) && category != "Tất cả")
            items = items.Where(s => s.Category == category);

        foreach (var snack in items)
            flpMenu.Controls.Add(CreateSnackCard(snack));

        flpMenu.ResumeLayout();
    }

    private Control CreateSnackCard(Snack snack)
    {
        var card = new Panel
        {
            Width = 178,
            Height = 210,
            Margin = new Padding(0, 0, 12, 12),
            BackColor = Color.FromArgb(30, 30, 48),
            Cursor = Cursors.Hand,
            Tag = snack,
            Padding = new Padding(10)
        };
        card.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var path = CreateRoundRectPath(new Rectangle(0, 0, card.Width - 1, card.Height - 1), 10);
            using var pen = new Pen(Color.FromArgb(55, 55, 78));
            e.Graphics.DrawPath(pen, path);
        };
        card.Resize += (s, e) =>
        {
            using var path = CreateRoundRectPath(new Rectangle(0, 0, card.Width, card.Height), 10);
            card.Region = new Region(path);
        };
        card.Click += SnackCard_Click;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
            Tag = snack,
            Cursor = Cursors.Hand
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
        layout.Click += SnackCard_Click;

        var imageHost = CreateSnackImageHost(snack);

        var name = new Label
        {
            Text = snack.Name,
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(235, 235, 245),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            AutoEllipsis = true,
            Margin = Padding.Empty,
            Tag = snack,
            Cursor = Cursors.Hand
        };
        name.Click += SnackCard_Click;

        var price = new Label
        {
            Text = $"{snack.Price:N0} đ",
            Dock = DockStyle.Fill,
            ForeColor = Color.FromArgb(80, 220, 120),
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft,
            Margin = Padding.Empty,
            Tag = snack,
            Cursor = Cursors.Hand
        };
        price.Click += SnackCard_Click;

        layout.Controls.Add(imageHost, 0, 0);
        layout.Controls.Add(name, 0, 1);
        layout.Controls.Add(price, 0, 2);
        card.Controls.Add(layout);
        return card;
    }

    private Control CreateSnackImageHost(Snack snack)
    {
        var imageHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(24, 24, 40),
            Margin = new Padding(0, 0, 0, 8),
            Tag = snack,
            Cursor = Cursors.Hand
        };
        imageHost.Click += SnackCard_Click;

        var image = LoadSnackImage(snack);
        if (image != null)
        {
            var pic = new PictureBox
            {
                Image = image,
                SizeMode = PictureBoxSizeMode.Zoom,
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(24, 24, 40),
                Tag = snack,
                Cursor = Cursors.Hand
            };
            pic.Click += SnackCard_Click;
            imageHost.Controls.Add(pic);
            return imageHost;
        }

        var color = snack.Category switch
        {
            "Food" => Color.FromArgb(235, 145, 60),
            "Drink" => Color.FromArgb(70, 150, 230),
            "Combo" => Color.FromArgb(125, 95, 235),
            _ => Color.FromArgb(100, 100, 130)
        };

        var placeholder = new Label
        {
            Text = snack.Category switch
            {
                "Food" => "FOOD",
                "Drink" => "DRINK",
                "Combo" => "COMBO",
                _ => "ITEM"
            },
            Dock = DockStyle.Fill,
            BackColor = color,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter,
            Tag = snack,
            Cursor = Cursors.Hand
        };
        placeholder.Click += SnackCard_Click;
        imageHost.Controls.Add(placeholder);
        return imageHost;
    }

    private static Image? LoadSnackImage(Snack snack)
    {
        if (string.IsNullOrWhiteSpace(snack.ImagePath)) return null;

        var candidates = new[]
        {
            Path.Combine(Application.StartupPath, "Resources", "Snacks", snack.ImagePath),
            Path.Combine(Application.StartupPath, "Resources", snack.ImagePath)
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;

            try
            {
                using var source = Image.FromFile(path);
                return new Bitmap(source);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }

    private static GraphicsPath CreateRoundRectPath(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void SnackCard_Click(object? sender, EventArgs e)
    {
        if ((sender as Control)?.Tag is not Snack snack) return;

        if (_cart.TryGetValue(snack.Id, out var item))
            item.Quantity++;
        else
            _cart[snack.Id] = new SnackCartItem { Snack = snack, Quantity = 1 };

        RefreshCart();
    }

    private void RefreshCart()
    {
        dgvCart.Columns.Clear();
        dgvCart.DataSource = _cart.Values
            .OrderBy(i => i.Snack.Category)
            .ThenBy(i => i.Snack.Name)
            .Select(i => new
            {
                i.Snack.Id,
                Món = i.Snack.Name,
                SL = i.Quantity,
                Giá = $"{i.Snack.Price:N0}",
                ThànhTiền = $"{i.LineTotal:N0}"
            })
            .ToList();

        if (dgvCart.Columns.Contains("Id"))
            dgvCart.Columns["Id"]!.Visible = false;

        var minus = new DataGridViewButtonColumn
        {
            Name = "Minus",
            HeaderText = "",
            Text = "-",
            UseColumnTextForButtonValue = true,
            Width = 34,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        var plus = new DataGridViewButtonColumn
        {
            Name = "Plus",
            HeaderText = "",
            Text = "+",
            UseColumnTextForButtonValue = true,
            Width = 34,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        };
        dgvCart.Columns.Add(minus);
        dgvCart.Columns.Add(plus);

        UpdateTotals();
    }

    private void DgvCart_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        if (dgvCart.Rows[e.RowIndex].Cells["Id"].Value is not int id) return;

        if (!_cart.TryGetValue(id, out var item)) return;

        if (dgvCart.Columns[e.ColumnIndex].Name == "Plus")
        {
            item.Quantity++;
        }
        else if (dgvCart.Columns[e.ColumnIndex].Name == "Minus")
        {
            item.Quantity--;
            if (item.Quantity <= 0) _cart.Remove(id);
        }

        RefreshCart();
    }

    private void UpdateTotals()
    {
        var snackTotal = _cart.Values.Sum(i => i.LineTotal);
        var grandTotal = _state.TicketTotal + snackTotal;

        lblSnackTotal.Text = $"Bắp nước: {snackTotal:N0} đ";
        lblGrandTotal.Text = $"Tổng bill: {grandTotal:N0} đ";
        CalculateChange();
    }

    private void CalculateChange()
    {
        if (_state == null) return;

        var total = _state.TicketTotal + _cart.Values.Sum(i => i.LineTotal);
        if (decimal.TryParse(txtReceived.Text.Replace(",", "").Replace(".", ""), out var received) && received >= total)
        {
            lblChange.Text = $"Tiền thối: {received - total:N0} đ";
            lblChange.ForeColor = Color.FromArgb(255, 200, 60);
        }
        else if (txtReceived.Text.Length > 0)
        {
            lblChange.Text = "Chưa đủ tiền";
            lblChange.ForeColor = Color.FromArgb(255, 90, 90);
        }
        else
        {
            lblChange.Text = "Tiền thối: 0 đ";
            lblChange.ForeColor = Color.FromArgb(255, 200, 60);
        }
    }

    private async void BtnCheckout_Click(object? sender, EventArgs e)
    {
        var total = _state.TicketTotal + _cart.Values.Sum(i => i.LineTotal);
        if (!decimal.TryParse(txtReceived.Text.Replace(",", "").Replace(".", ""), out var received))
        {
            MessageBox.Show("Vui lòng nhập số tiền nhận!", "Thiếu thông tin");
            txtReceived.Focus();
            return;
        }

        if (received < total)
        {
            MessageBox.Show($"Tiền nhận ({received:N0}đ) ít hơn tổng ({total:N0}đ)!", "Chưa đủ tiền");
            txtReceived.Focus();
            return;
        }

        var change = received - total;
        var seatList = string.Join(", ", _state.Seats.OrderBy(s => s.RowLabel).ThenBy(s => s.SeatNumber).Select(s => $"{s.RowLabel}{s.SeatNumber}"));
        var snackLines = _cart.Count == 0
            ? "Không mua bắp nước"
            : string.Join("\n", _cart.Values.Select(i => $"- {i.Snack.Name} x{i.Quantity}: {i.LineTotal:N0} đ"));

        var confirmMsg =
            $"Xác nhận thanh toán:\n\n" +
            $"Phim: {_state.Movie.Title}\n" +
            $"Suất: {_state.Showtime.StartTime:HH:mm dd/MM/yyyy}\n" +
            $"Phòng: {_state.Room.Name}\n" +
            $"Ghế: {seatList}\n\n" +
            $"Bắp nước:\n{snackLines}\n\n" +
            $"Tổng bill: {total:N0} đ\n" +
            $"Tiền nhận: {received:N0} đ\n" +
            $"Tiền thối: {change:N0} đ";

        if (MessageBox.Show(confirmMsg, "Xác nhận thanh toán",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
            return;

        var invoice = new Invoice
        {
            UserId = SessionManager.CurrentUser!.Id,
            CustomerName = string.IsNullOrWhiteSpace(_state.CustomerName) ? null : _state.CustomerName,
            CustomerPhone = string.IsNullOrWhiteSpace(_state.CustomerPhone) ? null : _state.CustomerPhone,
            TotalAmount = total,
            ReceivedAmount = received,
            ChangeAmount = change,
            CreatedAt = DateTime.Now
        };

        var tickets = _state.Seats.Select(s => new Ticket
        {
            ShowtimeId = _state.Showtime.Id,
            SeatId = s.Id,
            Price = _state.Showtime.BasePrice * s.PriceMultiplier
        }).ToList();

        var invoiceSnacks = _cart.Values.Select(i => new InvoiceSnack
        {
            SnackId = i.Snack.Id,
            Quantity = i.Quantity,
            UnitPrice = i.Snack.Price
        }).ToList();

        btnCheckout.Enabled = false;
        btnCheckout.Text = "ĐANG XỬ LÝ...";

        using var context = Program.CreateDbContext();
        var (ok, msg, invoiceId) = await new InvoiceService(context).CreateAsync(invoice, tickets, invoiceSnacks);

        if (ok)
        {
            MessageBox.Show(
                $"{msg}\n\n" +
                $"Ghế: {seatList}\n" +
                $"Bắp nước: {_cart.Values.Sum(i => i.LineTotal):N0} đ\n" +
                $"Tổng: {total:N0} đ\n" +
                $"Tiền thối: {change:N0} đ",
                "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            CheckoutCompleted?.Invoke();
        }
        else
        {
            MessageBox.Show(msg, "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnCheckout.Enabled = true;
            btnCheckout.Text = "THANH TOÁN";
        }
    }

    private static Label MakeTotalLabel(string text, Color color, int size = 11)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", size, FontStyle.Bold),
            ForeColor = color,
            TextAlign = ContentAlignment.MiddleLeft
        };
    }
}
