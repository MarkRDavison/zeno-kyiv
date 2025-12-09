namespace mark.davison.common.server.sample.api.Persistence.Configuration;

public class CommentEntityConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder
            .HasKey(e => e.Id);

        builder
            .Property(e => e.Id)
            .ValueGeneratedNever();

        builder
            .Property(_ => _.Created);

        builder
            .Property(_ => _.LastModified);

        builder
            .Property(_ => _.Content)
            .HasMaxLength(255);
    }
}