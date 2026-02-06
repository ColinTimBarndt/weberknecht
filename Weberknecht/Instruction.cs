using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace Weberknecht;

public struct Instruction
{

    public readonly OpCode OpCode { get; init; } = OpCodes.Nop;
    internal ushort _label = 0;
    internal object? _operand = null;
    public SequencePoint? DebugInfo { get; set; }

    internal Instruction(OpCode code, object? immadiate)
    {
        OpCode = code;
        _operand = immadiate;
    }

    public override readonly string ToString() => ToString(false);

    public readonly string ToString(bool label)
    {
        var prefix = label ? (_label == 0 ? "        " : $"L{_label:X4}:  ") : null;
        if (_operand == null)
        {
            return $"{prefix}{OpCode}";
        }

        switch (OpCode.OperandType)
        {
            case OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget:
                return $"{prefix}{OpCode} L{_operand:X4}";
            default:
                break;
        }

        if (_operand is string s)
        {
            return $"{prefix}{OpCode} \"{s}\"";
        }

        return $"{prefix}{OpCode} {_operand}";
    }

}
