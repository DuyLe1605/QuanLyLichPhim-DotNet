# 📋 Phân Chia Công Việc CineManager — 5 Người

**Ngày lập:** 04/05/2026  
**Dự án:** CineManager (WinForms .NET 10) — Phase 1-13  
**Thành viên:** Duy, Đức, Hưng, Sáng, Mạnh

---

## 📊 Tóm Tắt Phân Chia

| Người       | Module Chính    | Công Việc                                                    | Tổng KL | Ưu tiên  |
| ----------- | --------------- | ------------------------------------------------------------ | ------- | -------- |
| **👨‍💻 DUY**  | Backend Core    | AuthService, Services layers, DB migrations                  | 25%     | 🔴 Cao   |
| **👨‍💼 ĐỨC**  | Admin Module    | CRUD quản lý (phim/phòng/lịch/nhân viên), Dashboard, Voucher | 25%     | 🔴 Cao   |
| **👨‍🍳 HƯNG** | Staff Module    | Bán vé, bắp nước, soát vé, ca làm việc                       | 20%     | 🟡 Trung |
| **🛍️ SÁNG** | Customer Module | Storefront, đặt vé online, KH self-service, Loyalty          | 20%     | 🟡 Trung |
| **🎨 MẠNH** | UI/Integration  | Custom controls (GDI+), theme, hardware, testing             | 10%     | 🟡 Trung |

---

## 🎯 Chi Tiết Từng Người

### 1️⃣ DUY — Backend Core Architect (25%)

**🎖️ Tiêu đề:** Senior Backend Developer  
**📍 Vị trí:** Quản lý tầng database và services

#### **Trách Nhiệm Chính:**

- Thiết kế & maintain toàn bộ database schema
- Tạo/quản lý EF Core migrations
- Phát triển 60% Services layer (logic kinh doanh)
- Đảm bảo transaction, concurrency, performance

#### **Models & Database Schema:**

```
✅ HOÀN THÀNH (Phase 1-5.5):
  ├─ User (tài khoản staff/admin)
  ├─ Genre (thể loại phim)
  ├─ Movie (thông tin phim)
  ├─ MovieGenre (N-N: phim & thể loại)
  ├─ Room (phòng chiếu)
  ├─ Seat (ghế — có tọa độ grid)
  ├─ Showtime (lịch chiếu)
  ├─ Invoice (hóa đơn bán hàng)
  ├─ Ticket (vé — chiếc ghế đã bán)
  ├─ Snack (đồ ăn/nước)
  └─ InvoiceSnack (chi tiết bắp nước trong HĐ)

🔲 CẦN PHÁT TRIỂN (Phase 7-11):
  ├─ Customer (tài khoản khách hàng)
  ├─ Booking (đặt vé trước — kiểu eventbrite)
  ├─ PointTransaction (tích/đổi điểm)
  ├─ Voucher (mã giảm giá)
  └─ Shift (ca làm việc của nhân viên)
```

#### **Services — Backend Logic (Duy phát triển):**

**✅ Đã xong:**

```csharp
1. AuthService.cs
   - LoginAsync(username, password) → verify BCrypt
   - CreateUserAsync(username, password, role) → hash BCrypt
   - ChangePasswordAsync()
   - Verify role permission

2. MovieService.cs
   - GetAllAsync() / GetByIdAsync(id)
   - CreateAsync(movie) → validate code unique
   - UpdateAsync(id, movie)
   - SoftDeleteAsync(id) → IsActive = false
   - SearchAsync(keyword) → tìm theo mã, tên, đạo diễn
   - GetAllGenresAsync() → list thể loại
   - FilterByGenreAsync(genreId)

3. RoomService.cs
   - GetAllAsync() / GetByIdAsync(id)
   - CreateAsync(room, rowConfigs) → auto-generate seats
   - UpdateAsync(id, room)
   - GenerateSeatsFromRowConfig() → từ config hàng → tạo seats
   - GenerateSeatsFromCustomGrid() → từ grid builder → tạo seats
   - ToggleActiveAsync(id)

4. ShowtimeService.cs ⭐ (QUAN TRỌNG)
   - GetAllAsync() / GetByIdAsync(id)
   - CreateAsync(showtime) → kiểm tra trùng lịch (overlap detection)
   - UpdateAsync(id, showtime) → kiểm tra trùng + kiểm tra đã bán vé?
   - SoftDeleteAsync(id) → kiểm tra có vé chưa? Không cho xóa
   - GetByDateAsync(date) / GetByMovieAsync(movieId) / GetByRoomAsync(roomId)
   - CheckTimeConflictAsync(roomId, startTime, endTime) → trả về showtime trùng

5. TicketService.cs
   - GetSoldSeatIdsAsync(showtimeId) → list ID ghế đã bán
   - CountSoldAsync(showtimeId) → tổng vé đã bán
   - GetByInvoiceAsync(invoiceId) → list vé trong HĐ

6. InvoiceService.cs
   - CreateAsync(invoice, tickets, snacks) → transaction + double-check seats
     * Inside transaction:
       - Verify ghế chưa bán (select … with (updlock))
       - Create Invoice
       - Create Tickets
       - Create InvoiceSnacks
       - Update Customer spending (nếu có)
       - Commit hoặc Rollback
   - GetAllAsync() / GetByIdAsync(id)
   - GetByDateRangeAsync(fromDate, toDate)

7. SnackService.cs
   - GetAllAsync() / GetAllActiveAsync()
   - GetByIdAsync(id)
   - GetByCategoryAsync(category)
   - CreateAsync(snack) → validate tên, giá
   - UpdateAsync(id, snack)
   - SoftDeleteAsync(id)
   - SearchAsync(keyword)
```

**🔲 Cần làm (Phase 7-11):**

```csharp
8. CustomerService.cs (Phase 7)
   - RegisterAsync(email, phone, password, fullName)
     * Hash password BCrypt
     * Generate MemberCode: "CM-" + RandomString(6)
     * Set Tier = "Standard", TotalPoints = 0, TotalSpent = 0
   - LoginAsync(email, password) → verify BCrypt
   - GetByPhoneAsync(phone) → for staff lookup
   - GetByMemberCodeAsync(memberCode) → for staff lookup
   - GetByIdAsync(id)
   - SearchAsync(keyword) → tìm theo tên/SĐT/email/mã thành viên
   - GetByTierAsync(tier) → filter: Standard/VIP/Diamond
   - UpdateProfileAsync(id, fullName, phone, email, password)
   - ChangePasswordAsync(id, oldPwd, newPwd)
   - AddSpendingAsync(id, amount) → cộng TotalSpent + auto nâng hạng
     * Standard: < 2M
     * VIP: 2M-10M
     * Diamond: > 10M
   - UpdateTierAsync(id, newTier)
   - ToggleActiveAsync(id)

9. PointService.cs (Phase 8)
   - EarnPointsAsync(customerId, invoiceId, amount)
     * Tính điểm theo hạng (Standard: 1%, VIP: 1.5%, Diamond: 2%)
     * VD: Tier=Standard, TotalAmount=100k → 1000 điểm
     * Create PointTransaction (Type=Earn)
   - RedeemPointsAsync(customerId, points)
     * Kiểm tra điểm đủ, bội số 100
     * 1000 điểm = 10,000đ discount
     * Create PointTransaction (Type=Redeem)
   - GetBalanceAsync(customerId)
   - GetHistoryAsync(customerId) → list transactions
   - GetSummaryAsync(customerId) → {earned, redeemed, balance}

10. VoucherService.cs (Phase 10)
    - GetAllAsync() / GetAllActiveAsync()
    - GetByIdAsync(id)
    - CreateAsync(voucher) → validate code unique, dates
    - UpdateAsync(id, voucher)
    - ValidateAsync(code, orderTotal)
      * Kiểm tra: hạn sử dụng, MaxUses, orderTotal >= minAmount
      * Trả về: {isValid, error, discountAmount}
    - ApplyAsync(code, invoiceId)
      * Tăng UsedCount
      * Trả về discountAmount
    - DeactivateAsync(id)

11. ShiftService.cs (Phase 11)
    - OpenShiftAsync(userId, openingCash)
      * Kiểm tra user chưa có shift đang mở
      * Create Shift (Status=Open, OpenedAt=now)
    - CloseShiftAsync(shiftId, actualCash, note)
      * Sum ExpectedCash từ invoices của shift này
      * ExpectedCash = openingCash + σ(invoice tiền mặt)
      * CashDifference = actualCash - expectedCash
      * Update Shift (Status=Closed, ClosedAt=now)
    - GetCurrentShiftAsync(userId) → shift đang mở hoặc null
    - GetShiftReportAsync(shiftId) → {invoiceCount, cashTotal, cardTotal, transferTotal, difference}

12. BookingService.cs (Phase 9) [Duy help, có thể Sáng lead]
    - CreateAsync(customerId, showtimeId, tickets, snacks, bookingCode)
      * Similar CreateInvoice nhưng cho online
      * Create Booking (Status=Pending)
      * Return booking info + qrCode content
    - GetByCodeAsync(bookingCode)
    - UpdateStatusAsync(bookingId, newStatus) [Pending→Paid→CheckedIn]
    - CancelAsync(bookingId)
```

#### **Migrations (Database Versioning):**

```
✅ Đã cài:
  ├─ 20260503125346_InitialCreate (all base tables)
  ├─ 20260503150621_AddMovieCode
  ├─ 20260504012500_AddSnackImagePath
  ├─ 20260504014500_AddMoviePosterPathAndTicketUniqueIndex
  ├─ 20260504021000_AddSeatGridCoordinates
  └─ 20260504024752_AddMissingColumns

🔲 Cần tạo:
  ├─ AddCrmShiftVoucherBooking (Phase 7)
  └─ ...tùy theo thay đổi schema
```

**Duy chịu trách nhiệm:**

```bash
$ dotnet ef migrations add <MigrationName> --output-dir Migrations
$ dotnet ef database update
$ dotnet ef database drop (nếu cần reset)
```

#### **Configs & Helpers (Duy maintain):**

- `appsettings.json` — connection string, app settings
- `AppConfig.cs` — read connection string, static helpers
- `AppDbContextFactory.cs` — design-time factory for `dotnet ef`
- `SessionManager.cs` — manage login session (User hoặc Customer)

#### **Timeline & Dependencies:**

- **Phase 1:** Models + Migrations + AuthService
- **Phase 2:** MovieService, RoomService, ShowtimeService
- **Phase 3:** TicketService, InvoiceService
- **Phase 5:** SnackService
- **Phase 7:** Customer models + CustomerService + PointService (cơ bản)
- **Phase 8:** PointService (hoàn thiện)
- **Phase 9:** BookingService (possibly with Sáng)
- **Phase 10:** VoucherService
- **Phase 11:** ShiftService

**Người phụ thuộc:** Đức, Hưng, Sáng (dùng services)

---

### 2️⃣ ĐỨC — Admin Module & Dashboard (25%)

**🎖️ Tiêu đề:** Admin UI/Frontend Developer  
**📍 Vị trí:** Quản lý admin backoffice, dashboard, CRUD forms

#### **Trách Nhiệm Chính:**

- Phát triển Admin Module toàn bộ (Quản lý phim, phòng, lịch, nhân viên, snack)
- Xây dựng Dashboard & Reports
- Setup theme & shared UI components
- Integrate LiveCharts2 cho data visualization

#### **Admin Forms & Controls (Đức phát triển):**

**✅ Hoàn thành (Phase 2-5.5):**

```csharp
1. Theme & Shared Components:
   ├─ AdminTheme.cs
   │  └─ Design tokens: màu, font, padding, border-radius, status
   ├─ AdminControls.cs
   │  └─ Factory methods: CreateToolbar(), CreateButton(), CreateDataGridView()
   ├─ AdminLayouts.cs
   │  └─ CreateManagementPage(title, toolbar, gridView)
   └─ SeatLayoutPreviewControl.cs
      └─ Preview sơ đồ ghế dạng GDI+

2. Movie Management:
   ├─ UcMovieManagement.cs (UserControl)
   │  ├─ DataGridView: Mã phim, Tên, Đạo diễn, Thời lượng, Thể loại, Trạng thái
   │  ├─ Thanh tìm kiếm: theo mã, tên, đạo diễn
   │  ├─ ComboBox lọc thể loại
   │  ├─ Nút: Thêm, Sửa, Xóa, Refresh
   │  └─ Chống re-entrancy khi loading
   ├─ DlgMovieEdit.cs (Dialog thêm/sửa)
   │  ├─ Layout 2 cột: fields trái + poster/thể loại phải
   │  ├─ Fields: Mã, Tên, Đạo diễn, Diễn viên, Thời lượng, Rating tuổi, Mô tả
   │  ├─ Upload poster → lưu vào Resources/Posters + DB path
   │  ├─ CheckedListBox thể loại (multi-select)
   │  └─ ErrorProvider validate

3. Room Management:
   ├─ UcRoomManagement.cs
   │  ├─ DataGridView: Phòng, Loại, Số ghế, Hàng, Cột, Trạng thái
   │  ├─ Split layout: grid trái + SeatLayoutPreviewControl phải
   │  ├─ Nút: Thêm, Sửa, Xóa, Build ghế tùy chỉnh
   │  └─ Khi chọn phòng → reload preview
   ├─ DlgRoomEdit.cs (form tạo/sửa phòng)
   │  ├─ Fields: Tên phòng, Loại (2D/3D/IMAX), Hàng, Cột
   │  ├─ DataGridView cấu hình hàng:
   │  │  ├─ Col1: Label hàng (A, B, C,...)
   │  │  ├─ Col2: Số ghế/hàng
   │  │  ├─ Col3: Loại (Standard/VIP/Couple)
   │  │  ├─ Col4: Hệ số giá (1.0, 1.5, 2.0)
   │  │  └─ Nút: + Hàng, - Hàng
   │  ├─ Real-time tính tổng ghế
   │  ├─ Nút "Mở Seat Builder" → DlgSeatBuilder
   │  └─ ErrorProvider
   └─ DlgSeatBuilder.cs (custom seat map builder)
      ├─ Lưới tọa độ grid (20x10 cells)
      ├─ Toolbar: Standard | VIP | Couple | Xóa | Lối đi
      ├─ Click cell → tô màu loại ghế
      ├─ Lưu → auto-label: quét ltrái→phải, bỏ ô trống, sinh A1, A2...
      └─ Hiển thị preview live

4. Showtime Management:
   ├─ UcShowtimeManagement.cs
   │  ├─ DataGridView: Phim, Phòng, Ngày chiếu, Giờ, Giá, Ghế còn, Trạng thái
   │  ├─ Bộ lọc: ComboBox ngày, ComboBox phim, ComboBox phòng
   │  ├─ Nút: Thêm, Sửa, Xóa, Refresh
   │  ├─ Double-click row → sửa
   │  └─ Sau khi thêm/sửa → auto-filter ngày của lịch vừa lưu
   └─ DlgShowtimeEdit.cs (form thêm/sửa lịch)
      ├─ ComboBox phim (auto-cập nhật duration)
      ├─ ComboBox phòng
      ├─ DateTimePicker ngày + giờ chiếu
      ├─ Field giá vé cơ bản
      ├─ Auto tính EndTime = StartTime + Duration + 15 phút
      ├─ Kiểm tra trùng lịch → show warning chi tiết
      └─ ErrorProvider

5. Staff Management:
   ├─ UcStaffManagement.cs
   │  ├─ DataGridView: Tên, Username, SĐT, Role, Trạng thái, Ngày tạo
   │  ├─ Thanh tìm kiếm theo tên/username/SĐT
   │  ├─ Nút: Thêm, Sửa, Khóa/Mở, Reset MK, Xóa (soft)
   │  └─ Chống khóa bản thân
   └─ DlgStaffEdit.cs (form nhân viên)
      ├─ Fields: Tên, Username, Mật khẩu (chỉ thêm), SĐT
      ├─ Radio Role: Admin / Staff
      ├─ ErrorProvider
      └─ Hash password BCrypt

6. Snack Management:
   ├─ UcSnackManagement.cs
   │  ├─ DataGridView: Loại, Tên, Giá, Trạng thái
   │  ├─ Thanh tìm kiếm + ComboBox lọc loại (Food/Drink/Combo)
   │  ├─ Nút: Thêm, Sửa, Ẩn, Xóa
   │  ├─ Double-click → sửa
   │  └─ Refresh
   └─ DlgSnackEdit.cs (form đồ ăn)
      ├─ Fields: Tên, Loại (dropdown), Giá, ImagePath
      ├─ ErrorProvider
      └─ Validate tên, giá > 0

7. Dashboard (Phase 4):
   ├─ UcDashboard.cs (UserControl chính)
   │  ├─ 6 Stat Cards (accent colors):
   │  │  ├─ 🎬 Phim: N bộ
   │  │  ├─ 🏠 Phòng: N phòng
   │  │  ├─ 📅 Suất hôm nay: N suất
   │  │  ├─ 🎟 Vé bán: N vé
   │  │  ├─ 💰 Doanh thu: X.XXX.XXX đ
   │  │  └─ 📊 Hóa đơn: N HĐ
   │  ├─ Charts (LiveCharts2):
   │  │  ├─ Column Chart: doanh thu theo ngày/tháng (toggle)
   │  │  ├─ Bar Chart: Top 5 phim ăn khách (tên + số vé)
   │  │  └─ Pie Chart: tỷ lệ lấp đầy phòng
   │  ├─ Bộ lọc: DatePicker từ/đến, nút Làm mới
   │  └─ Async load dữ liệu (show loading state)
   └─ ReportService.cs (Services — Duy làm, Đức gọi):
      ├─ GetStatsAsync() → total stats
      ├─ GetRevenueByDateAsync(from, to) → daily revenue
      ├─ GetRevenueByMonthAsync(year) → monthly
      ├─ GetTopMoviesAsync(n) → top N movies
      └─ GetRoomOccupancyAsync() → occupancy rate per room
```

**🔲 Cần làm (Phase 8-11, 13):**

```csharp
8. Customer Management (Phase 8):
   ├─ UcCustomerManagement.cs
   │  ├─ DataGridView: Mã TV, Tên, SĐT, Email, Hạng, Điểm, Tổng chi, Trạng thái
   │  ├─ Split layout: grid trái + detail phải
   │  ├─ Right panel:
   │  │  ├─ QR code MemberCode (dùng BarcodeHelper)
   │  │  ├─ Thông tin: tên, hạng, điểm
   │  │  ├─ Progress bar → next tier
   │  │  └─ Lịch sử giao dịch (recent 5)
   │  ├─ Tìm kiếm: tên/SĐT/email/mã TV
   │  ├─ Lọc: Tier (Standard/VIP/Diamond)
   │  └─ Nút: Khóa, Mở, Xem chi tiết, Xóa

9. Voucher Management (Phase 10):
   ├─ UcVoucherManagement.cs
   │  ├─ DataGridView: Mã, Loại, Giá trị, Đã dùng/Tối đa, Ngày HH, Trạng thái
   │  ├─ Thanh tìm kiếm + lọc trạng thái
   │  ├─ Nút: Tạo, Sửa, Vô hiệu, Xóa
   │  └─ Refresh
   ├─ DlgVoucherEdit.cs (form voucher)
   │  ├─ Fields: Mã code, Loại (Percent/Fixed/FreeTicket)
   │  ├─ Giá trị, Max discount, Max uses
   │  ├─ DatePicker ngày bắt đầu/kết thúc
   │  └─ ErrorProvider

10. Shift Report (Phase 11):
    ├─ UcShiftReport.cs
    │  ├─ DataGridView: Nhân viên, Thời gian, Tiền mở, Tiền chốt, Chênh lệch, Trạng thái
    │  ├─ Lọc: ComboBox nhân viên, DatePicker ngày
    │  ├─ Nút: Xem chi tiết, In báo cáo
    │  └─ Chart: chênh lệch theo nhân viên
    └─ Shift detail: X-Report (số HĐ, tiền mặt/thẻ/CK, tổng)
```

#### **PDF Export:**

```csharp
PrintHelper.cs (QuestPDF — Duy phát triển, Đức dùng)
  ├─ ExportDailyReportAsync(from, to) → PDF báo cáo ngày
  └─ Template:
     ├─ Header: Logo text + tên rạp + ngày xuất + khoảng thời gian
     ├─ Section 1: 6 stat cards (dạng bảng)
     ├─ Section 2: Top 5 phim (bảng: STT | Tên | Số vé | Doanh thu)
     ├─ Section 3: Doanh thu theo ngày (bảng + tổng cộng)
     └─ Footer: Số trang | Ngày in
```

#### **Integration vào FrmMain:**

```csharp
// Sidebar menu (FrmMain.cs — Duy/Mạnh setup, Đức dùng)
"📂 Quản lý Phim" → LoadModule("Movies") → UcMovieManagement
"🏠 Quản lý Phòng" → LoadModule("Rooms") → UcRoomManagement
"📅 Quản lý Lịch" → LoadModule("Showtimes") → UcShowtimeManagement
"👨‍💼 Quản lý Nhân viên" → LoadModule("Staff") → UcStaffManagement
"🍿 Quản lý Bắp nước" → LoadModule("Snacks") → UcSnackManagement
"📊 Thống kê" → LoadModule("Dashboard") → UcDashboard
"👤 Khách hàng" → LoadModule("Customers") → UcCustomerManagement [Phase 8]
"💰 Khuyến mãi" → LoadModule("Vouchers") → UcVoucherManagement [Phase 10]
"📋 Ca làm việc" → LoadModule("ShiftReport") → UcShiftReport [Phase 11]
```

#### **Timeline:**

- Phase 2: MovieService UI, RoomService UI, ShowtimeService UI, StaffService UI
- Phase 4: Dashboard + ReportService integration
- Phase 4.5: Polish, AdminTheme, AdminLayouts standardize
- Phase 5: SnackService UI
- Phase 8: UcCustomerManagement
- Phase 10: UcVoucherManagement
- Phase 11: UcShiftReport
- Phase 13: Advanced reports, dashboard refinement

**Người phụ thuộc:** Mạnh (theme, layout), Duy (services)

---

### 3️⃣ HƯNG — Staff Module (POS/Frontdesk) (20%)

**🎖️ Tiêu đề:** POS/Frontdesk Developer  
**📍 Vị trí:** Quản lý staff-facing features (bán vé, soát vé, ca làm việc)

#### **Trách Nhiệm Chính:**

- Phát triển module bán vé cho Staff (Frontdesk)
- Xây dựng luồng checkout (ghế + bắp nước + thanh toán)
- Tích hợp check-in / soát vé
- Quản lý ca làm việc (mở/chốt ca)

#### **Staff Forms & Controls (Hưng phát triển):**

**✅ Hoàn thành (Phase 3, 5):**

```csharp
1. Now Showing (Danh sách phim):
   ├─ UcNowShowing.cs
   │  ├─ Left panel: Card phim dạng FlowLayoutPanel
   │  │  ├─ Card: poster + tên + thời lượng + thể loại
   │  │  └─ Click → hiển thị suất chiếu
   │  ├─ Right panel: Suất chiếu theo ngày
   │  │  ├─ DatePicker chọn ngày
   │  │  ├─ Button suất: giờ | phòng | ghế còn/tổng | giá
   │  │  └─ Click suất → fire ShowtimeSelected event
   │  └─ Event: ShowtimeSelected(showtimeId)

2. Seat Selection (Chọn ghế):
   ├─ SeatMapControl.cs (Mạnh phát triển GDI+, Hưng dùng)
   │  ├─ Vẽ ghế dạng rounded rectangles
   │  ├─ Màu sắc: Trống (xám) | Đang chọn (tím) | Đã bán (đỏ) | VIP (vàng) | Couple (hồng)
   │  ├─ Click ghế → chọn/bỏ chọn
   │  ├─ Hover → tooltip (tên ghế + loại + giá)
   │  ├─ Event: SeatSelectionChanged(selectedSeatIds)
   │  ├─ Legend ở dưới
   │  └─ "MÀN HÌNH" gradient ở trên
   ├─ UcSeatSelection.cs (Hưng phát triển)
   │  ├─ Left side: SeatMapControl scrollable, auto-center
   │  ├─ Right side:
   │  │  ├─ Thông tin phim/suất: tên, giờ, phòng, giá
   │  │  ├─ Ghế đã chọn: list ghế (A1, A2, ...)
   │  │  ├─ Ô tìm KH: SĐT / Mã TV + nút 🔍 tra cứu
   │  │  │  ├─ Nếu tìm thấy → hiển thị tên + hạng + điểm [Phase 8]
   │  │  │  └─ Nếu không → để trống (tiền mặt)
   │  │  ├─ Tổng tiền vé (realtime)
   │  │  └─ Nút: Quay lại, Tiếp tục → UcSnackOrder
   │  ├─ SaleOrderState state:
   │  │  ├─ ShowtimeId, SelectedSeatIds[], CustomerId (optional)
   │  │  └─ Truyền qua UcSnackOrder
   │  └─ Event: BackRequested(), NextClicked()

3. Snack Order (Bắp nước):
   ├─ UcSnackOrder.cs (Hưng phát triển, Phase 5)
   │  ├─ Top: Bộ lọc: All | Food | Drink | Combo
   │  ├─ Left side: Menu sản phẩm dạng FlowLayoutPanel
   │  │  ├─ Card snack: hình (nếu có) + tên + giá + nút +/-
   │  │  └─ Click ± → thêm/bớt vào giỏ
   │  ├─ Right side:
   │  │  ├─ Thông tin đơn:
   │  │  │  ├─ Tổng tiền vé: XXX,XXX đ
   │  │  │  ├─ Tổng tiền bắp: XXX,XXX đ
   │  │  │  ├─ Tổng bill: XXX,XXX đ
   │  │  │  └─ Mã giảm giá (nếu có): -XX,XXX đ
   │  │  ├─ Giỏ hàng DataGridView:
   │  │  │  ├─ Col: Tên | Số lượng | Đơn giá | Thành tiền | Xóa
   │  │  │  └─ Nút: +, - mỗi dòng
   │  │  └─ Nút: Quay lại, Thanh toán
   │  └─ Event: BackRequested(), CheckoutClicked()

4. Checkout (Thanh toán):
   ├─ UcSnackOrder checkout handler (Hưng phát triển)
   │  ├─ Dialog tóm tắt:
   │  │  ├─ Phim, Ghế, Bắp nước, Tổng
   │  │  ├─ Ô nhập tiền nhận (nếu tiền mặt)
   │  │  ├─ Tính tiền thối (nếu tiền mặt)
   │  │  └─ Nút: Xác nhận, Hủy
   │  ├─ Gọi InvoiceService.CreateAsync(invoice, tickets, snacks)
   │  │  ├─ Invoice lưu CustomerId (nếu có), ShiftId (nếu có)
   │  │  ├─ Lưu VoucherId (nếu dùng mã giảm giá)
   │  │  ├─ Transaction double-check ghế
   │  │  └─ Auto tính điểm & nâng hạng (Phase 8)
   │  ├─ Thông báo: "Lưu thành công! 🎁 Tích được X điểm"
   │  └─ Quay lại NowShowing

5. SaleOrderState.cs (State object)
   ├─ Giữ dữ liệu giữa UcNowShowing → UcSeatSelection → UcSnackOrder
   └─ Fields:
      ├─ ShowtimeId
      ├─ SelectedSeatIds[]
      ├─ SelectedSnacks[{snackId, qty}]
      ├─ CustomerId (optional, Phase 8)
      └─ BookingId (optional, Phase 9)
```

**🔲 Cần làm (Phase 8, 11, 12):**

```csharp
6. Customer Lookup (Phase 8):
   ├─ Chỉnh UcSeatSelection ô tìm KH:
   │  ├─ Gọi CustomerService.GetByPhoneAsync(phone)
   │  ├─ Hoặc GetByMemberCodeAsync(memberCode)
   │  ├─ Hiển thị: Tên + Hạng + Điểm hiện tại
   │  └─ State: SaleOrderState.CustomerId = customerId
   └─ Dùng BarcodeHelper để phát hiện scanner (gõ nhanh → Enter)

7. Shift Panel (Phase 11):
   ├─ UcShiftPanel.cs
   │  ├─ Khi staff đăng nhập → kiểm tra ca:
   │  │  ├─ Nếu chưa mở → bắt buộc form mở ca trước
   │  │  ├─ Form mở ca: nhập tiền đầu ca
   │  │  └─ After success → hiển thị badge "Ca đang mở từ HH:mm"
   │  ├─ Nút "Chốt ca" ở header/sidebar:
   │  │  ├─ Dialog: hiển thị ExpectedCash (hệ thống tính)
   │  │  ├─ Ô nhập ActualCash (tiền thực tế)
   │  │  ├─ Sau nhập → show "Cân bằng / Dư / Thiếu"
   │  │  └─ Nút Xác nhận chốt ca
   │  └─ Mọi Invoice tạo trong ca → tự động ShiftId = currentShift
   └─ Gọi ShiftService.OpenShiftAsync(), CloseShiftAsync()

8. Check-in / Soát vé (Phase 12):
   ├─ UcCheckIn.cs
   │  ├─ Ô nhập "Mã đặt chỗ"
   │  ├─ Support: scan barcode hoặc gõ tay
   │  ├─ Sau nhập → gọi BookingService.GetByCodeAsync()
   │  ├─ Hiển thị: Phim | Giờ | Ghế | Trạng thái
   │  ├─ Nút "Xác nhận vào rạp" → UpdateStatusAsync(bookingId, CheckedIn)
   │  └─ Nút "In vé vật lý" → gọi PrintHelper
   └─ Gọi HardwareHelper.OpenCashDrawer() (giả lập)

9. Staff Loyalty Features (Phase 8):
   ├─ Sau checkout, tự động:
   │  ├─ Gọi CustomerService.AddSpendingAsync(customerId, amount)
   │  ├─ Auto nâng hạng (Standard → VIP → Diamond)
   │  ├─ Gọi PointService.EarnPointsAsync(customerId, invoiceId, amount)
   │  └─ Hiển thị "🎁 Tích được X điểm!" notification
   └─ Thẻ thành viên: scan QR → lookup KH
```

#### **Integration vào FrmMain:**

```csharp
// Sidebar menu (Phase 3, 8, 11, 12)
"🛍️ Bán vé" → LoadModule("Sell") → UcNowShowing
  └─ Workflow: NowShowing → SeatSelection → SnackOrder → Checkout → NowShowing

"📋 Soát vé" → LoadModule("CheckIn") → UcCheckIn [Phase 12]

"⏱️ Ca làm việc" → UcShiftPanel (integrated, chỉ show khi open/close) [Phase 11]
```

#### **Timeline:**

- Phase 3: UcNowShowing, UcSeatSelection, SaleOrderState
- Phase 5: UcSnackOrder, checkout integration
- Phase 5.5: Test end-to-end
- Phase 8: Customer lookup, loyalty integration
- Phase 11: Shift management
- Phase 12: Check-in, hardware integration

**Người phụ thuộc:** Mạnh (SeatMapControl GDI+), Duy (services), Sáng (Customer/Booking models)

---

### 4️⃣ SÁNG — Customer Module (E-Commerce/Self-Service) (20%)

**🎖️ Tiêu đề:** Customer Experience Developer  
**📍 Vị trí:** Xây dựng Storefront & e-commerce, Customer Shell

#### **Trách Nhiệm Chính:**

- Xây dựng FrmCustomerMain (Customer Shell riêng biệt)
- Phát triển Storefront (trang chủ, duyệt phim, đặt vé online)
- Tích hợp Loyalty (điểm, hạng, voucher)
- Xử lý luồng thanh toán online

#### **Customer Forms & Controls (Sáng phát triển):**

**🔲 Cần làm (Phase 9-10):**

```csharp
1. FrmCustomerMain.cs (Phase 9) — Customer Shell
   ├─ FormBorderStyle.None, dark theme, draggable (như FrmMain)
   ├─ Top NavBar (height ~60px):
   │  ├─ Left: Logo + "CineManager"
   │  ├─ Center: Menu buttons
   │  │  ├─ "🏠 Trang chủ"
   │  │  ├─ "🎬 Phim Đang Chiếu"
   │  │  └─ "🎟️ Lịch Sử Vé"
   │  └─ Right: Avatar + CustomerName dropdown ▼
   │     ├─ "👤 Hồ sơ"
   │     ├─ "💎 Điểm thưởng"
   │     └─ "🚪 Đăng xuất"
   ├─ Content panel (Dock=Fill) swap UserControl
   ├─ Routing: SessionManager.CurrentCustomer → show UI for customer
   └─ Events: navigate between UC modules

2. UcStorefront.cs (Phase 9) — Trang chủ khách hàng
   ├─ Hero Slider:
   │  ├─ Auto-rotate poster phim nổi bật (5s)
   │  ├─ Manual arrows ← | →
   │  └─ Click poster → UcMovieDetail
   ├─ Section "🔥 Phim Đang Hot":
   │  ├─ FlowLayoutPanel card phim:
   │  │  ├─ Poster nhỏ + tên + rating + duration
   │  │  └─ Click → UcMovieDetail
   │  └─ HorizontalScroll
   ├─ Section "📅 Phim Sắp Chiếu":
   │  ├─ Similar, nhưng có tag "Coming Soon"
   │  └─ Disable click (chưa có lịch chiếu)
   └─ Section "🎊 Khuyến mãi hôm nay" [Phase 10]:
      ├─ List voucher active
      └─ Click → detail

3. UcMovieDetail.cs (Phase 9) — Chi tiết phim
   ├─ Layout:
   │  ├─ Left: Poster lớn (400x600px)
   │  └─ Right: Thông tin
   ├─ Thông tin phim:
   │  ├─ Tên phim (đậm, to)
   │  ├─ Đạo diễn, Diễn viên
   │  ├─ Thể loại (pill badges)
   │  ├─ Thời lượng, Rating tuổi
   │  ├─ Mô tả (text wrap)
   │  └─ Nút "▶ Xem Trailer" → mở browser TrailerUrl
   ├─ Danh sách suất chiếu:
   │  ├─ DatePicker chọn ngày
   │  ├─ Tái sử dụng logic `UcNowShowing`
   │  ├─ Suất button: giờ | phòng | ghế còn | giá
   │  └─ Click suất → UcCustomerBooking
   └─ Nút "← Quay lại" → UcStorefront

4. UcCustomerBooking.cs (Phase 9) — Luồng đặt vé
   ├─ Flow: Chọn ghế → Chọn bắp nước → Thanh toán
   ├─ Step 1: Seat Selection
   │  ├─ Tái sử dụng SeatMapControl nhưng Mode = CustomerMode
   │  ├─ CustomerMode: UI thân thiện hơn, nút lớn, màu sắc xanh lá
   │  └─ Thông tin bên phải: phim, giờ, ghế, giá
   ├─ Step 2: Snack Order
   │  ├─ Tái sử dụng UcSnackOrder nhưng Mode = CustomerMode
   │  └─ CustomerMode: ẩn thông tin nhạy cảm
   ├─ Step 3: Review + Payment
   │  └─ → UcPaymentGateway
   └─ State: SaleOrderState (reuse từ Staff bán hàng)

5. UcPaymentGateway.cs (Phase 9) — Thanh toán
   ├─ Hiển thị tóm tắt đơn:
   │  ├─ Phim | Ghế | Bắp nước
   │  ├─ Tổng tiền
   │  ├─ Voucher áp dụng (nếu có): -XX,XXX đ
   │  └─ Tổng cuối cùng
   ├─ Ô nhập "Mã giảm giá":
   │  ├─ Field + nút "Áp dụng"
   │  ├─ Gọi VoucherService.ValidateAsync()
   │  ├─ Nếu valid → show discount amount
   │  └─ Nếu invalid → show error
   ├─ Hiển thị QR Code:
   │  ├─ Giả lập Momo/VNPay QR (dùng BarcodeHelper.GenerateQrCode())
   │  ├─ Hiển thị "Quét QR để thanh toán"
   │  ├─ Giả lập: có checkbox "Đã thanh toán thành công"
   │  └─ Timer 10s countdown
   ├─ Auto thanh toán sau 10s (hoặc manual click)
   ├─ Nếu thanh toán thành công:
   │  ├─ Tạo Booking (Status=Paid)
   │  ├─ Sinh BookingCode (dùng BarcodeHelper.GenerateBookingCode())
   │  ├─ Show success animation (confetti/checkmark)
   │  ├─ Display "Booking Code: BK-XXXXXX"
   │  ├─ Nút "Xem vé của tôi" → UcMyTickets
   │  └─ Auto redirect sau 5s
   └─ Gọi BookingService.CreateAsync()

6. UcMyTickets.cs (Phase 9) — Lịch sử vé
   ├─ Card list vé:
   │  ├─ Poster nhỏ + phim + ngày giờ + ghế
   │  ├─ Hạng ghế (Standard/VIP/Couple)
   │  ├─ Trạng thái: Sắp tới | Đã xem | Hủy (với icon/màu)
   │  └─ Click card → detail popup
   ├─ Detail popup:
   │  ├─ Thông tin chi tiết
   │  ├─ Mã đặt chỗ (BookingCode)
   │  ├─ Hiển thị QR code của BookingCode
   │  ├─ Nút "In vé" → PrintHelper [Phase 12]
   │  └─ Nút "Hủy vé" [Phase 12]
   ├─ Bộ lọc: Sắp tới | Đã xem | Đã hủy
   └─ List pagination nếu > 20 vé

7. UcMyProfile.cs (Phase 9) — Hồ sơ khách hàng
   ├─ Tab 1: Thông tin cá nhân
   │  ├─ Fields: Tên, SĐT, Email (read-only hoặc editable)
   │  ├─ Nút "Sửa thông tin"
   │  └─ Nút "Đổi mật khẩu" → dialog
   ├─ Tab 2: Thẻ thành viên
   │  ├─ MemberCode hiển thị dạng QR code (BarcodeHelper)
   │  ├─ Tên thành viên + hạng (Badge: Standard | VIP | Diamond)
   │  ├─ Điểm hiện tại: X điểm
   │  ├─ Tổng chi tiêu: X.XXX.XXX đ
   │  ├─ Progress bar: còn Y đ để lên hạng tiếp theo
   │  └─ Lịch sử nâng hạng (timeline)
   ├─ Tab 3: Điểm thưởng
   │  ├─ Điểm hiện tại + tương đương tiền
   │  ├─ Nút "Đổi điểm thưởng" → hiển thị exchange options
   │  └─ Lịch sử tích/đổi điểm (paginated)
   └─ Nút "Đăng xuất"

8. Integration: FrmLogin → Customer Registration (Phase 9):
   ├─ Thêm nút "Chưa có tài khoản? Đăng ký ngay"
   └─ → DlgCustomerRegister (Sáng phát triển)
      ├─ Fields: Họ tên, Email, SĐT, Mật khẩu, Xác nhận MK
      ├─ Validate: email unique, phone format, password strength
      └─ Gọi CustomerService.RegisterAsync()
```

#### **Voucher Integration (Phase 10):**

```csharp
9. UcVoucherBrowser.cs (Optional, Phase 10):
   ├─ Hiển thị tất cả voucher active
   ├─ Card: code | loại | giá trị | ngày HH | nút "Sao chép"
   └─ Copy to clipboard → dùng trong checkout

10. Tích hợp voucher vào UcPaymentGateway:
    ├─ Ô nhập mã voucher
    ├─ Gọi VoucherService.ValidateAsync(code, orderTotal)
    ├─ Nếu valid → apply, show discount
    ├─ State: SaleOrderState.VoucherId
    └─ InvoiceService.CreateAsync() lưu VoucherId
```

#### **Integration vào FrmMain:**

```csharp
// FrmLogin (Phase 9):
Nút "Đăng ký" → DlgCustomerRegister
  ├─ Gọi CustomerService.RegisterAsync()
  └─ Auto sinh MemberCode, set Tier=Standard, Points=0

// Routing từ FrmLogin (Phase 9):
if (user != null) // User (Admin/Staff)
  → SessionManager.Login(user)
  → new FrmMain().Show()
else if (customer != null) // Customer
  → SessionManager.LoginAsCustomer(customer)
  → new FrmCustomerMain().Show()
```

#### **Timeline:**

- Phase 7: CustomerService + PointService (Duy làm, Sáng dùng để develop)
- Phase 8: PointService hoàn thiện, Sáng prep Storefront
- Phase 9: FrmCustomerMain, UcStorefront, UcMovieDetail, UcCustomerBooking, UcPaymentGateway, UcMyTickets, UcMyProfile
- Phase 10: Voucher integration (với Duy làm VoucherService)

**Người phụ thuộc:** Duy (CustomerService, BookingService, VoucherService), Mạnh (SeatMapControl, theme customer-friendly), Hưng (SaleOrderState, SeatMapControl reuse)

---

### 5️⃣ MẠNH — UI/UX, Graphics & Hardware Testing (10%)

**🎖️ Tiêu đề:** UI/Graphics & QA Engineer  
**📍 Vị trí:** Custom graphics, theming, hardware integration, quality assurance

#### **Trách Nhiệm Chính:**

- Phát triển custom UI controls (GDI+)
- Setup & maintain design system
- Tích hợp phần cứng (giả lập: scanner, printer, drawer)
- End-to-end testing & quality assurance

#### **Custom Controls & Graphics (Mạnh phát triển):**

**✅ Hoàn thành (Phase 1-5.5):**

```csharp
1. SeatMapControl.cs (Phase 3, 5.5)
   ├─ Kế thừa từ Control, double-buffered, anti-aliased
   ├─ OnPaint: vẽ ghế bằng Graphics.DrawRoundedRectangle()
   ├─ 5 màu sắc:
   │  ├─ Trống: Color.LightGray
   │  ├─ Đang chọn: Color.FromArgb(76, 175, 80) [xanh lá]
   │  ├─ Đã bán: Color.Red
   │  ├─ VIP: Color.Gold
   │  └─ Couple: Color.Pink
   ├─ Hit-test: OnMouseDown → tìm ghế tại (x, y)
   ├─ Hover tooltip: "A1 • VIP • 150,000đ"
   ├─ Events: SeatSelectionChanged
   ├─ Vẽ "MÀN HÌNH" gradient ở trên cùng
   ├─ Row labels (A, B, C...) bên trái, auto-center
   ├─ Legend ở dưới (5 màu + nhãn)
   └─ Scrollable (lớn > panel)

2. SeatLayoutPreviewControl.cs (Phase 4.5, 5.5)
   ├─ Read-only preview (không thể click)
   ├─ Vẽ ghế nhỏ hơn, compact
   ├─ Hiển thị mã ghế đầy đủ: "H4", "J9"
   ├─ Đọc tọa độ grid: GridRow, GridColumn, GridSpan
   ├─ Render flexible layout (không đều như ma trận)
   ├─ Màu phân biệt: Standard/VIP/Couple
   └─ Dùng trong DlgRoomEdit preview
```

**🔲 Cần làm (Phase 7-12):**

```csharp
3. Theme & Design System (Phase 1-13, ongoing)
   ├─ AdminTheme.cs (Đức setup, Mạnh maintain)
   │  ├─ Design tokens:
   │  │  ├─ Colors:
   │  │  │  ├─ Primary (tím): #6450FF
   │  │  │  ├─ Secondary (xanh): #00BCD4
   │  │  │  ├─ Success: #4CAF50
   │  │  │  ├─ Danger: #F44336
   │  │  │  └─ Warning: #FF9800
   │  │  ├─ Font: Segoe UI 9-14pt
   │  │  ├─ Spacing: 8px base unit
   │  │  ├─ BorderRadius: 4px default
   │  │  └─ Shadow: subtle drop shadow
   │  └─ Update khi cần polish
   ├─ CustomerTheme (Phase 9):
   │  ├─ Bright colors: xanh lá, vàng, trắng, cam
   │  ├─ Friendly fonts: Arial, sans-serif
   │  └─ More rounded, playful feel

4. BarcodeHelper.cs (Phase 8, 12)
   ├─ GenerateQrCode(content) → Bitmap
   │  ├─ Dùng QRCoder library
   │  ├─ Return Bitmap để draw vào PictureBox hoặc GDI+
   │  └─ Example: BarcodeHelper.GenerateQrCode("CM-ABC123")
   ├─ GenerateQrCode(content, width, height) → fixed size Bitmap
   ├─ GenerateBookingCode() → "BK-" + Random 6 ký tự
   ├─ EnableBarcodeMode(TextBox target)
   │  ├─ Bắt KeyDown events
   │  ├─ Phát hiện scanner: gõ nhanh (< 50ms/ký tự) + Enter
   │  ├─ Nếu detect scanner → fire event BarcodeScanned
   │  └─ Nếu gõ tay → cho phép edit bình thường
   └─ Scanner test: simulate by typing "CM-ABC123" + Enter

5. HardwareHelper.cs (Phase 12, giả lập)
   ├─ PrintReceipt(invoiceData) → giả lập ESC/POS
   │  ├─ Build template string (ESC/POS commands)
   │  ├─ Dùng QuestPDF export PDF (fallback)
   │  └─ Show MessageBox "In hoàn tất" (nếu không có máy in)
   ├─ OpenCashDrawer() → giả lập kick drawer
   │  └─ Show MessageBox "Mở ngăn tiền mặt" (chỉ simulate)
   └─ Placeholder cho future real hardware

6. Hardware Integration (Phase 12):
   ├─ Barcode Scanner (giả lập):
   │  ├─ BarcodeHelper.EnableBarcodeMode(textBox)
   │  ├─ Phát hiện scan: gõ nhanh + Enter
   │  └─ Fire event để UI handle
   ├─ Thermal Printer (giả lập):
   │  ├─ HardwareHelper.PrintReceipt(data)
   │  └─ Export PDF + show preview
   └─ Cash Drawer (giả lập):
      ├─ HardwareHelper.OpenCashDrawer()
      └─ Show visual feedback

7. Form Customization (Phase 1-13):
   ├─ FrmLogin: borderless, dark, draggable, custom close button
   ├─ FrmMain: custom title bar, window controls
   ├─ FrmCustomerMain: similar to FrmMain, bright theme [Phase 9]
   └─ All dialogs: consistent styling
```

#### **Testing & QA:**

```
✅ Unit Testing:
  ├─ Services: AuthService, MovieService, ShowtimeService, etc.
  └─ Helpers: BarcodeHelper, HardwareHelper

✅ Integration Testing (Mạnh lead):
  ├─ Scenario 1: Admin CRUD Phim
  ├─ Scenario 2: Staff bán vé (full workflow)
  ├─ Scenario 3: Customer đặt vé online
  ├─ Scenario 4: Loyalty & Voucher
  └─ Scenario 5: Shift management

✅ Performance Testing:
  ├─ SeatMapControl rendering 240 ghế → smooth?
  ├─ DataGridView 1000+ rows → responsive?
  ├─ Invoice query 10,000 records → fast?
  └─ UI responsive khi async loading?

✅ Cross-Browser / UI Testing:
  ├─ Different screen resolutions (1024x768, 1920x1080, tablet)
  ├─ Dark theme vs. bright theme
  ├─ Responsive layouts
  └─ DPI scaling (100%, 125%, 150%)

✅ Hardware Testing (giả lập):
  ├─ Barcode scanner: simulate by typing
  ├─ Printer: generate PDF
  └─ Cash drawer: show MessageBox

✅ Edge Cases:
  ├─ Double-checkout race condition (2 staff bán ghế sát thời điểm)
  ├─ Ghế đã bán nhưng customer vẫn chọn
  ├─ Hạn hóa đơn trùng (tích điểm, voucher, snack)
  ├─ Shift chốt trước khi lưu HĐ
  └─ Customer chuyển hạng khi mua lần thứ N
```

#### **Documentation (Mạnh + all members):**

```markdown
docs/
├─ ARCHITECTURE.md — kiến trúc toàn hệ
├─ API_DOCUMENTATION.md — services & helpers
├─ UI_GUIDELINES.md — theme, layout, components
├─ TEAM_ASSIGNMENT.md ← đây
├─ DEPLOYMENT.md — cài đặt, chạy app
├─ TESTING_CHECKLIST.md — kiểm thử
└─ USER_GUIDE.md — hướng dẫn từng role
```

#### **Timeline:**

- Phase 1: FrmLogin, theme setup
- Phase 3: SeatMapControl GDI+
- Phase 4.5: SeatLayoutPreviewControl, admin layout polish
- Phase 5.5: Refactor GDI+ performance
- Phase 8: BarcodeHelper (QRCoder)
- Phase 9: FrmCustomerMain, customer theme
- Phase 12: HardwareHelper (scanner, printer, drawer)
- Phase 13: End-to-end testing, documentation, final polish

**Người phụ thuộc:** Tất cả (dùng custom controls & theme)

---

## 🔄 Dependencies & Collaboration Matrix

```
Duy (Backend Core)
  ├─> Đức (Admin) — gọi ReportService, CRUD services
  ├─> Hưng (Staff) — gọi TicketService, InvoiceService, ShiftService
  ├─> Sáng (Customer) — gọi BookingService, VoucherService
  └─> Mạnh (QA) — cấu hình SessionManager, AppConfig

Đức (Admin)
  ├─> Duy — phụ thuộc vào services
  ├─> Mạnh — dùng AdminTheme, shared controls
  └─> Hưng/Sáng — integrate customer/shift features

Hưng (Staff)
  ├─> Mạnh — dùng SeatMapControl, theme
  ├─> Duy — services
  ├─> Sáng — SaleOrderState, loyalty features
  └─> Đức — UI reference

Sáng (Customer)
  ├─> Duy — services (Customer, Booking, Voucher)
  ├─> Mạnh — theme customer, BarcodeHelper
  ├─> Hưng — SeatMapControl reuse
  └─> Đức — UI patterns

Mạnh (UI/QA)
  ├─> Duy — SessionManager, Config
  ├─> Đức — Admin UI patterns
  ├─> Hưng — test staff workflow
  └─> Sáng — test customer workflow
```

---

## 📅 Milestone Roadmap

### **Milestone 1: Foundation** (1 tuần)

- ✅ Phase 1-2 hoàn thành: Base models, auth, admin CRUD
- [ ] Verify: Admin có thể CRUD phim, phòng, lịch, nhân viên

### **Milestone 2: Staff Module** (1 tuần)

- ✅ Phase 3-5.5 hoàn thành: Bán vé, bắp nước, thống kê
- [ ] Verify: Staff có thể bán vé full flow (ghế → bắp nước → checkout)

### **Milestone 3: CRM + Loyalty** (1 tuần)

- [ ] Phase 7-8 hoàn thành: Customer models, loyalty points
- [ ] Verify: Staff lookup KH, tích điểm, nâng hạng

### **Milestone 4: Customer Shell** (1-2 tuần)

- [ ] Phase 9-10 hoàn thành: Storefront, online booking, voucher
- [ ] Verify: Customer đăng ký → đặt vé → thanh toán QR → check-in

### **Milestone 5: Shift + Hardware** (1 tuần)

- [ ] Phase 11-12 hoàn thành: Ca làm việc, soát vé, barcode
- [ ] Verify: Staff mở/chốt ca, soát vé, in ticket

### **Milestone 6: Polish & Release** (3-5 ngày)

- [ ] Phase 13: Testing, seed data, documentation
- [ ] Go-live!

**Tổng: ~6-8 tuần** (nếu quanh đó 5 người làm fulltime)

---

## 🎯 Ground Rules

1. **Commit & PR Strategy:**
    - Branch naming: `feature/{name}-{module}` (e.g., `feature/duy-auth-service`)
    - PR description: module, changes, testing steps
    - Require 1 review trước merge vào `develop`

2. **Daily Standup** (optional, 10 min):
    - Ai làm gì hôm nay?
    - Có blocker không?
    - Cần help từ ai?

3. **Code Review Standard:**
    - Check logic, null safety, naming conventions
    - Test locally trước approve
    - Comment friendly, constructive

4. **Conflict Resolution:**
    - Database schema: Duy quyết định
    - UI/UX: Mạnh + Đức + Sáng vote
    - Business logic: Duy + người phụ trách module

5. **Backup & Knowledge Sharing:**
    - Documentation cập nhật realtime
    - Code comments nếu logic phức tạp
    - Weekly tech talk ~ 30 min nếu cần

---

## 📞 Liên Hệ & Support

- **Duy (Backend):** Questions về DB schema, services logic, migrations
- **Đức (Admin):** Questions về admin UI, dashboard, forms
- **Hưng (Staff):** Questions về bán vé workflow, checkout, shift
- **Sáng (Customer):** Questions về storefront, booking, loyalty
- **Mạnh (UI/QA):** Questions về design, theme, testing, hardware

---

**Chúc may mắn! 🚀**  
Dự án này là cơ hội tốt để 5 người học hỏi fullstack development.  
Hãy communication tốt, help lẫn nhau, và deliver kịp thời!
