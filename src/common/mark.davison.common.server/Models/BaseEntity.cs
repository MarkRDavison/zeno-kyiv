namespace mark.davison.common.server.Models;


public class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastModified { get; set; }
    public Guid UserId { get; set; }

    public virtual User? User { get; set; }
}
