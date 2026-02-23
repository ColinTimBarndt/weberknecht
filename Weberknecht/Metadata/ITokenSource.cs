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

}

internal readonly struct InfallibleTokenSource() : ITokenSource
{
    int ITokenSource.GetToken(Type type) => throw new NotImplementedException();

    int ITokenSource.GetToken(MethodBase method) => throw new NotImplementedException();

    int ITokenSource.GetToken(FieldInfo field) => throw new NotImplementedException();

    int ITokenSource.GetToken(string literal) => throw new NotImplementedException();

    int ITokenSource.GetToken(MethodSignature signature) => throw new NotImplementedException();
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
