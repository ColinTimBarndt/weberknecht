using System.Collections;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Weberknecht;

public partial class Method
{

    public readonly partial struct InstructionCollection : IList<Instruction>, IReadOnlyList<Instruction>, ICollection
    {

        public Method Method { get; }

        public ReadOnlySpan<Instruction> AsSpan() => CollectionsMarshal.AsSpan(Method._instructions);

        public int Count => Method._instructions.Count;

        public bool IsReadOnly => false;

        bool ICollection.IsSynchronized => false;

        object ICollection.SyncRoot => Method;

        public Instruction this[int index]
        {
            get => Method._instructions[index];

            set
            {
                AssertValidLabel(in value);
                Method._instructions[index] = value;
            }
        }

        internal InstructionCollection(Method method)
        {
            Method = method;
        }

        public Label GetLabel(int index)
        {
            ref var label = ref CollectionsMarshal.AsSpan(Method._instructions)[index]._label;
            return label.IsNull ? label = new(++Method.LabelCount) : label;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssertValidLabel(in Instruction instr)
            => AssertValidLabel(in instr, Method.LabelCount);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AssertValidLabel(in Instruction instr, int labelCount)
        {
            if (instr.Label.Id > labelCount)
                throw new InvalidOperationException($"Undefined label {instr.Label}");
        }

        public int IndexOf(Instruction item)
        {
            var instrs = AsSpan();
            for (int i = 0; i < instrs.Length; i++)
            {
                if (default(Instruction.LabelIgnoringComparer).Equals(item, instrs[i]))
                    return i;
            }
            return -1;
        }

        public void Insert(int index, Instruction item)
        {
            AssertValidLabel(in item);
            Method._instructions.Insert(index, item);
        }

        public void RemoveAt(int index) => Method._instructions.RemoveAt(index);

        public void Add(Instruction item)
        {
            AssertValidLabel(in item);
            Method._instructions.Add(item);
        }

        public void Clear() => Method._instructions.Clear();

        public bool Contains(Instruction item) => IndexOf(item) != -1;

        public void CopyTo(Instruction[] array, int arrayIndex)
            => Method._instructions.CopyTo(array, arrayIndex);

        public bool Remove(Instruction item)
            => Method._instructions.Remove(item);

        public List<Instruction>.Enumerator GetEnumerator()
            => Method._instructions.GetEnumerator();

        IEnumerator<Instruction> IEnumerable<Instruction>.GetEnumerator() => GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void ICollection.CopyTo(Array array, int index)
            => ((ICollection)Method._instructions).CopyTo(array, index);
    }

}
