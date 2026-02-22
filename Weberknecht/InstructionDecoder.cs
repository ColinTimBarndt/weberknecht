using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Weberknecht;

internal ref struct InstructionDecoder(ReadOnlySpan<byte> data, ResolutionContext ctx) : IEnumerator<Instruction>
{

    private int _index = 0;
    private readonly ReadOnlySpan<byte> _data = data;
    private readonly ResolutionContext _ctx = ctx;

    readonly object? IEnumerator.Current => Current;

    public Instruction Current { get; private set; }

    public int CurrentAddress { get; private set; }

    public bool MoveNext()
    {
        CurrentAddress = _index;
        if (_index >= _data.Length) return false;
        int code = _data[_index++];
        if (code == 0xFE)
        {
            if (_index >= _data.Length)
                throw new InvalidDataException();
            code <<= 8;
            code |= _data[_index++];
        }
        var op = OpCodeTable.Decode((short)code);
        var operandType = op.OperandType;
        var immSize = operandType.Size;
        if (_index + immSize > _data.Length)
            throw new InvalidDataException();
        var immData = _data.Slice(_index, immSize);
        _index += immSize;
        //Console.WriteLine(operandType);
        ValueTuple<object?, Instruction.UnmanagedOperand> immediate = operandType switch
        {
            OperandType.InlineBrTarget => (null, BinaryPrimitives.ReadInt32LittleEndian(immData) + _index),
            OperandType.InlineField => (GetTok(GetHandle(immData), EntityType.Field), default),
            OperandType.InlineMethod => (GetTok(GetHandle(immData), EntityType.Method), default),
            OperandType.InlineType => (GetTok(GetHandle(immData), EntityType.Type), default),
            OperandType.InlineTok => (GetTok(GetHandle(immData), EntityType.Type | EntityType.Method | EntityType.Field), default),
            OperandType.InlineSwitch => throw new NotImplementedException("switch"),
            OperandType.InlineI => (null, BinaryPrimitives.ReadInt32LittleEndian(immData)),
            OperandType.InlineSig => (GetSignature((StandaloneSignatureHandle)GetHandle(immData)), default),
            OperandType.InlineString => (_ctx.Meta.GetUserString((UserStringHandle)GetHandle(immData)), default),
            OperandType.InlineI8 => (null, BinaryPrimitives.ReadInt64LittleEndian(immData)),
            OperandType.InlineNone => default,
            OperandType.InlineR => (null, BinaryPrimitives.ReadDoubleLittleEndian(immData)),
            OperandType.InlineVar => (null, BinaryPrimitives.ReadUInt16LittleEndian(immData)),
            OperandType.ShortInlineBrTarget => (null, (sbyte)immData[0] + _index),
            OperandType.ShortInlineI => (null, (sbyte)immData[0]),
            OperandType.ShortInlineR => (null, BinaryPrimitives.ReadSingleLittleEndian(immData)),
            OperandType.ShortInlineVar => (null, immData[0]),
            _ => throw new UnreachableException(),
        };
        Current = new(op, immediate.Item1, immediate.Item2);

        return true;
    }

    private static Handle GetHandle(ReadOnlySpan<byte> span) => GetHandle(BitConverter.ToInt32(span));
    private static Handle GetHandle(int token) => MetadataTokens.Handle(token);

    private readonly object GetTok(Handle handle, EntityType allowed) => handle.Kind switch
    {
        HandleKind.MethodDefinition when allowed.HasFlag(EntityType.Method) => _ctx.ResolveMethodHandle((MethodDefinitionHandle)handle),
        HandleKind.MethodSpecification when allowed.HasFlag(EntityType.Method) => _ctx.ResolveMethodHandle((MethodSpecificationHandle)handle),
        HandleKind.TypeDefinition when allowed.HasFlag(EntityType.Type) => _ctx.ResolveTypeHandle((TypeDefinitionHandle)handle),
        HandleKind.TypeReference when allowed.HasFlag(EntityType.Type) => _ctx.ResolveTypeHandle((TypeReferenceHandle)handle),
        HandleKind.TypeSpecification when allowed.HasFlag(EntityType.Type) => _ctx.ResolveTypeHandle((TypeSpecificationHandle)handle),
        HandleKind.FieldDefinition when allowed.HasFlag(EntityType.Field) => _ctx.ResolveFieldHandle((FieldDefinitionHandle)handle),
        HandleKind.MemberReference => GetMember((MemberReferenceHandle)handle, allowed),
        _ => throw new NotImplementedException(Enum.GetName(handle.Kind))
    };

    private readonly object GetMember(MemberReferenceHandle handle, EntityType allowed)
    {
        var member = _ctx.Meta.GetMemberReference(handle);
        var kind = MetadataUtil.GetSignatureKind(_ctx.Meta, member.Signature);

        return kind switch
        {
            //case SignatureKind.MethodSpecification when allowed.HasFlag(EntityType.Method):
            SignatureKind.Method when allowed.HasFlag(EntityType.Method) => _ctx.ResolveMethod(member),
            SignatureKind.Field when allowed.HasFlag(EntityType.Field) => _ctx.ResolveField(member),
            _ => throw new InvalidDataException(),
        };
    }

    private readonly object GetSignature(StandaloneSignatureHandle handle) => GetSignature(_ctx.Meta.GetStandaloneSignature(handle));

    private readonly object GetSignature(StandaloneSignature sig)
    {
        switch (sig.GetKind())
        {
            case StandaloneSignatureKind.Method:
                break;

            case StandaloneSignatureKind.LocalVariables:
                break;

            default:
                throw new NotImplementedException(Enum.GetName(sig.GetKind()));
        }
        throw new NotImplementedException(); // TODO
    }

    public void Reset()
    {
        _index = 0;
    }

    public readonly void Dispose() { }

    [Flags]
    private enum EntityType
    {
        Type = 1,
        Method = 2,
        Field = 4,
    }

}