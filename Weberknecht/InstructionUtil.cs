using System.Reflection;

namespace Weberknecht;

using StaticCallReplacerFunc = Func<Method, Type, MethodInfo, List<Instruction>, bool>;

public static class InstructionUtil
{

    extension(Method self)
    {

        public void ReplaceInterfaceCalls(StaticCallReplacerFunc replacer)
            => Internal_ReplaceInterfaceCalls<CallReplacer>(self, replacer, []);

        public void ReplaceInterfaceCalls(StaticCallReplacerFunc replacer, List<Instruction> pooledList)
            => Internal_ReplaceInterfaceCalls<CallReplacer>(self, replacer, pooledList);

        public void ReplaceInterfaceCalls<TReplacer>(TReplacer replacer)
        where TReplacer : ICallReplacer, allows ref struct
            => Internal_ReplaceInterfaceCalls(self, replacer, []);

        public void ReplaceInterfaceCalls<TReplacer>(TReplacer replacer, List<Instruction> pooledList)
        where TReplacer : ICallReplacer, allows ref struct
            => Internal_ReplaceInterfaceCalls(self, replacer, pooledList);

    }

    private static void Internal_ReplaceInterfaceCalls<TReplacer>(Method self, TReplacer replacer, List<Instruction> pooledList)
    where TReplacer : ICallReplacer, allows ref struct
    {
        for (int i = 1; i < self.Instructions.Count; i++)
        {
            var window = self.Instructions.AsSpan().Slice(i - 1, 2);

            ref readonly var current = ref window[1];
            ref readonly var prev = ref window[0];

            // Match:
            // constrained. <Type>
            // call(virt) <Method>
            if ((ushort)current.OpCode.Value is not OpByteCodes.CALLVIRT
                and not OpByteCodes.CALL
                || (ushort)prev.OpCode.Value != OpByteCodes.CONSTRAINED)
                continue;

            var type = (Type)prev._operand!;
            var method = (MethodInfo)current._operand!;

            pooledList.Clear();
            if (replacer.ReplaceCall(self, type, method, pooledList))
            {
                i--; // Go to constrained prefix
                self.Instructions.InsertRange(i, 2, pooledList);
                i += pooledList.Count;
            }
        }
    }

    private readonly struct CallReplacer(StaticCallReplacerFunc func) : ICallReplacer
    {

        private readonly StaticCallReplacerFunc _func = func;

        bool ICallReplacer.ReplaceCall(Method method, Type callType, MethodInfo callMethod, List<Instruction> result)
            => _func(method, callType, callMethod, result);

        public static implicit operator CallReplacer(StaticCallReplacerFunc func) => new(func);

    }

}

public interface ICallReplacer
{

    bool ReplaceCall(Method method, Type callType, MethodInfo callMethod, List<Instruction> result);

}
