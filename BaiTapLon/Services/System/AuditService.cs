using BaiTapLon.Data;
using BaiTapLon.Models;
using Microsoft.EntityFrameworkCore;

namespace BaiTapLon.Services;

public class AuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogActionAsync(int? userId, string action, string entityType, int entityId, string? oldVal, string? newVal)
    {
        var log = new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValue = oldVal,
            NewValue = newVal,
            Timestamp = DateTime.Now
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<List<AuditLog>> GetLogsAsync(int take = 100)
    {
        return await _context.AuditLogs
            .Include(a => a.User)
            .OrderByDescending(a => a.Timestamp)
            .Take(take)
            .ToListAsync();
    }
}
