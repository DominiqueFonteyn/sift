using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sift.Domain.Identity;

namespace Sift.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();

        builder.Property(user => user.Email)
            .HasConversion(email => email.Value, value => Email.Create(value))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();

        builder.Property(user => user.PasswordHash).IsRequired();
        builder.Property(user => user.CreatedAt).IsRequired();
    }
}
