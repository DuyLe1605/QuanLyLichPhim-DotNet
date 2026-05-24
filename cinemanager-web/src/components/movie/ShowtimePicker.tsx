import { Link } from "react-router-dom";
import type { Showtime } from "../../lib/types";

export function ShowtimePicker({ showtimes }: { showtimes: Showtime[] }) {
  if (!showtimes.length) return <p className="muted">Chưa có suất chiếu cho ngày này.</p>;

  // Group by hardcoded cinema name for now since API doesn't provide cinema branch yet
  return (
    <div className="cinema-group">
      <div className="cinema-header">
        <img src="https://ui-avatars.com/api/?name=Star+Cinema&background=1f2937&color=8cc63f" alt="Star Cinema" />
        <span>Star Cinema (Tất cả cụm rạp)</span>
      </div>
      <div className="showtime-grid">
        {showtimes.map((showtime) => (
          <div key={showtime.id} style={{ display: 'flex', flexDirection: 'column', alignItems: 'center' }}>
            <Link className="showtime-button" to={`/booking/${showtime.id}`}>
              {new Date(showtime.startTime).toLocaleTimeString("vi-VN", { hour: "2-digit", minute: "2-digit" })}
            </Link>
            <span className="showtime-type">Phụ đề {showtime.room.type}</span>
          </div>
        ))}
      </div>
    </div>
  );
}
