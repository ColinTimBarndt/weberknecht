using System.Reflection;

namespace Weberknecht;

using ReturnReplacerFunc = Func<Method, List<Instruction>, bool>;
using ConstrainedCallReplacerFunc = Func<Method, Type, MethodInfo, List<Instruction>, bool>;
using CallReplacerFunc = Func<Method, MethodInfo, List<Instruction>, bool>;
using FieldAccessReplacerFunc = Func<Method, FieldInfo, FieldInfo?>;

public static class InstructionUtil
{

    extension(Method self)
    {

        // Returns

        public void ReplaceReturns(ReturnReplacerFunc replacer)
            => Internal_ReplaceReturns<ReturnReplacer>(self, replacer, []);

        public void ReplaceReturns(ReturnReplacerFunc replacer, List<Instruction> pooledList)
            => Internal_ReplaceReturns<ReturnReplacer>(self, replacer, pooledList);

        public void ReplaceReturns<TReplacer>(TReplacer replacer)
        where TReplacer : IReturnReplacer, allows ref struct
            => Internal_ReplaceReturns(self, replacer, []);

        public void ReplaceReturns<TReplacer>(TReplacer replacer, List<Instruction> pooledList)
        where TReplacer : IReturnReplacer, allows ref struct
            => Internal_ReplaceReturns(self, replacer, pooledList);

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

        // Field access

        public void ReplaceFieldAccess<TReplacer>(TReplacer replacer)
        where TReplacer : IFieldAccessReplacer, allows ref struct
        => Internal_ReplaceFieldAccess(self, replacer, []);

        public void ReplaceFieldAccess<TReplacer>(TReplacer replacer, List<Instruction> pooledList)
        where TReplacer : IFieldAccessReplacer, allows ref struct
            => Internal_ReplaceFieldAccess(self, replacer, pooledList);

        public void ReplaceFieldAccessSimple(FieldAccessReplacerFunc replacer)
            => Internal_ReplaceFieldAccess<FieldAccessReplacer<SimpleFieldAccessReplacer>>(self, new(replacer), []);

        public void ReplaceFieldAccessSimple(FieldAccessReplacerFunc replacer, List<Instruction> pooledList)
            => Internal_ReplaceFieldAccess<FieldAccessReplacer<SimpleFieldAccessReplacer>>(self, new(replacer), pooledList);

        public void ReplaceFieldAccessSimple<TReplacer>(TReplacer replacer)
        where TReplacer : ISimpleFieldAccessReplacer, allows ref struct
            => Internal_ReplaceFieldAccess<FieldAccessReplacer<TReplacer>>(self, new(replacer), []);

        public void ReplaceFieldAccessSimple<TReplacer>(TReplacer replacer, List<Instruction> pooledList)
        where TReplacer : ISimpleFieldAccessReplacer, allows ref struct
            => Internal_ReplaceFieldAccess<FieldAccessReplacer<TReplacer>>(self, new(replacer), pooledList);

    }

    private static void Internal_ReplaceReturns<TReplacer>(Method self, TReplacer replacer, List<Instruction> pooledList)
    where TReplacer : IReturnReplacer, allows ref struct
    {
        int i = 0;
        while (i < self.Instructions.Count)
        {
            ref readonly var current = ref self.Instructions.AsSpan()[i];

            bool replace;
            switch ((ushort)current.OpCode.Value)
            {
                case OpByteCodes.RET:
                    pooledList.Clear();
                    replace = replacer.ReplaceReturn(self, pooledList);
                    break;

                case OpByteCodes.JMP:
                    var target = (MethodInfo)current._operand!;
                    pooledList.Clear();
                    replace = replacer.ReplaceJump(self, target, pooledList);
                    break;

                default:
                    i++;
                    continue;
            }

            if (replace)
            {
                self.Instructions.InsertRange(i, 1, pooledList);
                i += pooledList.Count;
                continue;
            }

            i++;
        }
    }

    private readonly struct ReturnReplacer(ReturnReplacerFunc func) : IReturnReplacer
    {

        private readonly ReturnReplacerFunc _func = func;

        public bool ReplaceReturn(Method method, List<Instruction> result)
            => _func(method, result);

        public bool ReplaceJump(Method method, MethodInfo targetMethod, List<Instruction> result)
            => throw new NotSupportedException("JMP instructions are not supported");

        public static implicit operator ReturnReplacer(ReturnReplacerFunc func) => new(func);

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

    private readonly struct CallReplacer(CallReplacerFunc func) : ICallReplacer
    {

        private readonly CallReplacerFunc _func = func;

        bool ICallReplacer.ReplaceCall(Method method, MethodInfo callMethod, List<Instruction> result)
            => _func(method, callMethod, result);

        public static implicit operator CallReplacer(CallReplacerFunc func) => new(func);

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

    private readonly struct ConstrainedCallReplacer(ConstrainedCallReplacerFunc func) : IConstrainedCallReplacer
    {

        private readonly ConstrainedCallReplacerFunc _func = func;

        bool IConstrainedCallReplacer.ReplaceCall(Method method, Type callType, MethodInfo callMethod, List<Instruction> result)
            => _func(method, callType, callMethod, result);

        public static implicit operator ConstrainedCallReplacer(ConstrainedCallReplacerFunc func) => new(func);

    }

    private static bool IsFieldAccess(ushort opCode)
        => opCode is OpByteCodes.LDFLD
            or OpByteCodes.LDFLDA
            or OpByteCodes.LDSFLD
            or OpByteCodes.LDSFLDA
            or OpByteCodes.STFLD
            or OpByteCodes.STSFLD;

    private static void Internal_ReplaceFieldAccess<TReplacer>(Method self, TReplacer replacer, List<Instruction> pooledList)
    where TReplacer : IFieldAccessReplacer, allows ref struct
    {
        int i = 0;
        while (i < self.Instructions.Count)
        {
            ref readonly var current = ref self.Instructions.AsSpan()[i];

            var opCode = (ushort)current.OpCode.Value;
            if (!IsFieldAccess(opCode))
            {
                i++;
                continue;
            }

            var field = (FieldInfo)current._operand!;

            pooledList.Clear();
            bool shouldReplace = opCode switch
            {
                OpByteCodes.LDFLD or OpByteCodes.LDSFLD
                    => replacer.ReplaceLoad(self, field, pooledList),
                OpByteCodes.LDFLDA or OpByteCodes.LDSFLDA
                    => replacer.ReplaceLoadAddress(self, field, pooledList),
                OpByteCodes.STFLD or OpByteCodes.STSFLD
                    => replacer.ReplaceStore(self, field, pooledList),
                _ => false,
            };
            if (shouldReplace)
            {
                self.Instructions.InsertRange(i, 1, pooledList);
                i += pooledList.Count;
                continue;
            }

            i++;
        }
    }

    private readonly ref struct FieldAccessReplacer<TReplacer>(TReplacer replacer) : IFieldAccessReplacer
    where TReplacer : ISimpleFieldAccessReplacer, allows ref struct
    {

        private readonly TReplacer _replacer = replacer;

        bool IFieldAccessReplacer.ReplaceLoad(Method method, FieldInfo field, List<Instruction> result)
        {
            var replacement = _replacer.ReplaceField(method, field);
            if (replacement is null)
                return false;
            result.Add(Instruction.LoadField(replacement));
            return true;
        }

        bool IFieldAccessReplacer.ReplaceLoadAddress(Method method, FieldInfo field, List<Instruction> result)
        {
            var replacement = _replacer.ReplaceField(method, field);
            if (replacement is null)
                return false;
            result.Add(Instruction.LoadFieldAddress(replacement));
            return true;
        }

        bool IFieldAccessReplacer.ReplaceStore(Method method, FieldInfo field, List<Instruction> result)
        {
            var replacement = _replacer.ReplaceField(method, field);
            if (replacement is null)
                return false;
            result.Add(Instruction.StoreField(replacement));
            return true;
        }

    }

    private readonly struct SimpleFieldAccessReplacer(FieldAccessReplacerFunc func) : ISimpleFieldAccessReplacer
    {
        private readonly FieldAccessReplacerFunc _func = func;

        FieldInfo? ISimpleFieldAccessReplacer.ReplaceField(Method method, FieldInfo field)
            => _func(method, field);

        public static implicit operator SimpleFieldAccessReplacer(FieldAccessReplacerFunc func) => new(func);
    }

}

public interface IReturnReplacer
{

    bool ReplaceReturn(Method method, List<Instruction> result);

    bool ReplaceJump(Method method, MethodInfo targetMethod, List<Instruction> result);

}

public interface ICallReplacer
{

    bool ReplaceCall(Method method, MethodInfo callMethod, List<Instruction> result);

}

public interface IConstrainedCallReplacer
{

    bool ReplaceCall(Method method, Type callType, MethodInfo callMethod, List<Instruction> result);

}

public interface IFieldAccessReplacer
{

    bool ReplaceLoad(Method method, FieldInfo field, List<Instruction> result);

    bool ReplaceLoadAddress(Method method, FieldInfo field, List<Instruction> result);

    bool ReplaceStore(Method method, FieldInfo field, List<Instruction> result);

}

public interface ISimpleFieldAccessReplacer
{

    FieldInfo? ReplaceField(Method method, FieldInfo field);

}
