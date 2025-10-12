namespace mark.davison.kyiv.api.models.configuration.EntityConfiguration;

public sealed class UserRoleEntityConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
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
            .HasIndex(_ => new { _.UserId, _.RoleId })
            .IsUnique(true);

        builder
            .HasOne(_ => _.Role)
            .WithMany(_ => _.UserRoles)
            .HasForeignKey(_ => _.RoleId);

        builder
            .HasOne(_ => _.User)
            .WithMany(_ => _.UserRoles)
            .HasForeignKey(_ => _.UserId);
    }
}
