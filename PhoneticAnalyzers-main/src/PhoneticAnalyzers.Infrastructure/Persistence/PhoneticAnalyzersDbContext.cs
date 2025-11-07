using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PhoneticAnalyzers.Domain.Entities;
using PhoneticAnalyzers.Infrastructure.Persistence.Configurations;
using System.Reflection;

namespace PhoneticAnalyzers.Infrastructure.Persistence;

/// <summary>
/// Application database context
/// </summary>
public sealed class PhoneticAnalyzersDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the persons DbSet
    /// </summary>
    public DbSet<Person> Persons => Set<Person>();

    /// <summary>
    /// Gets or sets the Beider-Morse variants DbSet
    /// </summary>
    public DbSet<BeiderMorseVariant> BeiderMorseVariants => Set<BeiderMorseVariant>();

    /// <summary>
    /// Initializes a new instance of the PhoneticAnalyzersDbContext class
    /// </summary>
    /// <param name="options">The database context options</param>
    public PhoneticAnalyzersDbContext(DbContextOptions<PhoneticAnalyzersDbContext> options) : base(options)
    {
    }

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the current assembly
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        // Configure PostgreSQL-specific features
        ConfigurePostgreSqlFeatures(modelBuilder);

        // Configure partitioning
        ConfigurePartitioning(modelBuilder);
    }

    /// <inheritdoc/>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        // Enable sensitive data logging in development
        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
        {
            optionsBuilder.EnableSensitiveDataLogging();
            optionsBuilder.EnableDetailedErrors();
        }
    }

    /// <summary>
    /// Saves changes and publishes domain events
    /// </summary>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The number of entities saved</returns>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit fields before saving
        UpdateAuditFields();

        // Save changes
        var result = await base.SaveChangesAsync(cancellationToken);

        // Publish domain events (would be implemented with MediatR in a real application)
        await PublishDomainEventsAsync();

        return result;
    }

    /// <summary>
    /// Updates audit fields for tracked entities
    /// </summary>
    private void UpdateAuditFields()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is Domain.Common.BaseEntity && 
                       (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            var entity = (Domain.Common.BaseEntity)entry.Entity;
            
            if (entry.State == EntityState.Added)
            {
                // Use reflection to call protected SetCreatedTimestamp method
                var method = typeof(Domain.Common.BaseEntity)
                    .GetMethod("SetCreatedTimestamp", BindingFlags.NonPublic | BindingFlags.Instance);
                method?.Invoke(entity, new object?[] { null });
            }
            else if (entry.State == EntityState.Modified)
            {
                // Use reflection to call protected MarkAsUpdated method
                var method = typeof(Domain.Common.BaseEntity)
                    .GetMethod("MarkAsUpdated", BindingFlags.NonPublic | BindingFlags.Instance);
                method?.Invoke(entity, null);
            }
        }
    }

    /// <summary>
    /// Publishes domain events for aggregate roots
    /// </summary>
    private async Task PublishDomainEventsAsync()
    {
        var aggregateRoots = ChangeTracker.Entries<Domain.Common.AggregateRoot>()
            .Where(e => e.Entity.DomainEvents.Any())
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregateRoots
            .SelectMany(ar => ar.DomainEvents)
            .ToList();

        // Clear events before publishing to prevent re-publishing
        aggregateRoots.ForEach(ar => ar.ClearDomainEvents());

        // In a real application, you would publish these events using MediatR or another event publisher
        foreach (var domainEvent in domainEvents)
        {
            // TODO: Implement domain event publishing
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Configures PostgreSQL-specific features
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigurePostgreSqlFeatures(ModelBuilder modelBuilder)
    {
        // Enable PostgreSQL extensions
        modelBuilder.HasPostgresExtension("pg_trgm");
        
        // Configure default values and functions
        modelBuilder.Entity<Person>(entity =>
        {
            entity.Property(e => e.CreatedUtc)
                .HasDefaultValueSql("NOW()");
        });

        modelBuilder.Entity<BeiderMorseVariant>(entity =>
        {
            entity.Property(e => e.CreatedUtc)
                .HasDefaultValueSql("NOW()");
        });
    }

    /// <summary>
    /// Configures table partitioning
    /// </summary>
    /// <param name="modelBuilder">The model builder</param>
    private static void ConfigurePartitioning(ModelBuilder modelBuilder)
    {
        // Configure hash partitioning for person table
        // This would typically be done via raw SQL migrations
        modelBuilder.Entity<Person>().ToTable(tb =>
        {
            tb.HasComment("Person table partitioned by dm_hash for better performance");
        });

        modelBuilder.Entity<BeiderMorseVariant>().ToTable(tb =>
        {
            tb.HasComment("Beider-Morse variants table partitioned by first_letter");
        });
    }
}

/// <summary>
/// Database context factory for design-time operations
/// </summary>
public sealed class PhoneticAnalyzersDbContextFactory : IDesignTimeDbContextFactory<PhoneticAnalyzersDbContext>
{
    /// <summary>
    /// Creates a new instance of PhoneticAnalyzersDbContext for design-time operations
    /// </summary>
    public PhoneticAnalyzersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<PhoneticAnalyzersDbContext>();
        
        // Use a default connection string for migrations
        var connectionString = Environment.GetEnvironmentVariable("DefaultConnection") 
                            ?? "Host=localhost;Database=phonetic_analyzers_dev;Username=postgres;Password=postgres";
        
        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(PhoneticAnalyzersDbContext).Assembly.FullName);
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });

        return new PhoneticAnalyzersDbContext(optionsBuilder.Options);
    }
}