import type { Booking } from "../../lib/types";
import { formatCurrency, formatDateTime } from "../../lib/utils";

export function BookingHistory({ bookings }: { bookings: Booking[] }) {
  return (
    <div className="stack">
      {bookings.map((booking) => (
        <article className="history-row" key={booking.bookingCode}>
          <div>
            <strong>{booking.showtime?.movie ?? booking.bookingCode}</strong>
            <span>{formatDateTime(booking.showtime?.startTime ?? booking.createdAt)}</span>
          </div>
          <div>
            <strong>{formatCurrency(booking.totalAmount)}</strong>
            <span>{booking.status}</span>
          </div>
        </article>
      ))}
    </div>
  );
}
