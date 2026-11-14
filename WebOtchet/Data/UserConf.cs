
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebOtchet.Models;

namespace WebOtchet.Data.Configurations;

public sealed class UserConfiguration
    : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("USERS");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnName("ID")
            .HasColumnType("char(36)")
            .HasConversion<string>()
            .ValueGeneratedNever();

        builder.Property(x => x.Username)
            .HasColumnName("USERNAME")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Hash)
            .HasColumnName("PASSWORD_HASH")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(x => x.Role)
            .HasColumnName("ROLE")
            .HasConversion<byte>()
            .HasColumnType("tinyint unsigned")
            .IsRequired();

        builder.HasIndex(x => x.Username)
            .IsUnique()
            .HasDatabaseName("UX_USERS_USERNAME");
    }
}

