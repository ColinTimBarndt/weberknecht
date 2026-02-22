using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Weberknecht.Metadata;

namespace Weberknecht;

public partial class Method
{

	private byte[] EncodeLocalSignature<T>(T tokens)
	where T : ITokenSource
	{
		// See ECMA-335 11.23.2.6

		var blob = new BlobBuilder();
		var localSig = new BlobEncoder(blob).LocalVariableSignature(_localVariables.Count);

		foreach (var local in _localVariables)
		{
			var localBuilder = localSig.AddVariable();
			if (local.Type == typeof(TypedReference))
			{
				localBuilder.TypedReference();
				continue;
			}
			var typeEnc = localBuilder.Type(local.Type.IsByRef, local.IsPinned);
			TypeHelper.EncodeType(typeEnc, local.Type, tokens);
		}

		return blob.ToArray();
	}

}