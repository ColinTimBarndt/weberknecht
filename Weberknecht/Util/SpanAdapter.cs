/// <summary>
/// Allows using the same implementation for both spans and lists.
/// </summary>

namespace Weberknecht.Util;

internal interface IReadOnlyListAdapter<out T, out TEnumerator>
where TEnumerator : allows ref struct
{

    int Count { get; }
    T this[int index] { get; }
    TEnumerator GetEnumerator();

}

internal static class ReadOnlyListAdapter
{

    public static ReadOnlySpanAdapter<T> Adapter<T>(this ReadOnlySpan<T> span) => new(span);

    public static ReadOnlyListAdapter<T, TList> Adapter<T, TList>(this TList list) where TList : IReadOnlyList<T> => new(list);

}

internal readonly ref struct ReadOnlySpanAdapter<T>(ReadOnlySpan<T> span) : IReadOnlyListAdapter<T, ReadOnlySpan<T>.Enumerator>
{

    private readonly ReadOnlySpan<T> _span = span;

    public int Count => _span.Length;
    public T this[int index] => _span[index];

    public ReadOnlySpan<T>.Enumerator GetEnumerator() => _span.GetEnumerator();

}

internal readonly struct ReadOnlyListAdapter<T, TList>(TList list) : IReadOnlyListAdapter<T, IEnumerator<T>>
where TList : IReadOnlyList<T>
{

    private readonly TList _list = list;

    public int Count => _list.Count;
    public T this[int index] => _list[index];

    public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();

}
