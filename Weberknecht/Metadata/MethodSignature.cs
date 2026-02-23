using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Emit;

namespace Weberknecht.Metadata;

public sealed class MethodSignature
{

    public CallingConventions CallingConventions { get; }

    public Type ReturnType { get; }

    private readonly ImmutableArray<Type> _arguments;
    public IReadOnlyCollection<Type> Arguments => _arguments;

    public int RequiredArgumentCount { get; }

    internal MethodSignature(
        CallingConventions conventions,
        Type returnType,
        ImmutableArray<Type> args,
        int required)
    {
        CallingConventions = conventions;
        ReturnType = returnType;
        _arguments = args;
        RequiredArgumentCount = required;
    }

    public static MethodSignature Of(Delegate method) => Of(method.Method);

    public static MethodSignature Of(MethodInfo method)
    {
        var ps = method.GetParameters();
        int requiredParameters = ps.Length;
        if (method.CallingConvention.HasFlag(CallingConventions.VarArgs))
        {
            requiredParameters = 0;
            while (requiredParameters < ps.Length && !ps[requiredParameters].IsOptional)
                requiredParameters++;
        }
        return new(method.CallingConvention, method.ReturnType, [.. from p in ps select p.ParameterType], requiredParameters);
    }

    internal SignatureHelper GetHelper(Module? module = null)
    {
        var helper = SignatureHelper.GetMethodSigHelper(module, CallingConventions, ReturnType);
        foreach (var arg in _arguments[..RequiredArgumentCount])
            helper.AddArgument(arg);
        if (CallingConventions.HasFlag(CallingConventions.VarArgs))
        {
            helper.AddSentinel();
            foreach (var arg in _arguments[RequiredArgumentCount..])
                helper.AddArgument(arg);
        }
        return helper;
    }

}
