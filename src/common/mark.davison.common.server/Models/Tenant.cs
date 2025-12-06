namespace mark.davison.common.server.Models;

public class Tenant
{
    public required Guid Id { get; set; }
    public required DateTime CreatedAt { get; set; }
    public required DateTime LastModified { get; set; }
    public required string Name { get; set; }
}