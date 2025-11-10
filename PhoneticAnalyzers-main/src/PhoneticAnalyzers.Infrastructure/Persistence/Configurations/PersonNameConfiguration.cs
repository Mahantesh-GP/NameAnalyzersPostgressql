using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for PersonName entity (multilingual search)
/// </summary>
public sealed class PersonNameConfiguration : IEntityTypeConfiguration<PersonName>
{
    /// <summary>
    /// Configures the PersonName entity mapping
    /// </summary>
    public void Configure(EntityTypeBuilder<PersonName> builder)
    {
        builder.ToTable("person_names");

        // Primary key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Canonical name with unique constraint
        builder.Property(p => p.CanonicalName)
            .HasColumnName("canonical_name")
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(p => p.CanonicalName)
            .IsUnique()
            .HasDatabaseName("ix_person_names_canonical_name");

        // Locale hint
        builder.Property(p => p.LocaleHint)
            .HasColumnName("locale_hint")
            .HasMaxLength(5)
            .IsRequired()
            .HasConversion(
                locale => locale.Code,
                value => Locale.Create(value));

        // Script hint
        builder.Property(p => p.ScriptHint)
            .HasColumnName("script_hint")
            .HasMaxLength(4)
            .IsRequired()
            .HasConversion(
                script => script.Code,
                value => Script.Create(value));

        // Normalized name
        builder.Property(p => p.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(200)
            .IsRequired()
            .HasConversion(
                normalizedName => normalizedName.Value,
                value => NormalizedName.Create(value));

        // Double Metaphone code
        builder.Property(p => p.DoubleMetaphoneCode)
            .HasColumnName("dm_code")
            .HasMaxLength(128)
            .HasConversion(
                code => code != null ? code.Value : null,
                value => value != null ? PhoneticCode.Create(value, PhoneticAlgorithmType.DoubleMetaphone, true) : null);

        // Beider-Morse code
        builder.Property(p => p.BeiderMorseCode)
            .HasColumnName("bm_code")
            .HasMaxLength(128)
            .HasConversion(
                code => code != null ? code.Value : null,
                value => value != null ? PhoneticCode.Create(value, PhoneticAlgorithmType.BeiderMorse, true) : null);

        // Last enrichment timestamp
        builder.Property(p => p.LastEnrichmentUtc)
            .HasColumnName("last_enrichment_utc")
            .HasColumnType("timestamp with time zone");

        // Audit fields
        builder.Property(p => p.CreatedUtc)
            .HasColumnName("created_utc")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(p => p.UpdatedUtc)
            .HasColumnName("updated_utc")
            .HasColumnType("timestamp with time zone");

        // Configure relationships
        builder.HasMany(p => p.Aliases)
            .WithOne(a => a.PersonName)
            .HasForeignKey(a => a.PersonNameId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for multilingual search performance
        builder.HasIndex(p => p.NormalizedName)
            .HasDatabaseName("ix_person_names_normalized_name");

        builder.HasIndex(p => p.NormalizedName)
            .HasDatabaseName("ix_person_names_normalized_name_gin")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.HasIndex(p => p.DoubleMetaphoneCode)
            .HasDatabaseName("ix_person_names_dm_code")
            .HasFilter("dm_code IS NOT NULL");

        builder.HasIndex(p => p.BeiderMorseCode)
            .HasDatabaseName("ix_person_names_bm_code")
            .HasFilter("bm_code IS NOT NULL");

        builder.HasIndex(p => new { p.LocaleHint, p.ScriptHint })
            .HasDatabaseName("ix_person_names_locale_script");

        builder.HasIndex(p => p.LastEnrichmentUtc)
            .HasDatabaseName("ix_person_names_last_enrichment")
            .HasFilter("last_enrichment_utc IS NOT NULL");

        // Ignore domain events (not persisted)
        builder.Ignore(p => p.DomainEvents);
    }
}