using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace Weberknecht;

internal sealed partial class ResolutionContext : ISignatureTypeProvider<Type, GenericContext>
{

    private static readonly Type[] _primitives;

    static ResolutionContext()
    {
        _primitives = new Type[Enum.GetValues<PrimitiveTypeCode>().Select(t => (int)t).Aggregate(int.Max) + 1];
        _primitives[(int)PrimitiveTypeCode.Void] = typeof(void);
        _primitives[(int)PrimitiveTypeCode.Char] = typeof(char);
        _primitives[(int)PrimitiveTypeCode.SByte] = typeof(sbyte);
        _primitives[(int)PrimitiveTypeCode.Byte] = typeof(byte);
        _primitives[(int)PrimitiveTypeCode.Int16] = typeof(short);
        _primitives[(int)PrimitiveTypeCode.UInt16] = typeof(ushort);
        _primitives[(int)PrimitiveTypeCode.Int32] = typeof(int);
        _primitives[(int)PrimitiveTypeCode.UInt32] = typeof(uint);
        _primitives[(int)PrimitiveTypeCode.Int64] = typeof(long);
        _primitives[(int)PrimitiveTypeCode.UInt64] = typeof(ulong);
        _primitives[(int)PrimitiveTypeCode.Single] = typeof(float);
        _primitives[(int)PrimitiveTypeCode.Double] = typeof(double);
        _primitives[(int)PrimitiveTypeCode.String] = typeof(string);
        _primitives[(int)PrimitiveTypeCode.TypedReference] = typeof(TypedReference);
        _primitives[(int)PrimitiveTypeCode.IntPtr] = typeof(nint);
        _primitives[(int)PrimitiveTypeCode.UIntPtr] = typeof(nuint);
        _primitives[(int)PrimitiveTypeCode.Object] = typeof(object);
    }

    Type IConstructedTypeProvider<Type>
    .GetArrayType(Type elementType, ArrayShape shape)
    {
        return elementType.MakeArrayType(shape.Rank);
    }

    Type IConstructedTypeProvider<Type>
    .GetByReferenceType(Type elementType)
    {
        return elementType.MakeByRefType();
    }

    Type ISignatureTypeProvider<Type, GenericContext>
    .GetFunctionPointerType(MethodSignature<Type> signature)
    {
        throw new NotImplementedException("function pointer type"); // TODO
    }

    Type IConstructedTypeProvider<Type>
    .GetGenericInstantiation(Type genericType, ImmutableArray<Type> typeArguments)
    {
        return genericType.MakeGenericType([.. typeArguments]);
    }

    Type ISignatureTypeProvider<Type, GenericContext>
    .GetGenericMethodParameter(GenericContext genericContext, int index)
    {
        return genericContext.GetMethodParameter(index);
    }

    Type ISignatureTypeProvider<Type, GenericContext>
    .GetGenericTypeParameter(GenericContext genericContext, int index)
    {
        return genericContext.GetTypeParameter(index);
    }

    Type ISignatureTypeProvider<Type, GenericContext>
    .GetModifiedType(Type modifier, Type unmodifiedType, bool isRequired)
    {
        throw new NotImplementedException($"modifier {modifier} on {unmodifiedType}"); // TODO
    }

    Type ISignatureTypeProvider<Type, GenericContext>
    .GetPinnedType(Type elementType)
    {
        return elementType; // TODO: Is this correct?
    }

    Type IConstructedTypeProvider<Type>
    .GetPointerType(Type elementType)
    {
        return elementType.MakePointerType();
    }

    Type ISimpleTypeProvider<Type>
    .GetPrimitiveType(PrimitiveTypeCode typeCode) => _primitives[(int)typeCode];

    Type ISZArrayTypeProvider<Type>
    .GetSZArrayType(Type elementType)
    {
        return elementType.MakeArrayType();
    }

    Type ISimpleTypeProvider<Type>
    .GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
        => ResolveTypeHandle(handle);

    Type ISimpleTypeProvider<Type>
    .GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
        => ResolveTypeHandle(handle);

    Type ISignatureTypeProvider<Type, GenericContext>
    .GetTypeFromSpecification(MetadataReader reader, GenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
    {
        var spec = reader.GetTypeSpecification(handle);
        return spec.DecodeSignature(this, genericContext);
    }

}