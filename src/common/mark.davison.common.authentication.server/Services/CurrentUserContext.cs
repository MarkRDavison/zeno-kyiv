namespace mark.davison.common.authentication.server.Services;

public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
    public Guid UserId => Guid.Parse(_httpContextAccessor.HttpContext?.User.FindFirstValue(AuthConstants.InternalUserId) ?? string.Empty);
    public Guid TenantId => throw new NotImplementedException();
    public bool HasRole(string role)
    {
        var roles = _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role)
            .Select(r => r.Value)
            .ToList();

        if (roles?.Contains(role) ?? false)
        {
            return true;
        }

        return false;
    }
}
