using System.Buffers.Binary;
using System.Collections;
using System.Reflection;
using System.Reflection.Emit;
using Weberknecht.Metadata;

namespace Weberknecht.Test;

public readonly struct ILBodyBuilder()
{

    private static readonly StableHashTokenSource _tokens = TokenSource.CreateStable();

    private readonly List<byte> _body = [];

    public ILBodyBuilder Add(OpCode opCode)
    {
        Span<byte> bytes = stackalloc byte[2];
        BinaryPrimitives.WriteInt16BigEndian(bytes, opCode.Value);
        if (bytes[0] != 0)
            _body.AddRange(bytes);
        else
            _body.Add(bytes[1]);

        return this;
    }

    public ILBodyBuilder Add(OpCode opCode, byte operand)
    {
        Add(opCode);
        _body.Add(operand);

        return this;
    }

    public ILBodyBuilder Add(OpCode opCode, Type type) => Add(opCode, _tokens.GetToken(type));

    private const BindingFlags FIELD_FLAGS = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    public ILBodyBuilder Add(OpCode opCode, Type type, string field)
        => Add(opCode, _tokens.GetToken(
            type.GetField(field, FIELD_FLAGS)
                ?? throw new MissingFieldException(type.Name, field)));

    public ILBodyBuilder Add(OpCode opCode, Delegate method) => Add(opCode, method.Method);

    public ILBodyBuilder Add(OpCode opCode, MethodInfo method) => Add(opCode, _tokens.GetToken(method));

    public ILBodyBuilder Add(OpCode opCode, int token)
    {
        Add(opCode);

        Span<byte> bytes = stackalloc byte[4];
        BinaryPrimitives.WriteInt32LittleEndian(bytes, token);
        _body.AddRange(bytes);

        return this;
    }

    public void AssertEquals(byte[] other)
    {
        CollectionAssert.AreEqual(_body, other, "IL bytes match");
    }

}
