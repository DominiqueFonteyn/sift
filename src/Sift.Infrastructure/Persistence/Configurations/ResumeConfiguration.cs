using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sift.Domain.Identity;
using Sift.Domain.Resumes;

namespace Sift.Infrastructure.Persistence.Configurations;

public sealed class ResumeConfiguration : IEntityTypeConfiguration<Resume>
{
    public void Configure(EntityTypeBuilder<Resume> builder)
    {
        builder.ToTable("Resumes");

        // One resume per user: the owner id is the key.
        builder.HasKey(resume => resume.OwnerId);
        builder.Property(resume => resume.OwnerId).ValueGeneratedNever();

        builder.Property(resume => resume.Text).HasMaxLength(Resume.TextMaxLength).IsRequired();
        builder.Property(resume => resume.UpdatedAt).IsRequired();

        builder.HasOne<User>().WithOne().HasForeignKey<Resume>(resume => resume.OwnerId).IsRequired().OnDelete(DeleteBehavior.Cascade);
    }
}
