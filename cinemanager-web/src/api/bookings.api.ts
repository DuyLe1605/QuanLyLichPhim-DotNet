import { apiClient } from "./client";
import type { Booking, SeatMapResponse } from "../lib/types";

export const bookingsApi = {
  seats: async (showtimeId: string) =>
    (await apiClient.get<SeatMapResponse>(`/bookings/showtimes/${showtimeId}/seats`)).data,
  create: async (input: { showtimeId: number; seatIds: number[]; voucherCode?: string }) =>
    (await apiClient.post("/bookings", input)).data,
  my: async () => (await apiClient.get<Booking[]>("/bookings/my")).data,
  detail: async (code: string) => (await apiClient.get<Booking>(`/bookings/${code}`)).data,
  validateVoucher: async (code: string, orderTotal: number) =>
    (await apiClient.post("/vouchers/validate", { code, orderTotal })).data
};
