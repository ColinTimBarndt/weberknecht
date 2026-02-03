using System.Reflection;

namespace Weberknecht;

public abstract class WeberknechtException : Exception
{

    internal WeberknechtException(string? message) : base(message) { }

}

public sealed class TypeResolutionException : WeberknechtException
{

    public string? Namespace { get; }
    public string Name { get; }
    public Module? Module { get; } = null;
    public Assembly Assembly { get; }

    internal TypeResolutionException(string? ns, string name, Assembly scope) : base($"Unable to find type {ns}.{name} in assembly {scope}")
    {
        Namespace = ns;
        Name = name;
        Assembly = scope;
    }

    internal TypeResolutionException(string? ns, string name, Module scope) : base($"Unable to find type {ns}.{name} in module {scope}")
    {
        Namespace = ns;
        Name = name;
        Module = scope;
        Assembly = scope.Assembly;
    }

}

public sealed class ModuleResolutionException : WeberknechtException
{

    public string Name { get; }
    public Assembly Assembly { get; }

    internal ModuleResolutionException(string name, Assembly scope) : base($"Unable to find module {name} in assembly {scope}")
    {
        Name = name;
        Assembly = scope;
    }

}

public sealed class AssemblyResolutionException : WeberknechtException
{

    public AssemblyName Name { get; }

    internal AssemblyResolutionException(AssemblyName name) : base($"Unable to find assembly {name}")
    {
        Name = name;
    }

}