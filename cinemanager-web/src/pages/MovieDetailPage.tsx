import { ArrowLeft, Star } from "lucide-react";
import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ShowtimePicker } from "../components/movie/ShowtimePicker";
import { useMovie, useReviews, useShowtimes } from "../hooks/useMovies";
import { todayInputValue, getImageUrl } from "../lib/utils";

export function MovieDetailPage() {
  const { id = "" } = useParams();
  const [date, setDate] = useState(todayInputValue());
  const { data: movie } = useMovie(id);
  const { data: showtimes = [] } = useShowtimes(id, date);
  const { data: reviews = [] } = useReviews(id);

  if (!movie) return <div className="page narrow">Đang tải...</div>;

  // Generate next 14 days for calendar
  const today = new Date();
  const days = Array.from({ length: 14 }).map((_, i) => {
    const d = new Date(today);
    d.setDate(d.getDate() + i);
    return {
      date: d.toISOString().slice(0, 10),
      dayName: i === 0 ? "Hôm nay" : d.toLocaleDateString("vi-VN", { weekday: "short" }),
      dayNumber: d.getDate()
    };
  });

  return (
    <div className="page narrow">
      <Link className="back-link" to="/movies"><ArrowLeft size={18} /> Quay lại</Link>
      
      {/* Movie Info */}
      <section className="detail-layout">
        <div className="detail-poster">
          {movie.posterPath ? (
            <img 
              src={getImageUrl(movie.posterPath)} 
              alt={movie.title} 
              onError={(e) => { e.currentTarget.src = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=400&q=80"; }}
            />
          ) : movie.title}
        </div>
        <div>
          <h1 style={{ textTransform: 'uppercase', color: '#17202f', fontWeight: 900 }}>{movie.title}</h1>
          <p className="description" style={{ marginBottom: '16px' }}>{movie.description ?? "Thông tin phim đang được cập nhật."}</p>
          <div style={{ display: 'grid', gridTemplateColumns: '120px 1fr', gap: '8px', color: '#48566a' }}>
            <strong>Đạo diễn:</strong> <span>{movie.director ?? "Đang cập nhật"}</span>
            <strong>Diễn viên:</strong> <span>{movie.actors ?? "Đang cập nhật"}</span>
            <strong>Thể loại:</strong> <span style={{ color: '#8cc63f' }}>{movie.genres.map((g) => g.name).join(", ")}</span>
            <strong>Thời lượng:</strong> <span>{movie.duration} phút</span>
            <strong>Đánh giá:</strong> <span className="rating-line"><Star size={16} fill="#ffd166" color="#ffd166" /> {movie.averageRating.toFixed(1)}/5.0</span>
          </div>
        </div>
      </section>

      {/* Showtimes & Calendar */}
      <section className="showtime-layout">
        <div>
          <ShowtimePicker showtimes={showtimes} />
        </div>
        <div className="calendar-panel">
          <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', fontWeight: 'bold' }}>
            <button className="icon-button" style={{ background: 'transparent' }}>&lt;</button>
            <span>Tháng {new Date(date).getMonth() + 1} - {new Date(date).getFullYear()}</span>
            <button className="icon-button" style={{ background: 'transparent' }}>&gt;</button>
          </div>
          <div className="calendar-days">
            {days.map((d) => (
              <div 
                key={d.date} 
                className={`cal-day ${date === d.date ? 'active' : ''}`}
                onClick={() => setDate(d.date)}
              >
                <span>{d.dayName}</span>
                {d.dayNumber}
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* Reviews */}
      <section className="content-section flush" style={{ marginTop: '48px', borderTop: '1px solid #dde3ea', paddingTop: '32px' }}>
        <h2 style={{ color: '#8cc63f', textTransform: 'uppercase', marginBottom: '24px' }}>Bình luận từ khán giả</h2>
        <div className="stack">
          {reviews.length === 0 ? <p className="muted">Chưa có bình luận nào.</p> : reviews.map((r) => (
            <article className="review-row" key={r.id}>
              <strong>{r.customer.fullName} · <span style={{ color: '#ffd166' }}>{"★".repeat(r.rating)}</span></strong>
              <p>{r.comment}</p>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}
