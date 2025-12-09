using mark.davison.common.persistence;

namespace mark.davison.common.server.sample.api.Entities;

public class Post : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public Guid BlogId { get; set; }
    public virtual Blog? Blog { get; set; }
}