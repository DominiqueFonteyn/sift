using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sift.Domain.Identity;
using Sift.Domain.Requirements;

namespace Sift.Infrastructure.Persistence.Configurations;

public sealed class RequirementConfiguration : IEntityTypeConfiguration<Requirement>
{
    public void Configure(EntityTypeBuilder<Requirement> builder)
    {
        builder.ToTable("Requirements");

        builder.HasKey(requirement => requirement.Id);
        builder.Property(requirement => requirement.Id).ValueGeneratedNever();

        builder.Property(requirement => requirement.Text).HasMaxLength(Requirement.TextMaxLength).IsRequired();
        builder.Property(requirement => requirement.NormalizedText).HasMaxLength(Requirement.TextMaxLength).IsRequired();
        builder.Property(requirement => requirement.Kind)
            .HasConversion(kind => RequirementKinds.ToName(kind), name => RequirementKinds.Parse(name))
            .HasMaxLength(20)
            .IsRequired();
        builder.Property(requirement => requirement.CreatedAt).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(requirement => requirement.OwnerId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(requirement => new { requirement.OwnerId, requirement.NormalizedText }).IsUnique();
    }
}
