using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace Weberknecht;

public class Method
{

    private readonly List<Type> _genericArguments;
    private readonly List<Parameter> _parameters;
    private readonly List<LocalVariable> _localVariables;
    private readonly List<PseudoInstruction> _instructions;
    private int _labelCount;
    public Type ReturnType { get; }

    public Method(Type returnType) : this(returnType, [], [], [], [], 0) { }

    internal Method(Type returnType, List<Type> genericArguments, List<Parameter> parameters, List<LocalVariable> localVariables, List<PseudoInstruction> instrs, int labelCount)
    {
        _genericArguments = genericArguments;
        _parameters = parameters;
        _localVariables = localVariables;
        _instructions = instrs;
        ReturnType = returnType;
        _labelCount = labelCount;
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

    public DynamicMethod MakeDynamicMethod(string name)
    {
        if (_genericArguments.Count != 0)
            throw new InvalidOperationException("Dynamic methods can't be generic");

        var method = new DynamicMethod(name, ReturnType, [.. _parameters.Select(p => p.Type)], restrictedSkipVisibility: true);
        Emit(method.GetILGenerator());
        return method;
    }

    public void Emit(ILGenerator il)
    {
        Label[] labels = new Label[_labelCount];
        for (int i = 0; i < _labelCount; i++)
            labels[i] = il.DefineLabel();

        LocalBuilder[] locals = new LocalBuilder[_localVariables.Count];
        for (int i = 0; i < locals.Length; i++)
        {
            var localDef = _localVariables[i];
            locals[i] = il.DeclareLocal(localDef.Type, localDef.IsPinned);
        }

        Span<PseudoInstruction> instrs = CollectionsMarshal.AsSpan(_instructions);
        foreach (ref var instr in instrs)
        {
            switch (instr.Type)
            {
                case PseudoInstructionType.Instruction:
                    instr.AsInstructionRef().Emit(il, labels, locals);
                    continue;

                case PseudoInstructionType.Label:
                    il.MarkLabel(labels[instr.AsLabelRef() - 1]);
                    continue;

                default:
                    throw new NotImplementedException(); // TODO
            }
        }
    }

    public override string ToString() => ToString(false);

    public string ToString(bool debugInfo)
    {
        StringBuilder builder = new();
        builder.Append(ReturnType.Name)
            .Append(" Method");

        if (_genericArguments.Count > 0)
        {
            builder.Append('<')
                .AppendJoin(", ", _genericArguments)
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
            foreach (var pinstr in _instructions)
            {
                if (pinstr.Type is PseudoInstructionType.Instruction)
                {
                    var instr = pinstr.AsInstruction();
                    if (debugInfo && instr.DebugInfo is Metadata.SequencePoint seq)
                    {
                        builder.Append($"\n@ {seq}");
                    }
                    builder.Append("\n    ");
                }
                else
                {
                    builder.Append('\n');
                }
                builder.Append(in pinstr);
            }
        }
        return builder.ToString();
    }

}