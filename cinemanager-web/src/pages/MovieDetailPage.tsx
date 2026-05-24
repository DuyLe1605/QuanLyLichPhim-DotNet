import { ArrowLeft, Star } from "lucide-react";
import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ShowtimePicker } from "../components/movie/ShowtimePicker";
import { useMovie, useReviews, useShowtimes } from "../hooks/useMovies";
import { todayInputValue } from "../lib/utils";

export function MovieDetailPage() {
  const { id = "" } = useParams();
  const [date, setDate] = useState(todayInputValue());
  const { data: movie } = useMovie(id);
  const { data: showtimes = [] } = useShowtimes(id, date);
  const { data: reviews = [] } = useReviews(id);

  if (!movie) return <div className="page narrow">Đang tải...</div>;

  return (
    <div className="page narrow">
      <Link className="back-link" to="/movies"><ArrowLeft size={18} /> Quay lại</Link>
      <section className="detail-layout">
        <div className="detail-poster">{movie.posterPath ? <img src={movie.posterPath} alt={movie.title} /> : movie.title}</div>
        <div>
          <h1>{movie.title}</h1>
          <p className="rating-line"><Star size={18} /> {movie.averageRating.toFixed(1)} ({movie.reviewCount} đánh giá)</p>
          <p>Đạo diễn: {movie.director ?? "Đang cập nhật"}</p>
          <p>Thời lượng: {movie.duration} phút · Phân loại: {movie.ageRating}</p>
          <p>Thể loại: {movie.genres.map((g) => g.name).join(", ")}</p>
          <p className="description">{movie.description ?? "Thông tin phim đang được cập nhật."}</p>
        </div>
      </section>
      <section className="content-section flush">
        <div className="section-heading">
          <h2>Chọn suất chiếu</h2>
          <input type="date" value={date} onChange={(e) => setDate(e.target.value)} />
        </div>
        <ShowtimePicker showtimes={showtimes} />
      </section>
      <section className="content-section flush">
        <h2>Đánh giá & bình luận</h2>
        <div className="stack">
          {reviews.map((r) => (
            <article className="review-row" key={r.id}>
              <strong>{r.customer.fullName} · {"★".repeat(r.rating)}</strong>
              <p>{r.comment || "Không có bình luận."}</p>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}
