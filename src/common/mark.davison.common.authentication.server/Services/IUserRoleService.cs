namespace mark.davison.common.authentication.server.Services;

public interface IUserRoleService
{
    Task<IReadOnlyList<string>> GetRolesForUserAsync(Guid userId);
    Task InvalidateUserRolesAsync(Guid userId);
}
