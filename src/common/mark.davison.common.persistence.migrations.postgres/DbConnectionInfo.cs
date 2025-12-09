namespace mark.davison.common.persistence.migrations.postgres;

public sealed class DbConnectionInfo
{
    public string HOST { get; set; } = string.Empty;
    public string DATABASE { get; set; } = string.Empty;
    public int PORT { get; set; } = 5432;
    public string USERNAME { get; set; } = string.Empty;
    public string PASSWORD { get; set; } = string.Empty;
}