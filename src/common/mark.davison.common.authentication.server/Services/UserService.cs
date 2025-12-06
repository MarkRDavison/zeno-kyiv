namespace mark.davison.common.authentication.server.Services;

public class UserService<TDbContext> : IUserService
    where TDbContext : DbContext
{
    private readonly TDbContext _db;
    private readonly IMemoryCache _cache;

    public UserService(TDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<User> FindOrCreateUserAsync(string provider, string providerKey, string displayName, string email, Guid defaultTenantId)
    {
        // Try to find existing user via external login
        var existingLogin = await _db.Set<ExternalLogin>()
            .Include(x => x.User)
            .ThenInclude(x => x!.UserRoles)
            .ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderSubject == providerKey);

        if (existingLogin?.User != null)
        {
            return existingLogin.User;
        }

        // If no external login, try match by email
        User? user = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            user = await _db.Set<User>().FirstOrDefaultAsync(u => u.Email == email);
        }

        // Create new user if needed
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = defaultTenantId,
                DisplayName = displayName,
                Email = email,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            };
            _db.Set<User>().Add(user);
        }

        // Link external login
        var login = new ExternalLogin
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Provider = provider,
            ProviderSubject = providerKey,
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };

        _db.Set<ExternalLogin>().Add(login);
        await _db.SaveChangesAsync();

        // Cache roles (even if empty)
        _cache.Set($"roles:{user.Id}", user.UserRoles.Select(r => r.Role!.Name).ToList(), TimeSpan.FromMinutes(10));

        return user;
    }

    public async Task LinkExternalLoginAsync(Guid userId, string provider, string providerKey)
    {
        var user = await _db.Set<User>()
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
            throw new InvalidOperationException("User not found.");

        if (user.ExternalLogins.Any(l => l.Provider == provider))
            throw new InvalidOperationException($"Already linked to {provider}.");

        var newLink = new ExternalLogin
        {
            Id = Guid.NewGuid(),
            Provider = provider,
            ProviderSubject = providerKey,
            UserId = user.Id,
            Created = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };

        _db.Set<ExternalLogin>().Add(newLink);
        await _db.SaveChangesAsync();
    }

    public async Task UnlinkExternalLoginAsync(Guid userId, string provider)
    {
        var link = await _db.Set<ExternalLogin>()
            .FirstOrDefaultAsync(l => l.UserId == userId && l.Provider == provider);

        if (link == null)
            throw new InvalidOperationException("Link not found.");

        _db.Set<ExternalLogin>().Remove(link);
        await _db.SaveChangesAsync();
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        return await _db.Set<User>()
            .Include(u => u.UserRoles)
            .Include(u => u.ExternalLogins)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task UpdateTenant(Guid userId, Guid tenantId, CancellationToken cancellationToken)
    {
        var user = await _db
            .Set<User>()
            .FirstAsync(u => u.Id == userId, cancellationToken);

        user.TenantId = tenantId;

        _db.Update(user);
        await _db.SaveChangesAsync(cancellationToken);
    }
}