import { useMutation } from "@tanstack/react-query";
import { CheckCircle2, Minus, Plus, QrCode } from "lucide-react";
import { useMemo, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { bookingsApi, type VoucherValidationResult } from "../api/bookings.api";
import { BookingSummary } from "../components/booking/BookingSummary";
import { SeatMap } from "../components/booking/SeatMap";
import { useSeatMap } from "../hooks/useBookings";
import { useProfile } from "../hooks/useProfile";
import { useSnacks } from "../hooks/useSnacks";
import type { Seat, Snack, SnackSelection } from "../lib/types";
import { formatCurrency, getImageUrl } from "../lib/utils";

const POINT_VALUE = 100;

export function BookingPage() {
  const { showtimeId = "" } = useParams();
  const navigate = useNavigate();
  const [selectedIds, setSelectedIds] = useState<number[]>([]);
  const [snackQuantities, setSnackQuantities] = useState<Record<number, number>>({});
  const [voucherCode, setVoucherCode] = useState("");
  const [appliedVoucher, setAppliedVoucher] = useState<{ code: string; discount: number } | null>(null);
  const [voucherMessage, setVoucherMessage] = useState("");
  const [pointsToRedeem, setPointsToRedeem] = useState(0);
  const [paymentReady, setPaymentReady] = useState(false);
  const { data } = useSeatMap(showtimeId);
  const { data: snacks = [] } = useSnacks();
  const { data: profile } = useProfile();

  const loyaltyPoints = profile?.loyaltyPoints ?? profile?.totalPoints ?? 0;

  const selectedSeats = useMemo(
    () => data?.seats.filter((s) => selectedIds.includes(s.id)) ?? [],
    [data, selectedIds]
  );

  const selectedSnacks: SnackSelection[] = useMemo(
    () =>
      snacks
        .map((snack) => ({ snack, quantity: snackQuantities[snack.id] ?? 0 }))
        .filter((item) => item.quantity > 0),
    [snacks, snackQuantities]
  );

  const grandTotal = useMemo(() => {
    const tickets = selectedSeats.reduce((sum, seat) => sum + seat.price, 0);
    const snackTotal = selectedSnacks.reduce((sum, item) => sum + item.snack.price * item.quantity, 0);
    return tickets + snackTotal;
  }, [selectedSeats, selectedSnacks]);

  const voucherDiscount = appliedVoucher?.discount ?? 0;
  const totalAfterVoucher = Math.max(0, grandTotal - voucherDiscount);
  const maxUsablePoints = Math.min(loyaltyPoints, Math.ceil(totalAfterVoucher / POINT_VALUE));
  const safePointsToRedeem = Math.min(pointsToRedeem, maxUsablePoints);
  const pointsDiscount = Math.min(safePointsToRedeem * POINT_VALUE, totalAfterVoucher);
  const payableTotal = Math.max(0, totalAfterVoucher - pointsDiscount);

  const resetFinalStepAdjustments = () => {
    setPaymentReady(false);
    setAppliedVoucher(null);
    setVoucherMessage("");
    setPointsToRedeem(0);
  };

  const mutation = useMutation({
    mutationFn: () =>
      bookingsApi.create({
        showtimeId: Number(showtimeId),
        seatIds: selectedIds,
        voucherCode: appliedVoucher?.code,
        pointsToRedeem: safePointsToRedeem,
        snacks: selectedSnacks.map((item) => ({ snackId: item.snack.id, quantity: item.quantity }))
      }),
    onSuccess: () => navigate("/profile")
  });

  const voucherMutation = useMutation({
    mutationFn: () => bookingsApi.validateVoucher(voucherCode.trim(), grandTotal),
    onSuccess: (result: VoucherValidationResult) => {
      setAppliedVoucher({ code: result.code, discount: result.discount });
      setVoucherMessage(`Áp dụng ${result.code}: giảm ${formatCurrency(result.discount)}.`);
      setPointsToRedeem(0);
    },
    onError: () => {
      setAppliedVoucher(null);
      setVoucherMessage("Mã voucher không hợp lệ hoặc đã hết hạn.");
      setPointsToRedeem(0);
    }
  });

  const toggleSeat = (seat: Seat) => {
    resetFinalStepAdjustments();
    setSelectedIds((current) => current.includes(seat.id) ? current.filter((id) => id !== seat.id) : [...current, seat.id]);
  };

  const changeSnackQuantity = (snack: Snack, delta: number) => {
    resetFinalStepAdjustments();
    setSnackQuantities((current) => {
      const next = Math.max(0, Math.min(10, (current[snack.id] ?? 0) + delta));
      return { ...current, [snack.id]: next };
    });
  };

  if (!data) return <div className="page narrow">Đang tải sơ đồ ghế...</div>;

  return (
    <div className="page narrow booking-flow">
      <div className="booking-main">
        <section>
          <div className="booking-step">
            <span>1</span>
            <div>
              <h1>Chọn ghế</h1>
              <p>Suất chiếu tại {data.room.name}</p>
            </div>
          </div>
          <SeatMap data={data} selectedIds={selectedIds} onToggle={toggleSeat} />
        </section>

        <section className="snack-section">
          <div className="booking-step">
            <span>2</span>
            <div>
              <h2>Bắp nước</h2>
              <p>Thêm combo để nhận tại quầy cùng mã đặt vé.</p>
            </div>
          </div>
          <div className="snack-picker-grid">
            {snacks.map((snack) => (
              <article className="snack-picker-card" key={snack.id}>
                <img src={getImageUrl(snack.imageUrl)} alt={snack.name} />
                <div>
                  <strong>{snack.name}</strong>
                  <span>{formatCurrency(snack.price)}</span>
                </div>
                <div className="quantity-control">
                  <button type="button" className="icon-button" onClick={() => changeSnackQuantity(snack, -1)} aria-label={`Giảm ${snack.name}`}>
                    <Minus size={16} />
                  </button>
                  <strong>{snackQuantities[snack.id] ?? 0}</strong>
                  <button type="button" className="icon-button" onClick={() => changeSnackQuantity(snack, 1)} aria-label={`Tăng ${snack.name}`}>
                    <Plus size={16} />
                  </button>
                </div>
              </article>
            ))}
          </div>
        </section>
      </div>

      <div className="booking-side">
        <BookingSummary
          seats={selectedSeats}
          snacks={selectedSnacks}
          discount={voucherDiscount}
          pointsDiscount={pointsDiscount}
        />

        <section className={`payment-panel ${paymentReady ? "ready" : ""}`}>
          <div className="qr-placeholder">
            {paymentReady ? <CheckCircle2 size={42} /> : <QrCode size={42} />}
          </div>
          <div>
            <strong>{paymentReady ? "Bước cuối tính tiền" : "Thanh toán QR"}</strong>
            <p>{paymentReady ? `Số tiền cần thanh toán: ${formatCurrency(payableTotal)}.` : "Kiểm tra đơn trước khi sang bước cuối."}</p>
          </div>
        </section>

        {paymentReady && (
          <>
            <section className="voucher-panel">
              <label htmlFor="voucher-code">Voucher</label>
              <div className="voucher-row">
                <input
                  id="voucher-code"
                  value={voucherCode}
                  onChange={(e) => {
                    setVoucherCode(e.target.value.toUpperCase());
                    setAppliedVoucher(null);
                    setVoucherMessage("");
                    setPointsToRedeem(0);
                  }}
                  placeholder="Nhập mã voucher"
                />
                <button
                  className="ghost-button"
                  disabled={!voucherCode.trim() || voucherMutation.isPending || grandTotal <= 0}
                  onClick={() => voucherMutation.mutate()}
                  type="button"
                >
                  Kiểm tra
                </button>
              </div>
              {voucherMessage && <p className={appliedVoucher ? "voucher-ok" : "error"}>{voucherMessage}</p>}
            </section>

            <section className="points-panel">
              <label htmlFor="points-to-redeem">Dùng điểm</label>
              <div className="points-meta">
                <span>Bạn có {loyaltyPoints.toLocaleString("vi-VN")} điểm</span>
                <span>1 điểm = {formatCurrency(POINT_VALUE)}</span>
              </div>
              <input
                id="points-to-redeem"
                type="number"
                min={0}
                max={maxUsablePoints}
                value={pointsToRedeem}
                onChange={(e) => {
                  const value = Number(e.target.value);
                  setPointsToRedeem(Number.isFinite(value) ? Math.max(0, Math.min(maxUsablePoints, Math.floor(value))) : 0);
                }}
              />
              <p className="voucher-ok">
                Tối đa {maxUsablePoints.toLocaleString("vi-VN")} điểm, giảm {formatCurrency(pointsDiscount)}.
              </p>
            </section>
          </>
        )}

        {!paymentReady ? (
          <button className="primary-button full" disabled={!selectedIds.length} onClick={() => setPaymentReady(true)}>
            Xác nhận thanh toán
          </button>
        ) : (
          <button className="primary-button full" disabled={mutation.isPending} onClick={() => mutation.mutate()}>
            Hoàn tất đặt vé
          </button>
        )}
        {mutation.error && <p className="error">Không thể đặt vé. Vui lòng thử lại.</p>}
      </div>
    </div>
  );
}
