import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import QRCode from "react-qr-code";
import { profileApi } from "../api/profile.api";
import { BookingHistory } from "../components/profile/BookingHistory";
import { useAuth } from "../hooks/useAuth";
import { useMyBookings } from "../hooks/useBookings";
import { useProfile } from "../hooks/useProfile";
import { formatCurrency } from "../lib/utils";

export function ProfilePage() {
  const queryClient = useQueryClient();
  const { data: profile } = useProfile();
  const { data: bookings = [] } = useMyBookings();
  const { logout } = useAuth();
  const [fullName, setFullName] = useState("");
  const [phone, setPhone] = useState("");
  const [email, setEmail] = useState("");
  const [couponCode, setCouponCode] = useState("");
  const [couponMessage, setCouponMessage] = useState("");

  useEffect(() => {
    if (!profile) return;
    setFullName(profile.fullName ?? "");
    setPhone(profile.phone ?? "");
    setEmail(profile.email ?? "");
  }, [profile]);

  const updateMutation = useMutation({
    mutationFn: () => profileApi.update({ fullName, phone, email }),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ["profile"] });
    }
  });

  const couponMutation = useMutation({
    mutationFn: () => profileApi.redeemCoupon(couponCode.trim()),
    onSuccess: async (result) => {
      setCouponMessage(result.message);
      setCouponCode("");
      await queryClient.invalidateQueries({ queryKey: ["profile"] });
      await queryClient.invalidateQueries({ queryKey: ["points"] });
    },
    onError: (error: any) => {
      setCouponMessage(error?.response?.data?.message ?? "Không thể đổi mã coupon.");
    }
  });

  const totalSpent = profile?.totalSpent ?? bookings.reduce((sum, booking) => sum + booking.totalAmount, 0);

  return (
    <div className="page narrow">
      <section className="profile-layout">
        <div className="profile-form-section">
          <div className="profile-avatar">
            <img src={`https://api.dicebear.com/7.x/initials/svg?seed=${profile?.fullName}`} alt="avatar" />
            <div className="profile-stats">
              <h2 style={{ color: "#8cc63f", margin: "0 0 8px 0", fontSize: "1.4rem" }}>{profile?.fullName}</h2>
              <p>Điểm RP: <strong>{profile?.totalPoints ?? 0}</strong></p>
              <p>Tổng chi tiêu: <strong>{formatCurrency(totalSpent)}</strong></p>
              <p style={{ marginTop: "12px", fontSize: "0.8rem", fontStyle: "italic" }}>
                Cập nhật thông tin chính xác để nhận ưu đãi thành viên và hỗ trợ check-in nhanh.
              </p>
            </div>
          </div>

          <form
            onSubmit={(e) => {
              e.preventDefault();
              updateMutation.mutate();
            }}
          >
            <div className="form-row">
              <div className="form-group">
                <label>Họ và tên</label>
                <input type="text" value={fullName} onChange={(e) => setFullName(e.target.value)} required />
              </div>
              <div className="form-group">
                <label>Số điện thoại</label>
                <input type="text" value={phone} onChange={(e) => setPhone(e.target.value)} required />
              </div>
            </div>
            <div className="form-group">
              <label>Email</label>
              <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
            </div>

            <button className="primary-button" style={{ marginTop: "16px" }} disabled={updateMutation.isPending} type="submit">
              Cập nhật thông tin
            </button>
            {updateMutation.isSuccess && <p className="voucher-ok">Đã cập nhật hồ sơ.</p>}
            {updateMutation.isError && <p className="error">Không thể cập nhật hồ sơ.</p>}
          </form>
        </div>

        <div style={{ display: "flex", flexDirection: "column", gap: "16px" }}>
          <div className="member-card">
            <div className="member-card-header">Thẻ thành viên</div>
            <div className="qr-container">
              <div className="qr-code">
                <QRCode value={profile?.memberCode || "CINEMANAGER"} size={100} />
              </div>
              <div className="qr-info">
                <p>Tên đăng nhập:<br /><strong>{profile?.username}</strong></p>
                <p>Số thẻ: <strong>{profile?.memberCode || "Đang cập nhật"}</strong></p>
                <p>Hạng thẻ: <strong>{profile?.tier || "Standard"}</strong></p>
              </div>
            </div>
          </div>

          <form
            className="coupon-card"
            onSubmit={(e) => {
              e.preventDefault();
              couponMutation.mutate();
            }}
          >
            <label>Nhập coupon nhận điểm</label>
            <div className="voucher-row">
              <input value={couponCode} onChange={(e) => setCouponCode(e.target.value.toUpperCase())} placeholder="Mã coupon" />
              <button className="ghost-button" disabled={!couponCode.trim() || couponMutation.isPending} type="submit">
                Đổi mã
              </button>
            </div>
            {couponMessage && <p className={couponMutation.isSuccess ? "voucher-ok" : "error"}>{couponMessage}</p>}
          </form>

          <button className="primary-button full" onClick={logout} style={{ height: "48px", fontSize: "1.1rem" }}>
            Đăng xuất
          </button>
        </div>
      </section>

      <section className="content-section flush" style={{ marginTop: "48px" }}>
        <h2 style={{ textTransform: "uppercase", color: "#8cc63f", borderBottom: "2px solid #dde3ea", paddingBottom: "12px" }}>Lịch sử đặt vé</h2>
        <BookingHistory bookings={bookings} />
      </section>
    </div>
  );
}
