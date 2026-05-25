import { apiClient } from "./client";
import type { Genre, Movie, MovieDetail, Review, Showtime } from "../lib/types";

function normalizePosterPath(posterPath?: string) {
    if (!posterPath) return posterPath;

    const normalized = posterPath.replace(/\\/g, "/");
    if (normalized.startsWith("http") || normalized.startsWith("data:")) return normalized;

    // If the backend stores only a filename, assume it lives under Resources/Posters/
    if (!normalized.includes("/")) return `Posters/${normalized}`;

    return normalized;
}

export const moviesApi = {
    genres: async () => (await apiClient.get<Genre[]>("/genres")).data,
    list: async (params?: { search?: string; genreId?: number }) =>
        (await apiClient.get<Movie[]>("/movies", { params })).data.map((m) => ({
            ...m,
            posterPath: normalizePosterPath(m.posterPath),
        })),
    detail: async (id: string) => {
        const m = (await apiClient.get<MovieDetail>(`/movies/${id}`)).data;
        return { ...m, posterPath: normalizePosterPath(m.posterPath) };
    },
    showtimes: async (id: string, date: string) =>
        (await apiClient.get<Showtime[]>(`/movies/${id}/showtimes`, { params: { date } })).data,
    reviews: async (id: string) => (await apiClient.get<Review[]>(`/movies/${id}/reviews`)).data,
    createReview: async (id: string, input: { rating: number; comment?: string }) =>
        (await apiClient.post(`/movies/${id}/reviews`, input)).data,
};
