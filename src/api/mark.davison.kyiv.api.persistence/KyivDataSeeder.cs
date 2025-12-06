namespace mark.davison.kyiv.api.persistence;

public sealed class KyivDataSeeder : IDataSeeder
{
    private readonly IDbContextFactory<KyivDbContext> _dbContextFactory;

    public KyivDataSeeder(IDbContextFactory<KyivDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task SeedDataAsync(CancellationToken token)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();

        if (!await ExistsAsync<Tenant>(dbContext, _ => _.Id == TenantIds.SystemTenantId, token))
        {
            await dbContext.AddAsync(new Tenant
            {
                Id = TenantIds.SystemTenantId,
                Name = "System",
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            }, token);
            await dbContext.SaveChangesAsync(token);
        }

        if (!await ExistsAsync<User>(dbContext, _ => _.Id == UserIds.SystemUserId, token))
        {
            await dbContext.AddAsync(new User
            {
                Id = UserIds.SystemUserId,
                TenantId = TenantIds.SystemTenantId,
                Email = "system.kyiv@markdavison.kiwi",
                DisplayName = "System Kyiv",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                LastModified = DateTime.UtcNow
            }, token);

            await dbContext.SaveChangesAsync(token);
        }

        if (!await ExistsAsync<Role>(dbContext, _ => _.Id == Guid.Parse("02a740de-569f-4477-b5e7-d8622228db17"), token))
        {
            await dbContext.AddAsync(new Role
            {
                Id = Guid.Parse("02a740de-569f-4477-b5e7-d8622228db17"),
                Name = "Admin",
                Description = "Administrator with full access",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                UserId = UserIds.SystemUserId
            }, token);
        }

        if (!await ExistsAsync<Role>(dbContext, _ => _.Id == Guid.Parse("207af3cb-4a21-4d85-a93d-e16a8690eff2"), token))
        {
            await dbContext.AddAsync(new Role
            {
                Id = Guid.Parse("207af3cb-4a21-4d85-a93d-e16a8690eff2"),
                Name = "User",
                Description = "Standard user with limited access",
                Created = DateTime.UtcNow,
                LastModified = DateTime.UtcNow,
                UserId = UserIds.SystemUserId
            }, token);
        }


        await dbContext.SaveChangesAsync(token);
    }

    private async Task<bool> ExistsAsync<TEntity>(
        KyivDbContext dbContext,
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken token)
        where TEntity : class
    {
        return await dbContext.Set<TEntity>().Where(predicate).AnyAsync(token);
    }
}