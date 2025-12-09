using mark.davison.common.persistence;
using mark.davison.common.persistence.Helpers;
using mark.davison.common.persistence.migrations.sqlite;
using mark.davison.kyiv.api.persistence;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace mark.davison.api.migrations.sqlite;

[ExcludeFromCodeCoverage]
[DatabaseMigrationAssembly(DatabaseType.Sqlite)]
public sealed class SqliteContextFactory : SqliteDbContextFactory<KyivDbContext>
{
    protected override KyivDbContext DbContextCreation(
            DbContextOptions<KyivDbContext> options
        ) => new(options);
}
