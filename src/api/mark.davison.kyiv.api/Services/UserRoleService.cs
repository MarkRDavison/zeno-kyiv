
using Microsoft.Extensions.Caching.Memory;

namespace mark.davison.kyiv.api.Services;

public class UserRoleService : IUserRoleService
{
    private readonly KyivDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly ILogger<UserRoleService> _logger;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    public UserRoleService(KyivDbContext db, IMemoryCache cache, ILogger<UserRoleService> logger)
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

        roles = await _db.UserRoles
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
