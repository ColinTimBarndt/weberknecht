using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;

namespace Weberknecht;

public static class MethodReader
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

        List<PseudoInstruction> instructions = [];
        Dictionary<int, int> jumpTable = [];

        var il = new InstructionDecoder(ilBytes, ctx);
        while (il.MoveNext())
        {
            instructions.Add(PseudoInstruction.Label(0)); // Placeholder
            jumpTable.Add(il.CurrentAddress, instructions.Count);
            instructions.Add(il.Current);
        }

        Span<PseudoInstruction> instructionsSpan = CollectionsMarshal.AsSpan(instructions);

        // Assigns values to the interleaved labels when used
        int lastLabel = 0;
        for (int i = 1; i < instructionsSpan.Length; i += 2)
        {
            ref var instr = ref instructionsSpan[i].AsInstructionRef();
            if (instr.OpCode.OperandType is not OperandType.InlineBrTarget and not OperandType.ShortInlineBrTarget)
                continue;

            var targetIndex = jumpTable[instr._uoperand.@int];
            ref var target = ref instructionsSpan[targetIndex - 1].AsLabelRef();
            if (target == 0)
            {
                // New label
                instr._uoperand.@int = ++lastLabel;
                target = lastLabel;
            }
            else
            {
                instr._uoperand.@int = target;
            }
        }

        var debugMetadata = assembly.GetDebugMetadataReader();
        Dictionary<int, string>? localNames = null;

        if (debugMetadata != null)
        {
            Dictionary<DocumentHandle, Metadata.Document> documents = [];
            var debugInfo = debugMetadata.GetMethodDebugInformation(method.MetadataHandle);

            localNames = GetLocalNames(ctx, gctx, debugMetadata, method.MetadataHandle);

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
        instructions.RemoveAll(instr => instr.Type == PseudoInstructionType.Label && instr.AsLabel() == 0);

        List<Method.LocalVariable> localVariables = new(body.LocalVariables.Count);
        for (int i = 0; i < body.LocalVariables.Count; i++)
        {
            var local = body.LocalVariables[i];
            if (local.LocalIndex != i) throw new UnreachableException("local index does not match index in locals");
            var name = localNames?.GetValueOrDefault(i);
            localVariables.Add(new(local.LocalType, local.IsPinned, name));
        }

        var parameterInfos = method.GetParameters();
        List<Method.Parameter> parameters = new(parameterInfos.Length + (method.IsStatic ? 0 : 1));

        if (!method.IsStatic)
            parameters.Add(new(null, method.DeclaringType!, Method.ParameterModifier.None));

        for (int i = 0; i < parameterInfos.Length; i++)
            parameters.Add(parameterInfos[i]);

        return new(
            method.ReturnType,
            [.. method.GetGenericArguments()],
            parameters,
            localVariables,
            instructions,
            lastLabel
        );
    }

    private static Dictionary<int, string> GetLocalNames(ResolutionContext ctx, GenericContext gctx, MetadataReader debugMetadata, MethodDefinitionHandle methodHandle)
    {
        Dictionary<int, string> names = [];

        foreach (var scopeHandle in debugMetadata.GetLocalScopes(methodHandle))
        {
            var scope = debugMetadata.GetLocalScope(scopeHandle);

            foreach (var localHandle in scope.GetLocalVariables())
            {
                var local = debugMetadata.GetLocalVariable(localHandle);
                if (local.Name.IsNil) continue;

                names.Add(local.Index, debugMetadata.GetString(local.Name));
            }
        }

        return names;
    }

}