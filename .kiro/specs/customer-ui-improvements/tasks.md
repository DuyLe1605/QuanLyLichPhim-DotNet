# Implementation Plan: Customer UI Improvements

## Overview

Implement four improvements to the CineManager WinForms customer UI in C# (.NET 8):
1. Add `Username` field to the `Customer` entity, migration, registration dialog, login, and profile display.
2. Create a new customer-facing `UcNowShowing` and wire the "Phim đang chiếu" nav button to it, keeping `UcStorefront` as the Home page.
3. Merge the loyalty points history into `UcMyProfile` and remove the "Điểm thưởng" account-menu item.
4. Fix layout overlaps: nav bar `FlowLayoutPanel`, hero text clipping, profile panel overflow, and filter bar wrapping.

Each task builds on the previous ones. All changes are additive or in-place modifications to existing classes.

---

## Tasks

- [x] 1. Add Username to Customer entity and database

  - [x] 1.1 Add `Username` property to `Customer` model and configure `AppDbContext`
    - Add `public string Username { get; set; } = string.Empty;` to `Models/Customers/Customer.cs`
    - In `AppDbContext.OnModelCreating`, add `.Property(c => c.Username).HasMaxLength(50)` and `.HasIndex(c => c.Username).IsUnique()` under the Customer section
    - _Requirements: 1.1, 1.2_

  - [x] 1.2 Create EF Core migration `AddCustomerUsername`
    - Run `dotnet ef migrations add AddCustomerUsername` to scaffold the migration
    - Edit the generated migration: add the column as nullable, add a `migrationBuilder.Sql(...)` backfill statement (`UPDATE Customers SET Username = 'user_' + CAST(Id AS nvarchar(20)) WHERE Username IS NULL`), then alter the column to `NOT NULL`, and create the unique index `IX_Customers_Username`
    - _Requirements: 1.1, 1.2_

  - [x] 1.3 Write unit tests for Customer entity and migration
    - Verify `Customer` has a `Username` property of type `string`
    - Verify `AppDbContext` model has a unique index on `Customer.Username`
    - _Requirements: 1.1, 1.2_

- [x] 2. Add username validation and update CustomerService

  - [x] 2.1 Add `IsValidUsername` static helper to `CustomerService`
    - Implement `public static bool IsValidUsername(string username)` using `Regex.IsMatch(username, @"^[a-zA-Z0-9_.]{3,50}$")`/tp 100 70 50
    - _Requirements: 1.4_

  - [x] 2.2 Write property test for `IsValidUsername` (Property 1)
    - **Property 1: Username validation accepts only valid identifiers**
    - **Validates: Requirements 1.4**
    - Use FsCheck to generate arbitrary strings; assert `IsValidUsername(s) == true` iff `s` matches `^[a-zA-Z0-9_.]{3,50}$`
    - Tag: `// Feature: customer-ui-improvements, Property 1`

  - [x] 2.3 Update `CustomerService.RegisterAsync` to accept and validate `username`
    - Add `string username` parameter to `RegisterAsync`
    - Validate: not null/empty → return `(false, "Vui lòng nhập tên đăng nhập!", null)`
    - Validate: `IsValidUsername` → return `(false, "Tên đăng nhập chỉ được chứa chữ cái, số, dấu gạch dưới hoặc dấu chấm (3–50 ký tự).", null)`
    - Check duplicate: `AnyAsync(c => c.Username == username)` → return `(false, "Tên đăng nhập đã được sử dụng!", null)`
    - Set `customer.Username = username` when constructing the entity
    - _Requirements: 1.3, 1.4, 1.5_

  - [x] 2.4 Write property test for duplicate username rejection (Property 2)
    - **Property 2: Duplicate username registration is rejected**
    - **Validates: Requirements 1.5**
    - Use an in-memory SQLite EF Core provider; register a customer, then attempt to register again with the same username and different other fields; assert `Success == false` and `Message` is non-empty
    - Tag: `// Feature: customer-ui-improvements, Property 2`

  - [x] 2.5 Update `CustomerService.LoginAsync` to match by username instead of email
    - Change the parameter name from `email` to `username`
    - Change the EF query predicate from `c.Email == email` to `c.Username == username`
    - _Requirements: 1.6_

  - [x] 2.6 Write property test for login round-trip (Property 3)
    - **Property 3: Login by username round-trip**
    - **Validates: Requirements 1.6**
    - Use in-memory SQLite; register a customer with a given username/password; assert `LoginAsync(username, password)` returns that customer, and `LoginAsync(otherUsername, password)` and `LoginAsync(username, wrongPassword)` return null
    - Tag: `// Feature: customer-ui-improvements, Property 3`

- [~] 3. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 4. Update DlgCustomerRegister to include the username field

  - [x] 4.1 Add `txtUsername` field to `DlgCustomerRegister` and update layout
    - Declare `private readonly TextBox txtUsername = null!;` alongside the other field declarations
    - Increase `ClientSize.Height` from 520 to 590
    - Insert the "Tên đăng nhập" field between "Họ tên" (y=112) and "Email" (y=248); the new field sits at y=180; shift all subsequent fields down by 68px
    - Update the subtitle text to `"Dùng tên đăng nhập để đăng nhập, đặt vé và xem lịch sử vé."`
    - Update `lblError.Location` and button locations to match the new y offsets
    - _Requirements: 1.3_

  - [x] 4.2 Update `ValidateInput` and `RegisterAsync` call in `DlgCustomerRegister`
    - In `ValidateInput()`, add a check: `!CustomerService.IsValidUsername(txtUsername.Text.Trim())` → `errorProvider.SetError(txtUsername, "Tên đăng nhập không hợp lệ.")`
    - In `RegisterAsync()`, pass `txtUsername.Text.Trim()` as the new `username` argument to `service.RegisterAsync`
    - _Requirements: 1.3, 1.4, 1.5_

  - [x] 4.3 Write unit test for registration validation requiring username
    - Test that submitting with an empty username triggers an error (calls `ValidateInput` and returns false)
    - _Requirements: 1.3_

- [x] 5. Display username in UcMyProfile

  - [x] 5.1 Add read-only `txtUsername` field to `UcMyProfile.CreateFormPanel`
    - Declare `private readonly TextBox txtUsername = new() { ReadOnly = true };` in the field list
    - In `CreateFormPanel()`, add a new `RowStyle(SizeType.Absolute, 78)` row after the title row (row 0)
    - Insert `Field("Tên đăng nhập", txtUsername)` at the new row 1 (before "Họ tên")
    - Increment all subsequent row indices by 1; update `RowCount` from 9 to 10
    - In `LoadProfileAsync`, set `txtUsername.Text = customer.Username;`
    - _Requirements: 1.7_

  - [x] 5.2 Write unit test for profile username display
    - Verify `txtUsername.ReadOnly == true`
    - Verify `LoadProfileAsync` sets `txtUsername.Text` to the customer's username
    - _Requirements: 1.7_

- [ ] 6. Add Loyalty_Section to UcMyProfile

  - [x] 6.1 Add `FormatPointChange` static helper to `UcMyProfile`
    - Implement `public static (string text, Color color) FormatPointChange(int points)` returning `($"+{points:N0}", green)` for `points >= 0` and `($"-{Math.Abs(points):N0}", red)` for `points < 0`
    - Green: `Color.FromArgb(80, 200, 120)`, Red: `Color.FromArgb(220, 80, 80)`
    - _Requirements: 3.3, 3.4_

  - [x] 6.2 Write property test for `FormatPointChange` sign consistency (Property 9)
    - **Property 9: Point change formatting is sign-consistent**
    - **Validates: Requirements 3.3, 3.4**
    - Use FsCheck to generate arbitrary integers; assert result text starts with `"+"` and color is green when `points >= 0`; starts with `"-"` and color is red when `points < 0`
    - Tag: `// Feature: customer-ui-improvements, Property 9`

  - [x] 6.3 Add `flpTransactions` panel and `RenderTransactions` method to `UcMyProfile`
    - In `CreateMemberPanel()`, add two new rows to the `TableLayoutPanel` after `btnRedeem` (row 6): row 7 = "Lịch sử điểm thưởng" section title (height 48), row 8 = `flpTransactions` (AutoSize); update `RowCount` from 8 to 10
    - Declare `private readonly FlowLayoutPanel flpTransactions = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoScroll = true, MaximumSize = new Size(0, 280) };`
    - Implement `private void RenderTransactions(IEnumerable<PointTransaction> transactions)`: clear `flpTransactions`, then for each transaction add a `Panel` (height 28) with a left label (`{date:dd/MM/yy} — {description}`) and a right-aligned label using `FormatPointChange`
    - _Requirements: 3.1, 3.2, 3.3, 3.4_

  - [x] 6.4 Fetch and render transactions in `LoadProfileAsync`
    - After loading the customer, query `context.PointTransactions.Where(pt => pt.CustomerId == customer.Id).OrderByDescending(pt => pt.TransactionDate).Take(10).AsNoTracking().ToListAsync()`
    - Call `RenderTransactions(transactions)`
    - Wrap in the existing try/catch; on failure show `"Không thể tải lịch sử điểm thưởng."` in `flpTransactions`
    - _Requirements: 3.1, 3.2_

  - [-] 6.5 Write property test for loyalty transaction list bounds and order (Property 8)
    - **Property 8: Loyalty transaction list is bounded and ordered**
    - **Validates: Requirements 3.2**
    - Use in-memory SQLite; seed N transactions for a customer; assert the query returns `min(N, 10)` items ordered by `TransactionDate` descending
    - Tag: `// Feature: customer-ui-improvements, Property 8`

- [x] 7. Remove "Điểm thưởng" from account menu in FrmCustomerMain

  - [x] 7.1 Remove the "Điểm thưởng" `AddAccountItem` call and update menu height
    - In `FrmCustomerMain.InitializeComponent`, delete the line `AddAccountItem("Điểm thưởng", LoadProfile);`
    - In `ToggleAccountMenu`, change the height from `114` to `76` (2 items × 38px)
    - _Requirements: 3.5_

  - [x] 7.2 Write unit test for account menu item count
    - Verify `pnlAccountMenu` contains exactly 2 buttons: "Hồ sơ" and "Đăng xuất"
    - _Requirements: 3.5_

- [~] 8. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [x] 9. Fix nav bar layout in FrmCustomerMain

  - [x] 9.1 Replace absolute-positioned nav buttons with a `FlowLayoutPanel` in `FrmCustomerMain`
    - Declare `private FlowLayoutPanel flpNav = null!;` in the field list
    - In `InitializeComponent`, after adding the `logo` label, create `flpNav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = false, BackColor = Color.Transparent, Padding = new Padding(8, 12, 8, 0) }` and add it to `pnlNav`
    - Change the `account` button to `Dock = DockStyle.Right` and add it directly to `pnlNav` (not `flpNav`); remove the `pnlNav.Resize` handler that repositions it manually
    - Update `AddNav(string text, Action click)` to remove the `int x` parameter and the `button.Location` assignment; add the button to `flpNav` instead of `pnlNav`
    - Update all three `AddNav` call sites to remove the x-coordinate argument
    - _Requirements: 4.1, 4.4_

  - [x] 9.2 Write unit test for nav bar layout
    - Verify `pnlNav` contains a `FlowLayoutPanel` (`flpNav`)
    - Verify `flpNav` contains exactly 3 nav buttons
    - _Requirements: 4.1, 4.4_

- [ ] 10. Create customer-facing UcNowShowing and wire nav

  - [x] 10.1 Create `Forms/Customer/UcNowShowing.cs` with layout and data loading
    - Create a new `UserControl` in namespace `BaiTapLon.Forms.Customer`
    - Add fields: `txtSearch` (TextBox), `flpGenres` (FlowLayoutPanel, `WrapContents = true`), `flpMovies` (FlowLayoutPanel, `WrapContents = true`), `allMovies` (List<Movie>), `selectedGenres` (List<string>)
    - Add `public Action<Movie>? MovieSelected { get; set; }` property
    - Implement `InitializeComponent`: header panel (height 68) with title label and `txtSearch`; `flpGenres` below; `flpMovies` below, all inside a scrollable outer panel
    - Implement `LoadDataAsync`: query all active movies with at least one active future showtime, include `MovieGenres → Genre`, populate `allMovies`, build genre filter buttons, call `ApplyFilters()`
    - Wire `Load += async (s, e) => await LoadDataAsync()`
    - _Requirements: 2.4, 2.8_

  - [-] 10.2 Implement `ApplyFilters` and genre toggle buttons in `UcNowShowing`
    - Implement `ApplyFilters()`: filter `allMovies` by `txtSearch.Text` (case-insensitive `Contains`) and by `selectedGenres` (any genre match); render filtered list into `flpMovies` using the same card structure as `UcStorefront.CreateMovieCard`
    - Wire `txtSearch.TextChanged` to call `ApplyFilters()`
    - Build genre buttons: "Tất cả" clears `selectedGenres`; other genre buttons toggle membership in `selectedGenres`; selected state uses `CustomerUi.Accent` background; each click calls `ApplyFilters()`
    - On card click, invoke `MovieSelected?.Invoke(movie)`
    - On `LoadDataAsync` failure, show `"Không thể tải danh sách phim. Vui lòng thử lại."` in `flpMovies`
    - _Requirements: 2.4, 2.5, 2.6, 2.7_

  - [~] 10.3 Write property test for Now Showing title search (Property 6)
    - **Property 6: Now Showing title search is a subset filter**
    - **Validates: Requirements 2.5**
    - Extract the title-filter logic into a public static method `FilterByTitle(IEnumerable<Movie> movies, string query)`; use FsCheck to generate arbitrary movie lists and query strings; assert the result is a subset of the input and every returned movie's title contains the query (case-insensitive); assert empty query returns all movies
    - Tag: `// Feature: customer-ui-improvements, Property 6`

  - [~] 10.4 Write property test for Now Showing genre filter (Property 7)
    - **Property 7: Now Showing genre filter is a subset filter**
    - **Validates: Requirements 2.6**
    - Extract the genre-filter logic into a public static method `FilterByGenres(IEnumerable<Movie> movies, IEnumerable<string> genres)`; use FsCheck to generate arbitrary movie lists and genre sets; assert the result is a subset of the input and every returned movie has at least one genre in the set; assert empty genre set returns all movies
    - Tag: `// Feature: customer-ui-improvements, Property 7`

  - [~] 10.5 Wire "Phim đang chiếu" nav button to `UcNowShowing` in `FrmCustomerMain`
    - Add `LoadNowShowing()` method: `SetContent(new Customer.UcNowShowing { MovieSelected = ShowMovieDetail })`
    - Change the second `AddNav` call from `AddNav("Phim đang chiếu", LoadHome)` to `AddNav("Phim đang chiếu", LoadNowShowing)`
    - _Requirements: 2.8_

- [ ] 11. Fix UcStorefront hero text clipping

  - [x] 11.1 Extract `ComputeTextAreaWidth` and update `PaintHero` / `DrawHeroText` in `UcStorefront`
    - Add `public static float ComputeTextAreaWidth(int heroPanelWidth)`: compute `posterLeft = heroPanelWidth - 220`; return `Math.Max(200f, posterLeft - 50f)`
    - Update `PaintHero` to call `ComputeTextAreaWidth(rect.Width)` and pass the result to `DrawHeroText`
    - Update `DrawHeroText` signature to accept a `float textWidth` parameter; replace the hardcoded `720` and `680` widths with `textWidth` and `textWidth - 4`
    - _Requirements: 4.5_

  - [-] 11.2 Write property test for hero text area width (Property 10)
    - **Property 10: Hero text area never overlaps the poster**
    - **Validates: Requirements 4.5**
    - Use FsCheck to generate arbitrary `heroPanelWidth >= 400`; assert `ComputeTextAreaWidth(w) > 0` and `ComputeTextAreaWidth(w) <= w - 220 - 50 + 1` (text rect does not reach the poster)
    - Tag: `// Feature: customer-ui-improvements, Property 10`

- [ ] 12. Fix UcStorefront hot/upcoming movie query bounds

  - [x] 12.1 Enforce 6-item cap and correct sort order for hot and upcoming movie queries in `UcStorefront.LoadDataAsync`
    - Change `hotMovies` query: `.Take(12)` → `.Take(6)`, keep `.OrderBy(m => m.Showtimes.Min(s => s.StartTime))`
    - Change `upcoming` query: `.Take(8)` → `.Take(6)`, keep `.OrderBy(m => m.ReleaseDate)`
    - _Requirements: 2.2, 2.3_

  - [-] 12.2 Write property test for hot movies list bounds and sort (Property 4)
    - **Property 4: Home page hot-movies list is bounded and sorted**
    - **Validates: Requirements 2.2**
    - Extract the hot-movies query logic into a testable static method or test against an in-memory EF Core provider; use FsCheck to generate arbitrary movie/showtime collections; assert result count ≤ 6, all items have at least one active future showtime, and items are ordered by nearest showtime ascending
    - Tag: `// Feature: customer-ui-improvements, Property 4`

  - [ ] 12.3 Write property test for upcoming movies list bounds and sort (Property 5)
    - **Property 5: Home page upcoming-movies list is bounded and sorted**
    - **Validates: Requirements 2.3**
    - Use FsCheck with in-memory EF Core provider; generate arbitrary movie collections with varying release dates; assert result count ≤ 6, all items have `ReleaseDate > today`, and items are ordered by `ReleaseDate` ascending
    - Tag: `// Feature: customer-ui-improvements, Property 5`

- [x] 13. Fix UcMyProfile panel overflow

  - [x] 13.1 Set minimum width and proportional expansion on `UcMyProfile` form fields
    - In `CreateFormPanel()`, set `panel.MinimumSize = new Size(280, 0)` on the form `TableLayoutPanel`
    - In the `Field` helper, set `textBox.MinimumSize = new Size(280, 0)` so fields expand with the panel
    - _Requirements: 4.3, 4.7_

- [~] 14. Checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

- [ ] 15. Integration and final wiring

  - [~] 15.1 Verify `FrmLogin` passes username correctly to `CustomerService.LoginAsync`
    - Confirm `FrmLogin.BtnLogin_Click` already calls `customerService.LoginAsync(username, password)` where `username = txtUsername.Text.Trim()` — no change needed if already correct; otherwise update the call site
    - _Requirements: 1.6_

  - [~] 15.2 Apply `dotnet ef database update` and verify migration
    - Run `dotnet ef database update` to apply the `AddCustomerUsername` migration
    - Verify the `Customers` table has the `Username` column and `IX_Customers_Username` unique index
    - _Requirements: 1.1, 1.2_

  - [~] 15.3 Write integration test for unique index existence
    - Connect to a test database; verify `IX_Customers_Username` unique index exists on the `Customers` table after migration
    - _Requirements: 1.2_

  - [~] 15.4 Write integration test for cross-entity login isolation
    - Verify `CustomerService.LoginAsync` returns null for a username that belongs to a `User` (not a `Customer`), ensuring no cross-entity confusion
    - _Requirements: 1.6_

- [~] 16. Final checkpoint — Ensure all tests pass
  - Ensure all tests pass, ask the user if questions arise.

---

## Notes

- Tasks marked with `*` are optional and can be skipped for faster MVP delivery.
- Each task references specific requirements for traceability.
- Checkpoints ensure incremental validation after each major area.
- Property tests validate universal correctness properties using FsCheck (already present in `BaiTapLon.Tests`).
- Unit tests validate specific examples and edge cases.
- Properties 1, 9, 10 test pure functions — no database or UI required.
- Properties 4, 5, 6, 7, 8 test query/filter logic against an in-memory EF Core provider.
- Properties 2 and 3 use an in-memory SQLite EF Core provider.
- The `UcNowShowing` in `Forms/Customer/` is distinct from the existing `Forms/Staff/UcNowShowing.cs`.

## Task Dependency Graph

```json
{
  "waves": [
    { "id": 0, "tasks": ["1.1"] },
    { "id": 1, "tasks": ["1.2", "2.1"] },
    { "id": 2, "tasks": ["1.3", "2.2", "2.3"] },
    { "id": 3, "tasks": ["2.4", "2.5", "4.1"] },
    { "id": 4, "tasks": ["2.6", "4.2", "5.1", "6.1", "7.1"] },
    { "id": 5, "tasks": ["4.3", "5.2", "6.2", "6.3", "7.2", "9.1"] },
    { "id": 6, "tasks": ["6.4", "9.2", "10.1", "11.1", "12.1", "13.1"] },
    { "id": 7, "tasks": ["6.5", "10.2", "11.2", "12.2", "12.3"] },
    { "id": 8, "tasks": ["10.3", "10.4", "10.5"] },
    { "id": 9, "tasks": ["15.1", "15.2"] },
    { "id": 10, "tasks": ["15.3", "15.4"] }
  ]
}
```
