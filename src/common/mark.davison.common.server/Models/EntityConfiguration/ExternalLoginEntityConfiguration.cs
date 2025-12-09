namespace mark.davison.common.server.Models.EntityConfiguration;

public sealed class ExternalLoginEntityConfiguration : IEntityTypeConfiguration<ExternalLogin>
{
    public void Configure(EntityTypeBuilder<ExternalLogin> builder)
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
            .Property(e => e.Provider);

        builder
            .Property(e => e.ProviderSubject);

        builder
            .HasIndex(_ => new { _.Provider, _.ProviderSubject })
            .IsUnique(true);

        builder
            .HasOne(_ => _.User)
            .WithMany(_ => _.ExternalLogins)
            .HasForeignKey(_ => _.UserId);
    }
}
