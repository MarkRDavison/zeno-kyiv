namespace mark.davison.common.server.Models.EntityConfiguration;

public sealed class TenantEntityConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder
            .HasKey(e => e.Id);

        builder
            .Property(e => e.Id)
            .ValueGeneratedNever();

        builder
            .Property(e => e.CreatedAt);

        builder
            .Property(e => e.LastModified);

        builder
            .Property(e => e.Name);
    }
}
