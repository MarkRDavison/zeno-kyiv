using mark.davison.common.persistence;

namespace mark.davison.common.server.Models;

public class UserRole : BaseEntity
{
    public required Guid RoleId { get; set; }
    public virtual Role? Role { get; set; }
    public virtual User? User { get; set; }
}
