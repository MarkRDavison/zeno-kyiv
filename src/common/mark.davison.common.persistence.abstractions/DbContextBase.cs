namespace mark.davison.common.persistence;

public class DbContextTransaction<TContext> : ICommonDbContextTransaction<TContext> where TContext : DbContext
{
    private readonly DbContextBase<TContext> _context;
    private readonly IDbContextTransaction? _rootTransaction;
    private bool _disposedValue;

    public DbContextTransaction(
        DbContextBase<TContext> context,
        IDbContextTransaction? rootTransaction)
    {
        _context = context;
        _rootTransaction = rootTransaction;

        _context.RefCount++;
    }

    public bool RolledBack => _context.RolledBack;

    public async Task CommitTransactionAsync(CancellationToken cancellationToken)
    {
        if (RolledBack)
        {
            throw new InvalidOperationException("Cannot commit a transaction that has already been rolled back");
        }

        _context.RefCount--;

        if (_rootTransaction != null)
        {
            if (_context.RefCount == 0)
            {
                await _rootTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken)
    {
        _context.RolledBack = true;
        await _context.Database.RollbackTransactionAsync(cancellationToken);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _rootTransaction?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}

public abstract class DbContextBase<TContext> : DbContext, IDbContext<TContext> where TContext : DbContext
{
    private DbContextTransaction<TContext>? _root = null;

    public int RefCount { get; set; }
    public bool RolledBack { get; set; }

    protected DbContextBase(DbContextOptions options) : base(options)
    {

    }

    public async Task<bool> AnyAsync<TEntity>(Expression<Func<TEntity, bool>> predicate, CancellationToken cancellationToken) where TEntity : BaseEntity
    {
        return await Set<TEntity>()
            .Where(predicate)
            .AnyAsync(cancellationToken);
    }
    public async Task<bool> ExistsAsync<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : BaseEntity
    {
        return await Set<TEntity>()
            .Where(_ => _.Id == id)
            .AnyAsync(cancellationToken);
    }

    public async Task<TEntity?> GetByIdAsync<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : BaseEntity
    {
        return await FindAsync<TEntity>([id], cancellationToken: cancellationToken);
    }

    public async Task<TEntity> UpsertEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : BaseEntity
    {
        var existing = await GetByIdAsync<TEntity>(entity.Id, cancellationToken);

        entity.LastModified = DateTime.UtcNow;
        if (existing == null)
        {
            entity.Created = DateTime.UtcNow;
            await Set<TEntity>().AddAsync(entity, cancellationToken);

            return entity;
        }
        else
        {
            Entry(existing).CurrentValues.SetValues(entity);
            return existing;
        }
    }

    public async Task<List<TEntity>> UpsertEntitiesAsync<TEntity>(List<TEntity> entities, CancellationToken cancellationToken) where TEntity : BaseEntity
    {
        var results = new List<TEntity>();

        var ids = entities.Select(_ => _.Id).ToList();

        if (ids.Count != ids.Distinct().Count())
        {
            throw new InvalidOperationException("Duplicate entities detected.");
        }

        var existingEntities = await Set<TEntity>()
            .Where(_ => ids.Contains(_.Id))
            .ToListAsync(cancellationToken);

        foreach (var e in entities)
        {
            e.LastModified = DateTime.UtcNow;
            var existing = existingEntities.Find(_ => _.Id == e.Id);
            if (existing == null)
            {
                e.Created = DateTime.UtcNow;
                await Set<TEntity>().AddAsync(e, cancellationToken);

                results.Add(e);
            }
            else
            {
                Entry(existing).CurrentValues.SetValues(e);
                results.Add(existing);
            }
        }

        return results;
    }

    public async Task<TEntity?> DeleteEntityByIdAsync<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : BaseEntity
    {
        var entity = await GetByIdAsync<TEntity>(id, cancellationToken);

        if (entity == null)
        {
            return null;
        }

        return await DeleteEntityAsync<TEntity>(entity, cancellationToken);
    }

    public async Task<TEntity?> DeleteEntityAsync<TEntity>(TEntity entity, CancellationToken cancellationToken) where TEntity : BaseEntity
    {
        var response = await DeleteEntitiesAsync<TEntity>([entity], cancellationToken);

        return response.Find(_ => _.Id == entity.Id);
    }

    public async Task<List<TEntity>> DeleteEntitiesByIdAsync<TEntity>(List<Guid> ids, CancellationToken cancellationToken) where TEntity : BaseEntity
    {
        var entities = await Set<TEntity>().Where(_ => ids.Contains(_.Id)).ToListAsync(cancellationToken);

        return await DeleteEntitiesAsync<TEntity>(entities, cancellationToken);
    }

    public async Task<List<TEntity>> DeleteEntitiesAsync<TEntity>(List<TEntity> entities, CancellationToken cancellationToken) where TEntity : BaseEntity
    {
        var results = new List<TEntity>();

        var ids = entities.Select(_ => _.Id).ToList();

        if (ids.Count != ids.Distinct().Count())
        {
            throw new InvalidOperationException("Duplicate entities detected.");
        }

        var existingEntities = await Set<TEntity>()
            .Where(_ => ids.Contains(_.Id))
            .ToListAsync(cancellationToken);

        foreach (var e in entities)
        {
            var existing = existingEntities.Find(_ => _.Id == e.Id);

            if (existing != null)
            {
                results.Add(Set<TEntity>().Remove(existing).Entity);
            }
        }

        return results;
    }

    public async Task<EntityEntry<TEntity>?> RemoveAsync<TEntity>(Guid id, CancellationToken cancellationToken) where TEntity : BaseEntity
    {
        var entity = await FindAsync<TEntity>([id], cancellationToken: cancellationToken);

        if (entity == null)
        {
            return null;
        }

        return Remove(entity);
    }

    public async Task<ICommonDbContextTransaction<TContext>> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        if (_root == null)
        {
            var transaction = await Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            _root = new DbContextTransaction<TContext>(this, transaction);

            return _root;
        }

        return new DbContextTransaction<TContext>(this, null);
    }
}
