namespace PhoneticAnalyzers.Domain.Common;

/// <summary>
/// Base class for value objects providing equality comparison
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
    /// <summary>
    /// Gets the equality components for this value object
    /// </summary>
    /// <returns>A sequence of objects that participate in equality comparison</returns>
    protected abstract IEnumerable<object> GetEqualityComponents();

    /// <summary>
    /// Determines whether this value object is equal to another
    /// </summary>
    /// <param name="other">The other value object</param>
    /// <returns>True if equal, false otherwise</returns>
    public virtual bool Equals(ValueObject? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        if (other is null || GetType() != other.GetType())
            return false;

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Determines whether this value object is equal to another object
    /// </summary>
    /// <param name="obj">The other object</param>
    /// <returns>True if equal, false otherwise</returns>
    public override bool Equals(object? obj)
    {
        return Equals(obj as ValueObject);
    }

    /// <summary>
    /// Gets the hash code for this value object
    /// </summary>
    /// <returns>A hash code based on the equality components</returns>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(1, (current, obj) =>
            {
                unchecked
                {
                    return current * 23 + (obj?.GetHashCode() ?? 0);
                }
            });
    }

    /// <summary>
    /// Equality operator
    /// </summary>
    /// <param name="left">Left value object</param>
    /// <param name="right">Right value object</param>
    /// <returns>True if equal, false otherwise</returns>
    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        return Equals(left, right);
    }

    /// <summary>
    /// Inequality operator
    /// </summary>
    /// <param name="left">Left value object</param>
    /// <param name="right">Right value object</param>
    /// <returns>True if not equal, false otherwise</returns>
    public static bool operator !=(ValueObject? left, ValueObject? right)
    {
        return !(left == right);
    }
}