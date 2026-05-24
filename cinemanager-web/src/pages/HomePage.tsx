import { Play, Ticket } from "lucide-react";
import { Link } from "react-router-dom";
import { MovieCard } from "../components/movie/MovieCard";
import { useMovies } from "../hooks/useMovies";

export function HomePage() {
  const { data: movies = [], isLoading } = useMovies();
  const featured = movies[0];

  return (
    <div className="page">
      <section className="hero" style={{ backgroundImage: "linear-gradient(90deg, rgba(13,18,32,.92), rgba(13,18,32,.52)), url('https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=1600&q=80')" }}>
        <div className="hero-content">
          <span className="eyebrow">Phim nổi bật</span>
          <h1>{featured?.title ?? "CineManager"}</h1>
          <p>{featured?.genres?.map((g) => g.name).join(", ") ?? "Đặt vé nhanh, chọn ghế trực quan, quản lý lịch sử thành viên."}</p>
          <div className="hero-actions">
            <Link className="primary-button" to={featured ? `/movies/${featured.id}` : "/movies"}>
              <Ticket size={18} /> Đặt vé ngay
            </Link>
            {featured?.trailerUrl && (
              <a className="ghost-button" href={featured.trailerUrl} target="_blank" rel="noreferrer">
                <Play size={18} /> Trailer
              </a>
            )}
          </div>
        </div>
      </section>

      <section className="content-section">
        <div className="section-heading">
          <h2>Phim đang chiếu</h2>
          <Link to="/movies">Xem tất cả</Link>
        </div>
        {isLoading ? <p>Đang tải phim...</p> : <div className="movie-grid">{movies.slice(0, 8).map((m) => <MovieCard key={m.id} movie={m} />)}</div>}
      </section>
    </div>
  );
}
