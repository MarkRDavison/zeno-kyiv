namespace mark.davison.common.authentication.server.Models;

public record CreateTenantDto(Guid UserId, string TenantName);
