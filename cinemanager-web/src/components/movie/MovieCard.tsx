import { CalendarDays, Star } from "lucide-react";
import { Link } from "react-router-dom";
import type { Movie } from "../../lib/types";
import { getImageUrl } from "../../lib/utils";

export function MovieCard({ movie }: { movie: Movie }) {
  return (
    <article className="movie-card">
      <Link to={`/movies/${movie.id}`} className="poster-frame">
        <img src={getImageUrl(movie.posterPath)} alt={movie.title} />
      </Link>
      <div className="movie-card-body">
        <h3>{movie.title}</h3>
        <p>{movie.genres?.map((g) => g.name).join(", ") || "Đang cập nhật"}</p>
        <div className="meta-row">
          <span><Star size={16} /> {movie.averageRating.toFixed(1)}</span>
          <span><CalendarDays size={16} /> {movie.duration} phút</span>
        </div>
        <Link className="primary-button full" to={`/movies/${movie.id}`}>Đặt vé</Link>
      </div>
    </article>
  );
}
