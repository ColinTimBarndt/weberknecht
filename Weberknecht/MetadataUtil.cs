using System.Collections.Immutable;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;
using System.Text;

namespace Weberknecht;

internal static class MetadataUtil
{

    private static readonly ConditionalWeakTable<Assembly, MetadataReader?> _metadata = [];
    private static readonly ConditionalWeakTable<Assembly, MetadataReader?> _debugMetadata = [];

    extension(Assembly asm)
    {
        public unsafe MetadataReader? GetMetadataReader()
        {
            return _metadata.GetOrAdd(asm, static (asm) =>
            {
                if (!asm.TryGetRawMetadata(out byte* blob, out int length))
                    return null;
                return new MetadataReader(blob, length);
            });
        }

        public MetadataReader? GetDebugMetadataReader()
        {
            return _debugMetadata.GetOrAdd(asm, static (asm) =>
            {
                var location = asm.Location;
                if (string.IsNullOrEmpty(location))
                    return null;

                try
                {
                    var data = ImmutableArray.Create(File.ReadAllBytes(Path.ChangeExtension(location, "pdb")));
                    return MetadataReaderProvider.FromPortablePdbImage(data).GetMetadataReader();
                }
                catch (FileNotFoundException)
                {
                    return null;
                }
            });
        }
    }

    extension(MethodInfo method)
    {
        public MethodDefinitionHandle MetadataHandle => (MethodDefinitionHandle)MetadataTokens.Handle(method.MetadataToken);
    }

    extension(MetadataReader metadata)
    {
        public Metadata.DocumentName GetDocumentName(DocumentNameBlobHandle handle)
        {
            // https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md#document-name-blob
            var reader = metadata.GetBlobReader(handle);
            var separator = reader.ReadChar();
            var parts = ImmutableArray.CreateBuilder<string>();
            while (reader.RemainingBytes > 0)
            {
                var part = metadata.GetBlobBytes(reader.ReadBlobHandle());
                parts.Add(Encoding.UTF8.GetString(part));
            }
            return new(separator, parts.ToImmutable());
        }
    }

    public static (SignatureHeader, int) ReadMethodHeader(MetadataReader reader, BlobHandle signatureHandle)
    {
        var blobReader = reader.GetBlobReader(signatureHandle);
        var header = blobReader.ReadSignatureHeader();
        if (header.IsGeneric)
            return (header, blobReader.ReadCompressedInteger());
        else
            return (header, 0);
    }

    public static SignatureKind GetSignatureKind(MetadataReader reader, BlobHandle signatureHandle)
    {
        var blobReader = reader.GetBlobReader(signatureHandle);
        return blobReader.ReadSignatureHeader().Kind;
    }

    public static BindingFlags GetBindingFlags(SignatureHeader header)
    {
        var flags = BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic;
        if (header.IsInstance) flags |= BindingFlags.Instance;
        else flags |= BindingFlags.Static;
        return flags;
    }

    public static CallingConventions GetCallingConventions(SignatureHeader header)
    {
        var conv = header.CallingConvention switch
        {
            SignatureCallingConvention.Default => CallingConventions.Standard,
            SignatureCallingConvention.CDecl => CallingConventions.Any,
            SignatureCallingConvention.StdCall => CallingConventions.Any,
            SignatureCallingConvention.ThisCall => CallingConventions.Standard | CallingConventions.HasThis,
            SignatureCallingConvention.FastCall => throw new NotSupportedException(),
            SignatureCallingConvention.VarArgs => CallingConventions.VarArgs,
            SignatureCallingConvention.Unmanaged => CallingConventions.Any,
            _ => throw new NotSupportedException()
        };
        if (header.HasExplicitThis) conv |= CallingConventions.ExplicitThis;
        return conv;
    }

}