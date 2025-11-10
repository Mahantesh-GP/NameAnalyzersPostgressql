using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Domain.ValueObjects;

namespace PhoneticAnalyzers.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity configuration for NameAliasCache entity
/// </summary>
public sealed class NameAliasCacheConfiguration : IEntityTypeConfiguration<NameAliasCache>
{
    /// <summary>
    /// Configures the NameAliasCache entity mapping
    /// </summary>
    public void Configure(EntityTypeBuilder<NameAliasCache> builder)
    {
        builder.ToTable("name_alias_cache");

        // Primary key
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        // Input query (normalized)
        builder.Property(c => c.InputQuery)
            .HasColumnName("input_query")
            .HasMaxLength(200)
            .IsRequired()
            .HasConversion(
                inputQuery => inputQuery.Value,
                value => NormalizedName.Create(value));

        // Locale
        builder.Property(c => c.Locale)
            .HasColumnName("locale")
            .HasMaxLength(5)
            .IsRequired()
            .HasConversion(
                locale => locale.Code,
                value => Locale.Create(value));

        // Cached aliases (JSON)
        builder.Property(c => c.CachedAliases)
            .HasColumnName("cached_aliases")
            .HasColumnType("jsonb")
            .IsRequired();

        // Cache expiration
        builder.Property(c => c.ExpiresUtc)
            .HasColumnName("expires_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Hit count
        builder.Property(c => c.HitCount)
            .HasColumnName("hit_count")
            .IsRequired()
            .HasDefaultValue(0);

        // Last accessed
        builder.Property(c => c.LastAccessedUtc)
            .HasColumnName("last_accessed_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        // Query hash for fast lookups
        builder.Property(c => c.QueryHash)
            .HasColumnName("query_hash")
            .HasMaxLength(16)
            .IsRequired();

        // Audit fields
        builder.Property(c => c.CreatedUtc)
            .HasColumnName("created_utc")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Indexes for cache performance
        builder.HasIndex(c => new { c.QueryHash, c.Locale })
            .IsUnique()
            .HasDatabaseName("ix_name_alias_cache_hash_locale");

        builder.HasIndex(c => c.ExpiresUtc)
            .HasDatabaseName("ix_name_alias_cache_expires");

        builder.HasIndex(c => c.LastAccessedUtc)
            .HasDatabaseName("ix_name_alias_cache_last_accessed");

        builder.HasIndex(c => c.HitCount)
            .HasDatabaseName("ix_name_alias_cache_hit_count");

        // Index for active cache queries (without filter due to PostgreSQL NOW() immutability constraint)
        builder.HasIndex(c => new { c.InputQuery, c.Locale })
            .HasDatabaseName("ix_name_alias_cache_active");
    }
}