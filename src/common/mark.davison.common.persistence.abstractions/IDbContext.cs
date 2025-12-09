using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.Linq.Expressions;

namespace mark.davison.common.persistence;

public interface ICommonDbContextTransaction<TContext> : IDisposable where TContext : DbContext
{
    Task CommitTransactionAsync(CancellationToken cancellationToken);
    Task RollbackTransactionAsync(CancellationToken cancellationToken);

    bool RolledBack { get; }
}

public interface IDbContext
{
    DbSet<TEntity> Set<TEntity>() where TEntity : class;
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<bool> AnyAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) where TEntity : BaseEntity;
    Task<bool> ExistsAsync<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : BaseEntity;

    Task<TEntity?> GetByIdAsync<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : BaseEntity;
    Task<TEntity> UpsertEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : BaseEntity;
    Task<List<TEntity>> UpsertEntitiesAsync<TEntity>(List<TEntity> entities, CancellationToken cancellationToken) where TEntity : BaseEntity;

    Task<TEntity?> DeleteEntityByIdAsync<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : BaseEntity;
    Task<TEntity?> DeleteEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : BaseEntity;
    Task<List<TEntity>> DeleteEntitiesByIdAsync<TEntity>(List<Guid> ids, CancellationToken cancellationToken) where TEntity : BaseEntity;
    Task<List<TEntity>> DeleteEntitiesAsync<TEntity>(List<TEntity> entities, CancellationToken cancellationToken) where TEntity : BaseEntity;
}

public interface IDbContext<TContext> : IDbContext where TContext : DbContext
{
    ValueTask<EntityEntry<TEntity>> AddAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : class;

    EntityEntry<TEntity> Remove<TEntity>(TEntity entity) where TEntity : class;
    Task<EntityEntry<TEntity>?> RemoveAsync<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : BaseEntity;

    Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken);

    Task<ICommonDbContextTransaction<TContext>> BeginTransactionAsync(CancellationToken cancellationToken);
}