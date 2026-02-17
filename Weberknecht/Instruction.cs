using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using Weberknecht.Metadata;

namespace Weberknecht;

public struct Instruction
{

    public readonly OpCode OpCode { get; init; } = OpCodes.Nop;
    internal object? _operand = null;
    internal UnmanagedOperand _uoperand = default;
    public SequencePoint? DebugInfo { get; set; }

    [StructLayout(LayoutKind.Explicit)]
    internal struct UnmanagedOperand
    {
        [FieldOffset(0)]
        internal byte @byte;

        [FieldOffset(0)]
        internal sbyte @sbyte;

        [FieldOffset(0)]
        internal ushort @ushort;

        [FieldOffset(0)]
        internal int @int;

        [FieldOffset(0)]
        internal long @long;

        [FieldOffset(0)]
        internal float @float;

        [FieldOffset(0)]
        internal double @double;

        public static implicit operator UnmanagedOperand(byte value) => new() { @byte = value };

        public static implicit operator UnmanagedOperand(sbyte value) => new() { @sbyte = value };

        public static implicit operator UnmanagedOperand(ushort value) => new() { @ushort = value };

        public static implicit operator UnmanagedOperand(int value) => new() { @int = value };

        public static implicit operator UnmanagedOperand(long value) => new() { @long = value };

        public static implicit operator UnmanagedOperand(float value) => new() { @float = value };

        public static implicit operator UnmanagedOperand(double value) => new() { @double = value };
    }

    internal Instruction(OpCode code, object? operand, UnmanagedOperand uoperand)
    {
        OpCode = code;
        _operand = operand;
        _uoperand = uoperand;
    }

    public override readonly string ToString() => new StringBuilder().Append(in this).ToString();

    internal void Emit(ILGenerator il, ReadOnlySpan<Label> labels, ReadOnlySpan<LocalBuilder> locals)
    {
        switch (OpCode.OperandType)
        {
            case OperandType.InlineBrTarget:
            case OperandType.ShortInlineBrTarget:
                il.Emit(OpCode, labels[_uoperand.@int - 1]);
                return;

            case OperandType.InlineField:
                il.Emit(OpCode, (FieldInfo)_operand!);
                return;

            case OperandType.InlineI:
                il.Emit(OpCode, _uoperand.@int);
                return;

            case OperandType.InlineI8:
                il.Emit(OpCode, _uoperand.@byte);
                return;

            case OperandType.InlineMethod:
                if (_operand is MethodInfo method)
                    il.Emit(OpCode, method);
                else if (_operand is ConstructorInfo ctor)
                    il.Emit(OpCode, ctor);
                else
                    throw new NotImplementedException($"InlineMethod {_operand?.GetType()}");
                return;

            case OperandType.InlineNone:
                il.Emit(OpCode);
                return;

            case OperandType.InlineR:
                il.Emit(OpCode, _uoperand.@double);
                return;

            case OperandType.InlineSig:
                //il.Emit(OpCode, SignatureHelper.);
                throw new NotImplementedException("InlineSig"); // TODO

            case OperandType.InlineString:
                il.Emit(OpCode, (string)_operand!);
                return;

            case OperandType.InlineSwitch:
                throw new NotImplementedException("switch");

            case OperandType.InlineTok:
                if (_operand is FieldInfo field)
                    il.Emit(OpCode, field);
                else if (_operand is MethodInfo method2)
                    il.Emit(OpCode, method2);
                else if (_operand is ConstructorInfo ctor2)
                    il.Emit(OpCode, ctor2);
                else if (_operand is Type type)
                    il.Emit(OpCode, type);
                else
                    throw new NotImplementedException($"InlineTok {_operand?.GetType()}");
                return;

            case OperandType.InlineType:
                il.Emit(OpCode, (Type)_operand!);
                return;

            case OperandType.InlineVar:
                throw new NotImplementedException("InlineVar");
            //il.Emit(OpCode, _uoperand.@ushort); // TODO

            case OperandType.ShortInlineI:
                il.Emit(OpCode, _uoperand.@sbyte);
                return;

            case OperandType.ShortInlineR:
                il.Emit(OpCode, _uoperand.@float);
                return;

            case OperandType.ShortInlineVar:
                throw new NotImplementedException("ShortInlineVar");
            //il.Emit(OpCode, _uoperand.@byte); // TODO

            default:
                throw new NotImplementedException($"OperandType {Enum.GetName(OpCode.OperandType)}");
        }
    }

}

public static class InstructionExt
{

    public static StringBuilder Append(this StringBuilder builder, in Instruction self)
    {
        builder.Append(self.OpCode);

        if (self.OpCode.OperandType == OperandType.InlineNone)
            return builder;

        builder.Append(' ');

        if (self._operand != null)
            return self._operand switch
            {
                string s => FormatLiteral(builder, s),
                MethodInfo method => FormatMethod(builder, method),
                ConstructorInfo ctor => FormatConstructor(builder, ctor),
                _ => builder.Append(self._operand),
            };

        return self.OpCode.OperandType switch
        {
            OperandType.InlineBrTarget or
            OperandType.ShortInlineBrTarget => builder.AppendFormat("L{0:X}", self._uoperand.@int),
            OperandType.InlineI => builder.Append(self._uoperand.@int),
            OperandType.InlineI8 => builder.Append(self._uoperand.@byte),
            OperandType.InlineR => builder.Append(self._uoperand.@double),
            OperandType.InlineVar => builder.Append(self._uoperand.@ushort),
            OperandType.ShortInlineI => builder.Append(self._uoperand.@sbyte),
            OperandType.ShortInlineR => builder.Append(self._uoperand.@float),
            OperandType.ShortInlineVar => builder.Append(self._uoperand.@byte),
            _ => builder.Append("<unknown>"),
        }
        ;
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

    internal int _label;

    public override readonly string ToString() => new StringBuilder().Append(in this).ToString();

    private PseudoInstruction(PseudoInstructionType tag)
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

    public static ref int AsLabelRef(ref this PseudoInstruction self)
    {
        if (self.Type != PseudoInstructionType.Label)
            throw new InvalidOperationException();
        return ref self._label;
    }

    public static int AsLabel(this PseudoInstruction self)
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
