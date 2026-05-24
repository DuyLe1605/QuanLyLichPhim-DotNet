# Bugfix Requirements Document

## Introduction

Ứng dụng WinForms CineManager (.NET) có 4 màn hình Admin/Staff bị lỗi chồng chéo UI (layout overlap). Các panel chi tiết bên phải, biểu đồ thống kê và các bảng dữ liệu bị tràn ra ngoài vùng hiển thị, chồng lên nhau hoặc không xếp đúng thứ tự dọc. Lỗi xảy ra do các `RowStyle` trong `TableLayoutPanel` sử dụng kích thước tuyệt đối (`SizeType.Absolute`) cố định không đủ chứa nội dung thực tế, đồng thời thiếu `AutoScroll` hoặc `SizeType.Percent` cho các vùng cần co giãn.

Các màn hình bị ảnh hưởng:
1. **UcInvoiceManagement** – Panel chi tiết hóa đơn bên phải (sơ đồ ghế + bảng vé + bảng món ăn)
2. **UcCustomerManagement** – Panel chi tiết khách hàng bên phải (QR code + thông tin + lịch sử điểm)
3. **UcDashboard** – Khu vực biểu đồ bottom (Top 5 phim + Tỷ lệ lấp đầy phòng)
4. **UcSnackOrder** – Panel giỏ hàng bên phải (Staff)

---

## Bug Analysis

### Current Behavior (Defect)

1.1 WHEN màn hình `UcInvoiceManagement` được hiển thị với chiều cao cửa sổ bình thường (< 900px) THEN `CreateDetailPanel()` render các row với tổng chiều cao cố định (34 + 128 + 30 + 240 + 32 + Percent54 + Percent46) vượt quá không gian thực tế, khiến `dgvTickets` và `dgvSnacks` bị chồng lên nhau hoặc bị cắt mất

1.2 WHEN màn hình `UcInvoiceManagement` hiển thị panel chi tiết bên phải THEN `seatPreview` (row 3, Absolute 240px) chiếm quá nhiều chiều cao cố định, đẩy `dgvTickets` (row 5) và `dgvSnacks` (row 6) ra ngoài vùng nhìn thấy mà không có scrollbar

1.3 WHEN màn hình `UcCustomerManagement` được hiển thị THEN `CreateDetailPanel()` dùng tọa độ tuyệt đối (`Location = new Point(x, y)`) để đặt các control (picQr, lblCustName, lblCustInfo, dgvHistory) nhưng panel có `AutoScroll = true` mà không có `AutoScrollMinSize`, khiến nội dung bị tràn ra ngoài và QR code chồng lên thông tin khách hàng khi panel bị thu hẹp

1.4 WHEN màn hình `UcCustomerManagement` hiển thị panel chi tiết với chiều rộng nhỏ hơn 350px THEN `lblCustName` (Size 200px) và `lblCustInfo` (Size 200px) bị cắt text vì kích thước cố định không co giãn theo chiều rộng panel

1.5 WHEN màn hình `UcDashboard` được hiển thị với chiều rộng >= 900px THEN `ApplyResponsiveLayout()` gán `root.RowStyles[5].Height = 326` cho `bottomGrid`, nhưng `bottomGrid` dùng `RowStyle(SizeType.Percent, 100)` cho row 1 (chứa biểu đồ) – khi `root` không có chiều cao đủ lớn, biểu đồ `chartTopMovies` và `chartOccupancy` bị render với chiều cao 0 hoặc chồng lên nhau

1.6 WHEN màn hình `UcDashboard` được hiển thị với chiều rộng < 900px THEN `ApplyResponsiveLayout()` chuyển sang layout 1 cột với 4 rows nhưng `_sectionTitles` chỉ có index 1 và 2 (được thêm vào `bottomGrid` lúc khởi tạo), trong khi `_sectionTitles[0]` là label "Doanh thu" thuộc `root` – truy cập sai index gây `IndexOutOfRangeException` hoặc hiển thị sai label

1.7 WHEN màn hình `UcSnackOrder` được hiển thị với chiều cao cửa sổ nhỏ (< 700px) THEN `CreateCartSide()` tạo `TableLayoutPanel` với 9 rows có tổng chiều cao cố định (40 + 72 + Percent100 + 34 + 34 + 42 + 38 + 36 + 54 = ~350px cố định + phần Percent) nhưng không có `AutoScroll`, khiến `btnCheckout` bị cắt mất ở phía dưới

### Expected Behavior (Correct)

2.1 WHEN màn hình `UcInvoiceManagement` được hiển thị THEN hệ thống SHALL hiển thị panel chi tiết bên phải với `seatPreview`, `dgvTickets` và `dgvSnacks` xếp dọc không chồng lên nhau, trong đó `seatPreview` có chiều cao tối thiểu hợp lý và hai bảng dữ liệu chia sẻ phần không gian còn lại theo tỷ lệ `Percent`

2.2 WHEN màn hình `UcInvoiceManagement` hiển thị panel chi tiết bên phải THEN hệ thống SHALL đảm bảo `dgvTickets` và `dgvSnacks` luôn hiển thị đầy đủ trong vùng nhìn thấy, hoặc panel có `AutoScroll = true` với `AutoScrollMinSize` phù hợp để người dùng có thể cuộn xem

2.3 WHEN màn hình `UcCustomerManagement` được hiển thị THEN hệ thống SHALL hiển thị panel chi tiết với QR code, thông tin khách hàng và lịch sử điểm xếp dọc không chồng lên nhau, sử dụng layout manager thay vì tọa độ tuyệt đối

2.4 WHEN màn hình `UcCustomerManagement` hiển thị panel chi tiết với bất kỳ chiều rộng nào THEN hệ thống SHALL đảm bảo text không bị cắt và các control co giãn theo chiều rộng panel thực tế

2.5 WHEN màn hình `UcDashboard` được hiển thị với chiều rộng >= 900px THEN hệ thống SHALL hiển thị `chartTopMovies` và `chartOccupancy` cạnh nhau theo layout 2 cột với chiều cao đủ lớn để biểu đồ render đúng

2.6 WHEN màn hình `UcDashboard` được hiển thị với chiều rộng < 900px THEN hệ thống SHALL hiển thị `chartTopMovies` và `chartOccupancy` xếp dọc theo layout 1 cột mà không gây lỗi index và không chồng lên nhau

2.7 WHEN màn hình `UcSnackOrder` được hiển thị với bất kỳ chiều cao cửa sổ nào THEN hệ thống SHALL đảm bảo `btnCheckout` luôn hiển thị và có thể nhấn được, hoặc panel có `AutoScroll` để người dùng cuộn tới nút thanh toán

### Unchanged Behavior (Regression Prevention)

3.1 WHEN người dùng chọn một hóa đơn trong `UcInvoiceManagement` THEN hệ thống SHALL CONTINUE TO tải và hiển thị đúng thông tin chi tiết hóa đơn, sơ đồ ghế, danh sách vé và danh sách món ăn

3.2 WHEN người dùng chọn một khách hàng trong `UcCustomerManagement` THEN hệ thống SHALL CONTINUE TO tải và hiển thị đúng QR code, thông tin cá nhân, thống kê điểm và lịch sử điểm thưởng

3.3 WHEN `UcDashboard` tải dữ liệu THEN hệ thống SHALL CONTINUE TO hiển thị đúng các stat cards, biểu đồ doanh thu, top 5 phim và tỷ lệ lấp đầy phòng với dữ liệu thực từ database

3.4 WHEN `ApplyResponsiveLayout()` trong `UcDashboard` được gọi THEN hệ thống SHALL CONTINUE TO chuyển đổi đúng giữa layout 2 cột (>= 900px) và layout 1 cột (< 900px) cho stat cards

3.5 WHEN nhân viên thực hiện thanh toán trong `UcSnackOrder` THEN hệ thống SHALL CONTINUE TO xử lý đúng logic tính tiền, tích điểm và tạo hóa đơn

3.6 WHEN `UcInvoiceManagement` hoặc `UcCustomerManagement` được resize THEN hệ thống SHALL CONTINUE TO gọi `ApplyInvoiceLayout()` / `ApplyCustomerSplitLayout()` và điều chỉnh `SplitterDistance` đúng cách

3.7 WHEN `SeatLayoutPreviewControl` được cung cấp dữ liệu ghế THEN hệ thống SHALL CONTINUE TO vẽ đúng sơ đồ ghế với scale tự động theo kích thước control

---

## Bug Condition Pseudocode

**Bug Condition Function** – Xác định các trường hợp kích hoạt lỗi layout:

```pascal
FUNCTION isBugCondition(screen, windowHeight, windowWidth)
  INPUT: screen ∈ {InvoiceManagement, CustomerManagement, Dashboard, SnackOrder}
         windowHeight: integer (pixel)
         windowWidth: integer (pixel)
  OUTPUT: boolean

  IF screen = InvoiceManagement AND windowHeight < 900 THEN
    RETURN true  // detail panel rows overflow
  END IF

  IF screen = CustomerManagement AND windowWidth < 400 THEN
    RETURN true  // absolute-positioned controls overflow
  END IF

  IF screen = Dashboard AND windowWidth >= 900 THEN
    RETURN true  // bottomGrid Percent row has no absolute height anchor
  END IF

  IF screen = Dashboard AND windowWidth < 900 THEN
    RETURN true  // _sectionTitles index mismatch
  END IF

  IF screen = SnackOrder AND windowHeight < 700 THEN
    RETURN true  // btnCheckout clipped
  END IF

  RETURN false
END FUNCTION
```

**Property: Fix Checking**
```pascal
FOR ALL (screen, windowHeight, windowWidth) WHERE isBugCondition(screen, windowHeight, windowWidth) DO
  result ← render(screen, windowHeight, windowWidth)
  ASSERT noOverlap(result.controls)
  ASSERT noClipping(result.controls)
  ASSERT allVisible(result.criticalControls)
END FOR
```

**Property: Preservation Checking**
```pascal
FOR ALL (screen, windowHeight, windowWidth) WHERE NOT isBugCondition(screen, windowHeight, windowWidth) DO
  ASSERT render_fixed(screen) = render_original(screen)  // same data, same layout behavior
END FOR
```
