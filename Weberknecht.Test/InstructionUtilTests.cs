using System.Reflection;

namespace Weberknecht.Test;

[TestClass]
public sealed class InstructionUtilTests
{

    [TestMethod]
    public void ReplaceStaticInterfaceCalls()
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
