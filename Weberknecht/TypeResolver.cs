using System.Reflection;

namespace Weberknecht;

public sealed class TypeResolver
{
    private static readonly Dictionary<AssemblyName, WeakReference<Assembly>> _loadedAssemblies;

    static TypeResolver()
    {
        _loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToDictionary(
            asm => asm.GetName(),
            asm => new WeakReference<Assembly>(asm)
        );
        AppDomain.CurrentDomain.AssemblyLoad += (_, args) =>
        {
            _loadedAssemblies.Add(args.LoadedAssembly.GetName(), new(args.LoadedAssembly));
        };
    }

    private readonly List<Assembly> sources = [];

    public TypeResolver(Assembly asm)
    {
        sources.Add(asm);
        foreach (var refName in asm.GetReferencedAssemblies())
        {
            if (_loadedAssemblies.TryGetValue(refName, out var weak) && weak.TryGetTarget(out var assembly))
                sources.Add(assembly);
        }
    }

    public Type? GetType(string? ns, string name)
    {
        if (string.IsNullOrEmpty(ns))
            return GetType(name);
        else
            return GetType($"{ns}.{name}");
    }

    public Type? GetType(string fullName)
    {
        foreach (var asm in sources)
        {
            var found = asm.GetType(fullName);
            if (found != null)
                return found;
        }
        return null;
    }
}