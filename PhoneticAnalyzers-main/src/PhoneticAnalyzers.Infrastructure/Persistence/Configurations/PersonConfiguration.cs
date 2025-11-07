using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for Person entity
/// </summary>
public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    /// <summary>
    /// Configures the Person entity mapping
    /// </summary>
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("person");

        // Primary key
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // External ID with unique constraint
        builder.Property(p => p.ExternalId)
            .HasColumnName("external_id")
            .HasMaxLength(64)
            .IsRequired()
            .HasConversion(
                externalId => externalId.Value,
                value => ExternalId.Create(value));

        builder.HasIndex(p => p.ExternalId)
            .IsUnique()
            .HasDatabaseName("ix_person_external_id");

        // Full name
        builder.Property(p => p.FullName)
            .HasColumnName("full_name")
            .HasMaxLength(200)
            .IsRequired();

        // Normalized name
        builder.Property(p => p.NormalizedName)
            .HasColumnName("normalized_name")
            .HasMaxLength(200)
            .IsRequired()
            .HasConversion(
                normalizedName => normalizedName.Value,
                value => NormalizedName.Create(value));

        // Double Metaphone codes
        builder.Property(p => p.PrimaryDoubleMetaphone)
            .HasColumnName("dm_primary")
            .HasMaxLength(16)
            .HasConversion(
                code => code != null ? code.Value : null,
                value => value != null ? PhoneticCode.Create(value, PhoneticAlgorithmType.DoubleMetaphone, true) : null);

        builder.Property(p => p.AlternateDoubleMetaphone)
            .HasColumnName("dm_alternate")
            .HasMaxLength(16)
            .HasConversion(
                code => code != null ? code.Value : null,
                value => value != null ? PhoneticCode.Create(value, PhoneticAlgorithmType.DoubleMetaphone, false) : null);

        // First letter (computed from normalized name) - ignore since it's computed
        builder.Ignore(p => p.FirstLetter);

        // Partition hash (computed column)
        builder.Property(p => p.PartitionHash)
            .HasColumnName("dm_hash")
            .ValueGeneratedOnAddOrUpdate()
            .HasComputedColumnSql("ABS(HASHTEXT(normalized_name)) % 64", stored: true);

        // Audit fields
        builder.Property(p => p.CreatedUtc)
            .HasColumnName("created_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(p => p.UpdatedUtc)
            .HasColumnName("updated_utc")
            .HasColumnType("timestamp with time zone");

        // Configure relationships
        builder.HasMany(p => p.BeiderMorseVariants)
            .WithOne()
            .HasForeignKey("PersonId")
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for phonetic search
        builder.HasIndex(p => p.PrimaryDoubleMetaphone)
            .HasDatabaseName("ix_person_dm_primary")
            .HasFilter("dm_primary IS NOT NULL");

        builder.HasIndex(p => p.AlternateDoubleMetaphone)
            .HasDatabaseName("ix_person_dm_alternate")
            .HasFilter("dm_alternate IS NOT NULL");

        // Index for trigram similarity search
        builder.HasIndex(p => p.NormalizedName)
            .HasDatabaseName("ix_person_normalized_name_gin")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        // Partition configuration
        builder.UseTphMappingStrategy();
        
        // Ignore domain events (not persisted)
        builder.Ignore(p => p.DomainEvents);
    }
}

/// <summary>
/// Entity configuration for BeiderMorseVariant entity
/// </summary>
public sealed class BeiderMorseVariantConfiguration : IEntityTypeConfiguration<BeiderMorseVariant>
{
    /// <summary>
    /// Configures the BeiderMorseVariant entity mapping
    /// </summary>
    public void Configure(EntityTypeBuilder<BeiderMorseVariant> builder)
    {
        builder.ToTable("person_bm");

        // Composite primary key
        builder.HasKey(bm => new { bm.PersonId, bm.BeiderMorseCode });

        // Person ID
        builder.Property(bm => bm.PersonId)
            .HasColumnName("person_id")
            .IsRequired();

        // Beider-Morse code
        builder.Property(bm => bm.BeiderMorseCode)
            .HasColumnName("bm_code")
            .HasMaxLength(128)
            .IsRequired()
            .HasConversion(
                code => code.Value,
                value => PhoneticCode.Create(value, PhoneticAlgorithmType.BeiderMorse, false));

        // First letter (computed column) - ignore since it's computed from BeiderMorseCode
        builder.Ignore(bm => bm.FirstLetter);

        // Audit fields
        builder.Property(bm => bm.CreatedUtc)
            .HasColumnName("created_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(bm => bm.UpdatedUtc)
            .HasColumnName("updated_utc")
            .HasColumnType("timestamp with time zone");

        // Index for Beider-Morse search
        builder.HasIndex(bm => bm.BeiderMorseCode)
            .HasDatabaseName("ix_person_bm_code");

        // Foreign key relationship
        builder.HasOne<Person>()
            .WithMany(p => p.BeiderMorseVariants)
            .HasForeignKey(bm => bm.PersonId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}