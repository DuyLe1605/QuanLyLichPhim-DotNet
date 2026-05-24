using BaiTapLon.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Api.Controllers;

[Route("api/snacks")]
[ApiController]
public class SnacksController : ControllerBase
{
    private readonly AppDbContext _db;

    public SnacksController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetSnacks()
    {
        return await _db.Snacks
            .AsNoTracking()
            .OrderBy(s => s.Category).ThenBy(s => s.Name)
            .Select(s => new { s.Id, s.Name, s.Category, s.Price, s.ImagePath })
            .ToListAsync();
    }
}
