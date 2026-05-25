import { apiClient } from "./client";
import type { Booking, SeatMapResponse } from "../lib/types";

type BookingCreateInput = {
  showtimeId: number;
  seatIds: number[];
  voucherCode?: string;
  snacks?: { snackId: number; quantity: number }[];
  pointsToRedeem?: number;
};

export type VoucherValidationResult = {
  valid: boolean;
  code: string;
  discount: number;
};

export const bookingsApi = {
  seats: async (showtimeId: string) =>
    (await apiClient.get<SeatMapResponse>(`/bookings/showtimes/${showtimeId}/seats`)).data,
  create: async (input: BookingCreateInput) =>
    (await apiClient.post("/bookings", input)).data,
  my: async () => (await apiClient.get<Booking[]>("/bookings/my")).data,
  detail: async (code: string) => (await apiClient.get<Booking>(`/bookings/${code}`)).data,
  validateVoucher: async (code: string, orderTotal: number) =>
    (await apiClient.post<VoucherValidationResult>("/vouchers/validate", { code, orderTotal })).data
};
