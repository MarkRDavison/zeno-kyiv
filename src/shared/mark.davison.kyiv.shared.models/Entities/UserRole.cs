namespace mark.davison.kyiv.shared.models.Entities;

public class UserRole : KyivEntity
{
    public Guid RoleId { get; set; }
    public virtual Role? Role { get; set; }
}
