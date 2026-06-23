using Microsoft.EntityFrameworkCore;
using LibWiseApp.Data;
using LibWiseApp.Models;

namespace LibWiseApp.Services;

public class AuditLogService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContext;

    public AuditLogService(AppDbContext db, IHttpContextAccessor httpContext)
    {
        _db = db;
        _httpContext = httpContext;
    }

    public async Task LogAsync(string action, string entityType, string? entityId, string? details = null)
    {
        var userId = _httpContext.HttpContext?.User?.Identity?.Name != null
            ? (await _db.Users.Where(u => u.UserName == _httpContext.HttpContext.User.Identity.Name)
                .Select(u => u.Id).FirstOrDefaultAsync())
            : null;

        _db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = _httpContext.HttpContext?.Connection?.RemoteIpAddress?.ToString(),
            Timestamp = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }
}
