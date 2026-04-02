using System.Reflection.Emit;

namespace Weberknecht.Test;

[TestClass]
public sealed class InsertMethodTests
{

    [TestMethod]
    public void CombineMethod()
    {
        var add = Method.Read(Add);
        var next = Method.Read(Next);

        var myMethod = Method.Read(static (ref Context ctx) => { });
        int last = myMethod.Instructions.Count - 1;
        myMethod.InsertMethod(last, 1, add);

        last = myMethod.Instructions.Count - 1;
        myMethod.InsertMethod(last, 1, next);

        Assert.IsFalse(myMethod.Instructions.Any(i => IsCall(i.OpCode)), "no method calls");

        var dynMethod = myMethod.CreateDynamicMethod("AddPlusOne");
        var addPlusOne = dynMethod.CreateDelegate<ContextAction>();

        Context ctx = new()
        {
            a = 2,
            b = 6,
        };
        addPlusOne(ref ctx);
        Assert.AreEqual(9, ctx.a);
    }

    [TestMethod]
    public void CombineMethodWithExceptions()
    {
        var add = Method.Read(Add);
        var next = Method.Read(Next);
        var mayThrow = Method.Read(MayThrow);

        var myMethod = Method.Read(static (ref Context ctx) => { });
        int last = myMethod.Instructions.Count - 1;
        myMethod.InsertMethod(last, 1, add);

        last = myMethod.Instructions.Count - 1;
        myMethod.InsertMethod(last, 1, next);

        last = myMethod.Instructions.Count - 1;
        myMethod.InsertMethod(last, 1, mayThrow);

        Assert.IsFalse(myMethod.Instructions.Any(i => IsCall(i.OpCode)), "no method calls");

        var dynMethod = myMethod.CreateDynamicMethod("AddPlusOneEx");
        var addPlusOne = dynMethod.CreateDelegate<ContextAction>();

        {
            Context ctx = new()
            {
                a = 2,
                b = 6,
            };
            addPlusOne(ref ctx);
            Assert.AreEqual(9, ctx.a);
        }

        {
            Context ctx = new()
            {
                a = 2,
                b = 12,
            };
            addPlusOne(ref ctx);
            Assert.AreEqual(-15, ctx.a);
        }
    }

    private static bool IsCall(OpCode op)
        => op == OpCodes.Call || op == OpCodes.Callvirt || op == OpCodes.Calli;

    private delegate void ContextAction(ref Context ctx);

    private struct Context
    {
        public int a;
        public int b;
    }

    private void Add(ref Context ctx) => ctx.a += ctx.b;

    private void Next(ref Context ctx) => ctx.a += 1;

    private void MayThrow(ref Context ctx)
    {
        try
        {
            if (ctx.a > 10)
                throw new TestException(-1);
        }
        catch (TestException e)
        {
            ctx.a *= e.value;
        }
    }

    private class TestException(int value) : Exception
    {
        public int value = value;
    }

}
