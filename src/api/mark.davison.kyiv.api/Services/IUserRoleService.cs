namespace mark.davison.kyiv.api.Services;

public interface IUserRoleService
{
    Task<IReadOnlyList<string>> GetRolesForUserAsync(Guid userId);
    Task InvalidateUserRolesAsync(Guid userId);
}
