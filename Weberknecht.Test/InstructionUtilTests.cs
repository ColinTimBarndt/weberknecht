namespace Weberknecht.Test;

[TestClass]
public sealed class InstructionUtilTests
{

    [TestMethod]
    public void ReplaceReturns()
    {
        var method = Method.Read((int a, int b) => a > 5 ? a + b : a * b);

        method.ReplaceReturns((_, result) =>
        {
            result.AddRange(
                Instruction.Load(42),
                Instruction.Add(),
                Instruction.Return()
            );
            return true;
        });

        var dynMethod = method.CreateDynamicMethod("MulAdd42");
        var specialMul = dynMethod.CreateDelegate<Func<int, int, int>>();
        Assert.AreEqual(50, specialMul(4, 2)); // 4 * 2 + 42
        Assert.AreEqual(50, specialMul(6, 2)); // 6 + 2 + 42
        Assert.AreEqual(42, specialMul(0, -1));
    }

    [TestMethod]
    public void ReplaceCalls()
    {
        var method = Method.Read((int x, int y) => 5 + Add(x, 6) - Add(2, y));

        var add = ((Delegate)Add).Method;

        method.ReplaceCalls((_, method, result) =>
        {
            if (method != add)
                return false;

            result.Add(Instruction.Call(Sub));

            return true;
        });

        // (x, y) => 5 + (x-6) - (2-y)
        var dynMethod = method.CreateDynamicMethod("SpecialSub");
        var specialSub = dynMethod.CreateDelegate<Func<int, int, int>>();
        Assert.AreEqual(42, specialSub(12, 33));
    }

    private static int Add(int a, int b) => a + b;

    private static int Sub(int a, int b) => a - b;

    [TestMethod]
    public void ReplaceInterfaceCalls()
    {
        var doSomething = typeof(TestClass<>).GetMethod("DoSomething");
        Assert.IsNotNull(doSomething);

        var testClassArgs = typeof(TestClass<>).GetGenericArguments();
        Assert.HasCount(1, testClassArgs);
        var typeT = testClassArgs[0];

        var operation = typeof(IOperation).GetMethod("Operation");
        Assert.IsNotNull(operation);

        var method = Method.Read(doSomething);
        method.ReplaceInterfaceCalls((_, type, method, result) =>
        {
            if (type != typeT || method != operation)
                return false;

            result.Add(Instruction.Call(MyOperation));

            return true;
        });
        var dynMethod = method.CreateDynamicMethod("DoMyThing");
        var doMyThing = dynMethod.CreateDelegate<Func<int>>();
        Assert.AreEqual(42, doMyThing());
    }

    private static int MyOperation(int a, int b) => (a - 3) * b;

    private interface IOperation
    {

        static abstract int Operation(int a, int b);

    }

    private sealed class TestClass<T> where T : IOperation
    {

        public static int DoSomething()
        {
            int a = 5;
            int b = 7;
            return 3 * T.Operation(a, b);
        }

    }

    [TestMethod]
    public void ReplaceFieldAccess()
    {
        var getInt = typeof(TestClass).GetMethod(nameof(TestClass.GetInt));
        Assert.IsNotNull(getInt);

        var method = Method.Read(getInt);

        var x = typeof(TestClass).GetField(nameof(TestClass.x));
        var z = typeof(TestClass).GetField(nameof(TestClass.z));

        method.ReplaceFieldAccessSimple((_, field) => field == x ? z : null);

        var dynMethod = method.CreateDynamicMethod("GetMyInt");
        var getMyInt = dynMethod.CreateDelegate<Func<int>>(new TestClass());
        Assert.AreEqual(5, getMyInt());
    }

    private sealed class TestClass()
    {

        public static readonly int x = 1;
        public readonly int y = 2;
        public static readonly int z = 3;

        public int GetInt() => x + y;

    }

}
