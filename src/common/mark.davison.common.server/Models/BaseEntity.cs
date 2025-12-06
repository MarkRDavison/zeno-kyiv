namespace mark.davison.common.server.Models;


public class BaseEntity
{
    public required Guid Id { get; set; }
    public required DateTime Created { get; set; }
    public required DateTime LastModified { get; set; }
    public required Guid UserId { get; set; }

    public virtual User? User { get; set; }
}
