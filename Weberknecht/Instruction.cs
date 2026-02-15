using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using Weberknecht.Metadata;

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

        if (OpCode.OperandType is OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget)
            return $"{prefix}{OpCode} L{_operand:X4}";

        StringBuilder builder = new();
        builder.Append(prefix).Append(OpCode).Append(' ');
        return (_operand switch
        {
            string s => FormatLiteral(builder, s),
            MethodInfo method => FormatMethod(builder, method),
            ConstructorInfo ctor => FormatConstructor(builder, ctor),
            _ => builder.Append(_operand),
        }).ToString();
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
