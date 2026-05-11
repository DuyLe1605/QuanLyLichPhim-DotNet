# Design Document: Customer UI Improvements

## Overview

This document describes the technical design for four improvements to the CineManager customer-facing WinForms application:

1. **Add Username to Customer** — new `Username` column on the `Customer` entity, updated registration dialog, login-by-username, and read-only display in the profile page.
2. **Separate Home vs Now Showing** — create a new customer-facing `UcNowShowing` (distinct from the staff version), fix the nav bar so "Trang chủ" and "Phim đang chiếu" load different pages.
3. **Merge Loyalty into Profile** — embed a `Loyalty_Section` panel inside `UcMyProfile` showing the 10 most recent `PointTransaction` records; remove the redundant "Điểm thưởng" account-menu item.
4. **Fix layout overlaps** — replace absolute-positioned nav buttons with a `FlowLayoutPanel`, fix hero text clipping, ensure `UcMyProfile` panels don't overflow, add `WrapContents = true` to the Now Showing filter bar.

The stack is C# WinForms (.NET 8), Entity Framework Core 8, SQL Server. All UI is built programmatically in code-behind — no XAML/WPF.

---

## Architecture

The feature touches four layers:

```
┌─────────────────────────────────────────────────────────────┐
│  UI Layer (Forms/Customer, Forms/FrmCustomerMain)           │
│  ┌──────────────┐  ┌──────────────────┐  ┌──────────────┐  │
│  │ FrmCustomer  │  │  UcStorefront    │  │ UcMyProfile  │  │
│  │ Main.cs      │  │  (Home page)     │  │ (+ Loyalty)  │  │
│  └──────┬───────┘  └──────────────────┘  └──────────────┘  │
│         │  ┌──────────────────────────┐                     │
│         └─►│ UcNowShowing (Customer)  │  NEW                │
│            └──────────────────────────┘                     │
│  ┌──────────────────────────────────────────────────────┐   │
│  │ DlgCustomerRegister  (+ Username field)              │   │
│  └──────────────────────────────────────────────────────┘   │
├─────────────────────────────────────────────────────────────┤
│  Service Layer                                              │
│  CustomerService.RegisterAsync  (+ username param)         │
│  CustomerService.LoginAsync     (match by username)        │
├─────────────────────────────────────────────────────────────┤
│  Data Layer                                                 │
│  Customer entity  (+ Username property)                    │
│  AppDbContext     (+ unique index on Username)             │
│  EF Core Migration: AddCustomerUsername                    │
└─────────────────────────────────────────────────────────────┘
```

No new services or repositories are introduced. All changes are additive or in-place modifications to existing classes.

---

## Components and Interfaces

### 1. Customer Entity (`Models/Customers/Customer.cs`)

Add one property:

```csharp
/// <summary>Login username. Max 50 chars, unique, alphanumeric/underscore/dot.</summary>
public string Username { get; set; } = string.Empty;
```

The property is non-nullable in C# but the migration adds it as nullable initially (to allow backfill of existing rows), then a subsequent migration step enforces `NOT NULL` after backfill.

### 2. AppDbContext (`Data/AppDbContext.cs`)

Add a unique index on `Customer.Username` inside `OnModelCreating`:

```csharp
modelBuilder.Entity<Customer>()
    .Property(c => c.Username)
    .HasMaxLength(50);

modelBuilder.Entity<Customer>()
    .HasIndex(c => c.Username)
    .IsUnique();
```

### 3. EF Core Migration

Two-step migration strategy:

**Step 1 — `AddCustomerUsername`**
- Adds `Username` column as `nvarchar(50) NULL`.
- Backfill: `UPDATE Customers SET Username = 'user_' + CAST(Id AS nvarchar) WHERE Username IS NULL`.
- Adds unique index `IX_Customers_Username`.

**Step 2 — `EnforceCustomerUsernameNotNull`** (optional, can be combined)
- Alters column to `nvarchar(50) NOT NULL`.

In practice for a development database these can be a single migration that adds the column with a default value expression and immediately makes it non-nullable.

### 4. CustomerService (`Services/Customers/CustomerService.cs`)

#### 4a. `RegisterAsync` — add `username` parameter

```csharp
public async Task<(bool Success, string Message, Customer? Customer)> RegisterAsync(
    string fullName, string email, string phone, string password, string username)
```

New validation steps (before creating the entity):
1. Check `username` is not null/empty.
2. Check `username` matches `^[a-zA-Z0-9_.]{3,50}$`.
3. Check `await _context.Customers.AnyAsync(c => c.Username == username)` — return duplicate error if true.

Set `customer.Username = username` when constructing the entity.

#### 4b. `LoginAsync` — match by username instead of email

```csharp
public async Task<Customer?> LoginAsync(string username, string password)
{
    var customer = await _context.Customers
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.Username == username && c.IsActive);

    if (customer == null) return null;
    if (!BCrypt.Net.BCrypt.Verify(password, customer.PasswordHash)) return null;
    return customer;
}
```

The parameter is renamed from `email` to `username` to match the new semantics. The `FrmLogin` already passes `txtUsername.Text` to this method, so no change is needed in the login form itself.

#### 4c. Username validation helper (static, testable)

Extract the regex check into a public static method so it can be unit/property tested independently of the database:

```csharp
public static bool IsValidUsername(string username)
{
    if (string.IsNullOrWhiteSpace(username)) return false;
    return System.Text.RegularExpressions.Regex.IsMatch(
        username, @"^[a-zA-Z0-9_.]{3,50}$");
}
```

### 5. DlgCustomerRegister (`Forms/DlgCustomerRegister.cs`)

Changes:
- Increase `ClientSize.Height` from 520 to 590 to accommodate the new field.
- Add `txtUsername` field between "Họ tên" and "Email" (or after "Email" — between email and phone is the natural order: FullName → Username → Email → Phone → Password → Confirm).
- Update subtitle text: `"Dùng tên đăng nhập để đăng nhập, đặt vé và xem lịch sử vé."`.
- Update `ValidateInput()` to validate the username field using `CustomerService.IsValidUsername`.
- Pass `txtUsername.Text.Trim()` as the new `username` argument to `service.RegisterAsync`.

Field layout (y positions shift down by 68px after the new field):

| Field | y |
|---|---|
| Họ tên | 112 |
| Tên đăng nhập (NEW) | 180 |
| Email | 248 |
| Số điện thoại | 316 |
| Mật khẩu | 384 |
| Xác nhận mật khẩu | 452 |
| Error label | 528 |
| Buttons | 556 |

### 6. UcMyProfile (`Forms/Customer/UcMyProfile.cs`)

#### 6a. Add read-only Username field

Add `txtUsername` as a `TextBox` field (read-only):

```csharp
private readonly TextBox txtUsername = new() { ReadOnly = true };
```

Insert it into `CreateFormPanel()` after the title row, before "Họ tên". Add a new `RowStyle(SizeType.Absolute, 78)` row. In `LoadProfileAsync`, set:

```csharp
txtUsername.Text = customer.Username;
```

#### 6b. Loyalty_Section panel

Add a new `pnlLoyalty` panel below the member card in `CreateMemberPanel()`. The member panel's `TableLayoutPanel` gains additional rows:

| Row | Content | Height |
|---|---|---|
| 0 | "Thẻ thành viên" title | 58 |
| 1 | QR code | 250 |
| 2 | lblMember | 48 |
| 3 | lblTier | 48 |
| 4 | lblPoints | 48 |
| 5 | ProgressBar | 45 |
| 6 | btnRedeem | 50 |
| 7 | "Lịch sử điểm thưởng" section title (NEW) | 48 |
| 8 | flpTransactions FlowLayoutPanel (NEW) | AutoSize |
| 9 | Remaining space | 100% |

`flpTransactions` is a `FlowLayoutPanel` with:
- `Dock = DockStyle.Fill`
- `FlowDirection = FlowDirection.TopDown`
- `WrapContents = false`
- `AutoScroll = true`
- `MaximumSize = new Size(0, 280)` (shows ~10 rows without excessive height)

Each transaction row is a `Panel` (height 28) containing two `Label` controls:
- Left label: `{date:dd/MM/yy} — {description}` in `CustomerUi.Muted` color
- Right label: `+{points}` or `-{Math.Abs(points)}` right-aligned, green (`Color.FromArgb(80, 200, 120)`) for positive, red (`Color.FromArgb(220, 80, 80)`) for negative

`LoadProfileAsync` fetches transactions:

```csharp
var transactions = await context.PointTransactions
    .Where(pt => pt.CustomerId == customer.Id)
    .OrderByDescending(pt => pt.TransactionDate)
    .Take(10)
    .AsNoTracking()
    .ToListAsync();
RenderTransactions(transactions);
```

#### 6c. Static formatting helper (testable)

```csharp
public static (string text, Color color) FormatPointChange(int points)
{
    return points >= 0
        ? ($"+{points:N0}", Color.FromArgb(80, 200, 120))
        : ($"-{Math.Abs(points):N0}", Color.FromArgb(220, 80, 80));
}
```

This pure function is the target of property-based tests for requirements 3.3 and 3.4.

### 7. FrmCustomerMain (`Forms/FrmCustomerMain.cs`)

#### 7a. Nav bar layout — replace absolute positions with FlowLayoutPanel

Current problem: nav buttons use `Location = new Point(x, 12)` with hardcoded x values (245, 395, 565), causing overlap on resize.

**Solution**: Replace the three nav buttons with a `FlowLayoutPanel` (`flpNav`) docked to fill the center of `pnlNav`.

Layout strategy for `pnlNav` (height 68):

```
[Logo 210px fixed] [flpNav fills remaining] [account button 230px anchored right]
```

Implementation:
- Keep the `logo` Label with `Dock = DockStyle.Left`, `Width = 210`.
- Keep the `account` Button with `Dock = DockStyle.Right`, `Width = 230`.
- Add `flpNav = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, AutoSize = false, BackColor = Color.Transparent, Padding = new Padding(8, 12, 8, 0) }`.
- Add nav buttons to `flpNav` instead of directly to `pnlNav`.
- Remove the `AddNav(text, x, click)` helper's `Location` assignment; buttons are positioned by the flow panel.

The `AddNav` method becomes:

```csharp
private void AddNav(string text, Action click)
{
    var button = Customer.CustomerUi.NavButton(text);
    button.Click += (s, e) => click();
    flpNav.Controls.Add(button);
    navButtons.Add(button);
}
```

#### 7b. Wire "Phim đang chiếu" to new UcNowShowing

Change the second `AddNav` call:

```csharp
// Before:
AddNav("Phim đang chiếu", 395, LoadHome);

// After:
AddNav("Phim đang chiếu", LoadNowShowing);
```

Add `LoadNowShowing` method:

```csharp
private void LoadNowShowing()
{
    ToggleAccountMenu(false);
    SetContent(new Customer.UcNowShowing
    {
        MovieSelected = ShowMovieDetail
    });
}
```

#### 7c. Remove "Điểm thưởng" from account menu

Remove the `AddAccountItem("Điểm thưởng", LoadProfile)` call. The account menu retains only "Hồ sơ" and "Đăng xuất". Update `ToggleAccountMenu` height from 114 to 76 (2 items × 38px).

### 8. UcNowShowing (Customer) — NEW FILE

**Path**: `Forms/Customer/UcNowShowing.cs`

This is a new customer-facing UserControl, distinct from `Forms/Staff/UcNowShowing.cs`. It does not show a date picker or showtime buttons — it shows a searchable, filterable movie grid where clicking a card navigates to the movie detail page.

```csharp
namespace BaiTapLon.Forms.Customer;

public class UcNowShowing : UserControl
{
    private readonly TextBox txtSearch = new();
    private readonly FlowLayoutPanel flpGenres = new();   // genre filter buttons
    private readonly FlowLayoutPanel flpMovies = new();   // movie cards

    private List<Movie> allMovies = new();
    private List<string> selectedGenres = new();

    public Action<Movie>? MovieSelected { get; set; }

    public UcNowShowing() { InitializeComponent(); Load += async (s, e) => await LoadDataAsync(); }
    // ...
}
```

**Layout** (top-to-bottom, all inside a scroll panel):

```
┌─────────────────────────────────────────────────────┐
│  "Phim đang chiếu"  [search box]                    │  Header panel, height 68
├─────────────────────────────────────────────────────┤
│  [Tất cả] [Hành động] [Hài hước] [Kinh dị] ...     │  flpGenres, WrapContents=true
├─────────────────────────────────────────────────────┤
│  [card] [card] [card] [card] ...                    │  flpMovies, WrapContents=true
│  [card] [card] ...                                  │
└─────────────────────────────────────────────────────┘
```

**Data loading** (`LoadDataAsync`):
- Query all active movies with at least one active showtime in the future.
- Include `MovieGenres → Genre`.
- Populate `allMovies`.
- Build genre filter buttons from the distinct genres present in `allMovies`.
- Call `ApplyFilters()`.

**Filtering** (`ApplyFilters`):
- Start from `allMovies`.
- If `txtSearch.Text` is non-empty, filter by `movie.Title.Contains(query, StringComparison.OrdinalIgnoreCase)`.
- If `selectedGenres` is non-empty, filter by `movie.MovieGenres.Any(mg => selectedGenres.Contains(mg.Genre.Name))`.
- Render filtered list into `flpMovies`.

**Search**: `txtSearch.TextChanged` calls `ApplyFilters()` directly (real-time, no debounce needed for local in-memory filtering).

**Genre buttons**: Toggle buttons styled with `CustomerUi.NavButton`. Selected state uses `CustomerUi.Accent` background. "Tất cả" button clears `selectedGenres`. Clicking a genre button adds/removes it from `selectedGenres` and calls `ApplyFilters()`.

**Movie cards**: Reuse the same card structure as `UcStorefront.CreateMovieCard` (poster, title, duration, age rating, genre). Card click invokes `MovieSelected?.Invoke(movie)`.

### 9. UcStorefront — hero text clipping fix

In `PaintHero`, the poster is drawn at `rect.Right - 220` with width 160. The text area uses `RectangleF(34, 38, 720, 62)` which can overlap the poster at narrow widths.

**Fix**: Make the text rectangle width dynamic based on the hero panel's actual width:

```csharp
private void PaintHero(object? sender, PaintEventArgs e)
{
    // ...
    var posterLeft = rect.Right - 220;
    var textAreaWidth = Math.Max(200, posterLeft - 50); // at least 200px, stops before poster

    DrawHeroText(e.Graphics, movie.Title, infoLine, movie.Description, textAreaWidth);
}

private static void DrawHeroText(Graphics g, string title, string line,
    string? description, float textWidth)
{
    // Replace hardcoded 720/680 with textWidth parameter
    g.DrawString(title, titleFont, titleBrush, new RectangleF(34, 38, textWidth, 62));
    g.DrawString(line, lineFont, mutedBrush, new RectangleF(38, 104, textWidth - 4, 28));
    if (!string.IsNullOrWhiteSpace(description))
        g.DrawString(description, descFont, descBrush, new RectangleF(38, 144, textWidth - 4, 70));
}
```

---

## Data Models

### Customer entity changes

```csharp
// Models/Customers/Customer.cs — add one property
public string Username { get; set; } = string.Empty;
```

### Migration: `AddCustomerUsername`

```csharp
public partial class AddCustomerUsername : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Username",
            table: "Customers",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: true);

        // Backfill existing rows with a unique placeholder
        migrationBuilder.Sql(
            "UPDATE Customers SET Username = 'user_' + CAST(Id AS nvarchar(20)) WHERE Username IS NULL");

        // Now enforce NOT NULL
        migrationBuilder.AlterColumn<string>(
            name: "Username",
            table: "Customers",
            type: "nvarchar(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "");

        migrationBuilder.CreateIndex(
            name: "IX_Customers_Username",
            table: "Customers",
            column: "Username",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_Customers_Username", table: "Customers");
        migrationBuilder.DropColumn(name: "Username", table: "Customers");
    }
}
```

### PointTransaction entity (no changes)

The existing `PointTransaction` entity already has `CustomerId`, `TransactionDate`, `Description`, and `Points` fields. No schema changes are needed for Requirement 3.

---

## Correctness Properties

*A property is a characteristic or behavior that should hold true across all valid executions of a system — essentially, a formal statement about what the system should do. Properties serve as the bridge between human-readable specifications and machine-verifiable correctness guarantees.*

### Property 1: Username validation accepts only valid identifiers

*For any* string `s`, `CustomerService.IsValidUsername(s)` SHALL return `true` if and only if `s` matches the pattern `^[a-zA-Z0-9_.]{3,50}$` — i.e., length between 3 and 50 inclusive, containing only alphanumeric characters, underscores, or dots.

**Validates: Requirements 1.4**

### Property 2: Duplicate username registration is rejected

*For any* username that already exists in the customer table, calling `RegisterAsync` with that username SHALL return a failure result (Success = false) with a non-empty error message, regardless of the other registration fields.

**Validates: Requirements 1.5**

### Property 3: Login by username round-trip

*For any* customer registered with a given username and password, calling `CustomerService.LoginAsync(username, password)` SHALL return that customer. Calling it with any other username or a wrong password SHALL return null.

**Validates: Requirements 1.6**

### Property 4: Home page hot-movies list is bounded and sorted

*For any* collection of movies with varying showtime counts and dates, the "Phim đang hot" query result SHALL contain at most 6 items, all with at least one active future showtime, ordered ascending by nearest showtime start time.

**Validates: Requirements 2.2**

### Property 5: Home page upcoming-movies list is bounded and sorted

*For any* collection of movies with varying release dates, the "Phim sắp chiếu" query result SHALL contain at most 6 items, all with `ReleaseDate > today`, ordered ascending by `ReleaseDate`.

**Validates: Requirements 2.3**

### Property 6: Now Showing title search is a subset filter

*For any* non-empty search string `q` and any list of movies, `ApplyFilters(q, selectedGenres=[])` SHALL return only movies whose `Title` contains `q` (case-insensitive). For an empty search string, all movies SHALL be returned unchanged.

**Validates: Requirements 2.5**

### Property 7: Now Showing genre filter is a subset filter

*For any* non-empty set of selected genres `G` and any list of movies, `ApplyFilters(q="", selectedGenres=G)` SHALL return only movies that have at least one genre in `G`. For an empty genre set, all movies SHALL be returned unchanged.

**Validates: Requirements 2.6**

### Property 8: Loyalty transaction list is bounded and ordered

*For any* customer with `N` point transactions, `LoadTransactionsAsync` SHALL return `min(N, 10)` transactions ordered by `TransactionDate` descending.

**Validates: Requirements 3.2**

### Property 9: Point change formatting is sign-consistent

*For any* integer `points`, `UcMyProfile.FormatPointChange(points)` SHALL return a string starting with `"+"` and color green when `points >= 0`, and a string starting with `"-"` and color red when `points < 0`.

**Validates: Requirements 3.3, 3.4**

### Property 10: Hero text area never overlaps the poster

*For any* hero panel width `w >= 400`, the computed text area width (`posterLeft - 50`) SHALL be positive, ensuring the text drawing rectangle does not extend into the poster region.

**Validates: Requirements 4.5**

---

## Error Handling

### Username registration errors

| Condition | Response |
|---|---|
| Username is null/empty | `(false, "Vui lòng nhập tên đăng nhập!", null)` |
| Username fails regex | `(false, "Tên đăng nhập chỉ được chứa chữ cái, số, dấu gạch dưới hoặc dấu chấm (3–50 ký tự).", null)` |
| Username already taken | `(false, "Tên đăng nhập đã được sử dụng!", null)` |

### Login errors

`CustomerService.LoginAsync` returns `null` for any failure (username not found, wrong password, inactive account). `FrmLogin` shows the existing generic error message "Sai tài khoản/email hoặc mật khẩu!" — no change needed.

### Now Showing data load errors

`UcNowShowing.LoadDataAsync` wraps the EF query in a try/catch. On failure, `flpMovies` shows a single error label: `"Không thể tải danh sách phim. Vui lòng thử lại."`.

### Loyalty section load errors

`LoadProfileAsync` in `UcMyProfile` already has a null-guard on `customer`. The transaction query is added inside the same try/catch scope. On failure, `flpTransactions` shows `"Không thể tải lịch sử điểm thưởng."`.

### Migration rollback

The `Down` method of `AddCustomerUsername` drops the index and column cleanly. Existing data is not affected by rollback (the column is simply removed).

---

## Testing Strategy

### Unit tests (example-based)

Target: `BaiTapLon.Tests` project (existing).

| Test | What it verifies |
|---|---|
| `CustomerService_IsValidUsername_AcceptsValidInputs` | Specific valid usernames: "alice", "bob_99", "a.b.c", 50-char string |
| `CustomerService_IsValidUsername_RejectsInvalidInputs` | Empty string, 2-char string, 51-char string, string with spaces, string with "@" |
| `UcMyProfile_FormatPointChange_PositiveIsGreen` | `FormatPointChange(100)` returns `("+100", green)` |
| `UcMyProfile_FormatPointChange_NegativeIsRed` | `FormatPointChange(-50)` returns `("-50", red)` |
| `UcMyProfile_FormatPointChange_ZeroIsGreen` | `FormatPointChange(0)` returns `("+0", green)` |
| `DlgCustomerRegister_ValidateInput_RequiresUsername` | Submitting with empty username shows error |
| `FrmCustomerMain_AccountMenu_HasTwoItems` | Account menu contains exactly "Hồ sơ" and "Đăng xuất" |

### Property-based tests

Library: **FsCheck** (already used in the project via `BaiTapLon.Tests`). Minimum 100 iterations per property.

Each test is tagged with a comment in the format:
`// Feature: customer-ui-improvements, Property {N}: {property_text}`

| Property test | Targets |
|---|---|
| `Prop_IsValidUsername_AcceptsExactlyValidPattern` | Property 1 |
| `Prop_RegisterAsync_RejectsDuplicateUsername` | Property 2 |
| `Prop_LoginAsync_RoundTrip` | Property 3 |
| `Prop_HotMovies_BoundedAndSorted` | Property 4 |
| `Prop_UpcomingMovies_BoundedAndSorted` | Property 5 |
| `Prop_NowShowing_TitleSearchIsSubset` | Property 6 |
| `Prop_NowShowing_GenreFilterIsSubset` | Property 7 |
| `Prop_LoyaltyTransactions_BoundedAndOrdered` | Property 8 |
| `Prop_FormatPointChange_SignConsistent` | Property 9 |
| `Prop_HeroTextArea_NeverOverlapsPoster` | Property 10 |

**Notes on test implementation:**

- Properties 1, 9, 10 test pure functions — no database or UI required.
- Properties 4, 5, 6, 7, 8 test query/filter logic extracted into static helper methods or tested against an in-memory EF Core provider.
- Properties 2 and 3 use an in-memory SQLite EF Core provider to avoid SQL Server dependency in tests.
- Property 10 tests the arithmetic in `PaintHero` by extracting the width calculation into a static method `ComputeTextAreaWidth(int heroPanelWidth)`.

### Integration tests

- Verify `IX_Customers_Username` unique index exists after migration (single execution against a test database).
- Verify `CustomerService.LoginAsync` returns null for a username that belongs to a `User` (not a `Customer`) — ensuring no cross-entity confusion.

### Manual / smoke tests

- Register a new customer via `DlgCustomerRegister`, verify the username field appears and is required.
- Log in with the new username, verify `FrmCustomerMain` opens.
- Navigate to "Phim đang chiếu", verify `UcNowShowing` (customer) loads with search and genre filters.
- Navigate to "Hồ sơ", verify username is shown read-only and loyalty section appears below the member card.
- Resize `FrmCustomerMain` from minimum (1060px) to maximized, verify no nav button overlap.
