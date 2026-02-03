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
        if (_index >= _data.Length) return false;
        CurrentAddress = _index;
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
        object? immediate = operandType switch
        {
            OperandType.InlineBrTarget => BitConverter.ToInt32(immData) + _index,
            OperandType.InlineField => GetTok(GetHandle(immData), EntityType.Field),
            OperandType.InlineMethod => GetTok(GetHandle(immData), EntityType.Method),
            OperandType.InlineType => _ctx.ResolveTypeHandle((TypeDefinitionHandle)GetHandle(immData)),
            OperandType.InlineTok => GetTok(GetHandle(immData), EntityType.Type | EntityType.Method | EntityType.Field),
            OperandType.InlineSwitch or
            OperandType.InlineI => BitConverter.ToInt32(immData),
            OperandType.InlineSig => _ctx.Meta.GetStandaloneSignature((StandaloneSignatureHandle)GetHandle(immData)),
            OperandType.InlineString => _ctx.Meta.GetUserString((UserStringHandle)GetHandle(immData)),
            OperandType.InlineI8 => BitConverter.ToInt64(immData),
            OperandType.InlineNone => null,
            OperandType.InlineR => BitConverter.ToDouble(immData),
            OperandType.InlineVar => BitConverter.ToUInt16(immData),
            OperandType.ShortInlineBrTarget => (int)(sbyte)immData[0] + _index,
            OperandType.ShortInlineI => (int)(sbyte)immData[0],
            OperandType.ShortInlineR => BitConverter.ToSingle(immData),
            OperandType.ShortInlineVar => (ushort)immData[0],
            _ => throw new UnreachableException(),
        };
        Current = new(op, immediate);

        return true;
    }

    private readonly Handle GetHandle(ReadOnlySpan<byte> span) => GetHandle(BitConverter.ToInt32(span));
    private readonly Handle GetHandle(int token)
    {
        var handle = MetadataTokens.Handle(token);
        //Console.WriteLine($"Handle: {handle.Kind}");
        return handle;
    }

    private readonly object GetTok(Handle handle, EntityType allowed) => handle.Kind switch
    {
        HandleKind.MethodDefinition when allowed.HasFlag(EntityType.Method) => _ctx.ResolveMethodHandle((MethodDefinitionHandle)handle),
        HandleKind.MethodSpecification when allowed.HasFlag(EntityType.Method) => _ctx.ResolveMethodHandle((MethodSpecificationHandle)handle),
        HandleKind.TypeDefinition when allowed.HasFlag(EntityType.Type) => _ctx.ResolveTypeHandle((TypeDefinitionHandle)handle),
        HandleKind.TypeReference when allowed.HasFlag(EntityType.Type) => _ctx.ResolveTypeHandle((TypeReferenceHandle)handle),
        HandleKind.TypeSpecification when allowed.HasFlag(EntityType.Type) => _ctx.Meta.GetTypeSpecification((TypeSpecificationHandle)handle), // TODO: Generic context
        HandleKind.FieldDefinition when allowed.HasFlag(EntityType.Field) => _ctx.ResolveFieldHandle((FieldDefinitionHandle)handle),
        HandleKind.MemberReference => GetMember((MemberReferenceHandle)handle, allowed),
        _ => throw new NotImplementedException($"{handle.Kind}")
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