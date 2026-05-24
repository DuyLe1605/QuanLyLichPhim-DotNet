import { useQuery } from "@tanstack/react-query";
import { bookingsApi } from "../api/bookings.api";

export const useSeatMap = (showtimeId: string) =>
  useQuery({ queryKey: ["seat-map", showtimeId], queryFn: () => bookingsApi.seats(showtimeId) });
export const useMyBookings = () => useQuery({ queryKey: ["my-bookings"], queryFn: bookingsApi.my });
