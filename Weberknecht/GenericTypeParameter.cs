using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace Weberknecht;

internal sealed class GenericTypeParameterType(int position) : Type
{
    public override int GenericParameterPosition { get; } = position;

    public override bool IsGenericMethodParameter => false;

    public override bool IsGenericParameter => true;

    public override Assembly Assembly => throw new NotSupportedException();

    public override string? AssemblyQualifiedName => throw new NotSupportedException();

    public override Type? BaseType => null;

    public override string? FullName => null;

    public override Guid GUID => throw new NotSupportedException();

    public override Module Module => throw new NotSupportedException();

    public override string? Namespace => throw new NotSupportedException();

    public override Type UnderlyingSystemType => throw new NotSupportedException();

    public override string Name => $"T{GenericParameterPosition}";

    public override ConstructorInfo[] GetConstructors(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    public override object[] GetCustomAttributes(bool inherit)
    {
        throw new NotSupportedException();
    }

    public override object[] GetCustomAttributes(Type attributeType, bool inherit)
    {
        throw new NotSupportedException();
    }

    public override Type? GetElementType() => null;

    public override EventInfo? GetEvent(string name, BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    public override EventInfo[] GetEvents(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    public override FieldInfo? GetField(string name, BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    public override FieldInfo[] GetFields(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
    public override Type? GetInterface(string name, bool ignoreCase)
    {
        throw new NotSupportedException();
    }

    public override Type[] GetInterfaces()
    {
        throw new NotSupportedException();
    }

    public override MemberInfo[] GetMembers(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    public override MethodInfo[] GetMethods(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    public override Type? GetNestedType(string name, BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    public override Type[] GetNestedTypes(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    public override PropertyInfo[] GetProperties(BindingFlags bindingAttr)
    {
        throw new NotSupportedException();
    }

    public override object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
    {
        throw new NotSupportedException();
    }

    public override bool IsDefined(Type attributeType, bool inherit) => false;

    protected override TypeAttributes GetAttributeFlagsImpl() => default;

    protected override ConstructorInfo? GetConstructorImpl(BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[] types, ParameterModifier[]? modifiers)
    {
        return null;
    }

    protected override MethodInfo? GetMethodImpl(string name, BindingFlags bindingAttr, Binder? binder, CallingConventions callConvention, Type[]? types, ParameterModifier[]? modifiers)
    {
        return null;
    }

    protected override PropertyInfo? GetPropertyImpl(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[]? types, ParameterModifier[]? modifiers)
    {
        return null;
    }

    protected override bool HasElementTypeImpl() => false;

    protected override bool IsArrayImpl() => false;

    protected override bool IsByRefImpl() => false;

    protected override bool IsCOMObjectImpl() => false;

    protected override bool IsPointerImpl() => false;

    protected override bool IsPrimitiveImpl() => false;
}