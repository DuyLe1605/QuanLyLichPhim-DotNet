import { BookingHistory } from "../components/profile/BookingHistory";
import { useMyBookings } from "../hooks/useBookings";
import { useProfile } from "../hooks/useProfile";

export function ProfilePage() {
  const { data: profile } = useProfile();
  const { data: bookings = [] } = useMyBookings();

  return (
    <div className="page narrow">
      <section className="profile-card">
        <div>
          <h1>{profile?.fullName ?? "Tài khoản"}</h1>
          <p>{profile?.email} · {profile?.phone}</p>
        </div>
        <div className="points-badge">
          <strong>{profile?.totalPoints ?? 0}</strong>
          <span>điểm · {profile?.tier ?? "Standard"}</span>
        </div>
      </section>
      <section className="content-section flush">
        <h2>Lịch sử đặt vé</h2>
        <BookingHistory bookings={bookings} />
      </section>
    </div>
  );
}
