using System.Reflection;
using System.Reflection.Emit;

namespace Weberknecht;

public static class MethodReader
{

    public static Method Read(Delegate d) => Read(d.Method);

    public static Method Read(MethodInfo method)
    {
        var body = method.GetMethodBody()
            ?? throw new InvalidOperationException("Method has no available body");
        var assembly = method.Module.Assembly;
        var metadata = assembly.GetMetadataReader()
            ?? throw new InvalidOperationException("Cannot read assembly metadata");
        var ilBytes = body.GetILAsByteArray()
            ?? throw new InvalidOperationException("Method has no body");

        var ctx = new ResolutionContext(method.Module, metadata);

        List<Instruction> instructions = [];
        Dictionary<int, int> jumpTable = [];

        var il = new InstructionDecoder(ilBytes, ctx);
        while (il.MoveNext())
        {
            jumpTable.Add(il.CurrentAddress, instructions.Count);
            instructions.Add(il.Current);
        }

        ushort lastLabel = 0;
        for (int i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];
            if (instr.OpCode.OperandType is not OperandType.InlineBrTarget and not OperandType.ShortInlineBrTarget)
                continue;

            var targetIndex = jumpTable[(int)instr._operand!];
            var target = instructions[targetIndex];
            if (target._label == 0)
            {
                target._label = ++lastLabel;
                instructions[targetIndex] = target;
            }
            instr._operand = lastLabel;
            instructions[i] = instr;
        }

        return new(method.ReturnType, [.. method.GetParameters().Select(info => (Method.Parameter)info)], instructions);
    }

}