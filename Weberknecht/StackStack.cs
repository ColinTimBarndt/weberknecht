namespace Weberknecht;

/// <summary>
/// A stack that is allocated on the stack.
/// </summary>
internal ref struct StackStack<T>(Span<T> storage)
{

    private readonly Span<T> _storage = storage;
    public int Count { get; private set; } = 0;

    public void Push(T value)
    {
        _storage[Count] = value;
        Count++;
    }

    public bool TryPop(out T? value)
    {
        if (Count == 0)
        {
            value = default;
            return false;
        }

        value = _storage[--Count];
        return true;
    }

}
