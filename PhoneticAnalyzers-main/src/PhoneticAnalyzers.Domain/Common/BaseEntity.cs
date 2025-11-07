namespace PhoneticAnalyzers.Domain.Common;

/// <summary>
/// Base class for domain entities with audit properties
/// </summary>
public abstract class BaseEntity
{
    /// <summary>
    /// Gets the unique identifier for this entity
    /// </summary>
    public long Id { get; protected set; }

    /// <summary>
    /// Gets the creation timestamp in UTC
    /// </summary>
    public DateTime CreatedUtc { get; protected set; }

    /// <summary>
    /// Gets the last update timestamp in UTC
    /// </summary>
    public DateTime? UpdatedUtc { get; protected set; }

    /// <summary>
    /// Marks this entity as updated with the current UTC timestamp
    /// </summary>
    protected void MarkAsUpdated()
    {
        UpdatedUtc = DateTime.UtcNow;
    }

    /// <summary>
    /// Sets the creation timestamp (typically used during initial creation)
    /// </summary>
    protected void SetCreatedTimestamp(DateTime? createdUtc = null)
    {
        CreatedUtc = createdUtc ?? DateTime.UtcNow;
    }
}

/// <summary>
/// Base class for aggregate roots in the domain
/// </summary>
public abstract class AggregateRoot : BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    /// <summary>
    /// Gets the domain events that have been raised by this aggregate
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to be published
    /// </summary>
    /// <param name="domainEvent">The domain event to add</param>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears all domain events (typically called after publishing)
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}

/// <summary>
/// Marker interface for domain events
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Gets the timestamp when this domain event occurred
    /// </summary>
    DateTime OccurredOn { get; }
}

/// <summary>
/// Base implementation for domain events
/// </summary>
public abstract record DomainEvent : IDomainEvent
{
    /// <inheritdoc/>
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}