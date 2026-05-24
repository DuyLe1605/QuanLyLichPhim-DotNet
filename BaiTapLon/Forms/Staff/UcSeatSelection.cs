using Microsoft.EntityFrameworkCore;
using BaiTapLon.Models;
using BaiTapLon.Services;
using BaiTapLon.Helpers;
using BaiTapLon.Forms.Controls;
using BaiTapLon.Forms.Admin;

namespace BaiTapLon.Forms.Staff;

/// <summary>
/// Màn hình chọn ghế + thanh toán.
/// Bên trái: SeatMapControl, Bên phải: thông tin + checkout.
/// </summary>
public class UcSeatSelection : UserControl
{
    private SeatMapControl seatMap = null!;
    private Panel pnlRight = null!;
    private Label lblMovieTitle = null!;
    private Label lblShowInfo = null!;
    private Label lblSelectedCount = null!;
    private Label lblSelectedSeats = null!;
    private Label lblTotalPrice = null!;
    private TextBox txtCustomerName = null!;
    private TextBox txtCustomerPhone = null!;
    private TextBox txtReceived = null!;
    private Label lblChange = null!;
    private Button btnCheckout = null!;
    private Button btnBack = null!;
    private Label lblMemberInfo = null!;
    private ComboBox cboCustomerSearch = null!;
    private List<Models.Customer> _customers = new();
    private int? _customerId = null;

    private Showtime _showtime = null!;
    private Room _room = null!;
    private Movie _movie = null!;

    /// <summary>
    /// Event khi nhấn nút Quay lại.
    /// </summary>
    public event Action? BackRequested;

    /// <summary>
    /// Event khi nhân viên chọn xong ghế và tiếp tục sang màn bắp nước.
    /// </summary>
    public event Action<SaleOrderState>? ContinueRequested;

    public UcSeatSelection()
    {
        InitUI();
    }

    /// <summary>
    /// Nạp dữ liệu suất chiếu + vẽ sơ đồ ghế.
    /// </summary>
    public async Task LoadShowtimeAsync(Showtime showtime)
    {
        _showtime = showtime;

        using var ctx = Program.CreateDbContext();

        // Load movie + room + seats
        _movie = (await ctx.Movies
            .Include(m => m.MovieGenres).ThenInclude(mg => mg.Genre)
            .FirstOrDefaultAsync(m => m.Id == showtime.MovieId))!;

        _room = (await ctx.Rooms
            .Include(r => r.Seats.OrderBy(s => s.RowLabel).ThenBy(s => s.SeatNumber))
            .FirstOrDefaultAsync(r => r.Id == showtime.RoomId))!;

        var ticketService = new TicketService(ctx);
        var soldIds = await ticketService.GetSoldSeatIdsAsync(showtime.Id);

        // Cập nhật UI
        lblMovieTitle.Text = _movie.Title;
        string genres = string.Join(", ", _movie.MovieGenres.Select(mg => mg.Genre.Name));
        lblShowInfo.Text = $"🏠  {_room.Name} ({_room.Type})\n" +
                           $"📅  {showtime.StartTime:dd/MM/yyyy}\n" +
                           $"⏰  {showtime.StartTime:HH:mm} – {showtime.EndTime:HH:mm}\n" +
                           $"⏱  {_movie.Duration} phút  |  {_movie.AgeRating}\n" +
                           $"🏷️  {genres}\n" +
                           $"💰  Giá cơ bản: {showtime.BasePrice:N0} đ";

        // Nạp sơ đồ ghế
        seatMap.SetData(_room.Seats.ToList(), soldIds, _room.Rows, _room.Columns, showtime.BasePrice);
        
        // Nạp danh sách khách hàng
        _customers = await ctx.Customers.Where(c => c.IsActive).ToListAsync();
        cboCustomerSearch.Items.Clear();
        cboCustomerSearch.Items.Add("Khách vãng lai");
        foreach (var c in _customers)
            cboCustomerSearch.Items.Add($"{c.Phone} - {c.FullName}");
        cboCustomerSearch.SelectedIndex = 0;

        UpdateSelection();
    }

    private void InitUI()
    {
        this.Dock = DockStyle.Fill;
        this.BackColor = Color.FromArgb(18, 18, 30);

        // === Panel phải: thông tin + checkout ===
        pnlRight = new Panel
        {
            Dock = DockStyle.Right,
            Width = 340,
            BackColor = Color.FromArgb(22, 22, 38),
            AutoScroll = true,
            Padding = new Padding(15)
        };
        this.Controls.Add(pnlRight);

        int y = 10;

        // Nút Quay lại
        btnBack = new Button
        {
            Text = "← Quay lại",
            Font = new Font("Segoe UI", 10),
            Size = new Size(120, 32),
            Location = new Point(15, y),
            BackColor = Color.FromArgb(50, 50, 75),
            ForeColor = Color.FromArgb(200, 200, 225),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnBack.FlatAppearance.BorderSize = 0;
        btnBack.FlatAppearance.MouseOverBackColor = Color.FromArgb(70, 60, 100);
        btnBack.Click += (s, e) => BackRequested?.Invoke();
        pnlRight.Controls.Add(btnBack);
        y += 45;

        // Tên phim
        lblMovieTitle = new Label
        {
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(220, 220, 240),
            Location = new Point(15, y),
            Size = new Size(300, 40),
            MaximumSize = new Size(300, 0),
            AutoSize = true
        };
        pnlRight.Controls.Add(lblMovieTitle);
        y += 45;

        // Thông tin suất chiếu
        lblShowInfo = new Label
        {
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(150, 150, 180),
            Location = new Point(15, y),
            Size = new Size(300, 135),
            MaximumSize = new Size(300, 0),
            AutoSize = true
        };
        pnlRight.Controls.Add(lblShowInfo);
        y += 146;

        // Separator
        pnlRight.Controls.Add(new Panel { Location = new Point(15, y), Size = new Size(300, 1), BackColor = Color.FromArgb(50, 50, 75) });
        y += 12;

        // Ghế đã chọn
        lblSelectedCount = new Label
        {
            Text = "💺  Ghế đã chọn: 0",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(100, 80, 255),
            Location = new Point(15, y),
            AutoSize = true
        };
        pnlRight.Controls.Add(lblSelectedCount);
        y += 28;

        lblSelectedSeats = new Label
        {
            Text = "(Chưa chọn ghế nào)",
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(170, 170, 200),
            Location = new Point(15, y),
            Size = new Size(300, 40),
            MaximumSize = new Size(300, 0),
            AutoSize = true
        };
        pnlRight.Controls.Add(lblSelectedSeats);
        y += 45;

        // Tổng tiền
        lblTotalPrice = new Label
        {
            Text = "Tổng: 0 đ",
            Font = new Font("Segoe UI", 16, FontStyle.Bold),
            ForeColor = Color.FromArgb(80, 220, 120),
            Location = new Point(15, y),
            AutoSize = true
        };
        pnlRight.Controls.Add(lblTotalPrice);
        y += 42;

        // Separator
        pnlRight.Controls.Add(new Panel { Location = new Point(15, y), Size = new Size(300, 1), BackColor = Color.FromArgb(50, 50, 75) });
        y += 15;

        // Thông tin khách hàng
        pnlRight.Controls.Add(new Label
        {
            Text = "Thông tin khách hàng",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 160, 190),
            Location = new Point(15, y),
            AutoSize = true
        });
        y += 28;

        pnlRight.Controls.Add(new Label { Text = "Tìm từ Database:", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(130, 130, 160), Location = new Point(15, y + 3), AutoSize = true });
        cboCustomerSearch = new ComboBox
        {
            Font = new Font("Segoe UI", 10),
            Size = new Size(210, 28),
            Location = new Point(105, y),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DropDownStyle = ComboBoxStyle.DropDown,
            AutoCompleteMode = AutoCompleteMode.SuggestAppend,
            AutoCompleteSource = AutoCompleteSource.ListItems
        };
        cboCustomerSearch.SelectedIndexChanged += async (s, e) => 
        {
            if (cboCustomerSearch.SelectedIndex == 0)
            {
                txtCustomerName.Text = "";
                txtCustomerPhone.Text = "";
                _customerId = null;
                lblMemberInfo.Visible = false;
            }
            else if (cboCustomerSearch.SelectedIndex > 0)
            {
                var cust = _customers[cboCustomerSearch.SelectedIndex - 1];
                txtCustomerPhone.Text = cust.Phone;
                txtCustomerName.Text = cust.FullName;
                await LookupCustomerAsync();
            }
        };
        pnlRight.Controls.Add(cboCustomerSearch);
        y += 35;

        pnlRight.Controls.Add(new Label { Text = "Tên KH:", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(130, 130, 160), Location = new Point(15, y + 3), AutoSize = true });
        txtCustomerName = new TextBox
        {
            Font = new Font("Segoe UI", 10),
            Size = new Size(210, 28),
            Location = new Point(105, y),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "(không bắt buộc)"
        };
        pnlRight.Controls.Add(txtCustomerName);
        y += 35;

        pnlRight.Controls.Add(new Label { Text = "SĐT:", Font = new Font("Segoe UI", 9), ForeColor = Color.FromArgb(130, 130, 160), Location = new Point(15, y + 3), AutoSize = true });
        txtCustomerPhone = new TextBox
        {
            Font = new Font("Segoe UI", 10),
            Size = new Size(160, 28),
            Location = new Point(105, y),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "SĐT/Mã TV"
        };
        txtCustomerPhone.KeyDown += async (s, e) => { if (e.KeyCode == Keys.Enter) await LookupCustomerAsync(); };
        pnlRight.Controls.Add(txtCustomerPhone);

        var btnLookup = new Button
        {
            Text = "🔍",
            Font = new Font("Segoe UI", 10),
            Size = new Size(45, 28),
            Location = new Point(270, y),
            BackColor = Color.FromArgb(60, 120, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnLookup.FlatAppearance.BorderSize = 0;
        btnLookup.Click += async (s, e) => await LookupCustomerAsync();
        pnlRight.Controls.Add(btnLookup);
        y += 32;

        // Member info label
        lblMemberInfo = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(80, 220, 120),
            Location = new Point(15, y),
            Size = new Size(300, 20),
            Visible = false
        };
        pnlRight.Controls.Add(lblMemberInfo);
        y += 25;

        // Separator
        pnlRight.Controls.Add(new Panel { Location = new Point(15, y), Size = new Size(300, 1), BackColor = Color.FromArgb(50, 50, 75) });
        y += 15;

        // Bước tiếp theo
        pnlRight.Controls.Add(new Label
        {
            Text = "Bước tiếp theo",
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            ForeColor = Color.FromArgb(160, 160, 190),
            Location = new Point(15, y),
            AutoSize = true
        });
        y += 28;

        pnlRight.Controls.Add(new Label
        {
            Text = "Chọn bắp nước rồi thanh toán một lần.",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(130, 130, 160),
            Location = new Point(15, y + 3),
            Size = new Size(300, 34)
        });
        txtReceived = new TextBox
        {
            Font = new Font("Segoe UI", 11),
            Size = new Size(175, 28),
            Location = new Point(105, y),
            BackColor = Color.FromArgb(35, 35, 55),
            ForeColor = Color.FromArgb(80, 220, 120),
            BorderStyle = BorderStyle.FixedSingle,
            TextAlign = HorizontalAlignment.Right,
            Visible = false
        };
        txtReceived.TextChanged += (s, e) => CalculateChange();
        pnlRight.Controls.Add(txtReceived);
        y += 35;

        lblChange = new Label
        {
            Text = "Tiền thối: 0 đ",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(255, 200, 60),
            Location = new Point(15, y),
            AutoSize = true,
            Visible = false
        };
        pnlRight.Controls.Add(lblChange);
        y += 12;

        // Nút thanh toán
        btnCheckout = new Button
        {
            Text = "TIẾP TỤC",
            Font = new Font("Segoe UI", 12, FontStyle.Bold),
            Size = new Size(300, 48),
            Location = new Point(15, y),
            BackColor = Color.FromArgb(60, 170, 60),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Enabled = false
        };
        btnCheckout.FlatAppearance.BorderSize = 0;
        btnCheckout.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 200, 50);
        btnCheckout.Click += BtnCheckout_Click;
        pnlRight.Controls.Add(btnCheckout);

        // === Bên trái: SeatMapControl trong ScrollPanel ===
        var pnlSeatContainer = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = Color.FromArgb(18, 18, 30),
            Padding = new Padding(10)
        };
        this.Controls.Add(pnlSeatContainer);
        pnlSeatContainer.BringToFront();

        seatMap = new SeatMapControl
        {
            Location = new Point(10, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        seatMap.SeatSelectionChanged += (s, e) => UpdateSelection();
        pnlSeatContainer.Controls.Add(seatMap);

        // Center seat map khi resize
        pnlSeatContainer.Resize += (s, e) =>
        {
            int x = Math.Max(10, (pnlSeatContainer.ClientSize.Width - seatMap.Width) / 2);
            seatMap.Location = new Point(x, 10);
        };
    }

    private void UpdateSelection()
    {
        var selected = seatMap.SelectedSeats;
        int count = selected.Count;

        lblSelectedCount.Text = $"💺  Ghế đã chọn: {count}";

        if (count == 0)
        {
            lblSelectedSeats.Text = "(Chưa chọn ghế nào)";
            lblSelectedSeats.ForeColor = Color.FromArgb(110, 110, 140);
        }
        else
        {
            var seatLabels = selected
                .OrderBy(s => s.RowLabel)
                .ThenBy(s => s.SeatNumber)
                .Select(s =>
                {
                    string badge = s.Type switch
                    {
                        "VIP" => " ⭐",
                        "Couple" => " 💕",
                        _ => ""
                    };
                    return $"{s.RowLabel}{s.SeatNumber}{badge}";
                });
            lblSelectedSeats.Text = string.Join(",  ", seatLabels);
            lblSelectedSeats.ForeColor = Color.FromArgb(200, 200, 225);
        }

        lblTotalPrice.Text = $"Tổng: {seatMap.TotalPrice:N0} đ";
        btnCheckout.Enabled = count > 0;

        CalculateChange();
    }

    private void CalculateChange()
    {
        decimal total = seatMap.TotalPrice;

        if (decimal.TryParse(txtReceived.Text.Replace(",", "").Replace(".", ""), out decimal received) && received >= total && total > 0)
        {
            decimal change = received - total;
            lblChange.Text = $"Tiền thối: {change:N0} đ";
            lblChange.ForeColor = Color.FromArgb(255, 200, 60);
        }
        else if (total > 0 && txtReceived.Text.Length > 0)
        {
            lblChange.Text = "⚠ Chưa đủ tiền";
            lblChange.ForeColor = Color.FromArgb(255, 90, 90);
        }
        else
        {
            lblChange.Text = "Tiền thối: 0 đ";
            lblChange.ForeColor = Color.FromArgb(255, 200, 60);
        }
    }

    private void BtnCheckout_Click(object? sender, EventArgs e)
    {
        var selected = seatMap.SelectedSeats;
        if (selected.Count == 0)
        {
            MessageBox.Show("Vui lòng chọn ghế!", "Thiếu thông tin");
            return;
        }

        decimal total = seatMap.TotalPrice;

        ContinueRequested?.Invoke(new SaleOrderState
        {
            Showtime = _showtime,
            Movie = _movie,
            Room = _room,
            Seats = selected.ToList(),
            CustomerName = string.IsNullOrWhiteSpace(txtCustomerName.Text) ? null : txtCustomerName.Text.Trim(),
            CustomerPhone = string.IsNullOrWhiteSpace(txtCustomerPhone.Text) ? null : txtCustomerPhone.Text.Trim(),
            CustomerId = _customerId,
            TicketTotal = total
        });
    }

    /// <summary>
    /// Tra cứu khách hàng thành viên theo SĐT hoặc mã thành viên.
    /// </summary>
    private async Task LookupCustomerAsync()
    {
        string input = txtCustomerPhone.Text.Trim();
        if (string.IsNullOrWhiteSpace(input))
        {
            _customerId = null;
            lblMemberInfo.Visible = false;
            return;
        }

        try
        {
            using var ctx = Program.CreateDbContext();
            var svc = new CustomerService(ctx);

            // Thử tìm theo mã thành viên trước, rồi theo SĐT
            var customer = await svc.GetByMemberCodeAsync(input)
                        ?? await svc.GetByPhoneAsync(input);

            if (customer != null)
            {
                _customerId = customer.Id;
                txtCustomerName.Text = customer.FullName;
                string tierIcon = customer.Tier switch
                {
                    "Diamond" => "💎",
                    "VIP" => "⭐",
                    _ => "🎫"
                };
                lblMemberInfo.Text = $"{tierIcon} {customer.Tier} | {customer.TotalPoints:N0} điểm";
                lblMemberInfo.ForeColor = Color.FromArgb(80, 220, 120);
                lblMemberInfo.Visible = true;
            }
            else
            {
                _customerId = null;
                lblMemberInfo.Text = "⚠ Không tìm thấy thành viên";
                lblMemberInfo.ForeColor = Color.FromArgb(255, 180, 60);
                lblMemberInfo.Visible = true;
            }
        }
        catch
        {
            lblMemberInfo.Text = "⚠ Lỗi tra cứu";
            lblMemberInfo.ForeColor = Color.FromArgb(255, 90, 90);
            lblMemberInfo.Visible = true;
        }
    }
}
