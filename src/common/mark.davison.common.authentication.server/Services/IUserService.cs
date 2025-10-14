using mark.davison.common.server.Models;

namespace mark.davison.common.authentication.server.Services;

public interface IUserService
{
    Task<User> FindOrCreateUserAsync(string provider, string providerKey, string email);
    Task LinkExternalLoginAsync(Guid userId, string provider, string providerKey);
    Task UnlinkExternalLoginAsync(Guid userId, string provider);
    Task<User?> GetUserByIdAsync(Guid userId);
}
