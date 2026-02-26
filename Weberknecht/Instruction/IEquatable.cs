namespace Weberknecht;

public partial struct Instruction : IEquatable<Instruction>
{

    public readonly bool Equals(Instruction other)
        => OpCode == other.OpCode
        && _operand == other._operand
        && _uoperand.@long == other._uoperand.@long
        && _label == other.Label;

    public override readonly bool Equals(object? obj)
        => obj is Instruction instr && Equals(instr);

    public override readonly int GetHashCode()
        => _operand == null
            ? HashCode.Combine(_label, OpCode, _uoperand.@long)
            : HashCode.Combine(_label, OpCode, _operand);

    public static bool operator ==(Instruction left, Instruction right) => left.Equals(right);

    public static bool operator !=(Instruction left, Instruction right) => !left.Equals(right);

    public readonly struct LabelIgnoringComparer() : IEqualityComparer<Instruction>
    {

        public bool Equals(Instruction x, Instruction y)
            => x.OpCode == y.OpCode
            && x._operand == y._operand
            && x._uoperand.@long == y._uoperand.@long;

        public int GetHashCode(Instruction obj)
            => obj._operand == null
                ? HashCode.Combine(obj.OpCode, obj._uoperand.@long)
                : HashCode.Combine(obj.OpCode, obj._operand);

    }

}
