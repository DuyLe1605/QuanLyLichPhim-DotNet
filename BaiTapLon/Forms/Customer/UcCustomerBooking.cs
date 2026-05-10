using BaiTapLon.Forms.Controls;
using BaiTapLon.Models;
using BaiTapLon.Services;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace BaiTapLon.Forms.Customer;

public class UcCustomerBooking : UserControl
{
    private readonly Showtime inputShowtime;
    private readonly SeatMapControl seatMap = new();
    private readonly FlowLayoutPanel flpSnacks = new();
    private readonly DataGridView dgvCart = new();
    private readonly Label lblMovie = new();
    private readonly Label lblSeats = new();
    private readonly Label lblTicketTotal = new();
    private readonly Label lblSnackTotal = new();
    private readonly Label lblGrandTotal = new();
    private readonly Dictionary<int, SnackCartItem> cart = new();
    private Showtime showtime = null!;
    private Movie movie = null!;
    private Room room = null!;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action? BackRequested { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<CustomerBookingState>? PaymentRequested { get; set; }

    public UcCustomerBooking(Showtime showtime)
    {
        inputShowtime = showtime;
        InitializeComponent();
    }

    public async Task LoadAsync()
    {
        using var context = Program.CreateDbContext();

        showtime = (await context.Showtimes
            .Include(s => s.Movie).ThenInclude(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .Include(s => s.Room).ThenInclude(r => r.Seats.OrderBy(s => s.RowLabel).ThenBy(s => s.SeatNumber))
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == inputShowtime.Id))!;

        movie = showtime.Movie;
        room = showtime.Room;

        var soldIds = await new TicketService(context).GetSoldSeatIdsAsync(showtime.Id);
        seatMap.SetData(room.Seats.ToList(), soldIds, room.Rows, room.Columns, showtime.BasePrice);

        lblMovie.Text = $"{movie.Title}\n{room.Name} | {showtime.StartTime:HH:mm dd/MM/yyyy}";
        await LoadSnacksAsync();
        UpdateTotals();
    }

    private void InitializeComponent()
    {
        BackColor = CustomerUi.AppBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        Controls.Add(root);

        var left = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(22),
            BackColor = Color.Transparent
        };
        left.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
        left.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(left, 0, 0);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = Color.Transparent
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var back = CustomerUi.PrimaryButton("← Quay lại");
        back.BackColor = Color.FromArgb(52, 58, 76);
        back.Dock = DockStyle.Fill;
        back.Margin = new Padding(0, 20, 14, 20);
        back.Click += (s, e) => BackRequested?.Invoke();
        header.Controls.Add(back, 0, 0);
        lblMovie.Dock = DockStyle.Fill;
        lblMovie.ForeColor = CustomerUi.Text;
        lblMovie.Font = new Font("Segoe UI", 16, FontStyle.Bold);
        lblMovie.TextAlign = ContentAlignment.MiddleLeft;
        header.Controls.Add(lblMovie, 1, 0);
        left.Controls.Add(header, 0, 0);

        var seatHost = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(18, 20, 30),
            Padding = new Padding(10)
        };
        seatMap.Location = new Point(10, 10);
        seatMap.SeatSelectionChanged += (s, e) => UpdateTotals();
        seatHost.Controls.Add(seatMap);
        seatHost.Resize += (s, e) =>
        {
            seatMap.Location = new Point(Math.Max(10, (seatHost.ClientSize.Width - seatMap.Width) / 2), 10);
        };
        left.Controls.Add(seatHost, 0, 1);

        root.Controls.Add(CreateRightPanel(), 1, 0);
    }

    private Control CreateRightPanel()
    {
        var right = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 9,
            ColumnCount = 1,
            BackColor = CustomerUi.PanelBg,
            Padding = new Padding(16)
        };
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        right.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        right.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));

        right.Controls.Add(new Label
        {
            Text = "Ghế đã chọn",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        lblSeats.Dock = DockStyle.Fill;
        lblSeats.ForeColor = CustomerUi.Muted;
        lblSeats.Font = new Font("Segoe UI", 10);
        right.Controls.Add(lblSeats, 0, 1);

        right.Controls.Add(new Label
        {
            Text = "Bắp nước",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 15, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 2);

        flpSnacks.Dock = DockStyle.Fill;
        flpSnacks.AutoScroll = true;
        flpSnacks.WrapContents = true;
        flpSnacks.BackColor = Color.Transparent;
        right.Controls.Add(flpSnacks, 0, 3);

        right.Controls.Add(new Label
        {
            Text = "Giỏ hàng",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 4);

        dgvCart.Dock = DockStyle.Fill;
        dgvCart.AllowUserToAddRows = false;
        dgvCart.AllowUserToDeleteRows = false;
        dgvCart.ReadOnly = true;
        dgvCart.RowHeadersVisible = false;
        dgvCart.BackgroundColor = Color.FromArgb(28, 32, 46);
        dgvCart.BorderStyle = BorderStyle.None;
        dgvCart.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dgvCart.EnableHeadersVisualStyles = false;
        dgvCart.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(35, 40, 56);
        dgvCart.ColumnHeadersDefaultCellStyle.ForeColor = CustomerUi.Text;
        dgvCart.DefaultCellStyle.BackColor = Color.FromArgb(28, 32, 46);
        dgvCart.DefaultCellStyle.ForeColor = CustomerUi.Text;
        dgvCart.DefaultCellStyle.SelectionBackColor = Color.FromArgb(50, 65, 70);
        dgvCart.CellContentClick += DgvCart_CellContentClick;
        right.Controls.Add(dgvCart, 0, 5);

        var totals = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            BackColor = Color.Transparent
        };
        totals.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        totals.RowStyles.Add(new RowStyle(SizeType.Percent, 33));
        totals.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        lblTicketTotal.Dock = DockStyle.Fill;
        lblSnackTotal.Dock = DockStyle.Fill;
        lblGrandTotal.Dock = DockStyle.Fill;
        foreach (var label in new[] { lblTicketTotal, lblSnackTotal, lblGrandTotal })
        {
            label.ForeColor = CustomerUi.Text;
            label.Font = new Font("Segoe UI", label == lblGrandTotal ? 14 : 10.5f, FontStyle.Bold);
            label.TextAlign = ContentAlignment.MiddleLeft;
        }
        lblGrandTotal.ForeColor = CustomerUi.Accent;
        totals.Controls.Add(lblTicketTotal, 0, 0);
        totals.Controls.Add(lblSnackTotal, 0, 1);
        totals.Controls.Add(lblGrandTotal, 0, 2);
        right.Controls.Add(totals, 0, 6);

        var note = new Label
        {
            Text = "Thanh toán QR giả lập sẽ hoàn tất tự động sau 10 giây.",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Muted,
            Font = new Font("Segoe UI", 9),
            TextAlign = ContentAlignment.MiddleLeft
        };
        right.Controls.Add(note, 0, 7);

        var pay = CustomerUi.PrimaryButton("TIẾP TỤC THANH TOÁN");
        pay.Dock = DockStyle.Fill;
        pay.Click += (s, e) => ContinueToPayment();
        right.Controls.Add(pay, 0, 8);

        return right;
    }

    private async Task LoadSnacksAsync()
    {
        using var context = Program.CreateDbContext();
        var snacks = await new SnackService(context).GetAllActiveAsync();
        flpSnacks.Controls.Clear();
        foreach (var snack in snacks)
            flpSnacks.Controls.Add(CreateSnackButton(snack));
    }

    private Control CreateSnackButton(Snack snack)
    {
        var button = new Button
        {
            Text = $"{snack.Name}\n{snack.Price:N0} đ",
            Width = 145,
            Height = 58,
            Margin = new Padding(0, 0, 8, 8),
            BackColor = CustomerUi.CardBg,
            ForeColor = CustomerUi.Text,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8.5f),
            Cursor = Cursors.Hand,
            Tag = snack
        };
        button.FlatAppearance.BorderColor = CustomerUi.Border;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(45, 56, 64);
        button.Click += (s, e) =>
        {
            if (cart.TryGetValue(snack.Id, out var item))
                item.Quantity++;
            else
                cart[snack.Id] = new SnackCartItem { Snack = snack, Quantity = 1 };
            RefreshCart();
        };
        return button;
    }

    private void RefreshCart()
    {
        dgvCart.Columns.Clear();
        dgvCart.DataSource = cart.Values
            .OrderBy(i => i.Snack.Name)
            .Select(i => new
            {
                i.Snack.Id,
                Món = i.Snack.Name,
                SL = i.Quantity,
                Tiền = $"{i.LineTotal:N0}"
            })
            .ToList();

        if (dgvCart.Columns.Contains("Id"))
            dgvCart.Columns["Id"]!.Visible = false;

        dgvCart.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "Minus",
            HeaderText = "",
            Text = "-",
            UseColumnTextForButtonValue = true,
            Width = 34,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        dgvCart.Columns.Add(new DataGridViewButtonColumn
        {
            Name = "Plus",
            HeaderText = "",
            Text = "+",
            UseColumnTextForButtonValue = true,
            Width = 34,
            AutoSizeMode = DataGridViewAutoSizeColumnMode.None
        });
        UpdateTotals();
    }

    private void DgvCart_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || dgvCart.Rows[e.RowIndex].Cells["Id"].Value is not int id) return;
        if (!cart.TryGetValue(id, out var item)) return;

        var col = dgvCart.Columns[e.ColumnIndex].Name;
        if (col == "Plus") item.Quantity++;
        if (col == "Minus") item.Quantity--;
        if (item.Quantity <= 0) cart.Remove(id);
        RefreshCart();
    }

    private void UpdateTotals()
    {
        var seats = seatMap.SelectedSeats;
        lblSeats.Text = seats.Count == 0
            ? "Chưa chọn ghế."
            : string.Join(", ", seats.OrderBy(s => s.RowLabel).ThenBy(s => s.SeatNumber).Select(CustomerUi.SeatLabel));

        lblTicketTotal.Text = $"Tiền vé: {seatMap.TotalPrice:N0} đ";
        lblSnackTotal.Text = $"Bắp nước: {cart.Values.Sum(i => i.LineTotal):N0} đ";
        lblGrandTotal.Text = $"Tổng: {seatMap.TotalPrice + cart.Values.Sum(i => i.LineTotal):N0} đ";
    }

    private void ContinueToPayment()
    {
        var seats = seatMap.SelectedSeats;
        if (seats.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn ít nhất một ghế.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        PaymentRequested?.Invoke(new CustomerBookingState
        {
            Showtime = showtime,
            Movie = movie,
            Room = room,
            Seats = seats,
            TicketTotal = seatMap.TotalPrice,
            SnackTotal = cart.Values.Sum(i => i.LineTotal),
            Snacks = cart.Values
                .Where(i => i.Quantity > 0)
                .Select(i => new BookingSnackSelection(i.Snack.Id, i.Quantity))
                .ToList(),
            SnackLines = cart.Values
                .Where(i => i.Quantity > 0)
                .Select(i => $"{i.Snack.Name} x{i.Quantity}: {i.LineTotal:N0} đ")
                .ToList()
        });
    }

    private sealed class SnackCartItem
    {
        public Snack Snack { get; init; } = null!;
        public int Quantity { get; set; }
        public decimal LineTotal => Snack.Price * Quantity;
    }
}
