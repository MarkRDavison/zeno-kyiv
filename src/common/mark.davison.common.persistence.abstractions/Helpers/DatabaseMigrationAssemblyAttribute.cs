namespace mark.davison.common.persistence.Helpers;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class DatabaseMigrationAssemblyAttribute : Attribute
{
    public DatabaseMigrationAssemblyAttribute(DatabaseType databaseType)
    {
        DatabaseType = databaseType;
    }

    public DatabaseType DatabaseType { get; }
}
