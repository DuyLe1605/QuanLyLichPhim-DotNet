import { useMutation, useQueryClient } from "@tanstack/react-query";
import { ArrowLeft, Star } from "lucide-react";
import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { moviesApi } from "../api/movies.api";
import { ShowtimePicker } from "../components/movie/ShowtimePicker";
import { useMovie, useReviews, useShowtimes } from "../hooks/useMovies";
import { useProfile } from "../hooks/useProfile";
import { getImageUrl, todayInputValue } from "../lib/utils";

export function MovieDetailPage() {
  const { id = "" } = useParams();
  const queryClient = useQueryClient();
  const [date, setDate] = useState(todayInputValue());
  const [rating, setRating] = useState(5);
  const [comment, setComment] = useState("");
  const [editingReviewId, setEditingReviewId] = useState<number | null>(null);
  const [reviewMessage, setReviewMessage] = useState("");
  const { data: movie } = useMovie(id);
  const { data: showtimes = [] } = useShowtimes(id, date);
  const { data: reviews = [] } = useReviews(id);
  const { data: profile } = useProfile();

  const reviewMutation = useMutation({
    mutationFn: () => {
      const input = { rating, comment: comment.trim() || undefined };
      return editingReviewId
        ? moviesApi.updateReview(id, editingReviewId, input)
        : moviesApi.createReview(id, input);
    },
    onSuccess: async (result) => {
      setRating(5);
      setComment("");
      setEditingReviewId(null);
      setReviewMessage(
        result.pointsAwarded > 0
          ? `Cảm ơn bạn đã đánh giá. Bạn nhận được ${result.pointsAwarded} điểm thưởng.`
          : editingReviewId
            ? "Đã cập nhật đánh giá của bạn."
            : "Đã gửi đánh giá mới của bạn."
      );
      await queryClient.invalidateQueries({ queryKey: ["reviews", id] });
      await queryClient.invalidateQueries({ queryKey: ["movies", id] });
      await queryClient.invalidateQueries({ queryKey: ["profile"] });
      await queryClient.invalidateQueries({ queryKey: ["points"] });
    },
    onError: () => setReviewMessage("Không thể gửi đánh giá. Vui lòng thử lại.")
  });

  if (!movie) return <div className="page narrow">Đang tải...</div>;

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
          <h1 style={{ textTransform: "uppercase", color: "#17202f", fontWeight: 900 }}>{movie.title}</h1>
          <p className="description" style={{ marginBottom: "16px" }}>{movie.description ?? "Thông tin phim đang được cập nhật."}</p>
          <div style={{ display: "grid", gridTemplateColumns: "120px 1fr", gap: "8px", color: "#48566a" }}>
            <strong>Đạo diễn:</strong> <span>{movie.director ?? "Đang cập nhật"}</span>
            <strong>Diễn viên:</strong> <span>{movie.actors ?? "Đang cập nhật"}</span>
            <strong>Thể loại:</strong> <span style={{ color: "#8cc63f" }}>{movie.genres.map((g) => g.name).join(", ")}</span>
            <strong>Thời lượng:</strong> <span>{movie.duration} phút</span>
            <strong>Đánh giá:</strong>
            <span className="rating-line">
              <Star size={16} fill="#ffd166" color="#ffd166" /> {movie.averageRating.toFixed(1)}/5.0 ({movie.reviewCount})
            </span>
          </div>
        </div>
      </section>

      <section className="showtime-layout">
        <ShowtimePicker showtimes={showtimes} />
        <div className="calendar-panel">
          <div style={{ display: "flex", justifyContent: "space-between", alignItems: "center", fontWeight: "bold" }}>
            <button className="icon-button" style={{ background: "transparent" }}>&lt;</button>
            <span>Tháng {new Date(date).getMonth() + 1} - {new Date(date).getFullYear()}</span>
            <button className="icon-button" style={{ background: "transparent" }}>&gt;</button>
          </div>
          <div className="calendar-days">
            {days.map((d) => (
              <div key={d.date} className={`cal-day ${date === d.date ? "active" : ""}`} onClick={() => setDate(d.date)}>
                <span>{d.dayName}</span>
                {d.dayNumber}
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="content-section flush" style={{ marginTop: "48px", borderTop: "1px solid #dde3ea", paddingTop: "32px" }}>
        <div className="section-heading">
          <h2 style={{ color: "#8cc63f", textTransform: "uppercase" }}>Bình luận từ khán giả</h2>
          <span className="muted">Thưởng 5 điểm cho đánh giá đầu tiên</span>
        </div>
        <form
          className="review-form"
          onSubmit={(e) => {
            e.preventDefault();
            reviewMutation.mutate();
          }}
        >
          <div className="rating-picker" aria-label="Chọn điểm đánh giá">
            {[1, 2, 3, 4, 5].map((value) => (
              <button
                key={value}
                type="button"
                className={value <= rating ? "active" : ""}
                onClick={() => setRating(value)}
                aria-label={`${value} sao`}
              >
                <Star size={18} fill={value <= rating ? "#ffd166" : "none"} />
              </button>
            ))}
          </div>
          <input value={comment} onChange={(e) => setComment(e.target.value)} placeholder="Viết cảm nhận sau khi xem phim" />
          <button className="primary-button" disabled={reviewMutation.isPending} type="submit">
            {editingReviewId ? "Lưu chỉnh sửa" : "Gửi đánh giá"}
          </button>
          {editingReviewId && (
            <button
              className="ghost-button"
              type="button"
              onClick={() => {
                setEditingReviewId(null);
                setRating(5);
                setComment("");
              }}
            >
              Hủy
            </button>
          )}
        </form>
        {reviewMessage && <p className={reviewMutation.isError ? "error" : "voucher-ok"}>{reviewMessage}</p>}
        <div className="review-scroll">
          {reviews.length === 0 ? <p className="muted">Chưa có bình luận nào.</p> : reviews.map((r) => (
            <article className="review-row" key={r.id}>
              <div>
                <strong>{r.customer.fullName} · <span style={{ color: "#ffd166" }}>{"★".repeat(r.rating)}</span></strong>
                <span>{new Date(r.createdAt).toLocaleDateString("vi-VN")}</span>
                {profile?.id === r.customer.id && (
                  <button
                    className="review-edit-button"
                    type="button"
                    onClick={() => {
                      setEditingReviewId(r.id);
                      setRating(r.rating);
                      setComment(r.comment ?? "");
                      setReviewMessage("");
                    }}
                  >
                    Sửa
                  </button>
                )}
              </div>
              <p>{r.comment || "Không có bình luận."}</p>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}
