namespace mark.davison.common.authentication.server.Services;

public sealed class TenantService<TDbContext> : ITenantService
    where TDbContext : DbContext
{
    private readonly TDbContext _db;

    public TenantService(TDbContext db)
    {
        _db = db;
    }

    public async Task<Tenant> GetTenantById(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenant = await _db.Set<Tenant>()
            .FirstOrDefaultAsync(_ => _.Id == tenantId, cancellationToken);

        if (tenant is null)
        {
            throw new InvalidOperationException(nameof(tenant));
        }

        return tenant;
    }
    public async Task<Tenant> CreateNewTenant(string name, CancellationToken cancellationToken)
    {
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow,
            LastModified = DateTime.UtcNow
        };

        await _db.Set<Tenant>().AddAsync(tenant, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return tenant;
    }
}
