using BaiTapLon.Helpers;
using BaiTapLon.Services;
using System.ComponentModel;

namespace BaiTapLon.Forms.Customer;

public class UcPaymentGateway : UserControl
{
    private readonly CustomerBookingState state;
    private readonly Label lblTimer = new();
    private readonly Label lblStatus = new();
    private readonly TextBox txtVoucher = new();
    private readonly Button btnPay = new();
    private readonly System.Windows.Forms.Timer timer = new();
    private int secondsLeft = 10;
    private bool paymentStarted;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action? BackRequested { get; set; }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Action<string>? PaymentCompleted { get; set; }

    public UcPaymentGateway(CustomerBookingState state)
    {
        this.state = state;
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        BackColor = CustomerUi.AppBg;

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(34),
            BackColor = Color.Transparent
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 54));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 46));
        Controls.Add(root);

        root.Controls.Add(CreateSummary(), 0, 0);
        root.Controls.Add(CreateQrPanel(), 1, 0);

        timer.Interval = 1000;
        timer.Tick += async (s, e) => await TickPaymentAsync();
    }

    private Control CreateSummary()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 8,
            ColumnCount = 1,
            BackColor = CustomerUi.PanelBg,
            Padding = new Padding(24)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        panel.Controls.Add(new Label
        {
            Text = "Tóm tắt đơn đặt vé",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 19, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var seats = string.Join(", ", state.Seats.OrderBy(s => s.RowLabel).ThenBy(s => s.SeatNumber).Select(CustomerUi.SeatLabel));
        panel.Controls.Add(SummaryLabel(
            $"{state.Movie.Title}\n{state.Room.Name} | {state.Showtime.StartTime:HH:mm dd/MM/yyyy}\nGhế: {seats}"), 0, 1);

        panel.Controls.Add(SummaryLabel(
            $"Tiền vé: {state.TicketTotal:N0} đ\n" +
            $"Bắp nước: {state.SnackTotal:N0} đ\n" +
            $"Giảm giá: {state.DiscountAmount:N0} đ"), 0, 2);

        var snackText = state.SnackLines.Count == 0
            ? "Không mua bắp nước."
            : string.Join(Environment.NewLine, state.SnackLines);
        panel.Controls.Add(SummaryLabel(snackText), 0, 3);

        var voucherPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = Color.Transparent
        };
        voucherPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        voucherPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));
        txtVoucher.Dock = DockStyle.Fill;
        txtVoucher.BackColor = Color.FromArgb(35, 40, 56);
        txtVoucher.ForeColor = CustomerUi.Text;
        txtVoucher.BorderStyle = BorderStyle.FixedSingle;
        txtVoucher.PlaceholderText = "Mã giảm giá";
        voucherPanel.Controls.Add(txtVoucher, 0, 0);
        var apply = CustomerUi.PrimaryButton("Áp dụng");
        apply.Dock = DockStyle.Fill;
        apply.BackColor = CustomerUi.AccentBlue;
        apply.Click += (s, e) => MessageBox.Show("Voucher sẽ được kích hoạt ở Phase 10.", "Thông tin", MessageBoxButtons.OK, MessageBoxIcon.Information);
        voucherPanel.Controls.Add(apply, 1, 0);
        panel.Controls.Add(voucherPanel, 0, 4);

        var total = new Label
        {
            Text = $"Tổng thanh toán: {state.GrandTotal:N0} đ",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Accent,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        };
        panel.Controls.Add(total, 0, 5);

        btnPay.Text = "BẮT ĐẦU THANH TOÁN QR";
        btnPay.Dock = DockStyle.Fill;
        btnPay.BackColor = CustomerUi.Accent;
        btnPay.ForeColor = Color.White;
        btnPay.FlatStyle = FlatStyle.Flat;
        btnPay.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        btnPay.Cursor = Cursors.Hand;
        btnPay.FlatAppearance.BorderSize = 0;
        btnPay.Click += (s, e) => StartPayment();
        panel.Controls.Add(btnPay, 0, 6);

        var back = CustomerUi.PrimaryButton("← Quay lại chọn ghế");
        back.Dock = DockStyle.Fill;
        back.BackColor = Color.FromArgb(52, 58, 76);
        back.Click += (s, e) => BackRequested?.Invoke();
        panel.Controls.Add(back, 0, 7);

        return panel;
    }

    private Control CreateQrPanel()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            BackColor = Color.Transparent,
            Padding = new Padding(36, 10, 10, 10)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 300));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        panel.Controls.Add(new Label
        {
            Text = "QR thanh toán giả lập",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        }, 0, 0);

        var qr = new PictureBox
        {
            Dock = DockStyle.Fill,
            SizeMode = PictureBoxSizeMode.CenterImage,
            BackColor = Color.White,
            Image = BarcodeHelper.GenerateQrCode(
                $"CINEMANAGER|{state.Movie.Title}|{state.Showtime.Id}|{state.GrandTotal:N0}",
                240,
                240)
        };
        panel.Controls.Add(qr, 0, 1);

        lblTimer.Text = "Sẵn sàng thanh toán";
        lblTimer.Dock = DockStyle.Fill;
        lblTimer.ForeColor = CustomerUi.Accent;
        lblTimer.Font = new Font("Segoe UI", 15, FontStyle.Bold);
        lblTimer.TextAlign = ContentAlignment.MiddleCenter;
        panel.Controls.Add(lblTimer, 0, 2);

        lblStatus.Text = "Nhấn bắt đầu để hệ thống mô phỏng giao dịch.";
        lblStatus.Dock = DockStyle.Fill;
        lblStatus.ForeColor = CustomerUi.Muted;
        lblStatus.Font = new Font("Segoe UI", 10);
        lblStatus.TextAlign = ContentAlignment.MiddleCenter;
        panel.Controls.Add(lblStatus, 0, 3);

        return panel;
    }

    private static Label SummaryLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 10.5f),
            TextAlign = ContentAlignment.MiddleLeft
        };
    }

    private void StartPayment()
    {
        if (paymentStarted) return;
        paymentStarted = true;
        secondsLeft = 10;
        btnPay.Enabled = false;
        btnPay.Text = "ĐANG CHỜ THANH TOÁN...";
        lblStatus.Text = "Đang kiểm tra giao dịch QR...";
        lblTimer.Text = $"Còn {secondsLeft}s";
        timer.Start();
    }

    private async Task TickPaymentAsync()
    {
        secondsLeft--;
        lblTimer.Text = $"Còn {secondsLeft}s";

        if (secondsLeft > 0) return;

        timer.Stop();
        lblStatus.Text = "Đang tạo vé...";
        await CompletePaymentAsync();
    }

    private async Task CompletePaymentAsync()
    {
        try
        {
            var customer = SessionManager.CurrentCustomer;
            if (customer == null)
            {
                MessageBox.Show("Phiên đăng nhập khách hàng đã hết hạn.", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using var context = Program.CreateDbContext();
            var service = new BookingService(context);
            var result = await service.CreatePaidBookingAsync(
                customer.Id,
                state.Showtime.Id,
                state.Seats.Select(s => s.Id).ToList(),
                state.Snacks,
                state.DiscountAmount,
                state.VoucherId);

            if (!result.Success)
            {
                lblStatus.Text = result.Message;
                lblTimer.Text = "Thanh toán lỗi";
                btnPay.Enabled = true;
                btnPay.Text = "THỬ LẠI";
                paymentStarted = false;
                return;
            }

            lblTimer.Text = "Thành công";
            lblStatus.Text = $"Mã đặt vé: {result.BookingCode}";
            MessageBox.Show(
                $"Đặt vé thành công!\n\nMã đặt vé: {result.BookingCode}\nTổng tiền: {state.GrandTotal:N0} đ",
                "Thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            PaymentCompleted?.Invoke(result.BookingCode);
        }
        catch (Exception ex)
        {
            lblTimer.Text = "Thanh toán lỗi";
            lblStatus.Text = ex.Message;
            btnPay.Enabled = true;
            btnPay.Text = "THỬ LẠI";
            paymentStarted = false;
        }
    }
}
