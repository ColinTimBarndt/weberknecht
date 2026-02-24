using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Weberknecht;

public partial class Method
{

    public static Method Read(Delegate d) => Read(d.Method);

    public static Method Read(MethodInfo method)
    {
        var body = method.GetMethodBody()
            ?? throw new InvalidOperationException("Method has no available body");
        var assembly = method.Module.Assembly;
        var metadata = assembly.GetMetadataReader()
            ?? throw new InvalidOperationException("Cannot read assembly metadata");
        var ilBytes = body.GetILAsByteArray()
            ?? throw new InvalidOperationException("Method has no body");

        var gctx = new GenericContext(method.DeclaringType?.GetGenericArguments() ?? [], method.GetGenericArguments());
        var ctx = new ResolutionContext(method.Module, metadata, gctx);

        Method instance = new(method.ReturnType);

        List<PseudoInstruction> instructions = instance._instructions;
        Dictionary<int, int> jumpTable = [];

        var il = new InstructionDecoder(ilBytes, ctx);
        while (il.MoveNext())
        {
            instructions.Add(default(Label)); // Placeholder
            jumpTable.Add(il.CurrentAddress, instructions.Count);
            instructions.Add(il.Current);
        }

        instructions.Add(default(Label)); // Placeholder
        jumpTable.Add(il.CurrentAddress, instructions.Count);

        Span<PseudoInstruction> instructionsSpan = CollectionsMarshal.AsSpan(instructions);

        // Assigns values to the interleaved labels when used
        int lastLabel = 0;
        for (int i = 1; i < instructionsSpan.Length; i += 2)
        {
            ref var instr = ref instructionsSpan[i].AsInstructionRef();

            if (instr.OpCode.OperandType is OperandType.InlineSwitch)
            {
                var offsets = (ImmutableArray<int>)instr._operand!;
                instr._operand = Internal_ReadGetLabelArray(ref lastLabel, instructionsSpan, jumpTable, offsets);
                continue;
            }

            if (instr.OpCode.OperandType is not OperandType.InlineBrTarget and not OperandType.ShortInlineBrTarget)
                continue;

            instr._uoperand.@int = (int)Internal_ReadGetLabel(ref lastLabel, instructionsSpan, jumpTable, offset: instr._uoperand.@int);
        }

        if (body.ExceptionHandlingClauses.Count > 0)
        {
            List<ExceptionHandlingClause> clauses = [];
            instance._exceptionHandlers = clauses;

            foreach (var clause in body.ExceptionHandlingClauses)
            {
                LabelRange @try = Internal_ReadGetLabelRange(ref lastLabel, instructionsSpan, jumpTable, clause.TryOffset, clause.TryLength);
                LabelRange handler = Internal_ReadGetLabelRange(ref lastLabel, instructionsSpan, jumpTable, clause.HandlerOffset, clause.HandlerLength);

                clauses.Add(clause.Flags switch
                {
                    ExceptionHandlingClauseOptions.Clause => ExceptionHandlingClause.Clause(clause.CatchType!, @try, handler),
                    ExceptionHandlingClauseOptions.Filter => ExceptionHandlingClause.Filter(
                        Internal_ReadGetLabel(ref lastLabel, instructionsSpan, jumpTable, clause.FilterOffset),
                        @try, handler
                    ),
                    ExceptionHandlingClauseOptions.Finally => ExceptionHandlingClause.Finally(@try, handler),
                    ExceptionHandlingClauseOptions.Fault => ExceptionHandlingClause.Fault(@try, handler),
                    _ => throw new NotImplementedException(Enum.GetName(clause.Flags)),
                });
            }
        }

        var debugMetadata = assembly.GetDebugMetadataReader();
        Dictionary<int, string>? localNames = null;

        if (debugMetadata != null)
        {
            Dictionary<DocumentHandle, Metadata.Document> documents = [];
            var debugInfo = debugMetadata.GetMethodDebugInformation(method.MetadataHandle);

            localNames = debugMetadata.GetLocalNames(method.MetadataHandle);

            foreach (var point in debugInfo.GetSequencePoints())
            {
                if (!jumpTable.TryGetValue(point.Offset, out int index))
                    continue;

                ref var instr = ref instructionsSpan[index].AsInstructionRef();
                if (!documents.TryGetValue(point.Document, out var document))
                {
                    document = Metadata.Document.FromMetadata(debugMetadata, point.Document);
                    documents[point.Document] = document;
                }
                instr.DebugInfo = new(document, point.StartLine, point.StartColumn, point.EndLine, point.EndColumn);
            }
        }

        instructionsSpan = default; // Ensure span is not used
        // Remove unused interleaved labels
        instructions.RemoveAll(instr => instr.Type == PseudoInstructionType.Label && instr.AsLabel().IsNull);

        instance._localVariables.EnsureCapacity(body.LocalVariables.Count);
        for (int i = 0; i < body.LocalVariables.Count; i++)
        {
            var local = body.LocalVariables[i];
            if (local.LocalIndex != i) throw new UnreachableException("local index does not match index in locals");
            var name = localNames?.GetValueOrDefault(i);
            instance._localVariables.Add(new(local.LocalType, local.IsPinned, name));
        }

        var parameterInfos = method.GetParameters();
        bool implicitThis = method.CallingConvention.HasFlag(CallingConventions.HasThis) && !method.CallingConvention.HasFlag(CallingConventions.ExplicitThis);
        instance._parameters.EnsureCapacity(parameterInfos.Length + (implicitThis ? 1 : 0));

        // ECMA-335 1.8.6.1.5
        if (implicitThis)
        {
            Type declType = method.DeclaringType!;
            if (declType.IsValueType)
                declType = declType.MakeByRefType();
            instance._parameters.Add(new(null, declType, ParameterModifier.None));
        }

        for (int i = 0; i < parameterInfos.Length; i++)
            instance._parameters.Add(parameterInfos[i]);

        instance._genericParameters.AddRange(
            from arg in method.GetGenericArguments()
            select GenericParameter.FromTypeParameter(arg)
        );
        instance.LabelCount = lastLabel;

        return instance;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Label Internal_ReadGetLabel(
        ref int lastLabel,
        Span<PseudoInstruction> instructions,
        Dictionary<int, int> jumpTable,
        int offset)
    {
        ref var target = ref instructions[jumpTable[offset] - 1].AsLabelRef();

        return target.IsNull ? target = (Label)(++lastLabel) : target;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static LabelRange Internal_ReadGetLabelRange(
        ref int lastLabel,
        Span<PseudoInstruction> instructions,
        Dictionary<int, int> jumpTable,
        int offset,
        int length)
    {
        var start = Internal_ReadGetLabel(ref lastLabel, instructions, jumpTable, offset);
        var end = Internal_ReadGetLabel(ref lastLabel, instructions, jumpTable, offset + length);
        return (start, end);
    }

    private static ImmutableArray<Label> Internal_ReadGetLabelArray(
        ref int lastLabel,
        Span<PseudoInstruction> instructions,
        Dictionary<int, int> jumpTable,
        ImmutableArray<int> offsets)
    {
        Span<Label> targets = stackalloc Label[offsets.Length];

        for (int i = 0; i < offsets.Length; i++)
            targets[i] = Internal_ReadGetLabel(ref lastLabel, instructions, jumpTable, offsets[i]);

        return targets.ToImmutableArray();
    }
}
