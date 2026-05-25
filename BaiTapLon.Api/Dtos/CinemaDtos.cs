namespace BaiTapLon.Api.Dtos;

public record ReviewRequest(int Rating, string? Comment);
public record BookingSnackRequest(int SnackId, int Quantity);
public record BookingRequest(int ShowtimeId, int[] SeatIds, string? VoucherCode, BookingSnackRequest[]? Snacks, int PointsToRedeem = 0);
public record ProfileUpdateRequest(string FullName, string Email, string Phone);
public record VoucherValidateRequest(string Code, decimal OrderTotal);
public record CouponRedeemRequest(string Code);
