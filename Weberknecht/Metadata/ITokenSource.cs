using System.Reflection;
using System.Reflection.Emit;

namespace Weberknecht.Metadata;

internal interface ITokenSource
{

    int GetToken(Type type);
    int GetToken(MethodBase method);
    int GetToken(FieldInfo field);
    int GetToken(string literal);
    int GetToken(MethodSignature signature);

    int GetToken(MethodInfo method) => GetToken((MethodBase)method);
    int GetToken(ConstructorInfo ctor) => GetToken((MethodBase)ctor);

}

internal static class TokenSource
{

    public static DynamicTokenSource Create(DynamicILInfo info) => new(info);

    public static BuilderTokenSource Create(ModuleBuilder builder) => new(builder);

    internal static StableHashTokenSource CreateStable() => new();

}

internal readonly struct InfallibleTokenSource() : ITokenSource
{
    int ITokenSource.GetToken(Type type) => throw new NotImplementedException();

    int ITokenSource.GetToken(MethodBase method) => throw new NotImplementedException();

    int ITokenSource.GetToken(FieldInfo field) => throw new NotImplementedException();

    int ITokenSource.GetToken(string literal) => throw new NotImplementedException();

    int ITokenSource.GetToken(MethodSignature signature) => throw new NotImplementedException();
}

/// <summary>
/// Generated tokens represent the hash code of what they represent.
/// These are invalid and only intended for testing purposes.
/// </summary>
internal readonly struct StableHashTokenSource() : ITokenSource
{

    private static void HashModule(ref HashCode hash, Module module)
    {
        hash.Add(module.Name);
        hash.Add(module.Assembly.GetName().Name);
    }

    private static void HashType(ref HashCode hash, Type? type)
    {
        if (type == null)
            return;

        hash.Add(type.FullName ?? type.Name);
    }

    public int GetToken(Type type)
    {
        HashCode hash = new();
        HashType(ref hash, type);
        return hash.ToHashCode();
    }

    public int GetToken(MethodBase method)
    {
        HashCode hash = new();
        HashModule(ref hash, method.Module);
        HashType(ref hash, method.DeclaringType);
        hash.Add(method.Name);
        var ps = method.GetParameters();
        hash.Add(ps.Length);
        foreach (var p in ps)
            hash.Add(p.ParameterType.FullName ?? p.ParameterType.Name);
        return hash.ToHashCode();
    }

    public int GetToken(FieldInfo field)
    {
        HashCode hash = new();
        HashModule(ref hash, field.Module);
        HashType(ref hash, field.DeclaringType);
        hash.Add(field.Name);
        return hash.ToHashCode();
    }

    public int GetToken(string literal) => literal.GetHashCode();

    public int GetToken(MethodSignature signature)
    {
        HashCode hash = new();
        hash.Add(signature.CallingConventions);
        HashType(ref hash, signature.ReturnType);
        hash.Add(signature.Arguments.Count);
        foreach (var arg in signature.Arguments)
            HashType(ref hash, arg);
        if (signature.CallingConventions.HasFlag(CallingConventions.VarArgs))
            hash.Add(signature.RequiredArgumentCount);
        return hash.ToHashCode();
    }
}

internal readonly struct DynamicTokenSource(DynamicILInfo info) : ITokenSource
{

    private readonly DynamicILInfo _dynamicInfo = info;

    public int GetToken(Type type) => _dynamicInfo.GetTokenFor(type.TypeHandle);

    public int GetToken(MethodBase method) => _dynamicInfo.GetTokenFor(method.MethodHandle, method.DeclaringType!.TypeHandle);

    public int GetToken(FieldInfo field) => _dynamicInfo.GetTokenFor(field.FieldHandle, field.DeclaringType!.TypeHandle);

    public int GetToken(string literal) => _dynamicInfo.GetTokenFor(literal);

    public int GetToken(MethodSignature signature) => _dynamicInfo.GetTokenFor(signature.GetHelper().GetSignature());

}

internal readonly struct BuilderTokenSource(ModuleBuilder builder) : ITokenSource
{

    private readonly ModuleBuilder _builder = builder;

    public int GetToken(Type type) => _builder.GetTypeMetadataToken(type);

    public int GetToken(MethodBase method) => method switch
    {
        MethodInfo methodInfo => GetToken(methodInfo),
        ConstructorInfo ctor => GetToken(ctor),
        _ => throw new NotImplementedException(method.GetType().Name),
    };

    public int GetToken(MethodInfo method) => _builder.GetMethodMetadataToken(method);

    public int GetToken(ConstructorInfo ctor) => _builder.GetMethodMetadataToken(ctor);

    public int GetToken(FieldInfo field) => _builder.GetFieldMetadataToken(field);

    public int GetToken(string literal) => _builder.GetStringMetadataToken(literal);

    public int GetToken(MethodSignature signature) => _builder.GetSignatureMetadataToken(signature.GetHelper());

}
