import { Search } from "lucide-react";
import { useState } from "react";
import { MovieCard } from "../components/movie/MovieCard";
import { useGenres, useMovies } from "../hooks/useMovies";

export function MoviesPage() {
  const [search, setSearch] = useState("");
  const [genreId, setGenreId] = useState<number | undefined>();
  const { data: genres = [] } = useGenres();
  const { data: movies = [], isLoading } = useMovies({ search, genreId });

  return (
    <div className="page narrow">
      <div className="toolbar">
        <label className="search-box">
          <Search size={18} />
          <input value={search} onChange={(e) => setSearch(e.target.value)} placeholder="Tìm phim" />
        </label>
        <select value={genreId ?? ""} onChange={(e) => setGenreId(e.target.value ? Number(e.target.value) : undefined)}>
          <option value="">Tất cả thể loại</option>
          {genres.map((g) => <option key={g.id} value={g.id}>{g.name}</option>)}
        </select>
      </div>
      {isLoading ? <p>Đang tải phim...</p> : <div className="movie-grid">{movies.map((m) => <MovieCard key={m.id} movie={m} />)}</div>}
    </div>
  );
}
