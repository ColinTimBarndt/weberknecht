using System.Runtime.InteropServices;

namespace Weberknecht;

public partial class Method
{

    public void InsertMethod(int index, int replaceLength, Method method)
    {
        LabelMap<Label> labels = stackalloc Label[method.LabelCount];
        InsertMethod(index, replaceLength, method, labels);
    }

    public void InsertMethod(int index, int replaceLength, Method method, LabelMap<Label> labels)
    {
        Instructions.InsertMethodBody(index, replaceLength, method, labels);

        if (method._localVariables.Count > 0)
        {
            int localOffset = _localVariables.Count;
            _localVariables.AddRange(CollectionsMarshal.AsSpan(method._localVariables));
            if (localOffset == 0)
                goto EndLocals;

            var inserted = CollectionsMarshal.AsSpan(_instructions).Slice(index, method.Instructions.Count);
            foreach (ref var instr in inserted)
            {
                int local;
                Label label = instr._label;
                switch ((ushort)instr.OpCode.Value)
                {
                    case OpByteCodes.LDLOC_0:
                    case OpByteCodes.LDLOC_1:
                    case OpByteCodes.LDLOC_2:
                    case OpByteCodes.LDLOC_3:
                        local = (ushort)instr.OpCode.Value - OpByteCodes.LDLOC_0;
                        goto Ldloc;

                    case OpByteCodes.LDLOC_S:
                        local = instr._uoperand.@byte;
                        goto Ldloc;

                    case OpByteCodes.LDLOC:
                        local = instr._uoperand.@ushort;
                    Ldloc:
                        instr = Instruction.LoadLocal((ushort)local);
                        break;

                    case OpByteCodes.STLOC_0:
                    case OpByteCodes.STLOC_1:
                    case OpByteCodes.STLOC_2:
                    case OpByteCodes.STLOC_3:
                        local = (ushort)instr.OpCode.Value - OpByteCodes.STLOC_0;
                        goto Stloc;

                    case OpByteCodes.STLOC_S:
                        local = instr._uoperand.@byte;
                        goto Stloc;

                    case OpByteCodes.STLOC:
                        local = instr._uoperand.@ushort;
                    Stloc:
                        instr = Instruction.StoreLocal((ushort)local);
                        break;

                    case OpByteCodes.LDLOCA_S:
                        local = instr._uoperand.@byte;
                        goto Ldloca;

                    case OpByteCodes.LDLOCA:
                        local = instr._uoperand.@ushort;
                    Ldloca:
                        instr = Instruction.LoadLocalAddress((ushort)local);
                        break;

                    default:
                        continue;
                }
                instr._label = label;
            }
        }
    EndLocals:

        var exHandlers = CollectionsMarshal.AsSpan(method._exceptionHandlers);
        if (exHandlers.Length > 0)
        {
            _exceptionHandlers ??= [];
            var myExHandlers = _exceptionHandlers;
            myExHandlers.EnsureCapacity(exHandlers.Length);
            foreach (var clause in exHandlers)
                myExHandlers.Add(clause.Map(labels));
        }
    }

}
