using BaiTapLon.Models;

namespace BaiTapLon.Api.Dtos;

public record RegisterRequest(string FullName, string Username, string Email, string Phone, string Password);
public record LoginRequest(string Username, string Password);
public record RefreshRequest(string RefreshToken);
public record AuthResponse(string AccessToken, string RefreshToken, CustomerDto Customer);
public record CustomerDto(int Id, string FullName, string Username, string Email, string Phone, string MemberCode, string Tier, int TotalPoints);

public static class CustomerMapping
{
    public static CustomerDto ToDto(this Customer customer) =>
        new(customer.Id, customer.FullName, customer.Username, customer.Email, customer.Phone,
            customer.MemberCode, customer.Tier, customer.TotalPoints);
}
