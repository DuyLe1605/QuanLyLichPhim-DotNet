using BaiTapLon.Helpers;
using BaiTapLon.Models;
using BaiTapLon.Services;

namespace BaiTapLon.Forms.Customer;

public class UcMyTickets : UserControl
{
    private readonly ComboBox cboFilter = new();
    private readonly FlowLayoutPanel flpTickets = new();
    private List<Booking> bookings = new();

    public UcMyTickets()
    {
        InitializeComponent();
        Load += async (s, e) => await LoadTicketsAsync();
    }

    private void InitializeComponent()
    {
        BackColor = CustomerUi.AppBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 2,
            ColumnCount = 1,
            Padding = new Padding(28),
            BackColor = Color.Transparent
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        Controls.Add(root);

        var header = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = Color.Transparent
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        header.Controls.Add(new Label
        {
            Text = "Lịch sử vé",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        cboFilter.Items.AddRange(["Sắp tới", "Đã xem", "Đã hủy", "Tất cả"]);
        cboFilter.SelectedIndex = 0;
        cboFilter.DropDownStyle = ComboBoxStyle.DropDownList;
        cboFilter.Dock = DockStyle.Fill;
        cboFilter.Margin = new Padding(0, 16, 0, 14);
        cboFilter.SelectedIndexChanged += (s, e) => RenderTickets();
        header.Controls.Add(cboFilter, 1, 0);
        root.Controls.Add(header, 0, 0);

        flpTickets.Dock = DockStyle.Fill;
        flpTickets.AutoScroll = true;
        flpTickets.WrapContents = true;
        flpTickets.BackColor = Color.Transparent;
        root.Controls.Add(flpTickets, 0, 1);
    }

    private async Task LoadTicketsAsync()
    {
        var customer = SessionManager.CurrentCustomer;
        if (customer == null) return;

        using var context = Program.CreateDbContext();
        bookings = await new BookingService(context).GetCustomerBookingsAsync(customer.Id);
        RenderTickets();
    }

    private void RenderTickets()
    {
        flpTickets.Controls.Clear();
        var now = DateTime.Now;
        var filter = cboFilter.SelectedItem?.ToString();
        var items = bookings.AsEnumerable();

        items = filter switch
        {
            "Sắp tới" => items.Where(b => b.Status != "Cancelled" && b.Showtime.StartTime >= now),
            "Đã xem" => items.Where(b => b.Status == "CheckedIn" || b.Showtime.EndTime < now),
            "Đã hủy" => items.Where(b => b.Status == "Cancelled"),
            _ => items
        };

        var list = items.ToList();
        if (list.Count == 0)
        {
            flpTickets.Controls.Add(new Label
            {
                Text = "Chưa có vé trong nhóm này.",
                ForeColor = CustomerUi.Muted,
                Font = new Font("Segoe UI", 12),
                AutoSize = true,
                Padding = new Padding(0, 24, 0, 0)
            });
            return;
        }

        foreach (var booking in list)
            flpTickets.Controls.Add(CreateTicketCard(booking));
    }

    private Control CreateTicketCard(Booking booking)
    {
        var card = new Panel
        {
            Width = 420,
            Height = 170,
            BackColor = CustomerUi.CardBg,
            Margin = new Padding(0, 0, 16, 16),
            Cursor = Cursors.Hand,
            Tag = booking
        };

        var poster = new PictureBox
        {
            Location = new Point(12, 12),
            Size = new Size(96, 136),
            SizeMode = PictureBoxSizeMode.Zoom,
            BackColor = Color.FromArgb(38, 42, 58),
            Image = CustomerUi.LoadPosterImage(booking.Showtime.Movie)
        };
        card.Controls.Add(poster);

        var seats = string.Join(", ", booking.Tickets
            .OrderBy(t => t.Seat.RowLabel)
            .ThenBy(t => t.Seat.SeatNumber)
            .Select(t => $"{t.Seat.RowLabel}{t.Seat.SeatNumber}"));

        var status = booking.Status == "Cancelled"
            ? "Đã hủy"
            : booking.Showtime.EndTime < DateTime.Now ? "Đã xem" : "Chưa xem";

        card.Controls.Add(new Label
        {
            Text = booking.Showtime.Movie.Title,
            Location = new Point(124, 16),
            Size = new Size(270, 34),
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            AutoEllipsis = true
        });
        card.Controls.Add(new Label
        {
            Text =
                $"{booking.Showtime.StartTime:HH:mm dd/MM/yyyy}\n" +
                $"{booking.Showtime.Room.Name} | Ghế: {seats}\n" +
                $"Mã: {booking.BookingCode}\n" +
                $"{status} | {booking.TotalAmount:N0} đ",
            Location = new Point(124, 54),
            Size = new Size(270, 88),
            ForeColor = CustomerUi.Muted,
            Font = new Font("Segoe UI", 9.5f)
        });

        void Click(object? s, EventArgs e) => ShowTicketDetail(booking);
        card.Click += Click;
        foreach (Control c in card.Controls) c.Click += Click;
        return card;
    }

    private void ShowTicketDetail(Booking booking)
    {
        using var dialog = new Form
        {
            Text = "Chi tiết vé",
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(420, 560),
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = CustomerUi.PanelBg
        };

        var qr = BarcodeHelper.GenerateQrCode(booking.BookingCode, 190, 190);
        var seats = string.Join(", ", booking.Tickets
            .OrderBy(t => t.Seat.RowLabel)
            .ThenBy(t => t.Seat.SeatNumber)
            .Select(t => $"{t.Seat.RowLabel}{t.Seat.SeatNumber}"));

        dialog.Controls.Add(new Label
        {
            Text = booking.BookingCode,
            Dock = DockStyle.Top,
            Height = 58,
            ForeColor = CustomerUi.Accent,
            Font = new Font("Segoe UI", 20, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        });
        dialog.Controls.Add(new PictureBox
        {
            Image = qr,
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = Color.White,
            Location = new Point(112, 70),
            Size = new Size(196, 196)
        });
        dialog.Controls.Add(new Label
        {
            Text =
                $"{booking.Showtime.Movie.Title}\n\n" +
                $"Suất: {booking.Showtime.StartTime:HH:mm dd/MM/yyyy}\n" +
                $"Phòng: {booking.Showtime.Room.Name}\n" +
                $"Ghế: {seats}\n" +
                $"Trạng thái: {booking.Status}\n" +
                $"Tổng tiền: {booking.TotalAmount:N0} đ",
            Location = new Point(34, 286),
            Size = new Size(350, 170),
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 11),
            TextAlign = ContentAlignment.TopLeft
        });

        var btnReceipt = CustomerUi.PrimaryButton("📄 Hóa đơn");
        btnReceipt.BackColor = Color.FromArgb(70, 145, 230);
        btnReceipt.Location = new Point(40, 488);
        btnReceipt.Size = new Size(160, 42);
        btnReceipt.Click += async (s, e) =>
        {
            using var ctx = Program.CreateDbContext();
            var receiptData = await new InvoiceQueryService(ctx)
                .GetReceiptDataByBookingCodeAsync(booking.BookingCode);
            if (receiptData != null)
                new DlgReceiptPreview(receiptData).ShowDialog(dialog);
        };
        dialog.Controls.Add(btnReceipt);

        var close = CustomerUi.PrimaryButton("Đóng");
        close.Location = new Point(220, 488);
        close.Size = new Size(160, 42);
        close.Click += (s, e) => dialog.Close();
        dialog.Controls.Add(close);
        dialog.ShowDialog(this);
    }
}
