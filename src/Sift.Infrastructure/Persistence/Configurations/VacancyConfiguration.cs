using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sift.Domain.Identity;
using Sift.Domain.Vacancies;

namespace Sift.Infrastructure.Persistence.Configurations;

public sealed class VacancyConfiguration : IEntityTypeConfiguration<Vacancy>
{
    public void Configure(EntityTypeBuilder<Vacancy> builder)
    {
        builder.ToTable("Vacancies");

        builder.HasKey(vacancy => vacancy.Id);
        builder.Property(vacancy => vacancy.Id).ValueGeneratedNever();

        builder.Property(vacancy => vacancy.Title).HasMaxLength(Vacancy.TitleMaxLength).IsRequired();
        builder.Property(vacancy => vacancy.Company).HasMaxLength(Vacancy.CompanyMaxLength).IsRequired();
        builder.Property(vacancy => vacancy.RawText).HasMaxLength(Vacancy.RawTextMaxLength).IsRequired();
        builder.Property(vacancy => vacancy.CreatedAt).IsRequired();
        builder.Property(vacancy => vacancy.UpdatedAt).IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(vacancy => vacancy.OwnerId).IsRequired().OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(vacancy => new { vacancy.OwnerId, vacancy.CreatedAt });
    }
}
