using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Weberknecht;

public partial class Method
{

    public readonly partial struct InstructionCollection
    {

        public void AddRange(params ReadOnlySpan<Instruction> items)
            => Internal_InsertRange(Count, 0, items);

        public void AddRange(List<Instruction> items)
            => Internal_InsertRange(Count, 0, CollectionsMarshal.AsSpan(items));

        public void AddRange<T>(T items)
        where T : IReadOnlyCollection<Instruction>, allows ref struct
            => Internal_InsertRange(Count, 0, items);

        public void RemoveRange(int index, int count)
            => Internal_InsertRange(index, count, []);

        public void InsertRange(int index, int replaceLength, params ReadOnlySpan<Instruction> items)
            => Internal_InsertRange(index, replaceLength, items);

        public void InsertRange(int index, int replaceLength, List<Instruction> items)
            => Internal_InsertRange(index, replaceLength, CollectionsMarshal.AsSpan(items));

        public void InsertRange<T>(int index, int replaceLength, T items)
        where T : IReadOnlyCollection<Instruction>, allows ref struct
            => Internal_InsertRange(index, replaceLength, items);

        private void InsertRangeUnchecked(int index, int replaceLength, params ReadOnlySpan<Instruction> items)
            => Internal_InsertRange(index, replaceLength, items, validate: false);

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

        private static void CheckBounds(int instrCount, int index, int replaceLength)
        {
            if (index < 0 || index > instrCount)
                throw new ArgumentException("Index out of bounds", nameof(index));

            if (replaceLength < 0 || index + replaceLength > instrCount)
                throw new ArgumentException("Replaced length out of bounds", nameof(replaceLength));
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Internal_InsertRange(int index, int replaceLength, ReadOnlySpan<Instruction> items, bool validate = true)
        {
            var instrs = Method._instructions;

            CheckBounds(instrs.Count, index, replaceLength);

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

            if (validate)
            {
                int labelCount = Method.LabelCount;
                for (int i = 0; i < items.Length; i++)
                    AssertValidLabel(in items[i], labelCount);
            }

            items.CopyTo(span.Slice(index, items.Length));
        }

        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        private void Internal_InsertRange<T>(int index, int replaceLength, T items, bool validate = true)
        where T : IReadOnlyCollection<Instruction>, allows ref struct
        {
            var instrs = Method._instructions;
            var insertCount = items.Count;

            CheckBounds(instrs.Count, index, replaceLength);

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
            if (validate)
            {
                int labelCount = Method.LabelCount;
                foreach (ref var slot in span.Slice(index, insertCount))
                {
                    enumerator.MoveNext();
                    var value = enumerator.Current;
                    AssertValidLabel(in value, labelCount);
                    slot = value;
                }
            }
            else
            {
                foreach (ref var slot in span.Slice(index, insertCount))
                {
                    enumerator.MoveNext();
                    slot = enumerator.Current;
                }
            }
        }

    }

}
