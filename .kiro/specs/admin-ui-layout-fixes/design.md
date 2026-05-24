# Admin UI Layout Fixes – Bugfix Design

## Overview

Four WinForms screens in the CineManager application suffer from layout overflow bugs caused by
over-reliance on `SizeType.Absolute` row heights in `TableLayoutPanel` controls, missing
`AutoScrollMinSize` on scrollable panels, and absolute-coordinate positioning that does not
adapt to the actual panel size at runtime.

The fix strategy is purely UI-layout: no service layer, no data model, and no business logic
changes are required. Each screen receives the minimum targeted change that eliminates the
overflow while preserving all existing data-loading and event-handling behaviour.

Fix approach per screen:

| Screen | Root cause | Fix strategy |
|---|---|---|
| `UcInvoiceManagement` | `seatPreview` row is Absolute 240 px; no AutoScroll on detail panel | Reduce seatPreview row to Absolute 160 px; add `AutoScroll = true` + `AutoScrollMinSize` |
| `UcCustomerManagement` | Absolute `Location` positioning; no `AutoScrollMinSize`; fixed-width labels | Replace with `TableLayoutPanel` inside detail panel; set `AutoScrollMinSize`; use `Dock`/`Anchor` |
| `UcDashboard` | `bottomGrid` row 1 is `SizeType.Percent, 100` with no absolute anchor in wide layout | Change row 1 to `SizeType.Absolute, 284`; recalculate `root.Height` correctly |
| `UcSnackOrder` | Cart panel has no `AutoScroll`; `btnCheckout` clipped at small heights | Add `AutoScroll = true` + `AutoScrollMinSize = 350` to cart panel |

---

## Glossary

- **Bug_Condition (C)**: The set of (screen, windowWidth, windowHeight) tuples that trigger a
  layout overflow – defined formally in `bugfix.md` as `isBugCondition(screen, h, w)`.
- **Property (P)**: The desired post-fix layout invariant – no control overlap, no clipping of
  critical controls, all interactive controls reachable.
- **Preservation**: All data-loading, event-handling, and business-logic behaviour that must
  remain byte-for-byte identical after the layout fix.
- **`CreateDetailPanel()`**: The method in `UcInvoiceManagement` (line ~120) that builds the
  right-hand detail `TableLayoutPanel` containing `lblDetailTitle`, `lblDetailInfo`,
  `seatPreview`, `dgvTickets`, and `dgvSnacks`.
- **`ApplyResponsiveLayout()`**: The method in `UcDashboard` that recalculates `root` and
  `bottomGrid` row heights whenever the control is resized.
- **`CreateCartSide()`**: The method in `UcSnackOrder` that builds the right-hand cart
  `TableLayoutPanel` containing the back button, cart grid, totals, and checkout button.
- **`seatPreview`**: A `SeatLayoutPreviewControl` that renders a seat-map diagram; its minimum
  useful height is ~120 px.
- **`bottomGrid`**: The `TableLayoutPanel` inside `UcDashboard` that holds the two bottom
  charts (`chartTopMovies` and `chartOccupancy`).

---

## Bug Details

### Bug Condition

The bug manifests when any of the four screens is rendered at a window size that exposes the
fixed-height overflow. The `TableLayoutPanel` row arithmetic does not leave enough space for
the lower rows, and the panels either lack `AutoScroll` or lack `AutoScrollMinSize`, so no
scrollbar appears.

**Formal Specification (from bugfix.md):**

```
FUNCTION isBugCondition(screen, windowHeight, windowWidth)
  INPUT: screen ∈ {InvoiceManagement, CustomerManagement, Dashboard, SnackOrder}
         windowHeight: integer (pixels)
         windowWidth:  integer (pixels)
  OUTPUT: boolean

  IF screen = InvoiceManagement AND windowHeight < 900 THEN
    RETURN true   // detail panel rows overflow
  END IF

  IF screen = CustomerManagement AND windowWidth < 400 THEN
    RETURN true   // absolute-positioned controls overflow
  END IF

  IF screen = Dashboard AND windowWidth >= 900 THEN
    RETURN true   // bottomGrid Percent row has no absolute height anchor
  END IF

  IF screen = Dashboard AND windowWidth < 900 THEN
    RETURN true   // _sectionTitles index mismatch / height miscalculation
  END IF

  IF screen = SnackOrder AND windowHeight < 700 THEN
    RETURN true   // btnCheckout clipped
  END IF

  RETURN false
END FUNCTION
```

### Concrete Examples

- **Invoice, 800 px tall window**: Fixed rows sum to 34+128+30+240+32 = 464 px before the two
  Percent rows even start. With panel padding (14 px × 2 = 28 px) the Percent rows receive
  `800 - 464 - 28 = 308 px` at best, but the SplitContainer already allocates only ~350 px to
  Panel2, leaving the Percent rows with ≈ 0 px and both DataGridViews invisible.
- **Customer, 300 px wide panel**: `lblCustName` is `Size(200, 30)` at `Location(145, 10)` –
  its right edge is at x = 345, which overflows a 300 px panel. `AutoScroll` is `true` but
  `AutoScrollMinSize` is `Size.Empty`, so WinForms does not show a scrollbar.
- **Dashboard, wide layout**: `bottomGrid.RowStyles[1]` is `SizeType.Percent, 100`. Because
  `bottomGrid` itself is inside `root` row 5 which is `Absolute 326`, the Percent row resolves
  to `326 - 42 = 284 px` only when `root` has enough height. When `root.Height` is computed
  from the sum of its own RowStyles (which includes the 326 for row 5), the charts render
  correctly – but if `root.Height` is ever set before `ApplyResponsiveLayout` finishes
  recalculating, the Percent row collapses to 0.
- **SnackOrder, 600 px tall window**: The cart panel has 8 fixed rows totalling
  40+72+34+34+42+38+36+54 = 350 px plus one Percent row. At 600 px the Percent row gets
  `600 - 350 ≈ 250 px` for `dgvCart`, which is fine, but `btnCheckout` (row 8, 54 px) is
  placed after the Percent row and is clipped when the panel height is less than the sum of
  all rows.

---

## Expected Behavior

### Preservation Requirements

**Unchanged Behaviors:**

- Selecting an invoice in `UcInvoiceManagement` must continue to load and display the correct
  seat map, ticket list, and snack list in the detail panel.
- Selecting a customer in `UcCustomerManagement` must continue to load and display the QR
  code, personal info, loyalty stats, and point history.
- `UcDashboard` must continue to load stat cards, revenue chart, top-movies chart, and
  occupancy pie chart from the database.
- `ApplyResponsiveLayout()` must continue to switch correctly between the 2-column (≥ 900 px)
  and 1-column (< 900 px) layouts for stat cards and bottom charts.
- Checkout logic in `UcSnackOrder` (invoice creation, point earning, receipt preview) must be
  completely unaffected.
- `ApplyInvoiceLayout()` and `ApplyCustomerSplitLayout()` must continue to adjust
  `SplitterDistance` correctly on resize.
- `SeatLayoutPreviewControl` must continue to render the seat map with auto-scaling.

**Scope:**

All inputs that do NOT satisfy `isBugCondition` (i.e., large windows, wide panels) should
produce identical visual output before and after the fix. The fix touches only row-height
constants, `AutoScroll` flags, and the layout manager used inside `CreateDetailPanel()` of
`UcCustomerManagement`.

---

## Hypothesized Root Cause

### 1. UcInvoiceManagement – Oversized Absolute Row

`seatPreview` is assigned `RowStyle(SizeType.Absolute, 240)`. At typical window heights the
detail panel (Panel2 of the SplitContainer) is only 350–500 px tall. The five fixed rows
(34+128+30+240+32 = 464 px) already exceed that, leaving the two Percent rows with zero or
negative computed height. WinForms clamps them to 0, making both DataGridViews invisible.
There is no `AutoScroll` on the panel to compensate.

### 2. UcCustomerManagement – Absolute Coordinate Positioning Without AutoScrollMinSize

`CreateDetailPanel()` uses `Location = new Point(x, y)` for every control. This is the
classic WinForms "absolute layout" anti-pattern: controls do not reflow when the panel is
resized. `AutoScroll = true` is set, but `AutoScrollMinSize` is `Size.Empty`, so WinForms
never activates the scrollbar (it only does so when content exceeds `AutoScrollMinSize`).
Additionally, `lblCustName` and `lblCustInfo` have `Size(200, ...)` which clips text when the
panel is narrower than ~350 px.

### 3. UcDashboard – Percent Row With No Absolute Fallback

In the wide-layout branch, `bottomGrid.RowStyles[1]` is `SizeType.Percent, 100`. A Percent
row inside a `TableLayoutPanel` that is itself inside a fixed-height row of its parent works
correctly only when the parent's height is already finalised before layout. The sequence in
`ApplyResponsiveLayout()` sets `root.RowStyles[5].Height = 326` and then calls
`root.Height = totalHeight` at the end. If the Resize event fires before the first full
layout pass, `bottomGrid` may have `Height = 0` at the time WinForms resolves the Percent
row, collapsing the charts. Changing row 1 to `SizeType.Absolute, 284` (= 326 − 42) removes
the dependency on the parent's resolved height.

### 4. UcSnackOrder – Missing AutoScroll on Cart Panel

`CreateCartSide()` returns a `TableLayoutPanel` with no `AutoScroll`. When the panel height
is less than the sum of all fixed rows (~350 px), the last rows are simply clipped. Adding
`AutoScroll = true` and `AutoScrollMinSize = new Size(0, 350)` causes WinForms to show a
vertical scrollbar and make all rows reachable.

---

## Correctness Properties

Property 1: Bug Condition – Layout Controls Are Visible and Non-Overlapping

_For any_ (screen, windowHeight, windowWidth) where `isBugCondition` returns `true`, the
fixed rendering SHALL satisfy:
- No two sibling controls have overlapping bounding rectangles (`Bounds.IntersectsWith`).
- All critical controls (`dgvTickets`, `dgvSnacks`, `btnCheckout`, `chartTopMovies`,
  `chartOccupancy`, `picQr`, `lblCustName`) have `Height > 0` and `Visible = true`.
- If a panel has `AutoScroll = true`, its `AutoScrollMinSize.Height` is greater than zero so
  that a scrollbar appears when content overflows.

**Validates: Requirements 2.1, 2.2, 2.3, 2.4, 2.5, 2.6, 2.7**

Property 2: Preservation – Non-Buggy Inputs Produce Identical Layout Behaviour

_For any_ (screen, windowHeight, windowWidth) where `isBugCondition` returns `false`, the
fixed code SHALL produce the same control positions, sizes, and visibility states as the
original code, preserving all data-display and event-handling behaviour.

**Validates: Requirements 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7**

---

## Fix Implementation

### Screen 1 – UcInvoiceManagement (`CreateDetailPanel`)

**File:** `BaiTapLon/Forms/Admin/Invoices/UcInvoiceManagement.cs`

**Method:** `CreateDetailPanel()`

#### Before (row layout)

```
Row 0  Absolute  34   lblDetailTitle
Row 1  Absolute 128   lblDetailInfo
Row 2  Absolute  30   "So do ghe da mua" label
Row 3  Absolute 240   seatPreview          ← too tall
Row 4  Absolute  32   "Ve trong hoa don" label
Row 5  Percent   54   dgvTickets
Row 6  Percent   46   dgvSnacks
Total fixed: 464 px
```

#### After (row layout)

```
Row 0  Absolute  34   lblDetailTitle
Row 1  Absolute 128   lblDetailInfo
Row 2  Absolute  30   "So do ghe da mua" label
Row 3  Absolute 160   seatPreview          ← reduced from 240
Row 4  Absolute  32   "Ve trong hoa don" label
Row 5  Percent   54   dgvTickets
Row 6  Percent   46   dgvSnacks
Total fixed: 384 px
AutoScroll = true
AutoScrollMinSize = new Size(0, 500)
```

**Specific Changes:**

1. Change `new RowStyle(SizeType.Absolute, 240)` (row 3) to
   `new RowStyle(SizeType.Absolute, 160)`.
2. On the `panel` variable, set `panel.AutoScroll = true` and
   `panel.AutoScrollMinSize = new Size(0, 500)` after the `Padding` assignment.

No other code in the method changes. All seven controls are added to the same row indices.

---

### Screen 2 – UcCustomerManagement (`CreateDetailPanel`)

**File:** `BaiTapLon/Forms/Admin/Customers/UcCustomerManagement.cs`

**Method:** `CreateDetailPanel()`

#### Before (absolute positioning)

```
Panel (AutoScroll=true, no AutoScrollMinSize)
  picQr          Location(15, 10)   Size(120,120)   absolute
  lblCustName    Location(145, 10)  Size(200, 30)   absolute, fixed width
  lblCustInfo    Location(145, 42)  Size(200, 88)   absolute, fixed width
  separator      Location(15,145)   Size(320, 1)    absolute
  lblCustStats   Location(15,155)   Size(320, 80)   absolute
  separator      Location(15,245)   Size(320, 1)    absolute
  label "Lich su" Location(15,255)  AutoSize        absolute
  dgvHistory     Location(15,280)   Size(320,200)   absolute, Anchor TLR
```

#### After (TableLayoutPanel-based)

```
Panel (AutoScroll=true, AutoScrollMinSize=(0,520))
  └─ outerTlp  TableLayoutPanel  Dock=Fill
       Row 0  Absolute 130   topRow (inner TLP: QR left, name+info right)
       Row 1  Absolute   1   separator
       Row 2  Absolute  90   lblCustStats  Dock=Fill
       Row 3  Absolute   1   separator
       Row 4  Absolute  24   "Lich su" label  Dock=Fill
       Row 5  Percent  100   dgvHistory  Dock=Fill
```

The `topRow` is itself a `TableLayoutPanel` with 2 columns:

```
topRow
  Col 0  Absolute 130   picQr  (Size 120×120, centred)
  Col 1  Percent  100
    └─ nameInfoStack  TableLayoutPanel  Dock=Fill
         Row 0  Absolute  30   lblCustName  Dock=Fill  (no fixed Size)
         Row 1  Percent  100   lblCustInfo  Dock=Fill  (no fixed Size)
```

**Specific Changes:**

1. Replace the entire body of `CreateDetailPanel()` with the `TableLayoutPanel`-based
   implementation described above.
2. Remove all `Location = new Point(...)` and `Size = new Size(...)` assignments from
   `picQr`, `lblCustName`, `lblCustInfo`, `lblCustStats`, and `dgvHistory`.
3. Set `lblCustName.Dock = DockStyle.Fill` (remove `Size` constraint).
4. Set `lblCustInfo.Dock = DockStyle.Fill` (remove `Size` and `MaximumSize` constraints;
   keep `AutoSize = false` so it fills the cell).
5. Set `panel.AutoScrollMinSize = new Size(0, 520)`.
6. `dgvHistory` keeps all its style properties; only `Location`, `Size`, and `Anchor` are
   removed in favour of `Dock = DockStyle.Fill`.

The `pnlDetail` field still holds the returned `Panel`; the `LoadDetailAsync` method assigns
to `picQr.Image`, `lblCustName.Text`, `lblCustInfo.Text`, `lblCustStats.Text`, and
`dgvHistory.DataSource` exactly as before – no changes needed there.

---

### Screen 3 – UcDashboard (`ApplyResponsiveLayout`)

**File:** `BaiTapLon/Forms/Admin/Dashboard/UcDashboard.cs`

**Method:** `ApplyResponsiveLayout()`

#### Before (wide-layout branch, bottomGrid row styles)

```csharp
bottomGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
bottomGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));   // ← collapses to 0
root.RowStyles[5].Height = 326;
```

#### After (wide-layout branch, bottomGrid row styles)

```csharp
bottomGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
bottomGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 284));  // 326 - 42 = 284
root.RowStyles[5].Height = 326;
```

The narrow-layout branch already uses `SizeType.Absolute` for all four rows and is correct.
No change needed there.

**ASCII diagram – wide layout, root row 5:**

```
root row 5  (Absolute 326 px)
└─ bottomGrid
     Row 0  Absolute  42   [section title labels]
     Row 1  Absolute 284   [chartTopMovies | chartOccupancy]   ← was Percent 100
```

**Specific Changes:**

1. In the `else` branch of `if (width < 900)`, change:
   ```csharp
   bottomGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
   ```
   to:
   ```csharp
   bottomGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 284));
   ```
2. No other changes to `ApplyResponsiveLayout()`. The `root.Height` recalculation loop at the
   end of the method already sums all `RowStyle.Height` values correctly for Absolute rows.

---

### Screen 4 – UcSnackOrder (`CreateCartSide`)

**File:** `BaiTapLon/Forms/Staff/UcSnackOrder.cs`

**Method:** `CreateCartSide()`

#### Before (cart panel)

```
TableLayoutPanel (no AutoScroll)
  Row 0  Absolute  40   btnBack
  Row 1  Absolute  72   "Gio hang" label
  Row 2  Percent  100   dgvCart
  Row 3  Absolute  34   lblTicketTotal
  Row 4  Absolute  34   lblSnackTotal
  Row 5  Absolute  42   lblGrandTotal
  Row 6  Absolute  38   txtReceived
  Row 7  Absolute  36   lblChange
  Row 8  Absolute  54   btnCheckout   ← clipped when panel < 350 px
Fixed sum (excl. Percent): 350 px
```

#### After (cart panel)

```
TableLayoutPanel (AutoScroll=true, AutoScrollMinSize=(0,350))
  Row 0  Absolute  40   btnBack
  Row 1  Absolute  72   "Gio hang" label
  Row 2  Percent  100   dgvCart
  Row 3  Absolute  34   lblTicketTotal
  Row 4  Absolute  34   lblSnackTotal
  Row 5  Absolute  42   lblGrandTotal
  Row 6  Absolute  38   txtReceived
  Row 7  Absolute  36   lblChange
  Row 8  Absolute  54   btnCheckout   ← always reachable via scroll
```

**Specific Changes:**

1. After the `panel` variable is declared and its `Padding` is set, add:
   ```csharp
   panel.AutoScroll = true;
   panel.AutoScrollMinSize = new Size(0, 350);
   ```
2. No row heights, no control assignments, no event handlers change.

---

## Testing Strategy

### Validation Approach

Testing follows the two-phase bug-condition methodology:

1. **Exploratory phase** – run tests against the *unfixed* code to confirm the bug manifests
   and to validate the root-cause hypothesis.
2. **Fix + Preservation phase** – run tests against the *fixed* code to verify Property 1
   (bug inputs now render correctly) and Property 2 (non-bug inputs are unchanged).

### Exploratory Bug Condition Checking

**Goal:** Surface counterexamples that demonstrate the overflow on unfixed code.

**Test Plan:** Instantiate each UserControl in a test host form, set the form to a size that
satisfies `isBugCondition`, force a layout pass, then assert that critical controls have
`Height > 0` and do not overlap. These assertions will *fail* on unfixed code, confirming the
root cause.

**Test Cases:**

1. **Invoice detail overflow** – Create `UcInvoiceManagement`, set host height = 700 px,
   call `CreateDetailPanel()` via reflection or a test-visible overload, assert
   `dgvTickets.Height > 0` and `dgvSnacks.Height > 0`. Expected: FAIL on unfixed code
   (both heights = 0).

2. **Customer panel overflow** – Create `UcCustomerManagement`, set Panel2 width = 300 px,
   assert `lblCustName.Right <= panel.Width`. Expected: FAIL on unfixed code
   (`lblCustName.Right = 345 > 300`).

3. **Dashboard chart collapse** – Create `UcDashboard`, set host width = 1200 px, call
   `ApplyResponsiveLayout()`, assert `chartTopMovies.Height > 0`. Expected: FAIL on unfixed
   code when `root.Height` is not yet finalised.

4. **SnackOrder checkout clipped** – Create `UcSnackOrder`, set host height = 600 px, assert
   `btnCheckout.Bottom <= cartPanel.Height`. Expected: FAIL on unfixed code.

**Expected Counterexamples:**

- `dgvTickets.Height == 0` and `dgvSnacks.Height == 0` in Invoice detail panel.
- `lblCustName.Right > pnlDetail.Width` in Customer detail panel.
- `chartTopMovies.Height == 0` in Dashboard wide layout.
- `btnCheckout.Bottom > cartPanel.ClientSize.Height` in SnackOrder.

### Fix Checking

**Goal:** Verify that for all inputs where `isBugCondition` holds, the fixed code satisfies
Property 1.

**Pseudocode:**

```
FOR ALL (screen, h, w) WHERE isBugCondition(screen, h, w) DO
  panel := render_fixed(screen, h, w)
  ASSERT noOverlap(panel.criticalControls)
  ASSERT allVisible(panel.criticalControls)
  ASSERT panel.AutoScrollMinSize.Height > 0  // where applicable
END FOR
```

**Test Cases (post-fix):**

1. Invoice, h ∈ {600, 700, 800, 850} px → `dgvTickets.Height > 0`, `dgvSnacks.Height > 0`,
   no overlap between `seatPreview` and `dgvTickets`.
2. Customer, w ∈ {260, 300, 350} px → `lblCustName.Right <= pnlDetail.Width`,
   `picQr.Bottom <= lblCustName.Top` is false (they are side-by-side), no overlap.
3. Dashboard wide, w ∈ {900, 1024, 1280} px → `chartTopMovies.Height >= 200`,
   `chartOccupancy.Height >= 200`.
4. Dashboard narrow, w ∈ {600, 750, 880} px → all four bottomGrid rows have `Height > 0`,
   no `IndexOutOfRangeException`.
5. SnackOrder, h ∈ {500, 600, 650} px → `btnCheckout` is reachable (either
   `btnCheckout.Bottom <= cartPanel.ClientSize.Height` or `cartPanel.AutoScroll == true`).

### Preservation Checking

**Goal:** Verify that for all inputs where `isBugCondition` is false, the fixed code produces
the same layout as the original.

**Pseudocode:**

```
FOR ALL (screen, h, w) WHERE NOT isBugCondition(screen, h, w) DO
  ASSERT layout_fixed(screen, h, w) == layout_original(screen, h, w)
END FOR
```

**Testing Approach:** Property-based testing is recommended because:

- It generates many (h, w) pairs automatically, covering edge cases near the boundary of
  `isBugCondition`.
- It provides strong guarantees that the fix does not accidentally change behaviour for
  "healthy" window sizes.
- The WinForms layout engine is deterministic for a given (h, w), so the same input always
  produces the same output.

**Test Cases:**

1. **Invoice large window** – For h ∈ [900, 1400], assert `seatPreview.Height == 160` (new
   fixed value) and `dgvTickets.Height > 0` and `dgvSnacks.Height > 0`.
2. **Customer wide panel** – For w ∈ [400, 800], assert `lblCustName.Width == pnlDetail.Width`
   (Dock=Fill) and `picQr.Size == new Size(120, 120)`.
3. **Dashboard wide, large window** – For w ∈ [900, 1600], assert
   `root.RowStyles[5].Height == 326` and `bottomGrid.RowStyles[1].Height == 284`.
4. **SnackOrder large window** – For h ∈ [700, 1200], assert `btnCheckout.Bottom <=
   cartPanel.ClientSize.Height` (no scroll needed).

### Unit Tests

- Verify `CreateDetailPanel()` in `UcInvoiceManagement` returns a panel whose row 3 height is
  160 (not 240) and whose `AutoScroll` is `true`.
- Verify `CreateDetailPanel()` in `UcCustomerManagement` returns a panel that contains a
  `TableLayoutPanel` as its first child (not absolute-positioned controls).
- Verify `ApplyResponsiveLayout()` in `UcDashboard` sets `bottomGrid.RowStyles[1].SizeType`
  to `Absolute` (not `Percent`) in the wide-layout branch.
- Verify `CreateCartSide()` in `UcSnackOrder` returns a panel with `AutoScroll == true` and
  `AutoScrollMinSize.Height >= 350`.
- Verify that after `LoadDetailAsync` is called with a valid customer ID, `lblCustName.Text`
  is non-empty and `picQr.Image` is non-null (data loading unaffected by layout change).

### Property-Based Tests

- **PBT – Invoice detail heights**: For random `panelHeight` in [300, 1400], after layout,
  `dgvTickets.Height + dgvSnacks.Height > 0` whenever `panelHeight > 384` (sum of fixed rows).
- **PBT – Customer label width**: For random `panelWidth` in [200, 800], after layout,
  `lblCustName.Width <= panelWidth` always holds (Dock=Fill guarantees this).
- **PBT – Dashboard bottomGrid row sum**: For random `rootWidth` in [560, 1600], after
  `ApplyResponsiveLayout`, `sum(bottomGrid.RowStyles[i].Height) == root.RowStyles[5].Height`.
- **PBT – SnackOrder scroll invariant**: For random `panelHeight` in [300, 1200], either
  `btnCheckout.Bottom <= cartPanel.ClientSize.Height` OR `cartPanel.AutoScroll == true`.

### Integration Tests

- Open `FrmMain` as Admin, navigate to Invoice Management, resize window to 700 px tall,
  select an invoice, verify seat map and both grids are visible.
- Open `FrmMain` as Admin, navigate to Customer Management, resize Panel2 to 300 px wide,
  select a customer, verify QR code and name label do not overlap.
- Open `FrmMain` as Admin, navigate to Dashboard, resize to 1024 px wide, verify both bottom
  charts render with non-zero height.
- Open `FrmMain` as Admin, navigate to Dashboard, resize to 800 px wide, verify layout
  switches to 1-column without exception.
- Open `FrmMain` as Staff, proceed to Snack Order step, resize window to 600 px tall, verify
  `btnCheckout` is reachable by scrolling.
