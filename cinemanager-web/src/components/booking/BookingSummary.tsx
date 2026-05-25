import type { Seat, SnackSelection } from "../../lib/types";
import { formatCurrency } from "../../lib/utils";

export function BookingSummary({
  seats,
  snacks = [],
  discount = 0,
  pointsDiscount = 0
}: {
  seats: Seat[];
  snacks?: SnackSelection[];
  discount?: number;
  pointsDiscount?: number;
}) {
  const ticketSubtotal = seats.reduce((sum, seat) => sum + seat.price, 0);
  const snackSubtotal = snacks.reduce((sum, item) => sum + item.snack.price * item.quantity, 0);
  const subtotal = ticketSubtotal + snackSubtotal;
  const totalDiscount = discount + pointsDiscount;

  return (
    <aside className="summary-panel">
      <h2>Xác nhận</h2>
      <p>Ghế: {seats.map((s) => s.label).join(", ") || "Chưa chọn"}</p>
      {snacks.length > 0 && (
        <div className="summary-snacks">
          {snacks.map((item) => (
            <span key={item.snack.id}>
              {item.snack.name} x{item.quantity}
            </span>
          ))}
        </div>
      )}
      <dl>
        <div><dt>Vé</dt><dd>{formatCurrency(ticketSubtotal)}</dd></div>
        <div><dt>Bắp nước</dt><dd>{formatCurrency(snackSubtotal)}</dd></div>
        <div><dt>Voucher</dt><dd>{formatCurrency(discount)}</dd></div>
        <div><dt>Điểm</dt><dd>{formatCurrency(pointsDiscount)}</dd></div>
        <div className="total"><dt>Tổng</dt><dd>{formatCurrency(Math.max(0, subtotal - totalDiscount))}</dd></div>
      </dl>
    </aside>
  );
}
