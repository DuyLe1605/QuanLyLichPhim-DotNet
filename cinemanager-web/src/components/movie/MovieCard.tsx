import { CalendarDays, Star } from "lucide-react";
import { Link } from "react-router-dom";
import type { Movie } from "../../lib/types";
import { getImageUrl } from "../../lib/utils";
import { motion } from "framer-motion";

export function MovieCard({ movie }: { movie: Movie }) {
  const fallbackImg = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=400&q=80";

  return (
    <motion.article 
      className="movie-card"
      initial={{ opacity: 0, y: 20 }}
      whileInView={{ opacity: 1, y: 0 }}
      viewport={{ once: true }}
      transition={{ duration: 0.4 }}
      whileHover={{ y: -5 }}
    >
      <Link to={`/movies/${movie.id}`} className="poster-frame">
        <img 
          src={getImageUrl(movie.posterPath)} 
          alt={movie.title} 
          onError={(e) => { e.currentTarget.src = fallbackImg; }}
        />
        {movie.ageRating && <span className={`age-rating ${movie.ageRating}`}>{movie.ageRating}</span>}
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
    </motion.article>
  );
}
