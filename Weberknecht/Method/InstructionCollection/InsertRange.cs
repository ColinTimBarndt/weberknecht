using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Weberknecht;

public partial class Method
{

    public readonly partial struct InstructionCollection
    {

        public void AddRange(params ReadOnlySpan<Instruction> items)
            => Internal_InsertRange<ValidationCallback>(Count, 0, items);

        public void AddRange(List<Instruction> items)
            => Internal_InsertRange<ValidationCallback>(Count, 0, CollectionsMarshal.AsSpan(items));

        public void AddRange<T>(T items)
        where T : IReadOnlyCollection<Instruction>, allows ref struct
            => Internal_InsertRange<T, ValidationCallback>(Count, 0, items);

        public void RemoveRange(int index, int count)
            => Internal_InsertRange<ValidationCallback>(index, count, []);

        public void InsertRange(int index, int replaceLength, params ReadOnlySpan<Instruction> items)
            => Internal_InsertRange<ValidationCallback>(index, replaceLength, items);

        public void InsertRange(int index, int replaceLength, List<Instruction> items)
            => Internal_InsertRange<ValidationCallback>(index, replaceLength, CollectionsMarshal.AsSpan(items));

        public void InsertRange<T>(int index, int replaceLength, T items)
        where T : IReadOnlyCollection<Instruction>, allows ref struct
            => Internal_InsertRange<T, ValidationCallback>(index, replaceLength, items);

        private void InsertRangeUnchecked(int index, int replaceLength, params ReadOnlySpan<Instruction> items)
            => Internal_InsertRange<NoValidationCallback>(index, replaceLength, items);

        private interface IValidationCallback
        {
            static abstract void Validate(in Instruction instr, int labelCount);

            static abstract void CheckBounds(List<Instruction> instrs, int index, int replaceLength, int insertCount);
        }

        private readonly struct NoValidationCallback() : IValidationCallback
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Validate(in Instruction instr, int labelCount) { }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void CheckBounds(List<Instruction> instrs, int index, int replaceLength, int insertCount) { }
        }

        private readonly struct ValidationCallback() : IValidationCallback
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Validate(in Instruction instr, int labelCount)
                => AssertValidLabel(in instr, labelCount);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void CheckBounds(List<Instruction> instrs, int index, int replaceLength, int insertCount)
            {
                if (index < 0 || index > instrs.Count)
                    throw new ArgumentException("Index out of bounds", nameof(index));

                if (replaceLength < 0 || index + replaceLength > instrs.Count)
                    throw new ArgumentException("Replaced length out of bounds", nameof(replaceLength));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Internal_InsertRange<TValidate>(int index, int replaceLength, ReadOnlySpan<Instruction> items)
        where TValidate : IValidationCallback
        {
            var instrs = Method._instructions;
            TValidate.CheckBounds(instrs, index, replaceLength, items.Length);

            // Reserve additional memory
            var oldCount = instrs.Count;
            var newCount = oldCount - replaceLength + items.Length;
            instrs.EnsureCapacity(newCount);
            bool grow = newCount >= oldCount;
            if (grow)
                CollectionsMarshal.SetCount(instrs, newCount);

            // Make room for new items
            var moveStart = index + replaceLength;
            var moveCount = oldCount - moveStart;
            var span = CollectionsMarshal.AsSpan(instrs);
            span.Slice(moveStart, moveCount).CopyTo(span[(index + items.Length)..]);
            if (!grow)
                CollectionsMarshal.SetCount(instrs, newCount);

            int labelCount = Method.LabelCount;
            for (int i = 0; i < items.Length; i++)
                TValidate.Validate(in items[i], labelCount);

            items.CopyTo(span.Slice(index, items.Length));
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Internal_InsertRange<T, TValidate>(int index, int replaceLength, T items)
        where T : IReadOnlyCollection<Instruction>, allows ref struct
        where TValidate : IValidationCallback
        {
            var instrs = Method._instructions;
            var insertCount = items.Count;
            TValidate.CheckBounds(instrs, index, replaceLength, insertCount);

            // Reserve additional memory
            var oldCount = instrs.Count;
            var newCount = oldCount - replaceLength + insertCount;
            instrs.EnsureCapacity(newCount);
            bool grow = newCount >= oldCount;
            if (grow)
                CollectionsMarshal.SetCount(instrs, newCount);

            // Make room for new items
            var moveStart = index + replaceLength;
            var moveCount = oldCount - moveStart;
            var span = CollectionsMarshal.AsSpan(instrs);
            span.Slice(moveStart, moveCount).CopyTo(span[(index + insertCount)..]);
            if (!grow)
                CollectionsMarshal.SetCount(instrs, newCount);

            var enumerator = items.GetEnumerator();
            int labelCount = Method.LabelCount;
            foreach (ref var slot in span.Slice(index, insertCount))
            {
                enumerator.MoveNext();
                var value = enumerator.Current;
                TValidate.Validate(in value, labelCount);
                slot = value;
            }
        }

    }

}
