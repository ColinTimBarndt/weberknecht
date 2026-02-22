using System.Reflection;
using System.Reflection.Emit;

namespace Weberknecht;

public partial class Method
{

	private byte[] EncodeLocalSignature(Module? module = null)
	{
		// See ECMA-335 11.23.2.6

		var sig = SignatureHelper.GetLocalVarSigHelper(module);

		foreach (var local in _localVariables)
			sig.AddArgument(local.Type, local.IsPinned);

		return sig.GetSignature();
	}

}