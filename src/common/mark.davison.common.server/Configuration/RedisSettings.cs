namespace mark.davison.common.server.Configuration;

public class RedisSettings
{
    public string HOST { get; set; } = string.Empty;
    public int PORT { get; set; } = 6379;
    public string? PASSWORD { get; set; }
}
