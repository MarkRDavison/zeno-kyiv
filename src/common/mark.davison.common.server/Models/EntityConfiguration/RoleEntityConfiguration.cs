namespace mark.davison.common.server.Models.EntityConfiguration;

public sealed class RoleEntityConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder
            .HasKey(e => e.Id);

        builder
            .Property(e => e.Id)
            .ValueGeneratedNever();

        builder
            .Property(e => e.Created);

        builder
            .Property(e => e.LastModified);

        builder
            .Property(e => e.Name);

        builder
            .Property(e => e.Description);

        builder
            .HasOne(_ => _.User)
            .WithMany()
            .HasForeignKey(_ => _.UserId);

        builder
            .HasMany(_ => _.UserRoles)
            .WithOne(_ => _.Role)
            .HasForeignKey(_ => _.RoleId);
    }
}
