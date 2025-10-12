using mark.davison.kyiv.shared.models.Entities;
using Microsoft.Extensions.Caching.Memory;

namespace mark.davison.kyiv.api.Services;

public class UserService : IUserService
{
    private readonly KyivDbContext _db;
    private readonly IMemoryCache _cache;

    public UserService(KyivDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<User> FindOrCreateUserAsync(string provider, string providerKey, string email)
    {
        // Try to find existing user via external login
        var existingLogin = await _db.ExternalLogins
            .Include(x => x.User)
            .ThenInclude(x => x.UserRoles).
            ThenInclude(x => x.Role)
            .FirstOrDefaultAsync(x => x.Provider == provider && x.ProviderSubject == providerKey);

        if (existingLogin?.User != null)
        {
            return existingLogin.User;
        }

        // If no external login, try match by email
        User? user = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        // Create new user if needed
        if (user == null)
        {
            user = new User
            {
                Id = Guid.NewGuid(),
                Email = email,
                CreatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
        }

        // Link external login
        var login = new ExternalLogin
        {
            Id = Guid.NewGuid(),
            User = user,
            Provider = provider,
            ProviderSubject = providerKey
        };

        _db.ExternalLogins.Add(login);
        await _db.SaveChangesAsync();

        // Cache roles (even if empty)
        _cache.Set($"roles:{user.Id}", user.UserRoles.Select(r => r.Role!.Name).ToList(), TimeSpan.FromMinutes(10));

        return user;
    }

    public async Task LinkExternalLoginAsync(Guid userId, string provider, string providerKey)
    {
        var user = await _db.Users
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

        _db.ExternalLogins.Add(newLink);
        await _db.SaveChangesAsync();
    }

    public async Task UnlinkExternalLoginAsync(Guid userId, string provider)
    {
        var link = await _db.ExternalLogins
            .FirstOrDefaultAsync(l => l.UserId == userId && l.Provider == provider);

        if (link == null)
            throw new InvalidOperationException("Link not found.");

        _db.ExternalLogins.Remove(link);
        await _db.SaveChangesAsync();
    }

    public async Task<User?> GetUserByIdAsync(Guid userId)
    {
        var users = await _db.Users
            .Include(u => u.UserRoles)
            .Include(u => u.ExternalLogins)
            .ToListAsync();


        return users
            .FirstOrDefault(u => u.Id == userId);
    }
}