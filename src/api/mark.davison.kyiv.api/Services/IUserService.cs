using mark.davison.common.server.Models;

namespace mark.davison.kyiv.api.Services;

public interface IUserService
{
    Task<User> FindOrCreateUserAsync(string provider, string providerKey, string email);
    Task LinkExternalLoginAsync(Guid userId, string provider, string providerKey);
    Task UnlinkExternalLoginAsync(Guid userId, string provider);
    Task<User?> GetUserByIdAsync(Guid userId);
}
