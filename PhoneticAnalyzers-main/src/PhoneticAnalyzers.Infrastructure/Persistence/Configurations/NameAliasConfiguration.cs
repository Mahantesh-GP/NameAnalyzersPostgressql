using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.Enums;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for NameAlias entity
/// </summary>
public sealed class NameAliasConfiguration : IEntityTypeConfiguration<NameAlias>
{
    /// <summary>
    /// Configures the NameAlias entity mapping
    /// </summary>
    public void Configure(EntityTypeBuilder<NameAlias> builder)
    {
        builder.ToTable("name_aliases");

        // Primary key
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Foreign key to PersonName
        builder.Property(a => a.PersonNameId)
            .HasColumnName("person_name_id")
            .IsRequired();

        // Alias text
        builder.Property(a => a.Alias)
            .HasColumnName("alias")
            .HasMaxLength(200)
            .IsRequired();

        // Alias type enum
        builder.Property(a => a.AliasType)
            .HasColumnName("alias_type")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion<string>();

        // Locale
        builder.Property(a => a.Locale)
            .HasColumnName("locale")
            .HasMaxLength(5)
            .IsRequired()
            .HasConversion(
                locale => locale.Code,
                value => Locale.Create(value));

        // Script
        builder.Property(a => a.Script)
            .HasColumnName("script")
            .HasMaxLength(4)
            .IsRequired()
            .HasConversion(
                script => script.Code,
                value => Script.Create(value));

        // Source enum
        builder.Property(a => a.Source)
            .HasColumnName("source")
            .HasMaxLength(20)
            .IsRequired()
            .HasConversion<string>();

        // Confidence score
        builder.Property(a => a.Confidence)
            .HasColumnName("confidence")
            .HasColumnType("decimal(3,2)")
            .IsRequired();

        // Normalized alias
        builder.Property(a => a.NormalizedAlias)
            .HasColumnName("normalized_alias")
            .HasMaxLength(200)
            .IsRequired()
            .HasConversion(
                normalizedAlias => normalizedAlias.Value,
                value => NormalizedName.Create(value));

        // Double Metaphone code
        builder.Property(a => a.DoubleMetaphoneCode)
            .HasColumnName("dm_code")
            .HasMaxLength(128)
            .HasConversion(
                code => code != null ? code.Value : null,
                value => value != null ? PhoneticCode.Create(value, PhoneticAlgorithmType.DoubleMetaphone, true) : null);

        // Beider-Morse code
        builder.Property(a => a.BeiderMorseCode)
            .HasColumnName("bm_code")
            .HasMaxLength(128)
            .HasConversion(
                code => code != null ? code.Value : null,
                value => value != null ? PhoneticCode.Create(value, PhoneticAlgorithmType.BeiderMorse, true) : null);

        // Audit fields
        builder.Property(a => a.CreatedUtc)
            .HasColumnName("created_utc")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Configure relationships
        builder.HasOne(a => a.PersonName)
            .WithMany(p => p.Aliases)
            .HasForeignKey(a => a.PersonNameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for multilingual search performance
        builder.HasIndex(a => a.PersonNameId)
            .HasDatabaseName("ix_name_aliases_person_name_id");

        builder.HasIndex(a => a.NormalizedAlias)
            .HasDatabaseName("ix_name_aliases_normalized_alias");

        builder.HasIndex(a => a.NormalizedAlias)
            .HasDatabaseName("ix_name_aliases_normalized_alias_gin")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(a => a.DoubleMetaphoneCode)
            .HasDatabaseName("ix_name_aliases_dm_code")
            .HasFilter("dm_code IS NOT NULL");

        builder.HasIndex(a => a.BeiderMorseCode)
            .HasDatabaseName("ix_name_aliases_bm_code")
            .HasFilter("bm_code IS NOT NULL");

        builder.HasIndex(a => new { a.Locale, a.AliasType })
            .HasDatabaseName("ix_name_aliases_locale_type");

        builder.HasIndex(a => new { a.Source, a.Confidence })
            .HasDatabaseName("ix_name_aliases_source_confidence");

        // Unique constraint to prevent duplicate aliases for same person name
        builder.HasIndex(a => new { a.PersonNameId, a.Alias, a.Locale })
            .IsUnique()
            .HasDatabaseName("ix_name_aliases_unique");
    }
}