namespace mark.davison.kyiv.shared.models.Entities;

public class Role : KyivEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
}
