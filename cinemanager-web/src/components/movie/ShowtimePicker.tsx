import { Link } from "react-router-dom";
import type { Showtime } from "../../lib/types";

export function ShowtimePicker({ showtimes }: { showtimes: Showtime[] }) {
  if (!showtimes.length) return <p className="muted">Chưa có suất chiếu cho ngày này.</p>;

  return (
    <div className="showtime-grid">
      {showtimes.map((showtime) => (
        <Link key={showtime.id} className="showtime-button" to={`/booking/${showtime.id}`}>
          <strong>{new Date(showtime.startTime).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })}</strong>
          <span>{showtime.room.name} · {showtime.room.type}</span>
        </Link>
      ))}
    </div>
  );
}
