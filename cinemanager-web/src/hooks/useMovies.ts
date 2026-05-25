import { useQuery } from "@tanstack/react-query";
import { moviesApi } from "../api/movies.api";
import { todayInputValue } from "../lib/utils";

export const useGenres = () => useQuery({ queryKey: ["genres"], queryFn: moviesApi.genres });
export const useMovies = (params?: { search?: string; genreId?: number }) =>
  useQuery({ queryKey: ["movies", params], queryFn: () => moviesApi.list(params) });
export const useMovie = (id: string) => useQuery({ queryKey: ["movies", id], queryFn: () => moviesApi.detail(id) });
export const useShowtimes = (id: string, date = todayInputValue()) =>
  useQuery({ queryKey: ["showtimes", id, date], queryFn: () => moviesApi.showtimes(id, date) });
export const useReviews = (id: string) => useQuery({ queryKey: ["reviews", id], queryFn: () => moviesApi.reviews(id) });
export const createReview = moviesApi.createReview;
