using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for NicknameMap entity
/// </summary>
public sealed class NicknameMapConfiguration : IEntityTypeConfiguration<NicknameMap>
{
    /// <summary>
    /// Configures the NicknameMap entity mapping
    /// </summary>
    public void Configure(EntityTypeBuilder<NicknameMap> builder)
    {
        builder.ToTable("nickname_maps");

        // Primary key
        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Canonical name
        builder.Property(n => n.CanonicalName)
            .HasColumnName("canonical_name")
            .HasMaxLength(200)
            .IsRequired();

        // Nickname
        builder.Property(n => n.Nickname)
            .HasColumnName("nickname")
            .HasMaxLength(200)
            .IsRequired();

        // Locale
        builder.Property(n => n.Locale)
            .HasColumnName("locale")
            .HasMaxLength(5)
            .IsRequired()
            .HasConversion(
                locale => locale.Code,
                value => Locale.Create(value));

        // Normalized canonical name
        builder.Property(n => n.NormalizedCanonicalName)
            .HasColumnName("normalized_canonical_name")
            .HasMaxLength(200)
            .IsRequired()
            .HasConversion(
                normalizedName => normalizedName.Value,
                value => NormalizedName.Create(value));

        // Normalized nickname
        builder.Property(n => n.NormalizedNickname)
            .HasColumnName("normalized_nickname")
            .HasMaxLength(200)
            .IsRequired()
            .HasConversion(
                normalizedName => normalizedName.Value,
                value => NormalizedName.Create(value));

        // Is bidirectional
        builder.Property(n => n.IsBidirectional)
            .HasColumnName("is_bidirectional")
            .IsRequired()
            .HasDefaultValue(true);

        // Confidence score
        builder.Property(n => n.Confidence)
            .HasColumnName("confidence")
            .HasColumnType("decimal(3,2)")
            .IsRequired()
            .HasDefaultValue(1.0m);

        // Audit fields
        builder.Property(n => n.CreatedUtc)
            .HasColumnName("created_utc")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes for nickname lookup performance
        builder.HasIndex(n => new { n.NormalizedCanonicalName, n.Locale })
            .HasDatabaseName("ix_nickname_maps_canonical_name_locale");

        builder.HasIndex(n => new { n.NormalizedNickname, n.Locale })
            .HasDatabaseName("ix_nickname_maps_nickname_locale");

        builder.HasIndex(n => n.Locale)
            .HasDatabaseName("ix_nickname_maps_locale");

        builder.HasIndex(n => n.Confidence)
            .HasDatabaseName("ix_nickname_maps_confidence");

        // Unique constraint to prevent duplicate mappings
        builder.HasIndex(n => new { n.CanonicalName, n.Nickname, n.Locale })
            .IsUnique()
            .HasDatabaseName("ix_nickname_maps_unique");

        // Trigram indexes for fuzzy matching
        builder.HasIndex(n => n.NormalizedCanonicalName)
            .HasDatabaseName("ix_nickname_maps_canonical_name_gin")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(n => n.NormalizedNickname)
            .HasDatabaseName("ix_nickname_maps_nickname_gin")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
    }
}