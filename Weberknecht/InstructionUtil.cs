using System.Reflection;

namespace Weberknecht;

using ConstrainedCallReplacerFunc = Func<Method, Type, MethodInfo, List<Instruction>, bool>;
using CallReplacerFunc = Func<Method, MethodInfo, List<Instruction>, bool>;

public static class InstructionUtil
{

    extension(Method self)
    {

        // Calls

        public void ReplaceCalls(CallReplacerFunc replacer)
            => Internal_ReplaceCalls<CallReplacer>(self, replacer, []);

        public void ReplaceCalls(CallReplacerFunc replacer, List<Instruction> pooledList)
            => Internal_ReplaceCalls<CallReplacer>(self, replacer, pooledList);

        public void ReplaceCalls<TReplacer>(TReplacer replacer)
        where TReplacer : ICallReplacer, allows ref struct
            => Internal_ReplaceCalls(self, replacer, []);

        public void ReplaceCalls<TReplacer>(TReplacer replacer, List<Instruction> pooledList)
        where TReplacer : ICallReplacer, allows ref struct
            => Internal_ReplaceCalls(self, replacer, pooledList);

        // Constrained calls

        public void ReplaceInterfaceCalls(ConstrainedCallReplacerFunc replacer)
            => Internal_ReplaceInterfaceCalls<ConstrainedCallReplacer>(self, replacer, []);

        public void ReplaceInterfaceCalls(ConstrainedCallReplacerFunc replacer, List<Instruction> pooledList)
            => Internal_ReplaceInterfaceCalls<ConstrainedCallReplacer>(self, replacer, pooledList);

        public void ReplaceInterfaceCalls<TReplacer>(TReplacer replacer)
        where TReplacer : IConstrainedCallReplacer, allows ref struct
            => Internal_ReplaceInterfaceCalls(self, replacer, []);

        public void ReplaceInterfaceCalls<TReplacer>(TReplacer replacer, List<Instruction> pooledList)
        where TReplacer : IConstrainedCallReplacer, allows ref struct
            => Internal_ReplaceInterfaceCalls(self, replacer, pooledList);

    }

    private static bool IsCall(ushort opCode) => opCode is OpByteCodes.CALL or OpByteCodes.CALLVIRT;

    private static void Internal_ReplaceCalls<TReplacer>(Method self, TReplacer replacer, List<Instruction> pooledList)
    where TReplacer : ICallReplacer, allows ref struct
    {
        int i = 0;
        while (i < self.Instructions.Count)
        {
            ref readonly var current = ref self.Instructions.AsSpan()[i];

            // Match:
            // call(virt) <Method>
            if (!IsCall((ushort)current.OpCode.Value))
            {
                i++;
                continue;
            }

            var method = (MethodInfo)current._operand!;

            pooledList.Clear();
            if (replacer.ReplaceCall(self, method, pooledList))
            {
                self.Instructions.InsertRange(i, 1, pooledList);
                i += pooledList.Count;
                continue;
            }

            i++;
        }
    }

    private static void Internal_ReplaceInterfaceCalls<TReplacer>(Method self, TReplacer replacer, List<Instruction> pooledList)
    where TReplacer : IConstrainedCallReplacer, allows ref struct
    {
        int i = 1;
        while (i < self.Instructions.Count)
        {
            var window = self.Instructions.AsSpan().Slice(i - 1, 2);

            ref readonly var current = ref window[1];
            ref readonly var prev = ref window[0];

            // Match:
            // constrained. <Type>
            // call(virt) <Method>
            if (!IsCall((ushort)current.OpCode.Value)
                || (ushort)prev.OpCode.Value != OpByteCodes.CONSTRAINED)
            {
                i++;
                continue;
            }

            var type = (Type)prev._operand!;
            var method = (MethodInfo)current._operand!;

            pooledList.Clear();
            if (replacer.ReplaceCall(self, type, method, pooledList))
            {
                i--; // Go to constrained prefix
                self.Instructions.InsertRange(i, 2, pooledList);
                i += pooledList.Count;
                continue;
            }

            i++;
        }
    }

    private readonly struct CallReplacer(CallReplacerFunc func) : ICallReplacer
    {

        private readonly CallReplacerFunc _func = func;

        bool ICallReplacer.ReplaceCall(Method method, MethodInfo callMethod, List<Instruction> result)
            => _func(method, callMethod, result);

        public static implicit operator CallReplacer(CallReplacerFunc func) => new(func);

    }

    private readonly struct ConstrainedCallReplacer(ConstrainedCallReplacerFunc func) : IConstrainedCallReplacer
    {

        private readonly ConstrainedCallReplacerFunc _func = func;

        bool IConstrainedCallReplacer.ReplaceCall(Method method, Type callType, MethodInfo callMethod, List<Instruction> result)
            => _func(method, callType, callMethod, result);

        public static implicit operator ConstrainedCallReplacer(ConstrainedCallReplacerFunc func) => new(func);

    }

}

public interface ICallReplacer
{

    bool ReplaceCall(Method method, MethodInfo callMethod, List<Instruction> result);

}

public interface IConstrainedCallReplacer
{

    bool ReplaceCall(Method method, Type callType, MethodInfo callMethod, List<Instruction> result);

}
