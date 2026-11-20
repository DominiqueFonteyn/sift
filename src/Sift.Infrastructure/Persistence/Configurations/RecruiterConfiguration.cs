using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sift.Domain.Identity;
using Sift.Domain.Recruiters;

namespace Sift.Infrastructure.Persistence.Configurations;

public sealed class RecruiterConfiguration : IEntityTypeConfiguration<Recruiter>
{
    public void Configure(EntityTypeBuilder<Recruiter> builder)
    {
        builder.ToTable("Recruiters");

        builder.HasKey(recruiter => recruiter.Id);
        builder.Property(recruiter => recruiter.Id).ValueGeneratedNever();

        builder.Property(recruiter => recruiter.Name).HasMaxLength(Recruiter.NameMaxLength).IsRequired();
        builder.Property(recruiter => recruiter.Email).HasMaxLength(Recruiter.EmailMaxLength);
        builder.Property(recruiter => recruiter.Phone).HasMaxLength(Recruiter.PhoneMaxLength);
        builder.Property(recruiter => recruiter.LinkedInUrl).HasMaxLength(Recruiter.LinkedInUrlMaxLength);
        builder.Property(recruiter => recruiter.CreatedAt).IsRequired();
        builder.Property(recruiter => recruiter.UpdatedAt).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(recruiter => recruiter.OwnerId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(recruiter => recruiter.OwnerId);
    }
}
