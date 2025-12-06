namespace mark.davison.common.server.Models;

public class User
{
    public required Guid Id { get; set; }
    public required Guid TenantId { get; set; }
    public required string Email { get; set; }
    public required string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;

    public required DateTime CreatedAt { get; set; }
    public required DateTime LastModified { get; set; }

    public virtual Tenant? Tenant { get; set; }
    public virtual ICollection<UserRole> UserRoles { get; set; } = [];
    public virtual ICollection<ExternalLogin> ExternalLogins { get; set; } = [];
}
