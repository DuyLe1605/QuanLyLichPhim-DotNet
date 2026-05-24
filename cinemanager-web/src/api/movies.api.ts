import { apiClient } from "./client";
import type { Genre, Movie, MovieDetail, Review, Showtime } from "../lib/types";

export const moviesApi = {
  genres: async () => (await apiClient.get<Genre[]>("/genres")).data,
  list: async (params?: { search?: string; genreId?: number }) =>
    (await apiClient.get<Movie[]>("/movies", { params })).data,
  detail: async (id: string) => (await apiClient.get<MovieDetail>(`/movies/${id}`)).data,
  showtimes: async (id: string, date: string) =>
    (await apiClient.get<Showtime[]>(`/movies/${id}/showtimes`, { params: { date } })).data,
  reviews: async (id: string) => (await apiClient.get<Review[]>(`/movies/${id}/reviews`)).data,
  createReview: async (id: string, input: { rating: number; comment?: string }) =>
    (await apiClient.post(`/movies/${id}/reviews`, input)).data
};
