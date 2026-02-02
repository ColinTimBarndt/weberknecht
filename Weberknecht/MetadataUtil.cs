using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace Weberknecht;

internal static class MetadataUtil
{

    private static readonly ConditionalWeakTable<Assembly, MetadataReader?> _lookup = [];

    extension(Assembly asm)
    {
        public unsafe MetadataReader? GetMetadataReader()
        {
            return _lookup.GetOrAdd(asm, static (asm) =>
            {
                if (!asm.TryGetRawMetadata(out byte* blob, out int length))
                    return null;
                return new MetadataReader(blob, length);
            });
        }
    }

    public static Type ResolveType(MetadataReader reader, TypeResolver res, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.TypeDefinition => ResolveType(reader, res, (TypeDefinitionHandle)handle),
        HandleKind.TypeReference => ResolveType(reader, res, (TypeReferenceHandle)handle),
        _ => throw new InvalidOperationException(),
    };

    public static Type ResolveType(MetadataReader reader, TypeResolver res, EntityHandle handle, GenericContext ctx) => handle.Kind switch
    {
        HandleKind.TypeDefinition => ResolveType(reader, res, (TypeDefinitionHandle)handle),
        HandleKind.TypeReference => ResolveType(reader, res, (TypeReferenceHandle)handle),
        HandleKind.TypeSpecification => ResolveType(reader, res, (TypeSpecificationHandle)handle, ctx),
        _ => throw new InvalidOperationException(),
    };

    public static Type ResolveType(MetadataReader reader, TypeResolver res, TypeDefinitionHandle handle)
        => ResolveType(reader, res, reader.GetTypeDefinition(handle));

    public static Type ResolveType(MetadataReader reader, TypeResolver res, [NotNull] TypeDefinition typeDef)
    {
        var decl = typeDef.GetDeclaringType();
        if (decl.IsNil)
            return res.GetType(reader.GetString(typeDef.Namespace), reader.GetString(typeDef.Name)) ?? throw new NullReferenceException();
        var nestedName = new Stack<string>();
        while (true)
        {
            nestedName.Push(reader.GetString(typeDef.Name));
            if (decl.IsNil)
                break;
            typeDef = reader.GetTypeDefinition(decl);
            decl = typeDef.GetDeclaringType();
        }
        return res.GetType(reader.GetString(typeDef.Namespace), string.Join('+', nestedName)) ?? throw new NullReferenceException();
    }

    public static Type ResolveType(MetadataReader reader, TypeResolver res, TypeReferenceHandle handle)
        => ResolveType(reader, res, reader.GetTypeReference(handle));

    public static Type ResolveType(MetadataReader reader, TypeResolver res, TypeReference typeRef)
    {
        if (typeRef.ResolutionScope.IsNil) // TODO: What other resolution scopes exist?
            return res.GetType(reader.GetString(typeRef.Namespace), reader.GetString(typeRef.Name)) ?? throw new NullReferenceException();

        // Nested
        var nestedName = new Stack<string>();
        while (true)
        {
            nestedName.Push(reader.GetString(typeRef.Name));

            if (typeRef.ResolutionScope.Kind != HandleKind.TypeReference)
                break;

            typeRef = reader.GetTypeReference((TypeReferenceHandle)typeRef.ResolutionScope);
        }

        return res.GetType(reader.GetString(typeRef.Namespace), string.Join('+', nestedName)) ?? throw new NullReferenceException();
    }

    public static Type ResolveType(MetadataReader reader, TypeResolver res, TypeSpecificationHandle handle, GenericContext ctx)
        => ResolveType(res, reader.GetTypeSpecification(handle), ctx);

    public static Type ResolveType(TypeResolver res, TypeSpecification spec, GenericContext ctx)
        => spec.DecodeSignature(new SignatureTypeProvider(res), ctx);

    public static MethodInfo ResolveMethod(MetadataReader reader, TypeResolver res, EntityHandle handle) => handle.Kind switch
    {
        HandleKind.MethodDefinition => ResolveMethod(reader, res, (MethodDefinitionHandle)handle),
        HandleKind.MemberReference => ResolveMethod(reader, res, (MemberReferenceHandle)handle),
        HandleKind.MethodSpecification => ResolveMethod(reader, res, (MethodSpecificationHandle)handle),
        _ => throw new InvalidOperationException(),
    };

    public static MethodInfo ResolveMethod(MetadataReader reader, TypeResolver res, MethodSpecification spec)
    {
        var genMethodHandle = spec.Method;
        MethodInfo info = genMethodHandle.Kind switch
        {
            HandleKind.MethodDefinition => ResolveMethod(reader, res, (MethodDefinitionHandle)genMethodHandle),
            HandleKind.MemberReference => ResolveMethod(reader, res, (MemberReferenceHandle)genMethodHandle),
            _ => throw new UnreachableException(),
        };

        var ctx = new GenericContext(info.DeclaringType!.GetGenericArguments(), info.GetGenericArguments());
        var typeArgs = spec.DecodeSignature(new SignatureTypeProvider(res), ctx);
        return info.MakeGenericMethod([.. typeArgs]);
    }

    public static MethodInfo ResolveMethod(MetadataReader reader, TypeResolver res, MemberReferenceHandle handle)
        => ResolveMethod(reader, res, reader.GetMemberReference(handle));

    public static MethodInfo ResolveMethod(MetadataReader reader, TypeResolver res, MemberReference memberRef)
    {
        var type = ResolveType(reader, res, memberRef.Parent);

        var (header, mvarCount) = ReadMethodHeader(reader, memberRef.Signature);

        var ctx = new GenericContext(type.GetGenericArguments(), mvarCount);
        var sig = memberRef.DecodeMethodSignature(new SignatureTypeProvider(res), ctx);
        var method = type.GetMethod(
            reader.GetString(memberRef.Name),
            mvarCount,
            binder: null,
            bindingAttr: GetBindingFlags(header),
            callConvention: GetCallingConventions(header),
            types: [.. sig.ParameterTypes],
            modifiers: null
        ) ?? throw new NullReferenceException();

        if (method.ReturnType != sig.ReturnType)
            throw new Exception();

        return method;
    }

    public static MethodInfo ResolveMethod(MetadataReader reader, TypeResolver res, MethodDefinitionHandle handle)
        => ResolveMethod(reader, res, reader.GetMethodDefinition(handle));

    public static MethodInfo ResolveMethod(MetadataReader reader, TypeResolver res, MethodDefinition methodDef)
    {
        var typeDef = reader.GetTypeDefinition(methodDef.GetDeclaringType());
        var type = ResolveType(reader, res, typeDef);

        var (header, mvarCount) = ReadMethodHeader(reader, methodDef.Signature);

        var ctx = new GenericContext(type.GetGenericArguments(), mvarCount);
        var sig = methodDef.DecodeSignature(new SignatureTypeProvider(res), ctx);
        var method = type.GetMethod(
            reader.GetString(methodDef.Name),
            mvarCount,
            binder: null,
            bindingAttr: GetBindingFlags(header),
            callConvention: GetCallingConventions(header),
            types: [.. sig.ParameterTypes],
            modifiers: null
        ) ?? throw new NullReferenceException();

        if (method.ReturnType != sig.ReturnType)
            throw new Exception();

        return method;
    }

    public static FieldInfo ResolveField(MetadataReader reader, TypeResolver res, FieldDefinitionHandle handle)
        => ResolveField(reader, res, reader.GetFieldDefinition(handle));

    public static FieldInfo ResolveField(MetadataReader reader, TypeResolver res, FieldDefinition fieldDef)
    {
        var typeDef = reader.GetTypeDefinition(fieldDef.GetDeclaringType());
        var type = ResolveType(reader, res, typeDef);

        var ctx = new GenericContext(type.GetGenericArguments(), 0);
        var fieldType = fieldDef.DecodeSignature(new SignatureTypeProvider(res), ctx);
        var field = type.GetField(reader.GetString(fieldDef.Name))
            ?? throw new NullReferenceException();

        if (field.FieldType != fieldType)
            throw new Exception();

        return field;
    }

    private static (SignatureHeader, int) ReadMethodHeader(MetadataReader reader, BlobHandle signatureHandle)
    {
        var blobReader = reader.GetBlobReader(signatureHandle);
        var header = blobReader.ReadSignatureHeader();
        if (header.IsGeneric)
            return (header, blobReader.ReadCompressedInteger());
        else
            return (header, 0);
    }

    internal static SignatureKind GetSignatureKind(MetadataReader reader, BlobHandle signatureHandle)
    {
        var blobReader = reader.GetBlobReader(signatureHandle);
        return blobReader.ReadSignatureHeader().Kind;
    }

    public static BindingFlags GetBindingFlags(SignatureHeader header)
    {
        var flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;
        if (header.IsInstance) flags |= BindingFlags.Instance;
        else flags |= BindingFlags.Static;
        return flags;
    }

    public static CallingConventions GetCallingConventions(SignatureHeader header)
    {
        var conv = header.CallingConvention switch
        {
            SignatureCallingConvention.Default => CallingConventions.Standard,
            SignatureCallingConvention.CDecl => CallingConventions.Any,
            SignatureCallingConvention.StdCall => CallingConventions.Any,
            SignatureCallingConvention.ThisCall => CallingConventions.Standard | CallingConventions.HasThis,
            SignatureCallingConvention.FastCall => throw new NotSupportedException(),
            SignatureCallingConvention.VarArgs => CallingConventions.VarArgs,
            SignatureCallingConvention.Unmanaged => CallingConventions.Any,
            _ => throw new NotSupportedException()
        };
        if (header.HasExplicitThis) conv |= CallingConventions.ExplicitThis;
        return conv;
    }

    public readonly struct GenericContext
    {
        private readonly Type[] _typeParams;
        private readonly Type[] _methodParams;

        public GenericContext(Type[] typeParams, int mvarCount)
        {
            _typeParams = typeParams;
            _methodParams = new Type[mvarCount];
            for (int i = 0; i < mvarCount; i++)
                _methodParams[i] = Type.MakeGenericMethodParameter(i);
        }

        public GenericContext(Type[] typeParams, Type[] methodParams)
        {
            _typeParams = typeParams;
            _methodParams = methodParams;
        }

        public Type GetTypeParameter(int index) => _typeParams[index];

        public Type GetMethodParameter(int index) => _methodParams[index];
    }

    private class SignatureTypeProvider(TypeResolver res) : ISignatureTypeProvider<Type, GenericContext>
    {
        private static readonly Type[] _primitives;

        private readonly TypeResolver _res = res;

        static SignatureTypeProvider()
        {
            _primitives = new Type[(Enum.GetValues<PrimitiveTypeCode>().Select(t => (int)t).Aggregate(int.Max)) + 1];
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

        public Type GetArrayType(Type elementType, ArrayShape shape)
        {
            return elementType.MakeArrayType(shape.Rank);
        }

        public Type GetByReferenceType(Type elementType)
        {
            return elementType.MakeByRefType();
        }

        public Type GetFunctionPointerType(MethodSignature<Type> signature)
        {
            throw new NotImplementedException(); // TODO
        }

        public Type GetGenericInstantiation(Type genericType, ImmutableArray<Type> typeArguments)
        {
            return genericType.MakeGenericType([.. typeArguments]);
        }

        public Type GetGenericMethodParameter(GenericContext genericContext, int index)
        {
            throw new NotImplementedException(); // TODO
        }

        public Type GetGenericTypeParameter(GenericContext genericContext, int index)
        {
            throw new NotImplementedException(); // TODO
        }

        public Type GetModifiedType(Type modifier, Type unmodifiedType, bool isRequired)
        {
            throw new NotImplementedException($"modifier {modifier} on {unmodifiedType}"); // TODO
        }

        public Type GetPinnedType(Type elementType)
        {
            return elementType; // TODO: Is this correct?
        }

        public Type GetPointerType(Type elementType)
        {
            return elementType.MakePointerType();
        }

        public Type GetPrimitiveType(PrimitiveTypeCode typeCode) => _primitives[(int)typeCode];

        public Type GetSZArrayType(Type elementType)
        {
            return elementType.MakeArrayType();
        }

        public Type GetTypeFromDefinition(MetadataReader reader, TypeDefinitionHandle handle, byte rawTypeKind)
            => ResolveType(reader, _res, handle);

        public Type GetTypeFromReference(MetadataReader reader, TypeReferenceHandle handle, byte rawTypeKind)
            => ResolveType(reader, _res, handle);

        public Type GetTypeFromSpecification(MetadataReader reader, GenericContext genericContext, TypeSpecificationHandle handle, byte rawTypeKind)
        {
            var spec = reader.GetTypeSpecification(handle);
            return spec.DecodeSignature(this, genericContext);
        }
    }

}