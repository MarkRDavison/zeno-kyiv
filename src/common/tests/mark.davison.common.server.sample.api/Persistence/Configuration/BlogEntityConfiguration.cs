namespace mark.davison.common.server.sample.api.Persistence.Configuration;

public class BlogEntityConfiguration : IEntityTypeConfiguration<Blog>
{
    public void Configure(EntityTypeBuilder<Blog> builder)
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
            .Property(_ => _.Name)
            .HasMaxLength(255);

        builder
            .HasOne(_ => _.Author)
            .WithMany()
            .HasForeignKey(_ => _.AuthorId);
    }
}