import { useMutation } from "@tanstack/react-query";
import { useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { bookingsApi } from "../api/bookings.api";
import { BookingSummary } from "../components/booking/BookingSummary";
import { SeatMap } from "../components/booking/SeatMap";
import { useSeatMap } from "../hooks/useBookings";
import type { Seat } from "../lib/types";

export function BookingPage() {
  const { showtimeId = "" } = useParams();
  const navigate = useNavigate();
  const [selectedIds, setSelectedIds] = useState<number[]>([]);
  const [voucherCode, setVoucherCode] = useState("");
  const { data } = useSeatMap(showtimeId);
  const selectedSeats = useMemo(() => data?.seats.filter((s) => selectedIds.includes(s.id)) ?? [], [data, selectedIds]);

  const mutation = useMutation({
    mutationFn: () => bookingsApi.create({ showtimeId: Number(showtimeId), seatIds: selectedIds, voucherCode: voucherCode || undefined }),
    onSuccess: () => navigate("/profile")
  });

  const toggleSeat = (seat: Seat) => {
    setSelectedIds((current) => current.includes(seat.id) ? current.filter((id) => id !== seat.id) : [...current, seat.id]);
  };

  if (!data) return <div className="page narrow">Đang tải sơ đồ ghế...</div>;

  return (
    <div className="page narrow booking-layout">
      <SeatMap data={data} selectedIds={selectedIds} onToggle={toggleSeat} />
      <div className="booking-side">
        <BookingSummary seats={selectedSeats} />
        <input value={voucherCode} onChange={(e) => setVoucherCode(e.target.value)} placeholder="Mã giảm giá" />
        <button className="primary-button full" disabled={!selectedIds.length || mutation.isPending} onClick={() => mutation.mutate()}>
          Xác nhận đặt vé
        </button>
        {mutation.error && <p className="error">Không thể đặt vé. Vui lòng thử lại.</p>}
      </div>
    </div>
  );
}
