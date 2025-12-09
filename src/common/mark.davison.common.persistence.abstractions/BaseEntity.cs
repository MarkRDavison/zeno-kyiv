namespace mark.davison.common.persistence;

public class BaseEntity
{
    public required Guid Id { get; set; }
    public required DateTime Created { get; set; }
    public required DateTime LastModified { get; set; }
    public required Guid UserId { get; set; }
}
