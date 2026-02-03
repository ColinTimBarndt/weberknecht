using System.Reflection;
using System.Text;

namespace Weberknecht;

public class Method
{

    private readonly List<Parameter> _parameters;
    private readonly List<Instruction> _instructions;
    public Type ReturnType { get; }

    public Method(Type returnType) : this(returnType, [], []) { }

    internal Method(Type returnType, List<Parameter> parameters, List<Instruction> instrs)
    {
        _parameters = parameters;
        _instructions = instrs;
        ReturnType = returnType;
    }

    public readonly struct Parameter(string? name, Type type, ParameterModifier mod)
    {
        readonly string? Name { get; } = name;
        readonly Type Type { get; } = type;
        readonly ParameterModifier Modifier { get; } = mod;

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

    public enum ParameterModifier
    {
        None = 0,
        In,
        Out,
    }

    public override string ToString()
    {
        StringBuilder builder = new();
        builder.Append(ReturnType.Name);
        builder.Append(" Method(");
        builder.AppendJoin(", ", _parameters);
        builder.Append(")\n");
        if (_instructions.Count == 0)
            builder.Append("        <empty>");
        else
            builder.AppendJoin("\n", _instructions.Select(instr => instr.ToString(true)));
        return builder.ToString();
    }

}