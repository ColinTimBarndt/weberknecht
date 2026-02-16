using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Weberknecht.Metadata;

namespace Weberknecht;

public struct Instruction
{

    public readonly OpCode OpCode { get; init; } = OpCodes.Nop;
    internal object? _operand = null;
    public SequencePoint? DebugInfo { get; set; }

    internal Instruction(OpCode code, object? immadiate)
    {
        OpCode = code;
        _operand = immadiate;
    }

    public override readonly string ToString() => new StringBuilder().Append(in this).ToString();

}

public static class InstructionExt
{

    public static StringBuilder Append(this StringBuilder builder, in Instruction self)
    {
        builder.Append(self.OpCode);

        if (self._operand == null)
            return builder;

        builder.Append(' ');

        if (self.OpCode.OperandType is OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget)
            return builder.AppendFormat("L{0:X}", self._operand);

        return self._operand switch
        {
            string s => FormatLiteral(builder, s),
            MethodInfo method => FormatMethod(builder, method),
            ConstructorInfo ctor => FormatConstructor(builder, ctor),
            _ => builder.Append(self._operand),
        };
    }

    private static StringBuilder FormatLiteral(StringBuilder builder, string literal)
    {
        builder.Append('"');
        foreach (char ch in literal)
        {
            switch (ch)
            {
                case '"':
                case '\'':
                case '\\':
                    builder.Append('\\').Append(ch);
                    continue;

                case '\n':
                    builder.Append("\\n");
                    continue;

                case '\r':
                    builder.Append("\\r");
                    continue;

                case '\f':
                    builder.Append("\\f");
                    continue;

                default:
                    if (ch < 0x20)
                        builder.AppendFormat("\\x{0:02x}", ch);
                    else
                        builder.Append(ch);
                    continue;
            }
        }
        builder.Append('"');
        return builder;
    }

    private static StringBuilder FormatMethod(StringBuilder builder, MethodInfo method)
    {
        builder.Append(method.ReturnType).Append(' ');

        if (method.DeclaringType is Type declType)
            builder.Append(declType.Name).Append("::");

        builder.Append(method.Name);

        var generics = method.GetGenericArguments();
        if (generics.Length > 0)
        {
            builder.Append('<').AppendJoin(", ", generics.Select(t => t.Name)).Append('>');
        }

        builder.Append('(').AppendJoin(", ", method.GetParameters()).Append(')');

        return builder;
    }

    private static StringBuilder FormatConstructor(StringBuilder builder, ConstructorInfo ctor)
    {
        builder.Append(ctor.DeclaringType?.Name ?? "<unnamed>");

        builder.Append('(').AppendJoin(", ", ctor.GetParameters()).Append(')');

        return builder;
    }

}

public enum PseudoInstructionType : byte
{
    Invalid = 0,
    Instruction,
    Label,
    Try,
    Catch,
    Finally,
}

public struct PseudoInstruction
{
    public readonly PseudoInstructionType Type { get; } = PseudoInstructionType.Invalid;

    // managed type
    internal Instruction _instruction;

    internal uint _label;

    public override readonly string ToString() => new StringBuilder().Append(in this).ToString();

    private PseudoInstruction(PseudoInstructionType tag)
    {
        Type = tag;
    }

    private PseudoInstruction(Instruction instr) : this(PseudoInstructionType.Instruction)
    {
        _instruction = instr;
    }

    private PseudoInstruction(uint label) : this(PseudoInstructionType.Label)
    {
        _label = label;
    }

    public static implicit operator PseudoInstruction(Instruction instr) => new(instr);

    public static PseudoInstruction Label(uint label) => new(label);

    public static PseudoInstruction Try() => new(PseudoInstructionType.Try);

    public static PseudoInstruction Catch() => new(PseudoInstructionType.Catch);

    public static PseudoInstruction Finally() => new(PseudoInstructionType.Finally);

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

    public static Instruction AsInstruction(this PseudoInstruction self)
    {
        if (self.Type != PseudoInstructionType.Instruction)
            throw new InvalidOperationException();
        return self._instruction;
    }

    public static ref uint AsLabelRef(ref this PseudoInstruction self)
    {
        if (self.Type != PseudoInstructionType.Label)
            throw new InvalidOperationException();
        return ref self._label;
    }

    public static uint AsLabel(this PseudoInstruction self)
    {
        if (self.Type != PseudoInstructionType.Label)
            throw new InvalidOperationException();
        return self._label;
    }

    public static StringBuilder Append(this StringBuilder builder, in PseudoInstruction self)
    {
        return self.Type switch
        {
            PseudoInstructionType.Invalid => builder,
            PseudoInstructionType.Instruction => builder.Append(in self._instruction),
            PseudoInstructionType.Label => builder.AppendFormat("L{0:X}:", self._label),
            PseudoInstructionType.Try => builder.Append("try:"),
            PseudoInstructionType.Catch => builder.Append("catch:"), //TODO
            PseudoInstructionType.Finally => builder.Append("finally:"),
            _ => throw new NotImplementedException(Enum.GetName(self.Type)),
        };
    }

}
