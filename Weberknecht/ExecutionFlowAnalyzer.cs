using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;

namespace Weberknecht;

using LabelAddressMap = LabelMap<int>;

internal static class ExecutionFlowAnalyzer
{

    public static StackSizeResult GetMaxStackSize(
        ReadOnlySpan<Instruction> instructions,
        ReadOnlySpan<ExceptionHandlingClause> exceptionHandlers,
        int labelCount,
        bool hasReturnValue)
    {
        if (instructions.Length == 0)
            return StackSizeResult.Ok(0);

        LabelAddressMap labelTargets = stackalloc int[labelCount];
        for (int index = 0; index < instructions.Length; index++)
        {
            ref readonly var label = ref instructions[index]._label;
            if (label.IsNull)
                continue;
            labelTargets[label] = index;
        }

        int returnSize = hasReturnValue ? 2 : 1; // Starts at 1

        // Starts at 1, 0 indicates absence of a value
        int[] stackSize = new int[instructions.Length];
        StackStack<int> work = new(stackalloc int[labelCount]);

        foreach (var clause in exceptionHandlers)
        {
            int handlerIndex = labelTargets[clause.Handler.Start];
            stackSize[handlerIndex] = clause.Flags is ExceptionHandlingClauseOptions.Clause or ExceptionHandlingClauseOptions.Filter ? 2 : 1;
            work.Push(handlerIndex);
            if (!clause.FilterStart.IsNull)
            {
                int filterIndex = labelTargets[clause.FilterStart];
                stackSize[filterIndex] = 2;
                work.Push(filterIndex);
            }
        }

        stackSize[0] = 1;

        return InternalGetMaxStackSize(instructions, labelTargets, work, stackSize, returnSize);
        // var result = InternalGetMaxStackSize(instructions, labelTargets, work, stackSize, returnSize);
        // if (result.IsError)
        // {
        //     int index = (int)result.ConflictingInstructionIndex!;
        //     int maxIndex = int.Min(instructions.Length, index + 10);
        //     for (int i = int.Max(0, index - 20); i < maxIndex; i++)
        //         Console.WriteLine($"{(i == index ? '>' : ' ')}  {stackSize[i]} {instructions[i]}");
        // }
        // return result;
    }

    private static StackSizeResult InternalGetMaxStackSize(
        ReadOnlySpan<Instruction> instructions,
        LabelAddressMap labelTargets,
        StackStack<int> work,
        Span<int> stackSize,
        int returnSize)
    {
        StackSizeResult result;
        int index = 0;

        do
        {
        Start:
            ref readonly var instruction = ref instructions[index];
            int currentSize = stackSize[index];

            var opcode = instruction.OpCode;
            int target;
            int newSize = currentSize + opcode.EvaluationStackDelta;
            switch (opcode.FlowControl)
            {
                case FlowControl.Branch:
                    target = labelTargets[instruction._uoperand.label];
                    if ((result = SetStackSize(stackSize, index = target, newSize)).IsError)
                        return result;
                    if (result.IsZero)
                        goto Start;
                    continue;

                case FlowControl.Break: // Debugger break instruction
                case FlowControl.Meta: // Metadata instruction
                    if ((result = SetStackSize(stackSize, ++index, newSize)).IsError)
                        return result;
                    if (result.IsZero)
                        goto Start;
                    continue;

                case FlowControl.Call:
                    if (opcode == OpCodes.Jmp)
                    {
                        // JMP passes execution onto the target method
                        // Arguments are inherited, the stack must be empty
                        if (newSize != 1 /* 1 = empty, 0 = absent */)
                            goto Error;
                        continue;
                    }
                    var methodBase = (MethodBase)instruction._operand!;
                    var callingConv = methodBase.CallingConvention;
                    bool hasThis = callingConv.HasFlag(CallingConventions.HasThis);
                    if (hasThis && !callingConv.HasFlag(CallingConventions.ExplicitThis) && opcode.Value != OpByteCodes.NEWOBJ)
                        newSize--; // pop this
                    newSize -= methodBase.GetParameters().Length;
                    if (methodBase is MethodInfo method && method.ReturnType != typeof(void))
                        newSize++;
                    goto Next;

                case FlowControl.Cond_Branch:
                    switch (opcode.OperandType)
                    {
                        case OperandType.InlineBrTarget:
                        case OperandType.ShortInlineBrTarget:
                            target = labelTargets[instruction._uoperand.label];
                            if ((result = SetStackSize(stackSize, target, newSize)).IsError)
                                return result;
                            if (result.IsZero)
                                work.Push(target);
                            goto Next;

                        case OperandType.InlineSwitch:
                            foreach (var label in (ImmutableArray<Label>)instruction._operand!)
                            {
                                target = labelTargets[label];
                                if ((result = SetStackSize(stackSize, target, newSize)).IsError)
                                    return result;
                                if (result.IsZero)
                                    work.Push(target);
                            }
                            goto Next;

                        default:
                            goto Error;
                    }

                case FlowControl.Next:
                Next:
                    if ((result = SetStackSize(stackSize, ++index, newSize)).IsError)
                        return result;
                    if (result.IsZero)
                        goto Start;
                    continue;

                case FlowControl.Return:
                    int expectedSize = (ushort)opcode.Value == OpByteCodes.RET ? returnSize : 1;
                    if (newSize != expectedSize)
                        goto Error;
                    continue;

                case FlowControl.Throw:
                    if (newSize != 1)
                        goto Error;
                    continue;

                default:
#if DEBUG
                    throw new NotImplementedException(Enum.GetName(opcode.FlowControl));
#else
                    goto Error;
#endif
            }
        } while (work.TryPop(out index));

        int maxSize = 0;
        foreach (int currentSize in stackSize)
            maxSize = int.Max(maxSize, currentSize);
        return StackSizeResult.Ok(maxSize - 1);
    Error:
        return StackSizeResult.Err(index);
    }

    private static StackSizeResult SetStackSize(Span<int> stackSize, int index, int value)
    {
        ref int currentSizeRef = ref stackSize[index];
        int currentSize = currentSizeRef;
        if (value > 0 && (currentSize == 0 || currentSize == value))
        {
            currentSizeRef = value;
            return StackSizeResult.Ok(currentSize);
        }
        else
        {
            return StackSizeResult.Err(index);
        }
    }

    public readonly struct StackSizeResult
    {

        private readonly int _value;

        private StackSizeResult(int value)
        {
            _value = value;
        }

        public bool IsError => _value < 0;

        public bool IsZero => _value == 0;

        public int MaxStackSizeOrThrow() => IsError ? throw new ConflictingStackSizeException(~_value) : _value;

        public int? MaxStackSize => IsError ? null : _value;

        public int? ConflictingInstructionIndex => IsError ? ~_value : null;

        public static StackSizeResult Ok(int maxSize) => new(maxSize);

        public static StackSizeResult Err(int instructionIndex) => new(~instructionIndex);
        // throw new ConflictingStackSizeException(instructionIndex);

    }

}
