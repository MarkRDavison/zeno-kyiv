namespace mark.davison.common.server.Models;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string? DisplayName { get; set; }
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<ExternalLogin> ExternalLogins { get; set; } = [];
}
