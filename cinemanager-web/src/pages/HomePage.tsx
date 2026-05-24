import { Play, Ticket } from "lucide-react";
import { Link } from "react-router-dom";
import { MovieCard } from "../components/movie/MovieCard";
import { ComboCard } from "../components/movie/ComboCard";
import { useMovies } from "../hooks/useMovies";
import { useSnacks } from "../hooks/useSnacks";

export function HomePage() {
  const { data: movies = [], isLoading } = useMovies();
  const { data: snacks = [] } = useSnacks();
  const featured = movies[0];

  return (
    <div className="page">
      <section className="hero" style={{ backgroundImage: "linear-gradient(90deg, rgba(0,40,90,.92), rgba(0,40,90,.52)), url('https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=1600&q=80')" }}>
        <div className="hero-content">
          <span className="eyebrow" style={{ color: '#8cc63f' }}>Mùa hè sôi động cùng Star Cinema</span>
          <h1>{featured?.title ?? "Star Cinema"}</h1>
          <p>{featured?.genres?.map((g) => g.name).join(", ") ?? "Trải nghiệm điện ảnh đỉnh cao với hệ thống rạp chiếu chuẩn quốc tế."}</p>
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
        <div className="section-heading" style={{ justifyContent: 'center' }}>
          <h2 style={{ padding: '8px 24px', border: '2px solid #17202f', borderRadius: '24px', textTransform: 'uppercase', fontSize: '1.2rem' }}>Phim đang chiếu</h2>
        </div>
        {isLoading ? <p className="center">Đang tải phim...</p> : <div className="movie-grid">{movies.slice(0, 8).map((m) => <MovieCard key={m.id} movie={m} />)}</div>}
        <div className="center" style={{ marginTop: '24px' }}>
          <Link to="/movies" style={{ color: '#8cc63f', fontWeight: 700 }}>Xem tất cả phim &rarr;</Link>
        </div>
      </section>

      {snacks.length > 0 && (
        <section className="content-section" style={{ borderTop: '1px dashed #dde3ea', marginTop: '12px' }}>
          <div className="section-heading" style={{ justifyContent: 'center' }}>
            <h2 style={{ padding: '8px 24px', border: '2px solid #17202f', borderRadius: '24px', textTransform: 'uppercase', fontSize: '1.2rem' }}>Combo Ưu đãi</h2>
          </div>
          <div className="combo-grid">
            {snacks.map((s) => <ComboCard key={s.id} snack={s} />)}
          </div>
        </section>
      )}
    </div>
  );
}
