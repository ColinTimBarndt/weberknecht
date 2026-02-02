using System.Reflection.Emit;

namespace Weberknecht;

internal static class OperandTypes
{
    private static readonly byte[] _sizes;

    static OperandTypes()
    {
        _sizes = new byte[(Enum.GetValues<OperandType>().Select(t => (int)t).Aggregate(int.Max)) + 1];
        _sizes[(int)OperandType.InlineBrTarget] = 4;
        _sizes[(int)OperandType.InlineField] = 4;
        _sizes[(int)OperandType.InlineI] = 4;
        _sizes[(int)OperandType.InlineI8] = 1;
        _sizes[(int)OperandType.InlineMethod] = 4;
        _sizes[(int)OperandType.InlineNone] = 0;
        _sizes[(int)OperandType.InlineR] = 8;
        _sizes[(int)OperandType.InlineSig] = 4;
        _sizes[(int)OperandType.InlineString] = 4;
        _sizes[(int)OperandType.InlineSwitch] = 4;
        _sizes[(int)OperandType.InlineTok] = 4; // ?
        _sizes[(int)OperandType.InlineType] = 4;
        _sizes[(int)OperandType.InlineVar] = 2;
        _sizes[(int)OperandType.ShortInlineBrTarget] = 1;
        _sizes[(int)OperandType.ShortInlineI] = 1;
        _sizes[(int)OperandType.ShortInlineR] = 4;
        _sizes[(int)OperandType.ShortInlineVar] = 1;
    }

    extension(OperandType type)
    {
        public int Size => _sizes[(int)type];
    }
}