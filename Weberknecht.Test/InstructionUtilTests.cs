using System.Reflection;

namespace Weberknecht.Test;

[TestClass]
public sealed class InstructionUtilTests
{

    private readonly struct SimpleReplacer(Type type, MethodInfo method, MethodInfo replaceWith) : IStaticCallReplacer
    {

        private readonly Type _type = type;
        private readonly MethodInfo _method = method;
        private readonly MethodInfo _replaceWith = replaceWith;

        bool IStaticCallReplacer.ReplaceCall(Method _, Type type, MethodInfo method, List<Instruction> result)
        {
            if (type != _type || method != _method)
                return false;

            result.Add(Instruction.Call(_replaceWith));

            return true;
        }

        public SimpleReplacer(Type type, Delegate method, Delegate replaceWith)
            : this(type, method.Method, replaceWith.Method)
        { }

    }

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
        method.ReplaceStaticInterfaceCalls(new SimpleReplacer(
            typeT,
            operation,
            ((Delegate)MyOperation).Method
        ));
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
