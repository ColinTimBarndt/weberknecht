using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace Weberknecht;

public readonly struct Instruction
{

    public readonly OpCode OpCode { get; init; }
    internal readonly ushort _label = 0;
    internal readonly object? _operand = null;

    public Instruction()
    {
        OpCode = OpCodes.Nop;
    }

    internal Instruction(OpCode code, object? immadiate)
    {
        OpCode = code;
        _operand = immadiate;
    }

    // TODO: temp
    internal string ToString(MetadataReader? metadata)
    {
        if (_operand == null) return $"{OpCode}";

        switch (OpCode.OperandType)
        {
            case OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget:
                return $"{OpCode} {_operand:X4}";
            default:
                break;
        }

        if (_operand is string s)
        {
            return $"{OpCode} \"{s}\"";
        }

        if (metadata != null)
        {
            StringHandle? name = null;

            if (_operand is MemberReference member)
                name = member.Name;

            else if (_operand is FieldDefinition field)
                name = field.Name;

            else if (_operand is MethodDefinition method)
                name = method.Name;

            else if (_operand is StringHandle str)
                name = str;

            if (name != null)
            {
                return $"{OpCode} \"{metadata.GetString((StringHandle)name)}\"";
            }
        }

        return $"{OpCode} {_operand}";
    }

}
