using mark.davison.common.persistence;

namespace mark.davison.common.server.sample.api.Entities;

public class Comment : BaseEntity
{
    public string Content { get; set; } = string.Empty;
    public DateOnly Date { get; set; }
    public int Integer { get; set; }
    public Guid Guid { get; set; }
    public long Long { get; set; }
}