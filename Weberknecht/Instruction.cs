using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using Weberknecht.Metadata;

namespace Weberknecht;

using RLabel = System.Reflection.Emit.Label;

public partial struct Instruction
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

    internal Instruction(OpCode opCode) : this(opCode, null, default) { }

    internal Instruction(OpCode opCode, UnmanagedOperand operand) : this(opCode, null, operand) { }

    internal Instruction(OpCode opCode, object operand) : this(opCode, operand, default) { }

    internal Instruction(OpCode opCode, object? operand, UnmanagedOperand uoperand)
    {
        OpCode = opCode;
        _operand = operand;
        _uoperand = uoperand;
    }

    public override readonly string ToString() => new StringBuilder().Append(in this).ToString();

    public readonly int EncodedSize => ((ushort)OpCode.Value > 255 ? 2 : 1) + OpCode.OperandType.Size;

    internal readonly void Emit(ILGenerator il, ReadOnlySpan<RLabel> labels, ReadOnlySpan<LocalBuilder> locals)
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
                il.Emit(OpCode, ((MethodSignature)_operand!).GetHelper());
                return;

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
                il.Emit(OpCode, _uoperand.@ushort);
                return;

            case OperandType.ShortInlineI:
                il.Emit(OpCode, _uoperand.@sbyte);
                return;

            case OperandType.ShortInlineR:
                il.Emit(OpCode, _uoperand.@float);
                return;

            case OperandType.ShortInlineVar:
                il.Emit(OpCode, _uoperand.@byte);
                return;

            default:
                throw new NotImplementedException($"OperandType {Enum.GetName(OpCode.OperandType)}");
        }
    }

    /// <summary>
    /// Expands a short-form instruction into its full variant.
    /// </summary>
    public readonly Instruction Normalized() => (ushort)OpCode.Value switch
    {
        OpByteCodes.LDARG_0 => new(OpCodes.Ldarg, (ushort)0),
        OpByteCodes.LDARG_1 => new(OpCodes.Ldarg, (ushort)1),
        OpByteCodes.LDARG_2 => new(OpCodes.Ldarg, (ushort)2),
        OpByteCodes.LDARG_3 => new(OpCodes.Ldarg, (ushort)3),
        OpByteCodes.LDARG_S => new(OpCodes.Ldarg, (ushort)_uoperand.@byte),
        OpByteCodes.LDLOC_0 => new(OpCodes.Ldloc, (ushort)0),
        OpByteCodes.LDLOC_1 => new(OpCodes.Ldloc, (ushort)1),
        OpByteCodes.LDLOC_2 => new(OpCodes.Ldloc, (ushort)2),
        OpByteCodes.LDLOC_3 => new(OpCodes.Ldloc, (ushort)3),
        OpByteCodes.LDLOC_S => new(OpCodes.Ldloc, (ushort)_uoperand.@byte),
        OpByteCodes.STLOC_0 => new(OpCodes.Stloc, (ushort)0),
        OpByteCodes.STLOC_1 => new(OpCodes.Stloc, (ushort)1),
        OpByteCodes.STLOC_2 => new(OpCodes.Stloc, (ushort)2),
        OpByteCodes.STLOC_3 => new(OpCodes.Stloc, (ushort)3),
        OpByteCodes.STLOC_S => new(OpCodes.Stloc, (ushort)_uoperand.@byte),
        OpByteCodes.LDARGA_S => new(OpCodes.Ldarga, (ushort)_uoperand.@byte),
        OpByteCodes.STARG_S => new(OpCodes.Starg, (ushort)_uoperand.@byte),
        OpByteCodes.LDLOCA_S => new(OpCodes.Ldloca, (ushort)_uoperand.@byte),
        OpByteCodes.LDC_I4_M1 => new(OpCodes.Ldc_I4, -1),
        OpByteCodes.LDC_I4_0 => new(OpCodes.Ldc_I4, 0),
        OpByteCodes.LDC_I4_1 => new(OpCodes.Ldc_I4, 1),
        OpByteCodes.LDC_I4_2 => new(OpCodes.Ldc_I4, 2),
        OpByteCodes.LDC_I4_3 => new(OpCodes.Ldc_I4, 3),
        OpByteCodes.LDC_I4_4 => new(OpCodes.Ldc_I4, 4),
        OpByteCodes.LDC_I4_5 => new(OpCodes.Ldc_I4, 5),
        OpByteCodes.LDC_I4_6 => new(OpCodes.Ldc_I4, 6),
        OpByteCodes.LDC_I4_7 => new(OpCodes.Ldc_I4, 7),
        OpByteCodes.LDC_I4_8 => new(OpCodes.Ldc_I4, 8),
        OpByteCodes.LDC_I4_S => new(OpCodes.Ldc_I4, (int)_uoperand.@sbyte),
        OpByteCodes.BR_S => new(OpCodes.Br_S, (int)_uoperand.@sbyte),
        OpByteCodes.BRFALSE_S => new(OpCodes.Brfalse_S, (int)_uoperand.@sbyte),
        OpByteCodes.BRTRUE_S => new(OpCodes.Brtrue_S, (int)_uoperand.@sbyte),
        OpByteCodes.BEQ_S => new(OpCodes.Beq_S, (int)_uoperand.@sbyte),
        OpByteCodes.BGE_S => new(OpCodes.Bge_S, (int)_uoperand.@sbyte),
        OpByteCodes.BGT_S => new(OpCodes.Bgt_S, (int)_uoperand.@sbyte),
        OpByteCodes.BLE_S => new(OpCodes.Ble_S, (int)_uoperand.@sbyte),
        OpByteCodes.BLT_S => new(OpCodes.Blt_S, (int)_uoperand.@sbyte),
        OpByteCodes.BNE_UN_S => new(OpCodes.Bne_Un_S, (int)_uoperand.@sbyte),
        OpByteCodes.BGE_UN_S => new(OpCodes.Bge_Un_S, (int)_uoperand.@sbyte),
        OpByteCodes.BGT_UN_S => new(OpCodes.Bgt_Un_S, (int)_uoperand.@sbyte),
        OpByteCodes.BLE_UN_S => new(OpCodes.Ble_Un_S, (int)_uoperand.@sbyte),
        OpByteCodes.BLT_UN_S => new(OpCodes.Blt_Un_S, (int)_uoperand.@sbyte),
        OpByteCodes.LEAVE_S => new(OpCodes.Leave, (int)_uoperand.@sbyte),
        _ => this,
    };

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
                FieldInfo field => FormatField(builder, field),
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

    private static StringBuilder FormatField(StringBuilder builder, FieldInfo field)
    {
        builder.Append(field.FieldType).Append(' ');

        if (field.DeclaringType is Type declType)
            builder.Append(declType.Name).Append("::");

        builder.Append(field.Name);

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
