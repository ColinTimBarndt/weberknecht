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

public readonly ref struct LabelAddressMap(Span<int> map)
{

	private readonly Span<int> _addresses = map;

	public int this[Label label]
	{
		get => _addresses[label.Id - 1];

		set => _addresses[label.Id - 1] = value;
	}

	public (int, int) this[LabelRange range] => (this[range.Start], this[range.End]);

	public override readonly string ToString()
	{
		if (_addresses.Length == 0)
			return "{}";

		StringBuilder builder = new("{ ");
		for (int i = 0; i < _addresses.Length; i++)
		{
			if (i != 0)
				builder.Append(", ");
			builder.Append(new Label(i + 1)).Append(" => ").Append(_addresses[i].ToString("X04"));
		}
		builder.Append(" }");

		return builder.ToString();
	}

	public static implicit operator LabelAddressMap(Span<int> map) => new(map);

}