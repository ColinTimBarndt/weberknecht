using System.Text;

namespace Weberknecht;

public struct PseudoInstruction()
{

    public readonly PseudoInstructionType Type { get; } = PseudoInstructionType.Instruction;

    // managed type
    internal Instruction _instruction = new();

    internal int _label;

    public override readonly string ToString() => new StringBuilder().Append(in this).ToString();

    private PseudoInstruction(PseudoInstructionType tag) : this()
    {
        Type = tag;
    }

    private PseudoInstruction(Instruction instr) : this(PseudoInstructionType.Instruction)
    {
        _instruction = instr;
    }

    private PseudoInstruction(int label) : this(PseudoInstructionType.Label)
    {
        _label = label;
    }

    public static implicit operator PseudoInstruction(Instruction instr) => new(instr);

    public static PseudoInstruction Label(int label) => new(label);

}

public enum PseudoInstructionType : byte
{
    Instruction,
    Label,
}

// Extension class to receive 'this' by ref
public static class PseudoInstructionExt
{

    public static ref Instruction AsInstructionRef(ref this PseudoInstruction self)
    {
        if (self.Type != PseudoInstructionType.Instruction)
            throw new InvalidOperationException();
        return ref self._instruction;
    }

    public static ref readonly Instruction AsInstructionRefReadonly(ref readonly this PseudoInstruction self)
    {
        if (self.Type != PseudoInstructionType.Instruction)
            throw new InvalidOperationException();
        return ref self._instruction;
    }

    public static Instruction AsInstruction(this PseudoInstruction self)
    {
        if (self.Type != PseudoInstructionType.Instruction)
            throw new InvalidOperationException();
        return self._instruction;
    }

    public static ref Label AsLabelRef(ref this PseudoInstruction self)
    {
        if (self.Type != PseudoInstructionType.Label)
            throw new InvalidOperationException();
        return ref Label.CastRef(ref self._label);
    }

    public static Label AsLabel(this PseudoInstruction self)
    {
        if (self.Type != PseudoInstructionType.Label)
            throw new InvalidOperationException();
        return (Label)self._label;
    }

    public static StringBuilder Append(this StringBuilder builder, in PseudoInstruction self)
    {
        return self.Type switch
        {
            PseudoInstructionType.Instruction => builder.Append(in self._instruction),
            PseudoInstructionType.Label => builder.AppendLabel(self.AsLabel()).Append(':'),
            _ => throw new NotImplementedException(Enum.GetName(self.Type)),
        };
    }

}
