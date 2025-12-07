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
            .Select(ur => ur.Role!.Name)
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

    public async Task EnsureUserHasRole(Guid userId, string roleName, CancellationToken cancellationToken)
    {
        var adminRole = await _db.Set<Role>().FirstAsync(r => r.Name == roleName, cancellationToken);
        var alreadyHasRole = await _db.Set<UserRole>().AnyAsync(ur => ur.UserId == userId && ur.RoleId == adminRole.Id, cancellationToken);
        if (!alreadyHasRole)
        {
            _db.Set<UserRole>().Add(new UserRole
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                RoleId = adminRole.Id,
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow // TODO: DateTime.UtcNow -> IDateService.Now
            });
            await _db.SaveChangesAsync(cancellationToken);
            await InvalidateUserRolesAsync(userId);
        }

    }
}
