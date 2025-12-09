namespace mark.davison.common.persistence.migrations.sqlite;

public abstract class SqliteDbContextFactory<TDbContext> : IDesignTimeDbContextFactory<TDbContext>
    where TDbContext : DbContext
{
    protected abstract TDbContext DbContextCreation(DbContextOptions<TDbContext> options);

    public TDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TDbContext>();
        optionsBuilder.UseSqlite(
        "Data Source=migrations-db.db",
            b => b.MigrationsAssembly(GetType().Assembly.FullName));

        return DbContextCreation(optionsBuilder.Options);
    }
}
