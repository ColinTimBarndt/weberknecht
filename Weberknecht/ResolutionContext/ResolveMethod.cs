using System.Reflection;
using System.Reflection.Metadata;

namespace Weberknecht;

internal sealed partial class ResolutionContext
{

    public MethodInfo ResolveMethodHandle(EntityHandle handle) => handle.Kind switch
    {
        HandleKind.MethodDefinition => ResolveMethodHandle((MethodDefinitionHandle)handle),
        HandleKind.MemberReference => ResolveMethodHandle((MemberReferenceHandle)handle),
        HandleKind.MethodSpecification => ResolveMethodHandle((MethodSpecificationHandle)handle),
        _ => throw new InvalidOperationException(),
    };

    public MethodInfo ResolveMethodHandle(MethodSpecificationHandle handle)
        => ResolveMethod(Meta.GetMethodSpecification(handle));

    public MethodInfo ResolveMethod(MethodSpecification spec)
    {
        var genMethodHandle = spec.Method;
        MethodInfo info = genMethodHandle.Kind switch
        {
            HandleKind.MethodDefinition => ResolveMethodHandle((MethodDefinitionHandle)genMethodHandle),
            HandleKind.MemberReference => ResolveMethodHandle((MemberReferenceHandle)genMethodHandle),
            HandleKind.MethodSpecification => throw new InvalidOperationException(),
            _ => throw new NotImplementedException(genMethodHandle.Kind.ToString()),
        };

        var ctx = new GenericContext(info.DeclaringType!.GetGenericArguments(), info.GetGenericArguments());
        var typeArgs = spec.DecodeSignature(this, ctx);
        return info.MakeGenericMethod([.. typeArgs]);
    }

    public MethodInfo ResolveMethodHandle(MemberReferenceHandle handle)
        => ResolveMethod(Meta.GetMemberReference(handle));

    public MethodInfo ResolveMethod(MemberReference memberRef)
    {
        var type = ResolveTypeHandle(memberRef.Parent);

        var (header, mvarCount) = MetadataUtil.ReadMethodHeader(Meta, memberRef.Signature);

        var ctx = new GenericContext(type.GetGenericArguments(), mvarCount);
        var sig = memberRef.DecodeMethodSignature(this, ctx);
        var method = type.GetMethod(
            Meta.GetString(memberRef.Name),
            mvarCount,
            binder: null,
            bindingAttr: MetadataUtil.GetBindingFlags(header),
            callConvention: MetadataUtil.GetCallingConventions(header),
            types: [.. sig.ParameterTypes],
            modifiers: null
        ) ?? throw new NullReferenceException();

        if (method.ReturnType != sig.ReturnType)
            throw new Exception();

        return method;
    }

    public MethodInfo ResolveMethodHandle(MethodDefinitionHandle handle)
        => ResolveMethod(Meta.GetMethodDefinition(handle));

    public MethodInfo ResolveMethod(MethodDefinition methodDef)
    {
        var type = ResolveTypeHandle(methodDef.GetDeclaringType());

        var (header, mvarCount) = MetadataUtil.ReadMethodHeader(Meta, methodDef.Signature);

        var ctx = new GenericContext(type.GetGenericArguments(), mvarCount);
        var sig = methodDef.DecodeSignature(this, ctx);
        var method = type.GetMethod(
            Meta.GetString(methodDef.Name),
            mvarCount,
            binder: null,
            bindingAttr: MetadataUtil.GetBindingFlags(header),
            callConvention: MetadataUtil.GetCallingConventions(header),
            types: [.. sig.ParameterTypes],
            modifiers: null
        ) ?? throw new NullReferenceException();

        if (method.ReturnType != sig.ReturnType)
            throw new Exception();

        return method;
    }

}