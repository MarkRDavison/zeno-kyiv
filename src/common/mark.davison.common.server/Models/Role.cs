using mark.davison.common.persistence;

namespace mark.davison.common.server.Models;

public class Role : BaseEntity
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public virtual User? User { get; set; }
}
