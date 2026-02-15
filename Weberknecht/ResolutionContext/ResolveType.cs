using System.Reflection.Metadata;

namespace Weberknecht;

internal sealed partial class ResolutionContext
{

    public Type ResolveTypeHandle(EntityHandle handle) => handle.Kind switch
    {
        HandleKind.TypeDefinition => ResolveTypeHandle((TypeDefinitionHandle)handle),
        HandleKind.TypeReference => ResolveTypeHandle((TypeReferenceHandle)handle),
        HandleKind.TypeSpecification => ResolveTypeHandle((TypeSpecificationHandle)handle),
        _ => throw new InvalidOperationException(handle.Kind.ToString()),
    };

    public Type ResolveTypeHandle(TypeDefinitionHandle handle)
        => ResolveType(Meta.GetTypeDefinition(handle));

    public Type ResolveType(TypeDefinition typeDef)
    {
        var decl = typeDef.GetDeclaringType();
        string name;
        if (decl.IsNil)
        {
            name = Meta.GetString(typeDef.Name);
        }
        else
        {
            var nestedName = new Stack<string>();
            while (true)
            {
                nestedName.Push(Meta.GetString(typeDef.Name));
                if (decl.IsNil)
                    break;
                typeDef = Meta.GetTypeDefinition(decl);
                decl = typeDef.GetDeclaringType();
            }
            name = string.Join('+', nestedName);
        }
        return GetDeclaredType(Meta.GetString(typeDef.Namespace), name);
    }

    public Type ResolveTypeHandle(TypeReferenceHandle handle)
        => ResolveType(Meta.GetTypeReference(handle));

    public Type ResolveType(TypeReference typeRef)
    {
        string name;

        if (typeRef.ResolutionScope.Kind == HandleKind.TypeReference)
        {
            // Nested
            var nestedName = new Stack<string>();
            while (true)
            {
                nestedName.Push(Meta.GetString(typeRef.Name));

                if (typeRef.ResolutionScope.Kind != HandleKind.TypeReference)
                    break;

                typeRef = Meta.GetTypeReference((TypeReferenceHandle)typeRef.ResolutionScope);
            }
            name = string.Join('+', nestedName);
        }
        else
        {
            name = Meta.GetString(typeRef.Name);
        }

        var ns = Meta.GetString(typeRef.Namespace);
        switch (typeRef.ResolutionScope.Kind)
        {
            case HandleKind.ModuleDefinition:
                {
                    var module = ResolveModuleHandle((ModuleDefinitionHandle)typeRef.ResolutionScope);
                    return GetDeclaredType(ns, name, module);
                }

            case HandleKind.ModuleReference:
                {
                    var module = ResolveModuleHandle((ModuleReferenceHandle)typeRef.ResolutionScope);
                    return GetDeclaredType(ns, name, module);
                }

            case HandleKind.AssemblyDefinition:
                return GetDeclaredType(ns, name);

            case HandleKind.AssemblyReference:
                {
                    var asm = ResolveAssemblyHandle((AssemblyReferenceHandle)typeRef.ResolutionScope);
                    return GetDeclaredType(ns, name, asm);
                }

            default:
                throw new NotImplementedException(typeRef.ResolutionScope.Kind.ToString());

        }
    }

    public Type ResolveTypeHandle(TypeSpecificationHandle handle)
        => ResolveType(Meta.GetTypeSpecification(handle));

    public Type ResolveType(TypeSpecification spec)
        => spec.DecodeSignature(this, _gctx);

}