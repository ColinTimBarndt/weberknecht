namespace Weberknecht;

public partial struct Instruction : IEquatable<Instruction>
{

    public readonly bool Equals(Instruction other)
        => OpCode == other.OpCode
        && _operand == other._operand
        && _uoperand.@long == other._uoperand.@long;

    public override readonly bool Equals(object? other)
        => other is Instruction instr && Equals(instr);

    public override readonly int GetHashCode()
        => _operand == null
            ? HashCode.Combine(OpCode, _uoperand.@long)
            : HashCode.Combine(OpCode, _operand);

    public static bool operator ==(Instruction left, Instruction right) => left.Equals(right);

    public static bool operator !=(Instruction left, Instruction right) => !left.Equals(right);

}
