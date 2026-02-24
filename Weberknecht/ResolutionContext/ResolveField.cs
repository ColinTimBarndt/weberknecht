using System.Reflection;
using System.Reflection.Metadata;

namespace Weberknecht;

internal sealed partial class ResolutionContext
{

    public FieldInfo ResolveFieldHandle(MemberReferenceHandle handle)
        => ResolveField(Meta.GetMemberReference(handle));

    public FieldInfo ResolveField(MemberReference memberRef)
    {
        var type = ResolveTypeHandle(memberRef.Parent);

        var ctx = new GenericContext(type.GetGenericArguments(), 0);
        var fieldType = memberRef.DecodeFieldSignature(this, ctx);
        var fieldName = Meta.GetString(memberRef.Name);
        var field = type.GetField(fieldName)
            ?? throw new FieldResolutionException(type, fieldName, fieldType);

        if (field.FieldType != fieldType)
            throw new FieldResolutionException(type, fieldName, fieldType);

        return field;
    }

    public FieldInfo ResolveFieldHandle(FieldDefinitionHandle handle)
        => ResolveField(Meta.GetFieldDefinition(handle));

    public FieldInfo ResolveField(FieldDefinition fieldDef)
    {
        var typeDef = Meta.GetTypeDefinition(fieldDef.GetDeclaringType());
        var type = ResolveType(typeDef);

        var ctx = new GenericContext(type.GetGenericArguments(), 0);
        var fieldType = fieldDef.DecodeSignature(this, ctx);
        var fieldName = Meta.GetString(fieldDef.Name);
        var field = type.GetField(fieldName)
            ?? throw new FieldResolutionException(type, fieldName, fieldType);

        if (field.FieldType != fieldType)
            throw new FieldResolutionException(type, fieldName, fieldType);

        return field;
    }

}
