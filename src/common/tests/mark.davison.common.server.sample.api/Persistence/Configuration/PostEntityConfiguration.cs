namespace mark.davison.common.server.sample.api.Persistence.Configuration;

public class PostEntityConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
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
            .Property(_ => _.Title)
            .HasMaxLength(255);

        builder
            .HasOne(_ => _.Blog)
            .WithMany()
            .HasForeignKey(_ => _.BlogId);
    }
}