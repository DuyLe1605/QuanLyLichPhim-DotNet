using BaiTapLon.Api.Dtos;
using BaiTapLon.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Api.Controllers;

[Route("api/vouchers")]
[ApiController]
public class VouchersController : ControllerBase
{
    private readonly AppDbContext _db;

    public VouchersController(AppDbContext db)
    {
        _db = db;
    }

    [HttpPost("validate")]
    public async Task<ActionResult<object>> Validate(VoucherValidateRequest request)
    {
        var now = DateTime.Now;
        var voucher = await _db.Vouchers
            .AsNoTracking()
            .FirstOrDefaultAsync(v =>
                v.Code == request.Code.Trim() &&
                v.IsActive &&
                v.StartDate <= now &&
                v.EndDate >= now &&
                v.UsedCount < v.MaxUses);

        if (voucher is null)
        {
            return NotFound(new { valid = false, message = "Mã giảm giá không hợp lệ hoặc đã hết hạn." });
        }

        var discount = voucher.Type switch
        {
            "Percent" => request.OrderTotal * voucher.Value / 100m,
            "Fixed" => voucher.Value,
            "FreeTicket" => request.OrderTotal,
            _ => 0
        };

        if (voucher.MaxDiscount is not null)
        {
            discount = Math.Min(discount, voucher.MaxDiscount.Value);
        }

        discount = Math.Min(discount, request.OrderTotal);

        return new
        {
            valid = true,
            voucher.Id,
            voucher.Code,
            voucher.Type,
            voucher.Value,
            voucher.MaxDiscount,
            discount
        };
    }
}
