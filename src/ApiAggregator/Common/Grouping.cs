using System.Collections;

namespace ApiAggregator.Common;

public static class Grouping
{
    public static Grouping<TKey, TElement> Create<TKey, TElement>(TKey key, IEnumerable<TElement> elements)
    {
        return new(key, elements);
    }
}

public sealed class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
{
    private readonly IEnumerable<TElement> elements;

    public TKey Key { get; }

    public Grouping(TKey key, IEnumerable<TElement> elements)
    {
        Key = key;
        this.elements = elements;
    }

    public IEnumerator<TElement> GetEnumerator()
    {
        return elements.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
