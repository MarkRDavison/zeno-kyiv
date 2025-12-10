namespace mark.davison.example.api.migrations.sqlite;

[DatabaseMigrationAssembly(DatabaseType.Sqlite)]
public sealed class SqliteContextFactory : SqliteDbContextFactory<ExampleDbContext>
{
    protected override ExampleDbContext DbContextCreation(
            DbContextOptions<ExampleDbContext> options
        ) => new(options);
}
