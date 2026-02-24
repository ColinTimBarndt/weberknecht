using System.Reflection;
using System.Reflection.Metadata;

namespace Weberknecht;

internal sealed partial class ResolutionContext(Module module, MetadataReader meta, GenericContext gctx)
{

    public Module Module { get; } = module;
    public Assembly Assembly { get; } = module.Assembly;
    public MetadataReader Meta { get; } = meta;
    private readonly GenericContext _gctx = gctx;

    private Type GetDeclaredType(string? ns, string name)
        => GetDeclaredType(ns, name, Assembly);

    private static Type GetDeclaredType(string? ns, string name, Assembly scope)
    {
        if (string.IsNullOrEmpty(ns))
            return scope.GetType(name) ?? throw new TypeResolutionException(null, name, scope);
        else
            return scope.GetType($"{ns}.{name}") ?? throw new TypeResolutionException(ns, name, scope);
    }

    private static Type GetDeclaredType(string? ns, string name, Module scope)
    {
        if (string.IsNullOrEmpty(ns))
            return scope.GetType(name) ?? throw new TypeResolutionException(null, name, scope);
        else
            return scope.GetType($"{ns}.{name}") ?? throw new TypeResolutionException(ns, name, scope);
    }

    public Module ResolveModuleHandle(ModuleDefinitionHandle _)
        => ResolveModule(Meta.GetModuleDefinition(/*handle*/)); // TODO: Why does it not take a handle?

    public Module ResolveModule(ModuleDefinition def)
    {
        var name = Meta.GetString(def.Name);
        return Assembly.GetModule(name) ?? throw new ModuleResolutionException(name, Assembly);
    }

    public Module ResolveModuleHandle(ModuleReferenceHandle handle)
        => ResolveModule(Meta.GetModuleReference(handle));

    public Module ResolveModule(ModuleReference moduleRef)
    {
        var name = Meta.GetString(moduleRef.Name);
        // TODO: Is this correct?
        return Assembly.GetModule(name) ?? throw new ModuleResolutionException(name, Assembly);
    }

    public Assembly ResolveAssemblyHandle(AssemblyReferenceHandle handle)
        => ResolveAssembly(Meta.GetAssemblyReference(handle));

    public static Assembly ResolveAssembly(AssemblyReference asmRef)
    {
        var name = asmRef.GetAssemblyName();
        return AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(asm => AssemblyName.ReferenceMatchesDefinition(asm.GetName(), name))
            ?? throw new AssemblyResolutionException(name);
    }

}
