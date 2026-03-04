using System.Reflection;

namespace Weberknecht.Test;

[TestClass]
public sealed class InstructionUtilTests
{

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

}
