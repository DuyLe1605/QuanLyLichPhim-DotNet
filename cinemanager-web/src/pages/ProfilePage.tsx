import { BookingHistory } from "../components/profile/BookingHistory";
import { useMyBookings } from "../hooks/useBookings";
import { useProfile } from "../hooks/useProfile";
import { useAuth } from "../hooks/useAuth";
import QRCode from "react-qr-code";

export function ProfilePage() {
  const { data: profile } = useProfile();
  const { data: bookings = [] } = useMyBookings();
  const { logout } = useAuth();

  return (
    <div className="page narrow">
      <section className="profile-layout">
        
        {/* Left Column: Form & Stats */}
        <div className="profile-form-section">
          <div className="profile-avatar">
            <img src={`https://api.dicebear.com/7.x/initials/svg?seed=${profile?.fullName}`} alt="avatar" />
            <div className="profile-stats">
              <h2 style={{ color: '#8cc63f', margin: '0 0 8px 0', fontSize: '1.4rem' }}>{profile?.fullName}</h2>
              <p>Điểm RP: <strong>{profile?.totalPoints ?? 0}</strong></p>
              <p>Tổng chi tiêu: <strong>0 VND</strong></p>
              <p style={{ marginTop: '12px', fontSize: '0.8rem', fontStyle: 'italic' }}>
                Vui lòng cập nhật thông tin chính xác để nhận ưu đãi thành viên.
              </p>
            </div>
          </div>

          <form>
            <div className="form-row">
              <div className="form-group">
                <label>Họ và Tên</label>
                <input type="text" defaultValue={profile?.fullName} disabled />
              </div>
              <div className="form-group">
                <label>Số điện thoại</label>
                <input type="text" defaultValue={profile?.phone} disabled />
              </div>
            </div>
            <div className="form-group">
              <label>Email</label>
              <input type="text" defaultValue={profile?.email} disabled />
            </div>
            
            <button className="primary-button" style={{ marginTop: '16px' }} type="button" onClick={() => alert('Chức năng cập nhật đang phát triển!')}>CẬP NHẬT THÔNG TIN</button>
          </form>
        </div>

        {/* Right Column: Member Card */}
        <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
          <div className="member-card">
            <div className="member-card-header">
              Xóa thông tin
            </div>
            <div className="qr-container">
              <div className="qr-code">
                <QRCode value={profile?.memberCode || "BHDSTAR"} size={100} />
              </div>
              <div className="qr-info">
                <p>Tên đăng nhập:<br/><strong>{profile?.username}</strong></p>
                <p>Số thẻ: <strong>{profile?.memberCode || "ONLA1384074"}</strong></p>
                <p>Hạng thẻ: <strong>{profile?.tier || "Star"}</strong></p>
              </div>
            </div>
          </div>
          
          <button className="primary-button full" onClick={logout} style={{ height: '48px', fontSize: '1.1rem' }}>
            ĐĂNG XUẤT
          </button>
        </div>

      </section>

      <section className="content-section flush" style={{ marginTop: '48px' }}>
        <h2 style={{ textTransform: 'uppercase', color: '#8cc63f', borderBottom: '2px solid #dde3ea', paddingBottom: '12px' }}>Lịch sử đặt vé</h2>
        <BookingHistory bookings={bookings} />
      </section>
    </div>
  );
}
