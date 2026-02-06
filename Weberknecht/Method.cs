using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

namespace Weberknecht;

public class Method
{

    private readonly List<Parameter> _parameters;
    private readonly List<Instruction> _instructions;
    public Type ReturnType { get; }

    // TODO: Temp
    internal MetadataReader? metadata;

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

    public override string ToString() => ToString(false);

    public string ToString(bool debugInfo)
    {
        StringBuilder builder = new();
        builder.Append(ReturnType.Name)
            .Append(" Method(")
            .AppendJoin(", ", _parameters)
            .Append(')');
        if (_instructions.Count == 0)
            builder.Append("\n        <empty>");
        else
        {
            foreach (var instr in _instructions)
            {
                if (debugInfo && metadata != null && instr.DebugInfo.HasValue)
                {
                    var seq = instr.DebugInfo.Value;
                    if (seq.IsHidden)
                    {
                        builder.Append("\n  @ <hidden>");
                        continue;
                    }

                    var name = metadata.GetDocument(seq.Document).Name;
                    builder.Append("\n  @ ")
                        .Append(metadata.GetDocumentName(name))
                        .Append(':')
                        .Append(seq.StartLine)
                        .Append(':')
                        .Append(seq.StartColumn)
                        .Append(" - ")
                        .Append(seq.EndLine)
                        .Append(':')
                        .Append(seq.EndColumn);
                }
                builder.AppendLine()
                    .Append(instr.ToString(true));
            }
        }
        return builder.ToString();
    }

}