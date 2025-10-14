namespace mark.davison.common.server.Models.EntityConfiguration;

public sealed class UserEntityConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder
            .HasKey(e => e.Id);

        builder
            .Property(e => e.Id)
            .ValueGeneratedNever();

        builder
            .Property(e => e.Email);

        builder
            .Property(e => e.DisplayName);

        builder
            .Property(e => e.IsActive);

        builder
            .Property(_ => _.CreatedAt);

        builder
            .Property(_ => _.LastModified);
    }
}
