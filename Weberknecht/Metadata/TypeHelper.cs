using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace Weberknecht.Metadata;

internal static class TypeHelper
{

	private static readonly Dictionary<Type, PrimitiveTypeCode> Primitives = new()
	{
		[typeof(void)] = PrimitiveTypeCode.Void,
		[typeof(bool)] = PrimitiveTypeCode.Boolean,
		[typeof(char)] = PrimitiveTypeCode.Char,
		[typeof(sbyte)] = PrimitiveTypeCode.SByte,
		[typeof(byte)] = PrimitiveTypeCode.Byte,
		[typeof(short)] = PrimitiveTypeCode.Int16,
		[typeof(ushort)] = PrimitiveTypeCode.UInt16,
		[typeof(int)] = PrimitiveTypeCode.Int32,
		[typeof(uint)] = PrimitiveTypeCode.UInt32,
		[typeof(long)] = PrimitiveTypeCode.Int64,
		[typeof(ulong)] = PrimitiveTypeCode.UInt64,
		[typeof(float)] = PrimitiveTypeCode.Single,
		[typeof(double)] = PrimitiveTypeCode.Double,
		[typeof(string)] = PrimitiveTypeCode.String,
		[typeof(TypedReference)] = PrimitiveTypeCode.TypedReference,
		[typeof(nint)] = PrimitiveTypeCode.IntPtr,
		[typeof(nuint)] = PrimitiveTypeCode.UIntPtr,
		[typeof(object)] = PrimitiveTypeCode.Object,
	};

	public static void EncodeType<T>(SignatureTypeEncoder encoder, Type type, T tokens)
	where T : ITokenSource
	{
		Console.WriteLine($"EncodeType({type})");
		if (type.IsPrimitive)
		{
			encoder.PrimitiveType(Primitives[type]);
			return;
		}

		if (type.IsGenericParameter)
		{
			if (type.IsGenericTypeParameter)
			{
				encoder.GenericTypeParameter(type.GenericParameterPosition);
				return;
			}

			if (type.IsGenericMethodParameter)
			{
				encoder.GenericMethodTypeParameter(type.GenericParameterPosition);
				return;
			}

			throw new UnreachableException();
		}

		if (type.HasElementType)
		{
			var inner = type.GetElementType()!;

			if (type.IsByRef)
			{
				EncodeType(encoder, inner, tokens);
				return;
			}

			if (type.IsSZArray)
			{
				EncodeType(encoder.SZArray(), inner, tokens);
				return;
			}

			if (type.IsArray)
			{
				encoder.Array(out var elemType, out var shape);
				EncodeType(elemType, inner, tokens);
				shape.Shape(type.GetArrayRank(), [], default);
				return;
			}

			if (type.IsPointer)
			{
				EncodeType(encoder.Pointer(), inner, tokens);
				return;
			}

			throw new NotImplementedException(type.Name);
		}

		if (type.IsConstructedGenericType)
		{
			var genericDef = type.GetGenericTypeDefinition();
			var genArgs = type.GetGenericArguments();
			var argsEnc = encoder.GenericInstantiation(MetadataTokens.EntityHandle(tokens.GetToken(genericDef)), genArgs.Length, type.IsValueType);
			foreach (var genArg in genArgs)
				EncodeType(argsEnc.AddArgument(), genArg, tokens);
			return;
		}

		var token = tokens.GetToken(type);

		Console.WriteLine($"Token = {token:X08}, {MetadataTokens.Handle(token).Kind}");

		token = (token & 0xffffff) | ((int)HandleKind.TypeReference << 24);

		Console.WriteLine($"New Token = {token:X08}, {MetadataTokens.Handle(token).Kind}");

		encoder.Type(MetadataTokens.EntityHandle(token), type.IsValueType);
	}

}