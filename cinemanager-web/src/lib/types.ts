export type Customer = {
  id: number;
  fullName: string;
  username: string;
  email: string;
  phone: string;
  memberCode: string;
  tier: string;
  totalPoints: number;
};

export type AuthResponse = { accessToken: string; refreshToken: string; customer: Customer };
export type LoginInput = { username: string; password: string };
export type RegisterInput = LoginInput & { fullName: string; email: string; phone: string };
export type Genre = { id: number; name: string };
export type Movie = {
  id: number;
  code: string;
  title: string;
  duration: number;
  ageRating: string;
  posterPath?: string;
  trailerUrl?: string;
  releaseDate?: string;
  averageRating: number;
  reviewCount: number;
  genres: Genre[];
};
export type MovieDetail = Movie & {
  director?: string;
  actors?: string;
  description?: string;
  trailerUrl?: string;
};
export type Showtime = {
  id: number;
  startTime: string;
  endTime: string;
  basePrice: number;
  room: { id: number; name: string; type: string };
};
export type Review = {
  id: number;
  rating: number;
  comment?: string;
  createdAt: string;
  customer: { id: number; fullName: string };
};
export type Seat = {
  id: number;
  label: string;
  gridRow: number;
  gridColumn: number;
  gridSpan: number;
  type: string;
  priceMultiplier: number;
  price: number;
  status: "available" | "sold";
};
export type SeatMapResponse = {
  showtime: { id: number; startTime: string; endTime: string; basePrice: number };
  room: { id: number; name: string; rows: number; columns: number };
  seats: Seat[];
};
export type Booking = {
  bookingCode: string;
  status: string;
  totalAmount: number;
  discountAmount: number;
  createdAt: string;
  showtime?: { id: number; startTime: string; movie: string; room?: string };
};
export type PointTransaction = { id: number; points: number; type: string; description: string; createdAt: string };
