using System.Reflection.Emit;

namespace Weberknecht;

internal static class OpCodeTable
{

    private static readonly Dictionary<short, OpCode> _codes;

    static OpCodeTable()
    {
        _codes = [];
        foreach (var field in typeof(OpCodes).GetFields())
        {
            if (field.GetValue(null) is OpCode code)
                _codes.Add(code.Value, code);
        }
    }

    public static OpCode Decode(short code) => _codes[code];

}