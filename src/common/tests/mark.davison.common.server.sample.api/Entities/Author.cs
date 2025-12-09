namespace mark.davison.common.server.sample.api.Entities;

public class Author : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public virtual List<Blog>? Blogs { get; set; }
}