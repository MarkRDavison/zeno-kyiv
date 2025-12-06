namespace mark.davison.common.authentication.server.Services;

public interface IUserService
{
    Task<User> FindOrCreateUserAsync(string provider, string providerKey, string displayName, string email, Guid defaultTenantId);
    Task LinkExternalLoginAsync(Guid userId, string provider, string providerKey);
    Task UnlinkExternalLoginAsync(Guid userId, string provider);
    Task<User?> GetUserByIdAsync(Guid userId);
    Task UpdateTenant(Guid userId, Guid tenantId, CancellationToken cancellationToken);
}
