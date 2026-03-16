using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Weberknecht;

[StructLayout(LayoutKind.Explicit)]
public readonly struct Label(int id) : IEquatable<Label>
{

    [field: FieldOffset(0)]
    public int Id { get; } = id;

    public bool IsNull => Id == 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Label other) => Id == other.Id;

    public override bool Equals(object? obj) => obj is Label l && Equals(l);

    public override string ToString() => IsNull ? string.Empty : $"L{Id}";

    public override int GetHashCode() => Id.GetHashCode();

    public static explicit operator int(Label label) => label.Id;

    public static explicit operator Label(int id) => new(id);

    internal static ref Label CastRef(ref int id) => ref Unsafe.As<int, Label>(ref id);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Label left, Label right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Label left, Label right) => !left.Equals(right);

}

public readonly struct LabelRange(Label start, Label end) : IEquatable<LabelRange>
{

    public Label Start { get; } = start;
    public Label End { get; } = end;

    public bool IsNull => Start.IsNull;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(LabelRange other) => Start == other.Start && End == other.End;

    public override bool Equals(object? obj) => obj is LabelRange range && Equals(range);

    public override string ToString() => IsNull ? string.Empty : $"{Start} to {End}";

    public override int GetHashCode() => HashCode.Combine(Start, End);

    public static implicit operator (Label, Label)(LabelRange range) => (range.Start, range.End);

    public static implicit operator LabelRange((Label, Label) tuple) => new(tuple.Item1, tuple.Item2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(LabelRange left, LabelRange right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(LabelRange left, LabelRange right) => !left.Equals(right);

}

public static class LabelExt
{

    extension(StringBuilder builder)
    {
        public StringBuilder AppendLabel(Label label) => builder.Append('L').Append(label.Id);

        public StringBuilder AppendLabelRange(LabelRange range)
            => builder.AppendLabel(range.Start)
                .Append(" to ")
                .AppendLabel(range.End);
    }

}

public readonly ref struct LabelMap<TValue>(Span<TValue> map)
{

    private readonly Span<TValue> _span = map;

    public TValue this[Label label]
    {
        get => _span[label.Id - 1];

        set => _span[label.Id - 1] = value;
    }

    public (TValue, TValue) this[LabelRange range] => (this[range.Start], this[range.End]);

    public override readonly string ToString()
    {
        if (_span.Length == 0)
            return "{}";

        StringBuilder builder = new("{ ");
        if (typeof(TValue) == typeof(int))
        {
            for (int i = 0; i < _span.Length; i++)
            {
                if (i != 0)
                    builder.Append(", ");
                builder
                    .Append(new Label(i + 1))
                    .Append(" => ")
                    .Append(Unsafe.As<TValue, int>(ref _span[i]).ToString("X04", CultureInfo.InvariantCulture));
            }
        }
        else
        {
            for (int i = 0; i < _span.Length; i++)
            {
                if (i != 0)
                    builder.Append(", ");
                builder.Append(new Label(i + 1)).Append(" => ").Append(_span[i]);
            }
        }
        builder.Append(" }");

        return builder.ToString();
    }

    public static implicit operator LabelMap<TValue>(Span<TValue> map) => new(map);

    public static implicit operator Span<TValue>(LabelMap<TValue> map) => map._span;

}
