using System.Collections.Immutable;
using System.Collections.ObjectModel;

namespace Weberknecht.Test;

[TestClass]
public sealed class InsertInstructionTests
{

    private static Method CreateTestMethod()
    {
        Method method = new(typeof(void));
        method.Instructions.AddRange(
            Instruction.Load(0),
            Instruction.Load(1),
            Instruction.Load(2),
            Instruction.Load(3)
        );
        return method;
    }

    [TestMethod]
    public void TestCreateTestMethod()
    {
        Method method = CreateTestMethod();
        CollectionAssert.AreEqual((Instruction[])[
            Instruction.Load(0),
            Instruction.Load(1),
            Instruction.Load(2),
            Instruction.Load(3),
        ], method.Instructions);
    }

    [TestMethod]
    public void TestInsertSpan()
    {
        Method method = CreateTestMethod();

        method.Instructions.InsertRange(1, 0,
            Instruction.LoadArgument(0),
            Instruction.LoadArgument(1)
        );

        CollectionAssert.AreEqual((Instruction[])[
            Instruction.Load(0),
            Instruction.LoadArgument(0),
            Instruction.LoadArgument(1),
            Instruction.Load(1),
            Instruction.Load(2),
            Instruction.Load(3),
        ], method.Instructions);
    }

    [TestMethod]
    public void TestInsertList() => TestInsertCollection<List<Instruction>>([
        Instruction.LoadArgument(0),
        Instruction.LoadArgument(1),
    ]);

    [TestMethod]
    public void TestInsertImmutableArray() => TestInsertCollection<ImmutableArray<Instruction>>([
        Instruction.LoadArgument(0),
        Instruction.LoadArgument(1),
    ]);

    [TestMethod]
    public void TestInsertCollection() => TestInsertCollection<ReadOnlyCollection<Instruction>>([
        Instruction.LoadArgument(0),
        Instruction.LoadArgument(1),
    ]);

    private static void TestInsertCollection<TInstructions>(TInstructions insert)
    where TInstructions : IReadOnlyCollection<Instruction>, allows ref struct
    {
        Method method = CreateTestMethod();

        method.Instructions.InsertRange(1, 0, insert);

        CollectionAssert.AreEqual((Instruction[])[
            Instruction.Load(0),
            Instruction.LoadArgument(0),
            Instruction.LoadArgument(1),
            Instruction.Load(1),
            Instruction.Load(2),
            Instruction.Load(3),
        ], method.Instructions);
    }

    [TestMethod]
    public void TestReplaceSpan()
    {
        Method method = CreateTestMethod();

        method.Instructions.InsertRange(1, 1,
            Instruction.LoadArgument(0),
            Instruction.LoadArgument(1)
        );

        CollectionAssert.AreEqual((Instruction[])[
            Instruction.Load(0),
            Instruction.LoadArgument(0),
            Instruction.LoadArgument(1),
            Instruction.Load(2),
            Instruction.Load(3),
        ], method.Instructions);
    }

    [TestMethod]
    public void TestReplaceList() => TestReplaceCollection<List<Instruction>>([
        Instruction.LoadArgument(0),
        Instruction.LoadArgument(1),
    ]);

    [TestMethod]
    public void TestReplaceImmutableArray() => TestReplaceCollection<ImmutableArray<Instruction>>([
        Instruction.LoadArgument(0),
        Instruction.LoadArgument(1),
    ]);

    [TestMethod]
    public void TestReplaceCollection() => TestReplaceCollection<ReadOnlyCollection<Instruction>>([
        Instruction.LoadArgument(0),
        Instruction.LoadArgument(1),
    ]);

    private static void TestReplaceCollection<TInstructions>(TInstructions insert)
    where TInstructions : IReadOnlyCollection<Instruction>, allows ref struct
    {
        Method method = CreateTestMethod();

        method.Instructions.InsertRange(1, 1, insert);

        CollectionAssert.AreEqual((Instruction[])[
            Instruction.Load(0),
            Instruction.LoadArgument(0),
            Instruction.LoadArgument(1),
            Instruction.Load(2),
            Instruction.Load(3),
        ], method.Instructions);
    }

}
