using mark.davison.common.server.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace mark.davison.common.authentication.server.Services;

public class UserRoleService<TDbContext> : IUserRoleService
    where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserRoleService<TDbContext>> _logger;

    // TODO: Config?
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public UserRoleService(TDbContext db, IMemoryCache cache, ILogger<UserRoleService<TDbContext>> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> GetRolesForUserAsync(Guid userId)
    {
        if (_cache.TryGetValue(userId, out IReadOnlyList<string>? roles))
        {
            return roles!;
        }

        roles = await _db.Set<UserRole>()
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToListAsync();

        _cache.Set(userId, roles, CacheDuration);

        return roles;
    }

    public Task InvalidateUserRolesAsync(Guid userId)
    {
        _logger.LogInformation("Invalidating role cache for user {UserId}", userId);
        _cache.Remove(userId);
        return Task.CompletedTask;
    }
}
