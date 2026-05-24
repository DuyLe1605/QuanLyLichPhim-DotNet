import type { Seat } from "../../lib/types";
import { formatCurrency } from "../../lib/utils";

export function BookingSummary({ seats, discount = 0 }: { seats: Seat[]; discount?: number }) {
  const subtotal = seats.reduce((sum, seat) => sum + seat.price, 0);
  return (
    <aside className="summary-panel">
      <h2>Xác nhận</h2>
      <p>Ghế: {seats.map((s) => s.label).join(", ") || "Chưa chọn"}</p>
      <dl>
        <div><dt>Vé</dt><dd>{formatCurrency(subtotal)}</dd></div>
        <div><dt>Giảm</dt><dd>{formatCurrency(discount)}</dd></div>
        <div className="total"><dt>Tổng</dt><dd>{formatCurrency(Math.max(0, subtotal - discount))}</dd></div>
      </dl>
    </aside>
  );
}
