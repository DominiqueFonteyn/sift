using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sift.Domain.Bureaus;
using Sift.Domain.Identity;

namespace Sift.Infrastructure.Persistence.Configurations;

public sealed class BureauConfiguration : IEntityTypeConfiguration<Bureau>
{
    public void Configure(EntityTypeBuilder<Bureau> builder)
    {
        builder.ToTable("Bureaus");

        builder.HasKey(bureau => bureau.Id);
        builder.Property(bureau => bureau.Id).ValueGeneratedNever();

        builder.Property(bureau => bureau.Name).HasMaxLength(Bureau.NameMaxLength).IsRequired();
        builder.Property(bureau => bureau.CreatedAt).IsRequired();
        builder.Property(bureau => bureau.UpdatedAt).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(bureau => bureau.OwnerId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(bureau => bureau.OwnerId);
    }
}
