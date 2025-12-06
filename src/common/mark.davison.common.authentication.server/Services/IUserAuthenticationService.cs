using mark.davison.common.authentication.server.Models;

namespace mark.davison.common.authentication.server.Services;

public interface IUserAuthenticationService
{
    void SetToken(string token);
    Task<UserDto?> GetUserByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<UserDto?> GetUserByEmailAsync(string email, CancellationToken cancellationToken);
    Task<ExternalLoginDto?> GetExternalLoginForProviderAsync(string provider, string providerSub, CancellationToken cancellationToken);
    Task<IReadOnlyList<ExternalLoginDto>> GetExternalLoginsForUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task AddExternalLoginAsync(Guid userId, string provider, string providerSub, CancellationToken cancellationToken);
    Task CreateUserWithRolesAsync(UserDto user, IEnumerable<string> roles, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> GetRolesForUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task RemoveExternalLogin(Guid userId, Guid externalLoginId, CancellationToken cancellationToken);
    Task<TenantDto?> GetTenantById(Guid tenantId, CancellationToken cancellationToken);
    Task CreateTenantForUser(Guid userId, string name, CancellationToken cancellationToken);
}
