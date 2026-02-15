using System.Reflection;
using System.Text;

namespace Weberknecht;

public class Method
{

    private readonly List<Type> _genericArguments;
    private readonly List<Parameter> _parameters;
    private readonly List<LocalVariable> _localVariables;
    private readonly List<Instruction> _instructions;
    public Type ReturnType { get; }

    public Method(Type returnType) : this(returnType, [], [], [], []) { }

    internal Method(Type returnType, List<Type> genericArguments, List<Parameter> parameters, List<LocalVariable> localVariables, List<Instruction> instrs)
    {
        _genericArguments = genericArguments;
        _parameters = parameters;
        _localVariables = localVariables;
        _instructions = instrs;
        ReturnType = returnType;
    }

    public readonly struct Parameter(string? name, Type type, ParameterModifier mod)
    {
        string? Name { get; } = name;
        Type Type { get; } = type;
        ParameterModifier Modifier { get; } = mod;

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
        Type Type { get; } = type;
        bool IsPinned { get; } = pinned;
        string? Name { get; } = name;

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
            builder.Append("\n        <empty>");
        else
        {
            if (_localVariables.Count > 0)
            {
                builder.Append("\n.locals (")
                    .AppendJoin(", ", _localVariables)
                    .Append(')');
            }
            foreach (var instr in _instructions)
            {
                if (debugInfo && instr.DebugInfo.HasValue)
                {
                    var seq = instr.DebugInfo.Value;
                    builder.Append($"\n  @ {seq}");
                }
                builder.AppendLine()
                    .Append(instr.ToString(true));
            }
        }
        return builder.ToString();
    }

}