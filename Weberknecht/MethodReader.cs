using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

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

        List<Instruction> instructions = [];
        Dictionary<int, int> jumpTable = [];

        var il = new InstructionDecoder(ilBytes, ctx);
        while (il.MoveNext())
        {
            jumpTable.Add(il.CurrentAddress, instructions.Count);
            instructions.Add(il.Current);
        }

        ushort lastLabel = 0;
        for (int i = 0; i < instructions.Count; i++)
        {
            var instr = instructions[i];
            if (instr.OpCode.OperandType is not OperandType.InlineBrTarget and not OperandType.ShortInlineBrTarget)
                continue;

            var targetIndex = jumpTable[(int)instr._operand!];
            var target = instructions[targetIndex];
            if (target._label == 0)
            {
                target._label = ++lastLabel;
                instructions[targetIndex] = target;
            }
            instr._operand = lastLabel;
            instructions[i] = instr;
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

                var instr = instructions[index];
                if (!documents.TryGetValue(point.Document, out var document))
                {
                    document = Metadata.Document.FromMetadata(debugMetadata, point.Document);
                    documents[point.Document] = document;
                }
                instr.DebugInfo = new(document, point.StartLine, point.StartColumn, point.EndLine, point.EndColumn);
                instructions[index] = instr;
            }
        }

        List<Method.LocalVariable> localVariables = new(body.LocalVariables.Count);
        for (int i = 0; i < body.LocalVariables.Count; i++)
        {
            var local = body.LocalVariables[i];
            if (local.LocalIndex != i) throw new UnreachableException("local index does not match index in locals");
            var name = localNames?.GetValueOrDefault(i);
            localVariables.Add(new(local.LocalType, local.IsPinned, name));
        }

        return new(
            method.ReturnType,
            [.. method.GetGenericArguments()],
            [.. method.GetParameters().Select(info => (Method.Parameter)info)],
            localVariables,
            instructions
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