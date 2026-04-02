using System.Collections.ObjectModel;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;
using Weberknecht.Metadata;

namespace Weberknecht;

using RLabel = System.Reflection.Emit.Label;
using LabelAddressMap = LabelMap<int>;

public partial class Method(Type returnType)
{

    private readonly List<GenericParameter> _genericParameters = [];
    private readonly List<Parameter> _parameters = [];
    private readonly List<LocalVariable> _localVariables = [];
    private readonly List<Instruction> _instructions = [];
    private List<ExceptionHandlingClause>? _exceptionHandlers;
    public int LabelCount { get; private set; }

    public Type ReturnType { get; } = returnType;

    public InstructionCollection Instructions => new(this);

    public ReadOnlyCollection<ExceptionHandlingClause> ExceptionHandlers => _exceptionHandlers?.AsReadOnly() ?? [];

    public struct GenericParameter(string name, GenericParameterAttributes attributes)
    {

        public string Name { get; set; } = name;
        public GenericParameterAttributes Attributes { get; set; } = attributes;
        private Type? _baseTypeConstraint = null;

        public Type? BaseTypeConstraint
        {
            readonly get => _baseTypeConstraint;
            set
            {
                if (value != null && !value.IsClass)
                    throw new ArgumentException("Base type is not a class", nameof(value));
                _baseTypeConstraint = value;
            }
        }

        private readonly List<Type> _interfaceConstraints = [];

        public readonly ReadOnlyCollection<Type> InterfaceConstraints => _interfaceConstraints.AsReadOnly();

        public override readonly string ToString() => Name;

        public static GenericParameter FromTypeParameter(Type type)
        {
            if (!type.IsGenericMethodParameter)
                throw new ArgumentException("Not a generic method parameter", nameof(type));

            GenericParameter instance = new(type.Name, type.GenericParameterAttributes);
            var constraints = type.GetGenericParameterConstraints();
            var baseType = constraints.FirstOrDefault(ty => !ty.IsInterface);
            instance.BaseTypeConstraint = baseType;
            instance._interfaceConstraints.AddRange(
                from ty in constraints
                where ty.IsInterface
                select ty
            );
            return instance;
        }

    }

    public readonly struct Parameter(string? name, Type type, ParameterModifier mod)
    {
        public string? Name { get; } = name;
        public Type Type { get; } = type;
        public ParameterModifier Modifier { get; } = mod;

        public static implicit operator Parameter(ParameterInfo info)
        {
            ParameterModifier mod = ParameterModifier.None;
            if (info.IsIn) mod = ParameterModifier.In;
            else if (info.IsOut) mod = ParameterModifier.Out;
            return new(info.Name, info.GetModifiedParameterType(), mod);
        }

        public override string ToString()
        {
            var prefix = Modifier switch
            {
                ParameterModifier.None when Type.IsByRef => "ref ",
                ParameterModifier.In => "in ",
                ParameterModifier.Out => "out ",
                _ => null
            };
            return Name == null ? $"{prefix}{Type.Name}" : $"{prefix}{Type.Name} {Name}";
        }
    }

    public readonly struct LocalVariable(Type type, bool pinned, string? name = null)
    {
        public Type Type { get; } = type;
        public bool IsPinned { get; } = pinned;
        public string? Name { get; } = name;

        public override string ToString()
        {
            StringBuilder str = new();
            if (IsPinned) str.Append("pinned ");
            str.Append(Type.Name);
            if (Name != null) str.Append(' ').Append(Name);
            return str.ToString();
        }
    }

    public enum ParameterModifier
    {
        None = 0,
        In,
        Out,
    }

    public Label CreateLabel() => new(++LabelCount);

    public MethodBuilder DefineMethod(ModuleBuilder module, string name, MethodAttributes attributes)
    {
        var method = module.DefineGlobalMethod(name, attributes, ReturnType, [.. _parameters.Select(p => p.Type)]);
        BuildMethod(method);
        return method;
    }

    public MethodBuilder DefineMethod(TypeBuilder type, string name, MethodAttributes attributes)
    {
        var method = type.DefineMethod(name, attributes, ReturnType, [.. _parameters.Select(p => p.Type)]);
        BuildMethod(method);
        return method;
    }

    private void BuildMethod(MethodBuilder method)
    {
        if (_genericParameters.Count != 0)
        {
            var names = new string[_genericParameters.Count];
            for (int i = 0; i < names.Length; i++)
                names[i] = _genericParameters[i].Name;
            var builders = method.DefineGenericParameters(names);
            for (int i = 0; i < builders.Length; i++)
            {
                var builder = builders[i];
                var param = _genericParameters[i];
                builder.SetGenericParameterAttributes(param.Attributes);
                builder.SetBaseTypeConstraint(param.BaseTypeConstraint);
                builder.SetInterfaceConstraints([.. param.InterfaceConstraints]);
            }
        }

        Emit(method.GetILGenerator());
    }

    public DynamicMethod CreateDynamicMethod(string name)
    {
        if (_genericParameters.Count != 0)
            throw new InvalidOperationException("Dynamic methods can't be generic");

        var method = new DynamicMethod(name, ReturnType, [.. _parameters.Select(p => p.Type)], restrictedSkipVisibility: true);
        //var method = new DynamicMethod(name, ReturnType, [.. _parameters.Select(p => p.Type)]);

        var info = method.GetDynamicILInfo();

        var tokens = TokenSource.Create(info);

        ReadOnlySpan<ExceptionHandlingClause> exceptionHandlers = CollectionsMarshal.AsSpan(_exceptionHandlers);

        LabelAddressMap labels = stackalloc int[LabelCount];
        int maxStackSize = ExecutionFlowAnalyzer.GetMaxStackSize(CollectionsMarshal.AsSpan(_instructions), exceptionHandlers, LabelCount, ReturnType != typeof(void))
            .MaxStackSizeOrThrow();
        info.SetCode(EncodeBody(labels, tokens), maxStackSize);

        info.SetLocalSignature(EncodeLocalSignature());

        if (_exceptionHandlers != null)
            info.SetExceptions(ExceptionHandlingClause.EncodeExceptionHandlers(exceptionHandlers, labels, tokens));

        return method;
    }

    public int GetMaxStackSize()
    {
        ReadOnlySpan<ExceptionHandlingClause> exceptionHandlers = CollectionsMarshal.AsSpan(_exceptionHandlers);
        return ExecutionFlowAnalyzer.GetMaxStackSize(
            CollectionsMarshal.AsSpan(_instructions),
            exceptionHandlers,
            LabelCount,
            ReturnType != typeof(void)
        ).MaxStackSizeOrThrow();
    }

    private void Emit(ILGenerator il)
    {
        Span<RLabel> labels = stackalloc RLabel[LabelCount];
        for (int i = 0; i < LabelCount; i++)
            labels[i] = il.DefineLabel();

        Dictionary<Label, ExceptionHandlingClause> startClauses = [];
        if (_exceptionHandlers != null)
        {
            foreach (var clause in _exceptionHandlers)
                startClauses[clause.Try.Start] = clause;
        }

        LocalBuilder[] locals = new LocalBuilder[_localVariables.Count];
        for (int i = 0; i < locals.Length; i++)
        {
            var localDef = _localVariables[i];
            locals[i] = il.DeclareLocal(localDef.Type, localDef.IsPinned);
        }

        Span<Instruction> instrs = CollectionsMarshal.AsSpan(_instructions);
        ExceptionHandlingClauseHelper clauseHelper = new(CollectionsMarshal.AsSpan(_exceptionHandlers));
        foreach (ref var instr in instrs)
        {
            var label = instr.Label;
            if (!label.IsNull)
            {
                clauseHelper.OnMarkLabel(label, il);
                il.MarkLabel(labels[(int)label - 1]);
            }

            instr.Emit(il, labels, locals);
        }
    }

    internal byte[] EncodeBody<T>(LabelAddressMap labels, T tokens)
    where T : ITokenSource
    {
        var instrs = CollectionsMarshal.AsSpan(_instructions);

        int size = 0;
        foreach (ref var instr in instrs)
            size += instr.EncodedSize;

        byte[] buffer = new byte[size];

        InstructionEncoder<T> encoder = new(buffer.AsSpan(), tokens);

        foreach (var instr in instrs)
        {
            var label = instr.Label;
            if (!label.IsNull)
                labels[label] = encoder.CurrentAddress;

            encoder.Emit(instr);
        }

        encoder.WriteLabels(labels);

        return buffer;
    }

    public override string ToString() => ToString(false);

    public string ToString(bool debugInfo)
    {
        StringBuilder builder = new();
        builder.Append(ReturnType.Name)
            .Append(" Method");

        if (_genericParameters.Count > 0)
        {
            builder.Append('<')
                .AppendJoin(", ", _genericParameters)
                .Append('>');
        }

        builder.Append('(')
            .AppendJoin(", ", _parameters)
            .Append(')');
        if (_instructions.Count == 0)
            builder.Append("\n    <empty>");
        else
        {
            if (_localVariables.Count > 0)
            {
                builder.Append("\n.locals (")
                    .AppendJoin(", ", _localVariables)
                    .Append(')');
            }
            if (_exceptionHandlers != null)
            {
                builder.Append('\n')
                    .AppendJoin('\n', _exceptionHandlers);
            }
            foreach (ref var instr in CollectionsMarshal.AsSpan(_instructions))
            {
                if (debugInfo && instr.DebugInfo is SequencePoint seq)
                {
                    builder.Append("\n@ ").Append(seq);
                }

                var label = instr.Label;
                if (!label.IsNull)
                    builder.Append('\n').AppendLabel(label).Append(':');

                builder.Append("\n    ");
                builder.Append(in instr);
            }
        }
        return builder.ToString();
    }

}
