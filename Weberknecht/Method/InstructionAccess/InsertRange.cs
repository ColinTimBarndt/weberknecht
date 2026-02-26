using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Weberknecht;

public partial class Method
{

    public readonly partial struct InstructionAccess
    {

        public void InsertRange(int index, ReadOnlySpan<Instruction> items)
            => Internal_InsertRange<ValidationCallback>(index, items);

        public void InsertRange<T>(int index, T items)
        where T : IReadOnlyCollection<Instruction>, allows ref struct
            => Internal_InsertRange<T, ValidationCallback>(index, items);

        private void InsertRangeUnchecked(int index, ReadOnlySpan<Instruction> items)
            => Internal_InsertRange<NoValidationCallback>(index, items);

        private interface IValidationCallback
        {
            static abstract void Validate(in Instruction instr, int labelCount);
        }

        private readonly struct NoValidationCallback() : IValidationCallback
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Validate(in Instruction instr, int labelCount) { }
        }

        private readonly struct ValidationCallback() : IValidationCallback
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Validate(in Instruction instr, int labelCount)
                => AssertValidLabel(in instr, labelCount);
        }

        private void Internal_InsertRange<TValidate>(int index, ReadOnlySpan<Instruction> items)
        where TValidate : IValidationCallback
        {
            var instrs = Method._instructions;
            if (index < 0 || index > instrs.Count)
                throw new IndexOutOfRangeException();

            // Reserve additional memory
            var newCount = instrs.Count + items.Length;
            instrs.EnsureCapacity(newCount);
            CollectionsMarshal.SetCount(instrs, newCount);

            // Make room for new items
            var movedCount = instrs.Count - index;
            var span = CollectionsMarshal.AsSpan(instrs);
            span.Slice(index, movedCount).CopyTo(span[(index + movedCount)..]);

            int labelCount = Method.LabelCount;
            for (int i = 0; i < items.Length; i++)
                TValidate.Validate(in items[i], labelCount);

            items.CopyTo(span.Slice(index, items.Length));
        }

        private void Internal_InsertRange<T, TValidate>(int index, T items)
        where T : IReadOnlyCollection<Instruction>, allows ref struct
        where TValidate : IValidationCallback
        {
            var instrs = Method._instructions;
            if (index < 0 || index > instrs.Count)
                throw new IndexOutOfRangeException();

            // Reserve additional memory
            var count = items.Count;
            var newCount = instrs.Count + count;
            instrs.EnsureCapacity(newCount);
            CollectionsMarshal.SetCount(instrs, newCount);

            // Make room for new items
            var movedCount = instrs.Count - index;
            var span = CollectionsMarshal.AsSpan(instrs);
            span.Slice(index, movedCount).CopyTo(span[(index + movedCount)..]);

            var enumerator = items.GetEnumerator();
            int labelCount = Method.LabelCount;
            foreach (ref var slot in span.Slice(index, count))
            {
                enumerator.MoveNext();
                var value = enumerator.Current;
                TValidate.Validate(in value, labelCount);
                slot = value;
            }
        }

    }

}
