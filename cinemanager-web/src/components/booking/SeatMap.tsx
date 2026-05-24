import type { Seat, SeatMapResponse } from "../../lib/types";

type SeatMapProps = {
  data: SeatMapResponse;
  selectedIds: number[];
  onToggle: (seat: Seat) => void;
};

export function SeatMap({ data, selectedIds, onToggle }: SeatMapProps) {
  return (
    <section className="seat-zone">
      <div className="screen">MÀN HÌNH</div>
      <div className="seat-grid" style={{ gridTemplateColumns: `repeat(${data.room.columns}, minmax(34px, 1fr))` }}>
        {data.seats.map((seat) => {
          const selected = selectedIds.includes(seat.id);
          return (
            <button
              key={seat.id}
              className={`seat ${seat.status} ${selected ? "selected" : ""} ${seat.type.toLowerCase()}`}
              disabled={seat.status === "sold"}
              onClick={() => onToggle(seat)}
              title={`${seat.label} · ${seat.type}`}
            >
              {seat.label}
            </button>
          );
        })}
      </div>
      <div className="seat-legend">
        <span><i className="seat-dot available" /> Trống</span>
        <span><i className="seat-dot selected" /> Chọn</span>
        <span><i className="seat-dot sold" /> Đã bán</span>
        <span><i className="seat-dot vip" /> VIP/Couple</span>
      </div>
    </section>
  );
}
