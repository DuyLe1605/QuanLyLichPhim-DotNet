using BaiTapLon.Api.Dtos;
using BaiTapLon.Api.Services;
using BaiTapLon.Data;
using BaiTapLon.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Api.Controllers;

[Route("api/auth")]
public class AuthController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;

    public AuthController(AppDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterRequest request)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();
        var phone = request.Phone.Trim();

        if (await _db.Customers.AnyAsync(c => c.Username == username || c.Email == email))
        {
            return Conflict(new { message = "Username hoặc email đã tồn tại." });
        }

        var customer = new Customer
        {
            FullName = request.FullName.Trim(),
            Username = username,
            Email = email,
            Phone = phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            MemberCode = $"CM-{Guid.NewGuid():N}"[..9].ToUpperInvariant(),
            CreatedAt = DateTime.Now
        };

        _db.Customers.Add(customer);
        await _db.SaveChangesAsync();

        return CreateAuthResponse(customer);
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginRequest request)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c =>
            c.Username == request.Username.Trim() && c.IsActive);

        if (customer is null || !BCrypt.Net.BCrypt.Verify(request.Password, customer.PasswordHash))
        {
            return Unauthorized(new { message = "Sai tài khoản hoặc mật khẩu." });
        }

        return CreateAuthResponse(customer);
    }

    [HttpPost("refresh")]
    public ActionResult<object> Refresh(RefreshRequest request)
    {
        return BadRequest(new { message = "Refresh token persistence chưa được bật trong database hiện tại." });
    }

    [HttpGet("me")]
    public async Task<ActionResult<CustomerDto>> Me()
    {
        var customerId = GetCustomerId(_tokens);
        if (customerId is null) return Unauthorized();

        var customer = await _db.Customers.FindAsync(customerId.Value);
        return customer is null ? NotFound() : customer.ToDto();
    }

    private AuthResponse CreateAuthResponse(Customer customer) =>
        new(_tokens.CreateAccessToken(customer), _tokens.CreateRefreshToken(), customer.ToDto());
}
