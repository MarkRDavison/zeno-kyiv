namespace mark.davison.common.server.sample.api.Entities;

public class Blog : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public virtual Author? Author { get; set; }

    public virtual List<Post>? Posts { get; set; }

}