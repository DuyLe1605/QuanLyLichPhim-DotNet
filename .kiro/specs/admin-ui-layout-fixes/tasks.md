# Implementation Plan

- [ ] 1. Write bug condition exploration tests (BEFORE implementing any fix)
  - **Property 1: Bug Condition** - Admin UI Layout Overflow
  - **CRITICAL**: These tests MUST FAIL on unfixed code — failure confirms the bugs exist
  - **DO NOT attempt to fix the tests or the code when they fail**
  - **NOTE**: Tests encode the expected behavior; they will validate the fix when they pass after implementation
  - **GOAL**: Surface counterexamples that demonstrate each overflow on unfixed code
  - **Scoped PBT Approach**: Scope each property to the concrete failing window sizes defined in `isBugCondition`
  - Create test file `BaiTapLon.Tests/AdminUiLayoutBugConditionTests.cs`
  - **Test 1.1 – UcInvoiceManagement detail panel overflow**
    - Instantiate `UcInvoiceManagement` in a host `Form` with `ClientSize.Height = 700`
    - Force layout pass (`PerformLayout()` / `Update()`)
    - Assert `dgvTickets.Height > 0` AND `dgvSnacks.Height > 0`
    - From Bug Condition: `isBugCondition(InvoiceManagement, h=700, w=any)` → true
    - **EXPECTED OUTCOME on unfixed code**: FAIL — both heights collapse to 0 (fixed rows sum 464 px > panel height)
    - Document counterexample: `dgvTickets.Height == 0`, `dgvSnacks.Height == 0`
  - **Test 1.2 – UcCustomerManagement label overflow**
    - Instantiate `UcCustomerManagement`, set `Panel2.Width = 300`
    - Force layout pass
    - Assert `lblCustName.Right <= pnlDetail.Width`
    - From Bug Condition: `isBugCondition(CustomerManagement, h=any, w=300)` → true
    - **EXPECTED OUTCOME on unfixed code**: FAIL — `lblCustName.Right = 345 > 300`
    - Document counterexample: `lblCustName.Right = 345`, `pnlDetail.Width = 300`
  - **Test 1.3 – UcDashboard chart collapse (wide layout)**
    - Instantiate `UcDashboard` in a host `Form` with `ClientSize.Width = 1200`
    - Call `ApplyResponsiveLayout()` (via reflection or test-visible method)
    - Assert `chartTopMovies.Height > 0` AND `chartOccupancy.Height > 0`
    - From Bug Condition: `isBugCondition(Dashboard, h=any, w=1200)` → true
    - **EXPECTED OUTCOME on unfixed code**: FAIL — `SizeType.Percent, 100` row collapses to 0 before parent height is finalised
    - Document counterexample: `chartTopMovies.Height == 0`
  - **Test 1.4 – UcSnackOrder checkout button clipped**
    - Instantiate `UcSnackOrder` in a host `Form` with `ClientSize.Height = 600`
    - Force layout pass
    - Assert `btnCheckout.Bottom <= cartPanel.ClientSize.Height` OR `cartPanel.AutoScroll == true`
    - From Bug Condition: `isBugCondition(SnackOrder, h=600, w=any)` → true
    - **EXPECTED OUTCOME on unfixed code**: FAIL — `btnCheckout.Bottom > cartPanel.ClientSize.Height` and `AutoScroll == false`
    - Document counterexample: `btnCheckout.Bottom = 378`, `cartPanel.ClientSize.Height = 350`
  - Run all four tests on UNFIXED code
  - **EXPECTED OUTCOME**: All four tests FAIL (this is correct — it proves the bugs exist)
  - Mark task complete when tests are written, run, and failures are documented
  - _Requirements: 1.1, 1.2, 1.3, 1.4, 1.5, 1.6, 1.7_

- [ ] 2. Write preservation property tests (BEFORE implementing any fix)
  - **Property 2: Preservation** - Non-Buggy Window Sizes Produce Correct Layout
  - **IMPORTANT**: Follow observation-first methodology
  - Observe behavior on UNFIXED code for inputs where `isBugCondition` returns false
  - Create test file `BaiTapLon.Tests/AdminUiLayoutPreservationTests.cs`
  - **Test 2.1 – UcInvoiceManagement large window (h ≥ 900)**
    - Observe: at `h = 1000`, `seatPreview.Height == 240` (current Absolute value), `dgvTickets.Height > 0`
    - Write PBT: for random `panelHeight` in [900, 1400], after layout, `dgvTickets.Height > 0` AND `dgvSnacks.Height > 0`
    - Non-bug condition: `isBugCondition(InvoiceManagement, h, w)` is false when `h >= 900`
    - Verify test PASSES on unfixed code
  - **Test 2.2 – UcCustomerManagement wide panel (w ≥ 400)**
    - Observe: at `w = 500`, `lblCustName.Right <= pnlDetail.Width` (no overflow)
    - Write PBT: for random `panelWidth` in [400, 800], `lblCustName.Right <= pnlDetail.Width`
    - Non-bug condition: `isBugCondition(CustomerManagement, h, w)` is false when `w >= 400`
    - Verify test PASSES on unfixed code
  - **Test 2.3 – UcDashboard narrow layout (w < 900)**
    - Observe: at `w = 800`, narrow layout uses all `SizeType.Absolute` rows, no collapse
    - Write PBT: for random `width` in [560, 899], after `ApplyResponsiveLayout()`, all `bottomGrid.RowStyles` have `Height > 0` and no `IndexOutOfRangeException` is thrown
    - Non-bug condition: narrow layout is already correct (all Absolute rows)
    - Verify test PASSES on unfixed code
  - **Test 2.4 – UcSnackOrder large window (h ≥ 700)**
    - Observe: at `h = 800`, `btnCheckout.Bottom <= cartPanel.ClientSize.Height`
    - Write PBT: for random `panelHeight` in [700, 1200], `btnCheckout.Bottom <= cartPanel.ClientSize.Height`
    - Non-bug condition: `isBugCondition(SnackOrder, h, w)` is false when `h >= 700`
    - Verify test PASSES on unfixed code
  - Run all preservation tests on UNFIXED code
  - **EXPECTED OUTCOME**: All preservation tests PASS (confirms baseline behavior to preserve)
  - Mark task complete when tests are written, run, and passing on unfixed code
  - _Requirements: 3.1, 3.2, 3.3, 3.4, 3.5, 3.6, 3.7_

- [ ] 3. Fix UcInvoiceManagement – Reduce seatPreview row height and add AutoScroll

  - [ ] 3.1 Implement the fix in `CreateDetailPanel()` (`UcInvoiceManagement.cs`)
    - Change `new RowStyle(SizeType.Absolute, 240)` (row index 3, `seatPreview` row) to `new RowStyle(SizeType.Absolute, 160)`
    - After the `panel.Padding = new Padding(14)` line, add `panel.AutoScroll = true` and `panel.AutoScrollMinSize = new Size(0, 500)`
    - No other changes to the method — all seven controls remain at the same row indices
    - Total fixed rows after fix: 34 + 128 + 30 + 160 + 32 = 384 px (was 464 px)
    - _Bug_Condition: `isBugCondition(InvoiceManagement, h, w)` where `h < 900`_
    - _Expected_Behavior: `dgvTickets.Height > 0` AND `dgvSnacks.Height > 0`; panel scrollable when content overflows_
    - _Preservation: `ApplyInvoiceLayout()` and `SplitterDistance` logic unchanged; `LoadSelectedDetailAsync()` data binding unchanged_
    - _Requirements: 2.1, 2.2, 3.1, 3.6, 3.7_

  - [ ] 3.2 Verify bug condition exploration test now passes (Invoice)
    - **Property 1: Expected Behavior** - Invoice Detail Panel Overflow Fixed
    - **IMPORTANT**: Re-run the SAME test from task 1 (Test 1.1) — do NOT write a new test
    - Run `UcInvoiceManagement` bug condition test from step 1 against fixed code
    - **EXPECTED OUTCOME**: Test PASSES — `dgvTickets.Height > 0` AND `dgvSnacks.Height > 0` at `h = 700`
    - _Requirements: 2.1, 2.2_

  - [ ] 3.3 Verify preservation tests still pass (Invoice)
    - **Property 2: Preservation** - Invoice Large Window Behavior Unchanged
    - **IMPORTANT**: Re-run the SAME test from task 2 (Test 2.1) — do NOT write a new test
    - Run preservation PBT for `panelHeight` in [900, 1400] against fixed code
    - **EXPECTED OUTCOME**: Test PASSES — `dgvTickets.Height > 0` and `dgvSnacks.Height > 0` for all large heights

- [ ] 4. Fix UcCustomerManagement – Replace absolute positioning with TableLayoutPanel

  - [ ] 4.1 Implement the fix in `CreateDetailPanel()` (`UcCustomerManagement.cs`)
    - Replace the entire body of `CreateDetailPanel()` with a `TableLayoutPanel`-based layout as specified in design.md
    - Create `outerTlp` (`TableLayoutPanel`, `Dock = DockStyle.Fill`) with 6 rows:
      - Row 0: `Absolute 130` — `topRow` (inner TLP: col 0 = `picQr` 130 px wide, col 1 = `nameInfoStack`)
      - Row 1: `Absolute 1` — separator `Panel`
      - Row 2: `Absolute 90` — `lblCustStats` (`Dock = DockStyle.Fill`)
      - Row 3: `Absolute 1` — separator `Panel`
      - Row 4: `Absolute 24` — "Lịch sử điểm thưởng" label (`Dock = DockStyle.Fill`)
      - Row 5: `Percent 100` — `dgvHistory` (`Dock = DockStyle.Fill`)
    - `topRow` is a `TableLayoutPanel` with 2 columns (col 0 = `Absolute 130`, col 1 = `Percent 100`):
      - Col 0: `picQr` (`Size = new Size(120, 120)`, centered, keep all style properties)
      - Col 1: `nameInfoStack` (`TableLayoutPanel`, `Dock = DockStyle.Fill`, row 0 = `Absolute 30` for `lblCustName`, row 1 = `Percent 100` for `lblCustInfo`)
    - Remove all `Location = new Point(...)` assignments from `picQr`, `lblCustName`, `lblCustInfo`, `lblCustStats`, `dgvHistory`
    - Remove `Size = new Size(200, ...)` from `lblCustName` and `lblCustInfo`; set `Dock = DockStyle.Fill` on both
    - Remove `MaximumSize` from `lblCustInfo`; keep `AutoSize = false`
    - Set `panel.AutoScrollMinSize = new Size(0, 520)`
    - `dgvHistory` keeps all style properties; remove `Location`, `Size`, `Anchor`; set `Dock = DockStyle.Fill`
    - `pnlDetail` field still holds the returned `Panel`; `LoadDetailAsync` assigns to the same field references — no changes needed there
    - _Bug_Condition: `isBugCondition(CustomerManagement, h, w)` where `w < 400`_
    - _Expected_Behavior: `lblCustName.Right <= pnlDetail.Width`; no control overlap; `picQr` and `lblCustName` side-by-side_
    - _Preservation: `LoadDetailAsync` data binding (`picQr.Image`, `lblCustName.Text`, `lblCustInfo.Text`, `lblCustStats.Text`, `dgvHistory.DataSource`) unchanged; `ApplyCustomerSplitLayout()` unchanged_
    - _Requirements: 2.3, 2.4, 3.2, 3.6_

  - [ ] 4.2 Verify bug condition exploration test now passes (Customer)
    - **Property 1: Expected Behavior** - Customer Detail Panel Overflow Fixed
    - **IMPORTANT**: Re-run the SAME test from task 1 (Test 1.2) — do NOT write a new test
    - Run `UcCustomerManagement` bug condition test from step 1 against fixed code at `panelWidth = 300`
    - **EXPECTED OUTCOME**: Test PASSES — `lblCustName.Right <= pnlDetail.Width`
    - _Requirements: 2.3, 2.4_

  - [ ] 4.3 Verify preservation tests still pass (Customer)
    - **Property 2: Preservation** - Customer Wide Panel Behavior Unchanged
    - **IMPORTANT**: Re-run the SAME test from task 2 (Test 2.2) — do NOT write a new test
    - Run preservation PBT for `panelWidth` in [400, 800] against fixed code
    - **EXPECTED OUTCOME**: Test PASSES — `lblCustName.Right <= pnlDetail.Width` for all wide panel widths

- [ ] 5. Fix UcDashboard – Change bottomGrid row 1 from Percent to Absolute in wide layout

  - [ ] 5.1 Implement the fix in `ApplyResponsiveLayout()` (`UcDashboard.cs`)
    - In the `else` branch (wide layout, `width >= 900`), change:
      ```csharp
      bottomGrid.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
      ```
      to:
      ```csharp
      bottomGrid.RowStyles.Add(new RowStyle(SizeType.Absolute, 284));
      ```
    - `284 = 326 − 42` (root row 5 height minus the section-title row height)
    - No other changes to `ApplyResponsiveLayout()` — the narrow-layout branch is already correct
    - The `root.Height` recalculation loop at the end already sums Absolute rows correctly
    - _Bug_Condition: `isBugCondition(Dashboard, h, w)` where `w >= 900`_
    - _Expected_Behavior: `chartTopMovies.Height >= 200` AND `chartOccupancy.Height >= 200` in wide layout_
    - _Preservation: narrow layout (`w < 900`) unchanged; stat cards responsive layout unchanged; `LoadChartsAsync` data binding unchanged_
    - _Requirements: 2.5, 2.6, 3.3, 3.4_

  - [ ] 5.2 Verify bug condition exploration test now passes (Dashboard)
    - **Property 1: Expected Behavior** - Dashboard Chart Collapse Fixed
    - **IMPORTANT**: Re-run the SAME test from task 1 (Test 1.3) — do NOT write a new test
    - Run `UcDashboard` bug condition test from step 1 against fixed code at `width = 1200`
    - **EXPECTED OUTCOME**: Test PASSES — `chartTopMovies.Height > 0` AND `chartOccupancy.Height > 0`
    - _Requirements: 2.5, 2.6_

  - [ ] 5.3 Verify preservation tests still pass (Dashboard)
    - **Property 2: Preservation** - Dashboard Narrow Layout Behavior Unchanged
    - **IMPORTANT**: Re-run the SAME test from task 2 (Test 2.3) — do NOT write a new test
    - Run preservation PBT for `width` in [560, 899] against fixed code
    - **EXPECTED OUTCOME**: Test PASSES — all `bottomGrid.RowStyles` have `Height > 0`, no exception thrown

- [ ] 6. Fix UcSnackOrder – Add AutoScroll to cart panel

  - [ ] 6.1 Implement the fix in `CreateCartSide()` (`UcSnackOrder.cs`)
    - After `panel.Padding = new Padding(14)`, add:
      ```csharp
      panel.AutoScroll = true;
      panel.AutoScrollMinSize = new Size(0, 350);
      ```
    - No row heights change; no control assignments change; no event handlers change
    - Fixed rows sum: 40 + 72 + 34 + 34 + 42 + 38 + 36 + 54 = 350 px (plus one Percent row for `dgvCart`)
    - _Bug_Condition: `isBugCondition(SnackOrder, h, w)` where `h < 700`_
    - _Expected_Behavior: `btnCheckout` reachable — either `btnCheckout.Bottom <= cartPanel.ClientSize.Height` OR `cartPanel.AutoScroll == true`_
    - _Preservation: checkout logic (`BtnCheckout_Click`, `CalculateChange`, `RefreshCart`) completely unchanged; `CreateMenuSide()` unchanged_
    - _Requirements: 2.7, 3.5_

  - [ ] 6.2 Verify bug condition exploration test now passes (SnackOrder)
    - **Property 1: Expected Behavior** - SnackOrder Checkout Button Reachable
    - **IMPORTANT**: Re-run the SAME test from task 1 (Test 1.4) — do NOT write a new test
    - Run `UcSnackOrder` bug condition test from step 1 against fixed code at `h = 600`
    - **EXPECTED OUTCOME**: Test PASSES — `cartPanel.AutoScroll == true` (button reachable via scroll)
    - _Requirements: 2.7_

  - [ ] 6.3 Verify preservation tests still pass (SnackOrder)
    - **Property 2: Preservation** - SnackOrder Large Window Behavior Unchanged
    - **IMPORTANT**: Re-run the SAME test from task 2 (Test 2.4) — do NOT write a new test
    - Run preservation PBT for `panelHeight` in [700, 1200] against fixed code
    - **EXPECTED OUTCOME**: Test PASSES — `btnCheckout.Bottom <= cartPanel.ClientSize.Height` for all large heights (no scroll needed)

- [ ] 7. Checkpoint – Ensure all tests pass
  - Re-run the full test suite (`dotnet test BaiTapLon.Tests`)
  - Confirm all four bug condition tests (Property 1) now PASS
  - Confirm all four preservation tests (Property 2) still PASS
  - Confirm no regressions in existing tests
  - Manually verify each screen at a buggy window size to confirm visual fix:
    - `UcInvoiceManagement` at window height 700 px — `dgvTickets` and `dgvSnacks` visible
    - `UcCustomerManagement` at panel width 300 px — QR code and name label side-by-side, no overflow
    - `UcDashboard` at window width 1200 px — both bottom charts render with non-zero height
    - `UcSnackOrder` at window height 600 px — `btnCheckout` reachable by scrolling
  - Ask the user if any questions arise before closing the spec
