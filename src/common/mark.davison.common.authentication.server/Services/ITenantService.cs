namespace mark.davison.common.authentication.server.Services;

public interface ITenantService
{
    Task<Tenant> GetTenantById(Guid tenantId, CancellationToken cancellationToken);
    Task<Tenant> CreateNewTenant(string name, CancellationToken cancellationToken);
}
