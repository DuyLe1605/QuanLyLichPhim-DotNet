using BaiTapLon.Api.Dtos;
using BaiTapLon.Api.Services;
using BaiTapLon.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Api.Controllers;

[Route("api/profile")]
public class ProfileController : ApiControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenService _tokens;

    public ProfileController(AppDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    [HttpGet]
    public async Task<ActionResult<object>> GetProfile()
    {
        var customerId = GetCustomerId(_tokens);
        if (customerId is null) return Unauthorized();

        var profile = await _db.Customers
            .AsNoTracking()
            .Where(c => c.Id == customerId.Value)
            .Select(c => new
            {
                c.Id,
                c.FullName,
                c.Username,
                c.Email,
                c.Phone,
                c.MemberCode,
                c.Tier,
                c.TotalPoints,
                c.LoyaltyPoints,
                c.MembershipPoints,
                c.MonthlySpent,
                c.TotalSpent,
                c.CreatedAt
            })
            .FirstOrDefaultAsync();

        return profile is null ? NotFound() : profile;
    }

    [HttpPut]
    public async Task<ActionResult<CustomerDto>> UpdateProfile(ProfileUpdateRequest request)
    {
        var customerId = GetCustomerId(_tokens);
        if (customerId is null) return Unauthorized();

        var customer = await _db.Customers.FindAsync(customerId.Value);
        if (customer is null) return NotFound();

        var email = request.Email.Trim();
        var emailTaken = await _db.Customers.AnyAsync(c => c.Id != customer.Id && c.Email == email);
        if (emailTaken) return Conflict(new { message = "Email đã được sử dụng." });

        customer.FullName = request.FullName.Trim();
        customer.Email = email;
        customer.Phone = request.Phone.Trim();
        await _db.SaveChangesAsync();

        return customer.ToDto();
    }

    [HttpGet("points")]
    public async Task<ActionResult<IEnumerable<object>>> GetPoints()
    {
        var customerId = GetCustomerId(_tokens);
        if (customerId is null) return Unauthorized();

        return await _db.PointTransactions
            .AsNoTracking()
            .Where(p => p.CustomerId == customerId.Value)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new { p.Id, p.Points, p.Type, p.Description, p.CreatedAt })
            .ToListAsync();
    }
}
