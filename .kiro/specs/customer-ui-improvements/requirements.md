# Requirements Document

## Introduction

Cải thiện giao diện khách hàng (Customer UI) của ứng dụng CineManager WinForms. Bao gồm: thêm trường "tên đăng nhập" (username) cho Customer, phân biệt rõ trang Trang chủ và Phim đang chiếu, gộp Điểm thưởng vào trang Hồ sơ, và sửa lỗi layout (chồng chéo text/button) trên toàn bộ màn hình khách hàng.

## Glossary

- **Customer_Model**: Entity đại diện cho khách hàng trong hệ thống, lưu trữ thông tin cá nhân, thông tin đăng nhập và thành viên.
- **Login_Form**: Form đăng nhập (FrmLogin) cho phép cả User (Admin/Staff) và Customer đăng nhập.
- **Registration_Dialog**: Dialog đăng ký tài khoản khách hàng mới (DlgCustomerRegister).
- **Customer_Main_Form**: Form chính của khách hàng (FrmCustomerMain) chứa navigation bar và content panel.
- **Home_Page**: Trang chủ (UcStorefront) hiển thị hero banner và phim nổi bật.
- **Now_Showing_Page**: Trang "Phim đang chiếu" hiển thị danh sách phim có suất chiếu hiện tại, cho phép lọc và tìm kiếm.
- **Profile_Page**: Trang hồ sơ cá nhân (UcMyProfile) hiển thị thông tin cá nhân, thẻ thành viên và điểm thưởng.
- **Loyalty_Section**: Phần hiển thị lịch sử giao dịch điểm thưởng, được tích hợp trong Profile_Page.
- **Navigation_Bar**: Thanh điều hướng nằm trên cùng của Customer_Main_Form.
- **Account_Menu**: Menu dropdown hiển thị khi click vào tên khách hàng trên Navigation_Bar.

## Requirements

### Requirement 1: Thêm trường Username cho Customer

**User Story:** As a khách hàng, I want to đăng nhập bằng tên đăng nhập (username) thay vì email, so that tôi có thể đăng nhập nhanh hơn và dễ nhớ hơn.

#### Acceptance Criteria

1. THE Customer_Model SHALL include a Username property of type string with a maximum length of 50 characters.
2. THE Customer_Model SHALL enforce uniqueness on the Username property at the database level.
3. WHEN a customer registers, THE Registration_Dialog SHALL require the customer to provide a username in addition to email, phone, full name, and password.
4. WHEN a customer enters a username during registration, THE Registration_Dialog SHALL validate that the username contains only alphanumeric characters, underscores, or dots, and is between 3 and 50 characters long.
5. IF a customer provides a username that already exists, THEN THE Registration_Dialog SHALL display an error message indicating the username is already taken.
6. WHEN a customer logs in, THE Login_Form SHALL authenticate using the username field against both User.Username and Customer.Username.
7. THE Profile_Page SHALL display the current username as a read-only field in the personal information section.

### Requirement 2: Phân biệt Trang chủ và Phim đang chiếu

**User Story:** As a khách hàng, I want to trang Trang chủ và trang Phim đang chiếu có nội dung khác nhau rõ ràng, so that tôi có thể nhanh chóng tìm phim đang chiếu hoặc khám phá nội dung nổi bật.

#### Acceptance Criteria

1. THE Home_Page SHALL display a hero banner section featuring the currently promoted movie with auto-rotation every 5 seconds.
2. THE Home_Page SHALL display a "Phim đang hot" section showing up to 6 movies with active showtimes, sorted by nearest showtime.
3. THE Home_Page SHALL display a "Phim sắp chiếu" section showing up to 6 upcoming movies sorted by release date.
4. THE Now_Showing_Page SHALL display all movies that have active showtimes in a grid layout with movie poster, title, duration, age rating, and genre.
5. THE Now_Showing_Page SHALL provide a search text box that filters movies by title in real-time as the customer types.
6. THE Now_Showing_Page SHALL provide genre filter buttons that allow the customer to filter movies by one or more genres.
7. WHEN a customer clicks on a movie card on the Now_Showing_Page, THE Customer_Main_Form SHALL navigate to the movie detail page for that movie.
8. THE Navigation_Bar SHALL use the "Trang chủ" button to navigate to the Home_Page and the "Phim đang chiếu" button to navigate to the Now_Showing_Page as separate destinations.

### Requirement 3: Gộp Điểm thưởng vào Hồ sơ

**User Story:** As a khách hàng, I want to xem thông tin điểm thưởng ngay trong trang Hồ sơ, so that tôi không cần chuyển trang để kiểm tra điểm và lịch sử giao dịch.

#### Acceptance Criteria

1. THE Profile_Page SHALL display the Loyalty_Section below the membership card area, showing the customer's current points, tier, and progress to next tier.
2. THE Loyalty_Section SHALL display a list of the 10 most recent point transactions, each showing the transaction date, description, and point change amount.
3. WHEN a point transaction adds points, THE Loyalty_Section SHALL display the point change in green color with a "+" prefix.
4. WHEN a point transaction deducts points, THE Loyalty_Section SHALL display the point change in red color with a "-" prefix.
5. THE Account_Menu SHALL remove the "Điểm thưởng" menu item, retaining only "Hồ sơ" and "Đăng xuất".

### Requirement 4: Sửa lỗi layout và cải thiện UI toàn bộ màn hình khách hàng

**User Story:** As a khách hàng, I want to giao diện hiển thị đúng, không bị chồng chéo text hay button, so that tôi có trải nghiệm sử dụng mượt mà trên mọi kích thước cửa sổ.

#### Acceptance Criteria

1. WHILE the Customer_Main_Form is resized, THE Navigation_Bar SHALL reposition all navigation buttons and the account button without overlapping.
2. WHILE the Customer_Main_Form is resized, THE Home_Page SHALL reflow movie cards to fit the available width without clipping or overlapping.
3. WHILE the Customer_Main_Form is resized, THE Profile_Page SHALL maintain proper spacing between the form panel and the membership card panel without content overflow.
4. THE Navigation_Bar SHALL use anchored or docked layout so that navigation items remain properly spaced at window widths from 1060px to maximized state.
5. THE Home_Page hero banner SHALL scale its text and poster image proportionally to the available width without text being clipped by the poster.
6. WHEN a movie card title exceeds the card width, THE Home_Page SHALL truncate the title with an ellipsis rather than overlapping adjacent elements.
7. THE Profile_Page form fields SHALL use a minimum width of 280px and expand proportionally with the panel width.
8. THE Now_Showing_Page filter bar SHALL wrap to a new line when the window width is insufficient to display all genre buttons in a single row.
