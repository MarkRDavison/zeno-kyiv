namespace mark.davison.common.server.Models;

public class UserRole : BaseEntity
{
    public Guid RoleId { get; set; }
    public virtual Role? Role { get; set; }
}
