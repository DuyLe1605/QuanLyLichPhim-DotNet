using BaiTapLon.Helpers;
using BaiTapLon.Services;
using System.ComponentModel;

namespace BaiTapLon.Forms.Customer;

public class UcPaymentGateway : UserControl
{
    private readonly CustomerBookingState state;
    private readonly Label lblTimer = new();
    private readonly Label lblStatus = new();
    private readonly Button btnPay = new();
    private readonly System.Windows.Forms.Timer timer = new();
    private int secondsLeft = 10;
    private bool paymentStarted;

    // Coupon controls
    private readonly TextBox txtCoupon = new();
    private readonly Button btnApplyCoupon = new();
    private readonly Button btnRemoveCoupon = new();
    private readonly Label lblCouponStatus = new();

    // Points controls
    private readonly NumericUpDown nudPoints = new();
    private readonly Label lblPointsBalance = new();
    private readonly Label lblPointsDiscount = new();

    // Live total label
    private readonly Label lblTotal = new();

    // State
    private int _loyaltyBalance = 0;
    private decimal _couponDiscount = 0;
    private decimal _pointsDiscount = 0;

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
        _ = LoadLoyaltyBalanceAsync();
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

    private async Task LoadLoyaltyBalanceAsync()
    {
        var customer = SessionManager.CurrentCustomer;
        if (customer == null) return;
        using var ctx = Program.CreateDbContext();
        _loyaltyBalance = await new LoyaltyService(ctx, new TierService(ctx)).GetLoyaltyBalanceAsync(customer.Id);
        nudPoints.Maximum = _loyaltyBalance;
        lblPointsBalance.Text = $"(Bạn có {_loyaltyBalance:N0} điểm)";
    }

    private Control CreateSummary()
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 10,
            ColumnCount = 1,
            BackColor = CustomerUi.PanelBg,
            Padding = new Padding(24)
        };
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));   // row 0: title
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));   // row 1: movie/seat info
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));   // row 2: price breakdown
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // row 3: snack list
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));   // row 4: coupon label
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));   // row 5: coupon input row
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));   // row 6: coupon status
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));   // row 7: points label
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));   // row 8: points input row
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));   // row 9: total
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // row 10: pay button
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));   // row 11: back button

        // Adjust RowCount to match
        panel.RowCount = 12;

        // Row 0: Title
        panel.Controls.Add(new Label
        {
            Text = "Tóm tắt đơn đặt vé",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Text,
            Font = new Font("Segoe UI", 19, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        // Row 1: Movie / seat info
        var seats = string.Join(", ", state.Seats.OrderBy(s => s.RowLabel).ThenBy(s => s.SeatNumber).Select(CustomerUi.SeatLabel));
        panel.Controls.Add(SummaryLabel(
            $"{state.Movie.Title}\n{state.Room.Name} | {state.Showtime.StartTime:HH:mm dd/MM/yyyy}\nGhế: {seats}"), 0, 1);

        // Row 2: Price breakdown
        panel.Controls.Add(SummaryLabel(
            $"Tiền vé: {state.TicketTotal:N0} đ\n" +
            $"Bắp nước: {state.SnackTotal:N0} đ\n" +
            $"Giảm giá: {state.DiscountAmount:N0} đ"), 0, 2);

        // Row 3: Snack list
        var snackText = state.SnackLines.Count == 0
            ? "Không mua bắp nước."
            : string.Join(Environment.NewLine, state.SnackLines);
        panel.Controls.Add(SummaryLabel(snackText), 0, 3);

        // ── Section A: Coupon code ──────────────────────────────────────────

        // Row 4: Coupon section label
        panel.Controls.Add(new Label
        {
            Text = "🎟️ Mã thưởng",
            Dock = DockStyle.Fill,
            ForeColor = CustomerUi.Muted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft
        }, 0, 4);

        // Row 5: Coupon input row
        var couponRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        couponRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        couponRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        couponRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80));

        txtCoupon.Dock = DockStyle.Fill;
        txtCoupon.BackColor = Color.FromArgb(35, 40, 56);
        txtCoupon.ForeColor = CustomerUi.Text;
        txtCoupon.BorderStyle = BorderStyle.FixedSingle;
        txtCoupon.PlaceholderText = "Nhập mã coupon...";
        couponRow.Controls.Add(txtCoupon, 0, 0);

        btnApplyCoupon.Text = "Áp dụng";
        btnApplyCoupon.Dock = DockStyle.Fill;
        btnApplyCoupon.BackColor = CustomerUi.AccentBlue;
        btnApplyCoupon.ForeColor = Color.White;
        btnApplyCoupon.FlatStyle = FlatStyle.Flat;
        btnApplyCoupon.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        btnApplyCoupon.Cursor = Cursors.Hand;
        btnApplyCoupon.FlatAppearance.BorderSize = 0;
        btnApplyCoupon.Click += async (s, e) => await ApplyCouponAsync();
        couponRow.Controls.Add(btnApplyCoupon, 1, 0);

        btnRemoveCoupon.Text = "Xóa mã";
        btnRemoveCoupon.Dock = DockStyle.Fill;
        btnRemoveCoupon.BackColor = Color.FromArgb(200, 60, 60);
        btnRemoveCoupon.ForeColor = Color.White;
        btnRemoveCoupon.FlatStyle = FlatStyle.Flat;
        btnRemoveCoupon.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
        btnRemoveCoupon.Cursor = Cursors.Hand;
        btnRemoveCoupon.FlatAppearance.BorderSize = 0;
        btnRemoveCoupon.Visible = false;
        btnRemoveCoupon.Click += (s, e) => RemoveCoupon();
        couponRow.Controls.Add(btnRemoveCoupon, 2, 0);

        panel.Controls.Add(couponRow, 0, 5);

        // Row 6: Coupon status label
        lblCouponStatus.Text = "";
        lblCouponStatus.Dock = DockStyle.Fill;
        lblCouponStatus.ForeColor = CustomerUi.Accent;
        lblCouponStatus.Font = new Font("Segoe UI", 9f);
        lblCouponStatus.TextAlign = ContentAlignment.MiddleLeft;
        panel.Controls.Add(lblCouponStatus, 0, 6);

        // ── Section B: Points discount ──────────────────────────────────────

        // Row 7: Points section label row (label + balance)
        var pointsLabelRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pointsLabelRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        pointsLabelRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        pointsLabelRow.Controls.Add(new Label
        {
            Text = "💎 Dùng điểm thưởng",
            AutoSize = true,
            ForeColor = CustomerUi.Muted,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            TextAlign = ContentAlignment.BottomLeft,
            Dock = DockStyle.Fill
        }, 0, 0);

        lblPointsBalance.Text = "(Bạn có 0 điểm)";
        lblPointsBalance.AutoSize = false;
        lblPointsBalance.Dock = DockStyle.Fill;
        lblPointsBalance.ForeColor = CustomerUi.Muted;
        lblPointsBalance.Font = new Font("Segoe UI", 9f);
        lblPointsBalance.TextAlign = ContentAlignment.BottomLeft;
        pointsLabelRow.Controls.Add(lblPointsBalance, 1, 0);

        panel.Controls.Add(pointsLabelRow, 0, 7);

        // Row 8: Points input row (NumericUpDown + discount label)
        var pointsRow = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.Transparent
        };
        pointsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pointsRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        nudPoints.Dock = DockStyle.Fill;
        nudPoints.Minimum = 0;
        nudPoints.Maximum = 0; // updated after balance loads
        nudPoints.BackColor = Color.FromArgb(35, 40, 56);
        nudPoints.ForeColor = CustomerUi.Text;
        nudPoints.Font = new Font("Segoe UI", 10f);
        nudPoints.ValueChanged += (s, e) =>
        {
            int pts = (int)nudPoints.Value;
            state.PointsToRedeem = pts;
            decimal maxDiscount = pts * 1000m;
            _pointsDiscount = Math.Min(maxDiscount, state.GrandTotal - _couponDiscount);
            lblPointsDiscount.Text = pts > 0 ? $"-{_pointsDiscount:N0}đ" : "";
            UpdateTotal();
        };
        pointsRow.Controls.Add(nudPoints, 0, 0);

        lblPointsDiscount.Text = "";
        lblPointsDiscount.Dock = DockStyle.Fill;
        lblPointsDiscount.ForeColor = CustomerUi.Accent;
        lblPointsDiscount.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
        lblPointsDiscount.TextAlign = ContentAlignment.MiddleRight;
        pointsRow.Controls.Add(lblPointsDiscount, 1, 0);

        panel.Controls.Add(pointsRow, 0, 8);

        // Row 9: Total label
        lblTotal.Text = $"Tổng thanh toán: {state.GrandTotal:N0} đ";
        lblTotal.Dock = DockStyle.Fill;
        lblTotal.ForeColor = CustomerUi.Accent;
        lblTotal.Font = new Font("Segoe UI", 18, FontStyle.Bold);
        lblTotal.TextAlign = ContentAlignment.MiddleLeft;
        panel.Controls.Add(lblTotal, 0, 9);

        // Row 10: Pay button
        btnPay.Text = "BẮT ĐẦU THANH TOÁN QR";
        btnPay.Dock = DockStyle.Fill;
        btnPay.BackColor = CustomerUi.Accent;
        btnPay.ForeColor = Color.White;
        btnPay.FlatStyle = FlatStyle.Flat;
        btnPay.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        btnPay.Cursor = Cursors.Hand;
        btnPay.FlatAppearance.BorderSize = 0;
        btnPay.Click += (s, e) => StartPayment();
        panel.Controls.Add(btnPay, 0, 10);

        // Row 11: Back button
        var back = CustomerUi.PrimaryButton("← Quay lại chọn ghế");
        back.Dock = DockStyle.Fill;
        back.BackColor = Color.FromArgb(52, 58, 76);
        back.Click += (s, e) => BackRequested?.Invoke();
        panel.Controls.Add(back, 0, 11);

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

    // ── Coupon logic ────────────────────────────────────────────────────────

    private async Task ApplyCouponAsync()
    {
        var code = txtCoupon.Text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code)) return;

        var customer = SessionManager.CurrentCustomer;
        if (customer == null) return;

        using var ctx = Program.CreateDbContext();
        var result = await new CouponService(ctx).ValidateForCheckoutAsync(customer.Id, code);

        if (result.Success)
        {
            state.AppliedCouponCode = code;
            _couponDiscount = result.PointsAwarded * 1000m; // points → VND
            lblCouponStatus.Text = $"✅ Mã hợp lệ! Giảm {_couponDiscount:N0}đ ({result.PointsAwarded:N0} điểm)";
            lblCouponStatus.ForeColor = CustomerUi.Accent;
            btnRemoveCoupon.Visible = true;
            btnApplyCoupon.Visible = false;
            txtCoupon.Enabled = false;
        }
        else
        {
            lblCouponStatus.Text = $"❌ {result.Message}";
            lblCouponStatus.ForeColor = Color.FromArgb(255, 80, 80);
            _couponDiscount = 0;
            state.AppliedCouponCode = null;
        }
        UpdateTotal();
    }

    private void RemoveCoupon()
    {
        state.AppliedCouponCode = null;
        _couponDiscount = 0;
        txtCoupon.Text = "";
        txtCoupon.Enabled = true;
        lblCouponStatus.Text = "";
        btnRemoveCoupon.Visible = false;
        btnApplyCoupon.Visible = true;
        UpdateTotal();
    }

    private void UpdateTotal()
    {
        decimal total = state.GrandTotal - _couponDiscount - _pointsDiscount;
        total = Math.Max(0, total);
        if (lblTotal != null)
            lblTotal.Text = $"Tổng thanh toán: {total:N0} đ";
    }

    // ── Payment flow ─────────────────────────────────────────────────────────

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

            // Apply coupon if used
            if (!string.IsNullOrEmpty(state.AppliedCouponCode))
            {
                using var couponCtx = Program.CreateDbContext();
                await new CouponService(couponCtx).RedeemAsync(customer.Id, state.AppliedCouponCode);
            }

            // Redeem points if used
            if (state.PointsToRedeem > 0)
            {
                using var pointsCtx = Program.CreateDbContext();
                var loyaltySvc = new LoyaltyService(pointsCtx, new TierService(pointsCtx));
                await loyaltySvc.RedeemPointsForDiscountAsync(customer.Id, state.PointsToRedeem, state.GrandTotal);
            }

            // Accrue post-payment points
            {
                using var accrueCtx = Program.CreateDbContext();
                var loyaltySvc = new LoyaltyService(accrueCtx, new TierService(accrueCtx));
                await loyaltySvc.AccruePointsAsync(customer.Id, state.GrandTotal, null);
            }

            lblTimer.Text = "Thành công";
            lblStatus.Text = $"Mã đặt vé: {result.BookingCode}";
            MessageBox.Show(
                $"Đặt vé thành công!\n\nMã đặt vé: {result.BookingCode}\nTổng tiền: {state.GrandTotal:N0} đ",
                "Thành công",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            using var receiptCtx = Program.CreateDbContext();
            var receiptData = await new InvoiceQueryService(receiptCtx).GetReceiptDataByBookingCodeAsync(result.BookingCode);
            if (receiptData != null)
            {
                using var dlg = new DlgReceiptPreview(receiptData);
                dlg.ShowDialog(this);
            }

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
