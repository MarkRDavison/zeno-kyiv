namespace mark.davison.common.server.sample.api.Persistence.Configuration;

public class AuthorEntityConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
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
            .Property(_ => _.FirstName)
            .HasMaxLength(255);

        builder
            .Property(_ => _.LastModified)
            .HasMaxLength(255);
    }
}