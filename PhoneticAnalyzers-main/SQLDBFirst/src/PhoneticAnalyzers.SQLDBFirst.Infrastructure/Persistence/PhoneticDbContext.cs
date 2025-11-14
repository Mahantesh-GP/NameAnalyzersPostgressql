using Microsoft.EntityFrameworkCore;
using PhoneticAnalyzers.SQLDBFirst.Domain.Entities;

namespace PhoneticAnalyzers.SQLDBFirst.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for Database-First approach.
/// This will be replaced/supplemented by scaffolded context after running scaffold-models.ps1
/// </summary>
public class PhoneticDbContext : DbContext
{
    public PhoneticDbContext(DbContextOptions<PhoneticDbContext> options) : base(options)
    {
    }

    public DbSet<Person> Persons => Set<Person>();
    public DbSet<PersonName> PersonNames => Set<PersonName>();
    public DbSet<PersonBm> PersonBms => Set<PersonBm>();
    public DbSet<NicknameMap> NicknameMaps => Set<NicknameMap>();
    public DbSet<NameAlias> NameAliases => Set<NameAlias>();
    public DbSet<NameAliasCache> NameAliasCaches => Set<NameAliasCache>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // NOTE: After scaffolding, the generated context will have proper configurations
        // These are temporary configurations for placeholder entities

        modelBuilder.Entity<Person>(entity =>
        {
            entity.ToTable("person");
            entity.HasKey(e => e.PersonId);
            entity.Property(e => e.PersonId).HasColumnName("person_id");
            entity.Property(e => e.ExternalId).HasColumnName("external_id");
            entity.Property(e => e.FullName).HasColumnName("full_name");
            entity.Property(e => e.NormalizedName).HasColumnName("normalized_name");
            entity.Property(e => e.PrimaryMetaphone).HasColumnName("primary_metaphone");
            entity.Property(e => e.AlternateMetaphone).HasColumnName("alternate_metaphone");
            entity.Property(e => e.County).HasColumnName("county");
            entity.Property(e => e.Flag).HasColumnName("flag");
            entity.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            entity.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
        });

        modelBuilder.Entity<PersonName>(entity =>
        {
            entity.ToTable("person_names");
            entity.HasKey(e => e.PersonNameId);
            entity.Property(e => e.PersonNameId).HasColumnName("person_name_id");
            entity.Property(e => e.PersonId).HasColumnName("person_id");
            entity.Property(e => e.NameToken).HasColumnName("name_token");
            entity.Property(e => e.TokenPosition).HasColumnName("token_position");
            entity.Property(e => e.PrimaryMetaphone).HasColumnName("primary_metaphone");
            entity.Property(e => e.AlternateMetaphone).HasColumnName("alternate_metaphone");
            entity.Property(e => e.CreatedUtc).HasColumnName("created_utc");

            entity.HasOne(d => d.Person)
                .WithMany(p => p.PersonNames)
                .HasForeignKey(d => d.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonBm>(entity =>
        {
            entity.ToTable("person_bm");
            entity.HasKey(e => e.PersonBmId);
            entity.Property(e => e.PersonBmId).HasColumnName("person_bm_id");
            entity.Property(e => e.PersonId).HasColumnName("person_id");
            entity.Property(e => e.BmCode).HasColumnName("beider_morse_code");
            entity.Property(e => e.CreatedUtc).HasColumnName("created_utc");

            entity.HasOne(d => d.Person)
                .WithMany(p => p.PersonBms)
                .HasForeignKey(d => d.PersonId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<NicknameMap>(entity =>
        {
            entity.ToTable("nickname_maps");
            entity.HasKey(e => e.NicknameMapId);
            entity.Property(e => e.NicknameMapId).HasColumnName("nickname_map_id");
            entity.Property(e => e.CanonicalName).HasColumnName("canonical_name");
            entity.Property(e => e.Nickname).HasColumnName("nickname");
            entity.Property(e => e.Locale).HasColumnName("locale");
            entity.Property(e => e.Confidence).HasColumnName("confidence");
            entity.Property(e => e.IsBidirectional).HasColumnName("is_bidirectional");
            entity.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            entity.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
        });

        modelBuilder.Entity<NameAlias>(entity =>
        {
            entity.ToTable("name_aliases");
            entity.HasKey(e => e.NameAliasId);
            entity.Property(e => e.NameAliasId).HasColumnName("name_alias_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Alias).HasColumnName("alias");
            entity.Property(e => e.Culture).HasColumnName("culture");
            entity.Property(e => e.CreatedUtc).HasColumnName("created_utc");
            entity.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
        });

        modelBuilder.Entity<NameAliasCache>(entity =>
        {
            entity.ToTable("name_alias_cache");
            entity.HasKey(e => e.NameAliasCacheId);
            entity.Property(e => e.NameAliasCacheId).HasColumnName("name_alias_cache_id");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.AllAliases).HasColumnName("all_aliases");
            entity.Property(e => e.CachedUtc).HasColumnName("cached_utc");
            entity.Property(e => e.UpdatedUtc).HasColumnName("updated_utc");
        });
    }
}
