using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;

namespace Weberknecht;

public abstract class WeberknechtException : Exception
{

    internal WeberknechtException(string? message) : base(message) { }

}

public abstract class ResolutionException(string? message) : WeberknechtException(message) { }

public sealed class TypeResolutionException : ResolutionException
{

    public string? Namespace { get; }
    public string Name { get; }
    public Module? Module { get; }
    public Assembly Assembly { get; }

    internal TypeResolutionException(string? ns, string name, Assembly scope)
    : base($"Unable to find type {ns}.{name} in assembly {scope}")
    {
        Namespace = ns;
        Name = name;
        Assembly = scope;
    }

    internal TypeResolutionException(string? ns, string name, Module scope)
    : base($"Unable to find type {ns}.{name} in module {scope}")
    {
        Namespace = ns;
        Name = name;
        Module = scope;
        Assembly = scope.Assembly;
    }

}

public sealed class FieldResolutionException : ResolutionException
{

    public Type DeclaringType { get; }
    public string FieldName { get; }
    public Type FieldType { get; }

    internal FieldResolutionException(Type declType, string name, Type fieldType)
    : base($"Unable to find field {fieldType} {declType}::{name}")
    {
        DeclaringType = declType;
        FieldName = name;
        FieldType = fieldType;
    }

}

public sealed class MethodResolutionException : ResolutionException
{

    public Type DeclaringType { get; }
    public string MethodName { get; }
    public Type ReturnType { get; }
    public ImmutableArray<Type> ArgumentTypes { get; }
    public bool IsStatic { get; }

    internal MethodResolutionException(Type declType, string name, bool isStatic, Type returnType, ImmutableArray<Type> argumentTypes)
    : base($"Unable to find {(isStatic ? "static" : string.Empty)}method {returnType} {declType}::{name}({string.Join(", ", argumentTypes)})")
    {
        DeclaringType = declType;
        MethodName = name;
        ReturnType = returnType;
        ArgumentTypes = argumentTypes;
        IsStatic = isStatic;
    }

}

public sealed class ModuleResolutionException : ResolutionException
{

    public string Name { get; }
    public Assembly Assembly { get; }

    internal ModuleResolutionException(string name, Assembly scope) : base($"Unable to find module {name} in assembly {scope}")
    {
        Name = name;
        Assembly = scope;
    }

}

public sealed class AssemblyResolutionException : ResolutionException
{

    public AssemblyName Name { get; }

    internal AssemblyResolutionException(AssemblyName name) : base($"Unable to find assembly {name}")
    {
        Name = name;
    }

}

public abstract class MetadataException(string? message) : WeberknechtException(message) { }

public sealed class UnsupportedHashAlgorithmException : MetadataException
{

    public Guid HashAlgorithm { get; }

    internal UnsupportedHashAlgorithmException(Guid algorithm) : base($"Unsupported hash algorithm {{{algorithm}}}")
    {
        HashAlgorithm = algorithm;
    }

}

public sealed class InvalidStandaloneSignatureKindException : MetadataException
{

    public StandaloneSignatureKind ExpectedSignatureKind { get; }

    public StandaloneSignatureKind FoundSignatureKind { get; }

    internal InvalidStandaloneSignatureKindException(
        StandaloneSignatureKind expected,
        StandaloneSignatureKind found)
    : base($"Expected signature kind {Enum.GetName(expected)}, found {Enum.GetName(found)}")
    {
        ExpectedSignatureKind = found;
        FoundSignatureKind = found;
    }

    internal static void Assert(
        StandaloneSignatureKind expected,
        StandaloneSignatureKind found)
    {
        if (expected != found)
            throw new InvalidStandaloneSignatureKindException(expected, found);
    }

}

public sealed class UnsupportedCallingConventionException : MetadataException
{

    public SignatureCallingConvention CallingConvention { get; }

    internal UnsupportedCallingConventionException(
        SignatureCallingConvention convention)
    : base($"Unsupported calling convention: {Enum.GetName(convention)}")
    {
        CallingConvention = convention;
    }

}

public sealed class ConflictingStackSizeException : WeberknechtException
{

    public int InstructionIndex { get; }

    internal ConflictingStackSizeException(int index) : base($"Conflicting stack size at index {index}")
    {
        InstructionIndex = index;
    }

}
