namespace mark.davison.common.authentication.server.abstractions.Services;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
    Guid TenantId { get; }
    bool HasRole(string role);
}
