namespace mark.davison.common.authentication.server.Services;

public sealed class CurrentUserContext<TDbContext> : ICurrentUserContext
    where TDbContext : DbContext
{
    private readonly TDbContext _dbContext;
    private readonly HashSet<string> _roles = [];

    public CurrentUserContext(TDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public bool IsAuthenticated { get; private set; }
    public Guid UserId { get; private set; }
    public Guid TenantId { get; private set; }
    public bool HasRole(string role)
    {
        return _roles.Contains(role);
    }


    public async Task<ClaimsPrincipal> PopulateFromPrincipal(ClaimsPrincipal principal, string provider)
    {
        _roles.Clear();
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var sub = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier);

        // TODO: Cache
        Console.WriteLine("TODO: Cache this...");
        var user = await _dbContext
            .Set<User>()
            .AsNoTracking()
            .Include(_ => _.UserRoles)
            .ThenInclude(_ => _.Role)
            .Where(_ => _.Email == email && _.ExternalLogins
                .Any(el => el.Provider == provider && (sub == null || el.ProviderSubject == sub)))
            .FirstOrDefaultAsync(CancellationToken.None);

        var identity = new ClaimsIdentity(principal.Identity);
        var newPrincipal = new ClaimsPrincipal(identity);

        if (user is not null && principal.Identity is not null)
        {
            IsAuthenticated = principal.Identity.IsAuthenticated;
            UserId = user.Id;
            identity.AddClaim(new Claim(AuthConstants.InternalUserId, UserId.ToString()));
            TenantId = user.TenantId;
            identity.AddClaim(new Claim(AuthConstants.TenantId, TenantId.ToString()));
            foreach (var role in user.UserRoles)
            {
                _roles.Add(role.Role!.Name);
                identity.AddClaim(new Claim(ClaimTypes.Role, role.Role!.Name));
            }
        }

        return newPrincipal;
    }
}
