namespace BaiTapLon.Api.Dtos;

public record ReviewRequest(int Rating, string? Comment);
public record BookingRequest(int ShowtimeId, int[] SeatIds, string? VoucherCode);
public record ProfileUpdateRequest(string FullName, string Email, string Phone);
public record VoucherValidateRequest(string Code, decimal OrderTotal);
