using System.Reflection.Metadata;
using Weberknecht.Metadata;

namespace Weberknecht;

internal sealed partial class ResolutionContext
{

    public MethodSignature ResolveMethodSignatureHandle(StandaloneSignatureHandle handle)
        => ResolveMethodSignature(Meta.GetStandaloneSignature(handle));

    public MethodSignature ResolveMethodSignature(StandaloneSignature sig)
    {
        InvalidStandaloneSignatureKindException.Assert(
            StandaloneSignatureKind.Method, sig.GetKind());

        var msig = sig.DecodeMethodSignature(this, _gctx);
        var conv = MetadataUtil.GetMethodCallingConventions(msig.Header);
        return new(conv, msig.ReturnType, msig.ParameterTypes, msig.RequiredParameterCount);
    }

}
