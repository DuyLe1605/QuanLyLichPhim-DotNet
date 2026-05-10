# Requirements Document

## Introduction

The Loyalty Coupon System extends CineManager's existing membership infrastructure to support reward code redemption, point-based discounts at checkout, post-payment loyalty/membership point accrual, tier-based benefits with automatic demotion, and admin coupon management. The system integrates into the existing Customer profile page and Payment Gateway, and adds a new admin management screen for coupons.
thêm tên đăng nhập vào register chưa

và tôi muốn bạn research và thiết kế module cho phần nhập mã thưởng, từ đó có thể giảm giá cho phim

mô tả chi tiết sẽ là:

-người dùng có thể nhập để kiếm điểm thưởng cho tài khoản của mình, và khi thanh toán có thể dùng điểm để chiết khấu giảm giá

-sau khi thanh toán, người dùng sẽ được cộng điểm thưởng, và cộng điểm thành viên, thành viên sẽ có thể nhiều ưu đãi hơn. sau 1 tháng, hệ thống sẽ có jobs chạy để quản lí xem nếu tháng đó không đủ chi tiêu thì giáng hạng

-admin có thể tạo và quản lí coupon điểm thưởng,(ngày hết hạn, số điểm cộng, và số lần mã có thể được nhập) nếu là 4, sẽ có 4 tài khoản dc dùng. và mỗi mã chỉ được dùng cho 1 tài khoản.

-người dùng có thể nhập trong trang hồ sơ( khi bấm sẽ hiện dialog cho nhập) hoặc là trong trang chi tiết thanh toán( giống các phần mềm thanh toán khác, khi đúng mã sẽ tự khấu trừ tiền, thanh toán xong sẽ đánh dấu là đã dùng mã thưởng đó với tài khoản dc nhập
## Glossary

- **Coupon_System**: The subsystem responsible for creating, validating, redeeming, and tracking reward/coupon codes
- **Customer**: A registered member of CineManager with a membership account (existing entity with Tier, TotalPoints, TotalSpent fields)
- **Loyalty_Points**: Points earned by redeeming coupon codes or completing payments; can be spent as discount during checkout
- **Membership_Points**: Points that accumulate based on spending and determine the Customer's tier; cannot be spent
- **Tier**: The membership rank of a Customer — "Standard", "VIP", or "Diamond" — determined by Membership_Points
- **Coupon**: A reward code created by Admin that grants Loyalty_Points when redeemed by a Customer
- **Redemption**: The act of a Customer entering a valid Coupon code to receive Loyalty_Points
- **Points_Discount**: A discount applied at checkout by converting Loyalty_Points into a monetary reduction
- **Demotion_Job**: A monthly background process that evaluates Customer spending and demotes tier if spending threshold is not met
- **Admin**: A user with administrative privileges who manages Coupons

## Requirements

### Requirement 1: Coupon Creation

**User Story:** As an Admin, I want to create reward coupons with configurable parameters, so that I can run promotional campaigns that reward customers with loyalty points.

#### Acceptance Criteria

1. THE Coupon_System SHALL allow Admin to create a Coupon with the following fields: code (unique string), points awarded (positive integer), expiration date, and maximum redemption count.
2. WHEN Admin creates a Coupon, THE Coupon_System SHALL validate that the code is unique across all existing Coupons.
3. WHEN Admin creates a Coupon with an expiration date in the past, THE Coupon_System SHALL reject the creation and display an error message.
4. WHEN Admin creates a Coupon with a maximum redemption count less than 1, THE Coupon_System SHALL reject the creation and display an error message.
5. THE Coupon_System SHALL display all Coupons in a paginated list showing code, points awarded, expiration date, maximum redemptions, current redemption count, and active status.

### Requirement 2: Coupon Management

**User Story:** As an Admin, I want to edit and deactivate existing coupons, so that I can control active promotions.

#### Acceptance Criteria

1. WHEN Admin edits a Coupon, THE Coupon_System SHALL allow modification of expiration date, maximum redemption count, and active status.
2. THE Coupon_System SHALL prevent Admin from modifying the Coupon code after creation.
3. WHEN Admin deactivates a Coupon, THE Coupon_System SHALL prevent any further redemptions of that Coupon.
4. THE Coupon_System SHALL allow Admin to filter Coupons by active status and search by code.

### Requirement 3: Coupon Redemption from Profile Page

**User Story:** As a Customer, I want to enter a reward code on my profile page, so that I can earn loyalty points without making a purchase.

#### Acceptance Criteria

1. WHEN Customer clicks the "Nhập mã thưởng" (Enter Reward Code) button on the profile page, THE Coupon_System SHALL display a dialog with a text input field and a submit button.
2. WHEN Customer submits a valid Coupon code, THE Coupon_System SHALL add the Coupon's point value to the Customer's Loyalty_Points balance.
3. WHEN Customer submits a valid Coupon code, THE Coupon_System SHALL record a PointTransaction with Type "Earn" and a description referencing the Coupon code.
4. IF Customer submits a Coupon code that does not exist, THEN THE Coupon_System SHALL display an error message "Mã không hợp lệ" (Invalid code).
5. IF Customer submits a Coupon code that has already been redeemed by the same Customer, THEN THE Coupon_System SHALL display an error message "Bạn đã sử dụng mã này rồi" (You have already used this code).
6. IF Customer submits a Coupon code that has reached its maximum redemption count, THEN THE Coupon_System SHALL display an error message "Mã đã hết lượt sử dụng" (Code has no remaining uses).
7. IF Customer submits a Coupon code that has expired, THEN THE Coupon_System SHALL display an error message "Mã đã hết hạn" (Code has expired).
8. IF Customer submits a Coupon code that is inactive, THEN THE Coupon_System SHALL display an error message "Mã không còn hoạt động" (Code is no longer active).

### Requirement 4: Coupon Redemption at Checkout

**User Story:** As a Customer, I want to enter a reward code during checkout, so that I can earn loyalty points and have the corresponding discount applied to my current payment.

#### Acceptance Criteria

1. THE Coupon_System SHALL provide a text input field on the Payment Gateway page for entering a Coupon code.
2. WHEN Customer enters a valid Coupon code at checkout, THE Coupon_System SHALL display the points that will be earned and auto-apply the equivalent discount to the order total.
3. WHEN payment completes successfully with a Coupon code applied, THE Coupon_System SHALL add the Coupon's point value to the Customer's Loyalty_Points balance and record the Redemption.
4. WHEN payment completes successfully with a Coupon code applied, THE Coupon_System SHALL mark the Coupon as used for that Customer.
5. IF Customer enters an invalid or expired Coupon code at checkout, THEN THE Coupon_System SHALL display the appropriate error message and not apply any discount.
6. THE Coupon_System SHALL allow Customer to remove an applied Coupon code before completing payment.

### Requirement 5: Points-Based Discount at Checkout

**User Story:** As a Customer, I want to use my accumulated loyalty points to get a discount when paying for tickets, so that I can benefit from my loyalty.

#### Acceptance Criteria

1. THE Coupon_System SHALL display the Customer's current Loyalty_Points balance on the Payment Gateway page.
2. WHEN Customer chooses to use points for discount, THE Coupon_System SHALL allow the Customer to specify the number of points to redeem.
3. THE Coupon_System SHALL convert points to monetary discount at a fixed rate of 1 point = 1,000 VND.
4. THE Coupon_System SHALL prevent Customer from redeeming more points than their current Loyalty_Points balance.
5. THE Coupon_System SHALL prevent the points discount from exceeding the order total amount.
6. WHEN payment completes with points redeemed, THE Coupon_System SHALL deduct the redeemed points from the Customer's Loyalty_Points balance and record a PointTransaction with Type "Redeem".

### Requirement 6: Post-Payment Point Accrual

**User Story:** As a Customer, I want to earn loyalty points and membership points after completing a payment, so that I can accumulate rewards and advance my membership tier.

#### Acceptance Criteria

1. WHEN a payment completes successfully, THE Coupon_System SHALL award Loyalty_Points to the Customer based on the payment amount at a rate of 1 point per 10,000 VND spent.
2. WHEN a payment completes successfully, THE Coupon_System SHALL award Membership_Points to the Customer based on the payment amount at a rate of 1 point per 10,000 VND spent.
3. WHEN a payment completes successfully, THE Coupon_System SHALL record a PointTransaction with Type "Earn" and a description referencing the Booking code.
4. THE Coupon_System SHALL update the Customer's TotalSpent field by adding the payment amount.

### Requirement 7: Tier Promotion

**User Story:** As a Customer, I want to be automatically promoted to a higher tier when my spending reaches the threshold, so that I can enjoy better benefits.

#### Acceptance Criteria

1. WHEN Customer's TotalSpent reaches 2,000,000 VND, THE Coupon_System SHALL promote the Customer from "Standard" to "VIP" tier.
2. WHEN Customer's TotalSpent reaches 10,000,000 VND, THE Coupon_System SHALL promote the Customer from "VIP" to "Diamond" tier.
3. WHEN a tier promotion occurs, THE Coupon_System SHALL display a notification to the Customer.

### Requirement 8: Tier Benefits

**User Story:** As a Customer, I want to receive better rewards based on my membership tier, so that higher tiers feel more valuable.

#### Acceptance Criteria

1. WHILE Customer has "VIP" tier, THE Coupon_System SHALL award a 1.5x multiplier on Loyalty_Points earned from payments.
2. WHILE Customer has "Diamond" tier, THE Coupon_System SHALL award a 2x multiplier on Loyalty_Points earned from payments.
3. WHILE Customer has "Standard" tier, THE Coupon_System SHALL award a 1x multiplier on Loyalty_Points earned from payments.

### Requirement 9: Monthly Tier Demotion

**User Story:** As a system operator, I want the system to automatically demote customers who do not maintain sufficient spending, so that tier benefits remain meaningful.

#### Acceptance Criteria

1. THE Demotion_Job SHALL execute once per month on the first day of the month.
2. WHEN the Demotion_Job executes, THE Demotion_Job SHALL evaluate each Customer's spending in the previous calendar month.
3. IF a "Diamond" Customer spent less than 500,000 VND in the previous month, THEN THE Demotion_Job SHALL demote the Customer to "VIP" tier.
4. IF a "VIP" Customer spent less than 200,000 VND in the previous month, THEN THE Demotion_Job SHALL demote the Customer to "Standard" tier.
5. WHEN a demotion occurs, THE Demotion_Job SHALL record the demotion event in the Customer's PointTransaction history with a descriptive message.

### Requirement 10: Coupon Redemption Tracking

**User Story:** As an Admin, I want to track which customers have redeemed which coupons, so that I can monitor campaign effectiveness.

#### Acceptance Criteria

1. THE Coupon_System SHALL maintain a record of each Redemption including the Customer, Coupon, and timestamp.
2. THE Coupon_System SHALL enforce that each Customer can redeem a specific Coupon at most once.
3. WHEN a Coupon is redeemed, THE Coupon_System SHALL increment the Coupon's used count.
4. THE Coupon_System SHALL allow Admin to view the list of redemptions for a specific Coupon.
