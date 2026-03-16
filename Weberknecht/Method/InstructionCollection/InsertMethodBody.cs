using System.Reflection.Emit;

namespace Weberknecht;

public partial class Method
{

    public readonly partial struct InstructionCollection
    {
        
        public void InsertMethodBody(int index, int replaceLength, Method method, LabelMap<Label> labels)
        {
            var methodInstrs = method.Instructions.AsSpan();
            InsertRangeUnchecked(index, replaceLength, methodInstrs);

            var inserted = AsMutableSpan().Slice(index, methodInstrs.Length);
            // Iterate over all added instructions
            foreach (ref var instr in inserted)
            {
                ref var selfLabel = ref instr._label;
                if (!selfLabel.IsNull)
                {
                    var newLabel = Method.CreateLabel();
                    labels[selfLabel] = newLabel;
                    selfLabel = newLabel;
                }
            }

            foreach (ref var instr in inserted)
            {
                switch (instr.OpCode.OperandType)
                {
                    case OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget:
                        ref var label = ref instr._uoperand.label;
                        label = labels[label];
                        break;

                    case OperandType.InlineSwitch:
                        break;
                }
                continue;
            }
        }

    }

}
