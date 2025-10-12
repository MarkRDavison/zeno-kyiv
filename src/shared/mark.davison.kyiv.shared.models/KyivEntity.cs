using mark.davison.kyiv.shared.models.Entities;

namespace mark.davison.kyiv.shared.models;

public class KyivEntity
{
    public Guid Id { get; set; }
    public DateTime Created { get; set; }
    public DateTime LastModified { get; set; }
    public Guid UserId { get; set; }

    public virtual User? User { get; set; }
}
