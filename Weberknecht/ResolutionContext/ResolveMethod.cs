using System.Diagnostics;
using System.Reflection;
using System.Reflection.Metadata;

namespace Weberknecht;

internal sealed partial class ResolutionContext
{

    public MethodBase ResolveMethodHandle(EntityHandle handle) => handle.Kind switch
    {
        HandleKind.MethodDefinition => ResolveMethodHandle((MethodDefinitionHandle)handle),
        HandleKind.MemberReference => ResolveMethodHandle((MemberReferenceHandle)handle),
        HandleKind.MethodSpecification => ResolveMethodHandle((MethodSpecificationHandle)handle),
        _ => throw new InvalidOperationException(),
    };

    public MethodBase ResolveMethodHandle(MethodSpecificationHandle handle)
        => ResolveMethod(Meta.GetMethodSpecification(handle));

    public MethodBase ResolveMethod(MethodSpecification spec)
    {
        var genMethodHandle = spec.Method;
        var info = genMethodHandle.Kind switch
        {
            HandleKind.MethodDefinition => ResolveMethodHandle((MethodDefinitionHandle)genMethodHandle),
            HandleKind.MemberReference => ResolveMethodHandle((MemberReferenceHandle)genMethodHandle),
            HandleKind.MethodSpecification => throw new InvalidOperationException(),
            _ => throw new NotImplementedException(genMethodHandle.Kind.ToString()),
        };

        var typeArgs = spec.DecodeSignature(this, _gctx);
        return info switch
        {
            MethodInfo method => method.MakeGenericMethod([.. typeArgs]),
            ConstructorInfo ctor => ctor,
            _ => throw new UnreachableException(),
        };
    }

    public MethodBase ResolveMethodHandle(MemberReferenceHandle handle)
        => ResolveMethod(Meta.GetMemberReference(handle));

    public MethodBase ResolveMethod(MemberReference memberRef)
    {
        var type = ResolveTypeHandle(memberRef.Parent);

        var (header, mvarCount) = MetadataUtil.ReadMethodHeader(Meta, memberRef.Signature);

        var gctx = new GenericContext(type.GetGenericArguments(), mvarCount);
        var sig = memberRef.DecodeMethodSignature(this, gctx);
        return ResolveMethod(type, Meta.GetString(memberRef.Name), header, mvarCount, sig);
    }

    public MethodBase ResolveMethodHandle(MethodDefinitionHandle handle)
        => ResolveMethod(Meta.GetMethodDefinition(handle));

    public MethodBase ResolveMethod(MethodDefinition methodDef)
    {
        var type = ResolveTypeHandle(methodDef.GetDeclaringType());

        var (header, mvarCount) = MetadataUtil.ReadMethodHeader(Meta, methodDef.Signature);

        var gctx = new GenericContext(type.GetGenericArguments(), mvarCount);
        var sig = methodDef.DecodeSignature(this, gctx);
        return ResolveMethod(type, Meta.GetString(methodDef.Name), header, mvarCount, sig);
    }

    private static MethodBase ResolveMethod(Type type, string name, SignatureHeader header, int mvarCount, MethodSignature<Type> sig)
    {
        var method = type.GetMethod(
            name,
            mvarCount,
            binder: null,
            bindingAttr: MetadataUtil.GetBindingFlags(header),
            callConvention: MetadataUtil.GetCallingConventions(header),
            types: [.. sig.ParameterTypes],
            modifiers: null
        );

        if (method != null)
        {
            if (method.ReturnType != sig.ReturnType)
                throw new Exception();

            return method;
        }

        var ctor = type.GetConstructor(bindingAttr: MetadataUtil.GetBindingFlags(header), binder: null, types: [.. sig.ParameterTypes], modifiers: null)
                ?? throw new MissingMethodException(type.Name, name);

        return ctor;
    }

}