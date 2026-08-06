using System.Collections.ObjectModel;

namespace AegiDocs.Domain.Operations;

/// <summary>
/// Provides deterministic, immutable operations for collections whose elements
/// have stable identities and an externally represented order.
/// </summary>
/// <remarks>
/// These operations arrange collection positions only. Aggregates remain
/// responsible for translating positions into their own persisted order values
/// and enforcing domain-specific invariants. Every result is materialized in a
/// new read-only collection, so caller-owned input collections are never mutated.
/// </remarks>
public static class OrderedCollectionOperations
{
    /// <summary>
    /// Returns a new collection with an item inserted at a zero-based position.
    /// </summary>
    /// <remarks>
    /// Valid insertion positions range from zero through the source count.
    /// </remarks>
    public static IReadOnlyList<T> Insert<T, TIdentity>(
        IEnumerable<T> items,
        T item,
        int position,
        Func<T, TIdentity> identitySelector)
        where TIdentity : notnull
    {
        ArgumentNullException.ThrowIfNull(item);
        var materializedItems = MaterializeAndValidate(items, identitySelector);
        ValidateInsertPosition(position, materializedItems.Count);

        var itemIdentity = identitySelector(item);
        if (materializedItems.Any(current => EqualityComparer<TIdentity>.Default.Equals(identitySelector(current), itemIdentity)))
        {
            throw new ArgumentException("The collection already contains an item with the supplied identity.", nameof(item));
        }

        materializedItems.Insert(position, item);
        return ToReadOnly(materializedItems);
    }

    /// <summary>
    /// Returns a new collection with an existing item moved to a zero-based position.
    /// </summary>
    /// <remarks>
    /// Valid destination positions range from zero through one less than the source count.
    /// </remarks>
    public static IReadOnlyList<T> Move<T, TIdentity>(
        IEnumerable<T> items,
        TIdentity itemIdentity,
        int position,
        Func<T, TIdentity> identitySelector)
        where TIdentity : notnull
    {
        var materializedItems = MaterializeAndValidate(items, identitySelector);
        ValidateExistingItemIdentity(itemIdentity);
        ValidateMovePosition(position, materializedItems.Count);

        var sourceIndex = FindIndexByIdentity(materializedItems, itemIdentity, identitySelector);
        var item = materializedItems[sourceIndex];
        materializedItems.RemoveAt(sourceIndex);
        materializedItems.Insert(position, item);
        return ToReadOnly(materializedItems);
    }

    /// <summary>
    /// Returns a new collection with a duplicate of an existing item inserted at a zero-based position.
    /// </summary>
    /// <remarks>
    /// The caller must explicitly provide a factory that creates a new stable identity.
    /// The operation rejects a factory result whose identity is already present.
    /// Valid insertion positions range from zero through the source count.
    /// </remarks>
    public static IReadOnlyList<T> Duplicate<T, TIdentity>(
        IEnumerable<T> items,
        TIdentity sourceIdentity,
        int position,
        Func<T, TIdentity> identitySelector,
        Func<T, T> duplicateFactory)
        where TIdentity : notnull
    {
        ArgumentNullException.ThrowIfNull(duplicateFactory);
        var materializedItems = MaterializeAndValidate(items, identitySelector);
        ValidateExistingItemIdentity(sourceIdentity);
        ValidateInsertPosition(position, materializedItems.Count);

        var sourceIndex = FindIndexByIdentity(materializedItems, sourceIdentity, identitySelector);
        var duplicate = duplicateFactory(materializedItems[sourceIndex]);
        ArgumentNullException.ThrowIfNull(duplicate);

        var duplicateIdentity = identitySelector(duplicate);
        if (materializedItems.Any(current => EqualityComparer<TIdentity>.Default.Equals(identitySelector(current), duplicateIdentity)))
        {
            throw new ArgumentException("The duplicate factory must create an item with a new identity.", nameof(duplicateFactory));
        }

        materializedItems.Insert(position, duplicate);
        return ToReadOnly(materializedItems);
    }

    /// <summary>
    /// Returns a new collection without an existing item identified by its stable identity.
    /// </summary>
    public static IReadOnlyList<T> Remove<T, TIdentity>(
        IEnumerable<T> items,
        TIdentity itemIdentity,
        Func<T, TIdentity> identitySelector)
        where TIdentity : notnull
    {
        var materializedItems = MaterializeAndValidate(items, identitySelector);
        ValidateExistingItemIdentity(itemIdentity);

        materializedItems.RemoveAt(FindIndexByIdentity(materializedItems, itemIdentity, identitySelector));
        return ToReadOnly(materializedItems);
    }

    private static List<T> MaterializeAndValidate<T, TIdentity>(IEnumerable<T> items, Func<T, TIdentity> identitySelector)
        where TIdentity : notnull
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(identitySelector);

        var materializedItems = items.ToList();
        if (materializedItems.Any(static item => item is null))
        {
            throw new ArgumentException("An ordered collection cannot contain null items.", nameof(items));
        }

        var identities = materializedItems.Select(identitySelector).ToArray();
        if (identities.GroupBy(static identity => identity).Any(static group => group.Count() > 1))
        {
            throw new ArgumentException("An ordered collection cannot contain duplicate identities.", nameof(items));
        }

        return materializedItems;
    }

    private static int FindIndexByIdentity<T, TIdentity>(IReadOnlyList<T> items, TIdentity itemIdentity, Func<T, TIdentity> identitySelector)
        where TIdentity : notnull
    {
        for (var index = 0; index < items.Count; index++)
        {
            if (EqualityComparer<TIdentity>.Default.Equals(identitySelector(items[index]), itemIdentity))
            {
                return index;
            }
        }

        throw new ArgumentException("The collection does not contain the requested identity.", nameof(itemIdentity));
    }

    private static ReadOnlyCollection<T> ToReadOnly<T>(List<T> items) => new(items);

    private static void ValidateExistingItemIdentity<TIdentity>(TIdentity itemIdentity)
        where TIdentity : notnull
    {
        if (EqualityComparer<TIdentity>.Default.Equals(itemIdentity, default!))
        {
            throw new ArgumentException("A non-default item identity is required.", nameof(itemIdentity));
        }
    }

    private static void ValidateInsertPosition(int position, int count)
    {
        if (position < 0 || position > count)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "The insertion position must be between zero and the collection count, inclusive.");
        }
    }

    private static void ValidateMovePosition(int position, int count)
    {
        if (position < 0 || position >= count)
        {
            throw new ArgumentOutOfRangeException(nameof(position), position, "The destination position must identify an existing zero-based collection position.");
        }
    }
}
