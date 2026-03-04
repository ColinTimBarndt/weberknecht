using System.Reflection;

namespace Weberknecht;

using StaticCallReplacerFunc = Func<Method, Type, MethodInfo, List<Instruction>, bool>;

public static class InstructionUtil
{

    extension(Method self)
    {

        public void ReplaceStaticInterfaceCalls(StaticCallReplacer replacer)
            => self.ReplaceStaticInterfaceCalls(replacer, []);

        public void ReplaceStaticInterfaceCalls(StaticCallReplacer replacer, List<Instruction> pooledList)
            => self.ReplaceStaticInterfaceCalls<StaticCallReplacer>(replacer, pooledList);

        public void ReplaceStaticInterfaceCalls<TReplacer>(TReplacer replacer)
        where TReplacer : IStaticCallReplacer, allows ref struct
            => self.ReplaceStaticInterfaceCalls(replacer, []);

        public void ReplaceStaticInterfaceCalls<TReplacer>(TReplacer replacer, List<Instruction> pooledList)
        where TReplacer : IStaticCallReplacer, allows ref struct
        {
            Type? currentType = null;
            for (int i = 0; i < self.Instructions.Count; i++)
            {
                var current = self.Instructions[i];
                switch ((ushort)current.OpCode.Value)
                {
                    case OpByteCodes.CONSTRAINED:
                        currentType = (Type)current._operand!;
                        continue;

                    case OpByteCodes.CALL when currentType is not null:
                        var method = (MethodInfo)current._operand!;
                        pooledList.Clear();
                        if (replacer.ReplaceCall(self, currentType, method, pooledList))
                        {
                            i--; // Go to constrained prefix
                            self.Instructions.InsertRange(i, 2, pooledList);
                            i += pooledList.Count;
                        }
                        continue;

                    default:
                        currentType = null;
                        continue;
                }
            }
        }

    }

}

public interface IStaticCallReplacer
{

    bool ReplaceCall(Method method, Type callType, MethodInfo callMethod, List<Instruction> result);

}

public readonly struct StaticCallReplacer(StaticCallReplacerFunc func) : IStaticCallReplacer
{

    private readonly StaticCallReplacerFunc _func = func;

    bool IStaticCallReplacer.ReplaceCall(Method method, Type callType, MethodInfo callMethod, List<Instruction> result)
        => _func(method, callType, callMethod, result);

    public static implicit operator StaticCallReplacer(StaticCallReplacerFunc func) => new(func);

}
