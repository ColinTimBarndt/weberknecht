using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace Weberknecht;

internal static class MetadataUtil
{

    private static readonly ConditionalWeakTable<Assembly, MetadataReader?> _lookup = [];

    extension(Assembly asm)
    {
        public unsafe MetadataReader? GetMetadataReader()
        {
            return _lookup.GetOrAdd(asm, static (asm) =>
            {
                if (!asm.TryGetRawMetadata(out byte* blob, out int length))
                    return null;
                return new MetadataReader(blob, length);
            });
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