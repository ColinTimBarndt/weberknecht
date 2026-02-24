using System.Buffers.Binary;
using System.Reflection.Emit;
using Weberknecht.Metadata;

namespace Weberknecht.Test;

[TestClass]
public sealed class DynamicMethodTests
{

    [TestMethod]
    public void TestAddInts()
    {
        var method = Method.Read(AddInts);
        CollectionAssert.AreEqual((PseudoInstruction[])[
            Instruction.LoadArgument(0),
            Instruction.LoadArgument(1),
            Instruction.Add(),
            Instruction.Return(),
        ], method.Instructions, "instructions match");

        LabelAddressMap labels = stackalloc int[method.LabelCount];
        byte[] result = method.EncodeBody(labels, TokenSource.CreateStable());
        new ILBodyBuilder()
            .Add(OpCodes.Ldarg_0)
            .Add(OpCodes.Ldarg_1)
            .Add(OpCodes.Add)
            .Add(OpCodes.Ret)
            .AssertEquals(result);
    }

    private static int AddInts(int a, int b) => a + b;

    [TestMethod]
    public void TestCallMethod()
    {
        var method = Method.Read(CallMethod);
        CollectionAssert.AreEqual((PseudoInstruction[])[
            Instruction.LoadArgument(0),
            Instruction.Load(10),
            Instruction.Call(AddInts),
            Instruction.Return(),
        ], method.Instructions, "instructions match");

        LabelAddressMap labels = stackalloc int[method.LabelCount];
        var tokens = TokenSource.CreateStable();
        byte[] result = method.EncodeBody(labels, tokens);

        new ILBodyBuilder()
            .Add(OpCodes.Ldarg_0)
            .Add(OpCodes.Ldc_I4_S, (byte)10)
            .Add(OpCodes.Call, AddInts)
            .Add(OpCodes.Ret)
            .AssertEquals(result);
    }

    private static int CallMethod(int a) => AddInts(a, 10);

    [TestMethod]
    public void TestHandleExceptions()
    {
        var method = Method.Read(HandleExceptions);

        LabelAddressMap labels = stackalloc int[method.LabelCount];
        var tokens = TokenSource.CreateStable();
        byte[] result = method.EncodeBody(labels, tokens);

        var expectedBody = ((Delegate)HandleExceptions).Method.GetMethodBody()!;
        var expected = expectedBody.GetILAsByteArray()!;

        Assert.HasCount(expected.Length, result, "IL body length matches");

        // Replace referenced metadata tokens. These should be the only difference
        Span<byte> exceptionB = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(exceptionB, tokens.GetToken(typeof(TestExceptionB)));

        Span<byte> exceptionBValue = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(exceptionBValue, tokens.GetToken(typeof(TestExceptionB).GetField("Value")!));

        int exceptionBIndex = result.IndexOf(exceptionB);
        Assert.IsGreaterThanOrEqualTo(0, exceptionBIndex, "TestExceptionB is referenced");
        exceptionB.CopyTo(expected.AsSpan(exceptionBIndex, 4));

        int exceptionBValueIndex = result.IndexOf(exceptionBValue);
        Assert.IsGreaterThanOrEqualTo(0, exceptionBValueIndex, "TestExceptionB::Value is referenced");
        exceptionBValue.CopyTo(expected.AsSpan(exceptionBValueIndex, 4));

        CollectionAssert.AreEqual(expected, result, "IL bytes match");

        Assert.AreEqual(expectedBody.MaxStackSize, method.GetMaxStackSize(), "stack size");

        var exceptionHandlers = ExceptionHandlingClause.EncodeExceptionHandlers(method.ExceptionHandlers, labels, tokens);

        byte[] expectedExceptionHandlers = [
            0x01, // Small table
            40, // Length
            0, 0,
            // [4] Catch
            0, 0,
            0x03, 0x00,
            0x07,
            0x0a, 0x00,
            0x06,
            0, 0, 0, 0, // Metadata token at [12]
            // [16] Filter
            1, 0,
            0x03, 0x00,
            0x07,
            0x2c, 0x00,
            0x06,
            0x10, 0x00, 0x00, 0x00, // Filter offset
            // [28] Finally
            2, 0,
            0x03, 0x00,
            0x2f,
            0x32, 0x00,
            0x06,
            0, 0, 0, 0,
        ];
        BinaryPrimitives.WriteInt32LittleEndian(expectedExceptionHandlers.AsSpan(12, 4), tokens.GetToken(typeof(TestExceptionA)));

        CollectionAssert.AreEqual(expectedExceptionHandlers, exceptionHandlers, "exception handlers match");

        var emitted = method.CreateDynamicMethod(nameof(HandleExceptions)).CreateDelegate<Func<Exception?, long>>();

        Assert.AreEqual(HandleExceptions(null), emitted(null));
        Assert.AreEqual(HandleExceptions(new TestExceptionA()), emitted(new TestExceptionA()));
        Assert.Throws<TestExceptionB>(() => HandleExceptions(new TestExceptionB(24)));
        Assert.Throws<TestExceptionB>(() => emitted(new TestExceptionB(24)));
        Assert.AreEqual(HandleExceptions(new TestExceptionB(42)), emitted(new TestExceptionB(42)));
        Assert.AreEqual(4, HandleExceptions(new TestExceptionB(42)));
    }

    private sealed class TestExceptionA() : Exception("Test A") { }

    private sealed class TestExceptionB(int value) : Exception("Test B")
    {
        public int Value = value;
    }

    private long HandleExceptions(Exception? e)
    {
        long result = 0;
        try
        {
            if (e != null) throw e;
        }
        catch (TestExceptionA)
        {
            result = 1;
        }
        catch (TestExceptionB ex) when (ex.Value == 42)
        {
            result = 2;
        }
        finally
        {
            result *= 2;
        }
        return result;
    }

    [TestMethod]
    public void TestSwitchOnValue()
    {
        var method = Method.Read(SwitchOnValue);
        Assert.IsTrue(method.Instructions.Any(
            instr => instr.Type == PseudoInstructionType.Instruction
                && instr.AsInstruction().OpCode == OpCodes.Switch),
            "contains switch instruction"
        );

        LabelAddressMap labels = stackalloc int[method.LabelCount];
        byte[] result = method.EncodeBody(labels, TokenSource.CreateStable());

        var expectedBody = ((Delegate)SwitchOnValue).Method.GetMethodBody()!;
        var expected = expectedBody.GetILAsByteArray()!;

        Assert.HasCount(expected.Length, result, "IL body length matches");

        CollectionAssert.AreEqual(expected, result);

        var emitted = method.CreateDynamicMethod(nameof(SwitchOnValue)).CreateDelegate<Func<uint, int, int>>();

        for (uint i = 0; i < 5; i++)
            for (int j = 0; j < 10; j++)
                Assert.AreEqual(SwitchOnValue(i, j), emitted(i, j));
    }

    private int SwitchOnValue(uint value, int x)
    {
    Start:
        switch (value)
        {
            case 0: return x;
            case 1: return 2 * x;
            case 2: return x * x;
            case 3: return x + 1;
            default:
                value--;
                goto Start;
        }
    }
}
