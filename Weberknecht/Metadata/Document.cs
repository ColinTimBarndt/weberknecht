using System.Collections.Immutable;
using RM = System.Reflection.Metadata;

namespace Weberknecht.Metadata;

public sealed class Document(DocumentName name, Guid language, Guid hashAlgorithm, ImmutableArray<byte> hash)
{

	// https://github.com/dotnet/runtime/blob/main/docs/design/specs/PortablePdb-Metadata.md
	private static readonly Guid
		VISUAL_C_SHARP = new("3f5162f8-07c6-11d3-9053-00c04fa302a1"),
		VISUAL_BASIC = new("3a12d0b8-c26c-11d0-b442-00a0244a1dd2"),
		VISUAL_F_SHARP = new("ab4f38c9-b6e6-43ba-be3b-58080b2ccce3"),
		SHA1 = new("ff1816ec-aa5e-4d10-87f7-6f4963833460"),
		SHA256 = new("8829d00f-11b8-4213-878b-770e8597ac16");

	public ImmutableArray<byte> Hash { get; } = hash;

	public Guid HashAlgorithm { get; } = hashAlgorithm;

	public HashAlgorithmType HashAlgorithmType
	{
		get
		{
			if (HashAlgorithm == Guid.Empty)
				return HashAlgorithmType.None;

			if (HashAlgorithm == SHA1)
				return HashAlgorithmType.Sha1;

			if (HashAlgorithm == SHA256)
				return HashAlgorithmType.Sha256;

			return HashAlgorithmType.Unknown;
		}
	}

	public Guid Language { get; } = language;

	public DocumentLanguageName LanguageName
	{
		get
		{
			if (Language == VISUAL_C_SHARP)
				return DocumentLanguageName.VisualCSharp;

			if (Language == VISUAL_BASIC)
				return DocumentLanguageName.VisualBasic;

			if (Language == VISUAL_F_SHARP)
				return DocumentLanguageName.VisualFSharp;

			return DocumentLanguageName.Unknown;
		}
	}

	public DocumentName Name { get; } = name;

	public Document(DocumentName name, DocumentLanguageName language) : this(name, LanguageGuid(language), Guid.Empty, []) { }

	public Document(DocumentName name, Guid language) : this(name, language, Guid.Empty, []) { }

	public Document(DocumentName name, DocumentLanguageName language, HashAlgorithmType hashAlgorithm, ImmutableArray<byte> hash) : this(name, language, HashAlgorithmGuid(hashAlgorithm), hash) { }

	public Document(DocumentName name, DocumentLanguageName language, Guid hashAlgorithm, ImmutableArray<byte> hash) : this(name, LanguageGuid(language), hashAlgorithm, hash) { }

	public Document(DocumentName name, Guid language, HashAlgorithmType hashAlgorithm, ImmutableArray<byte> hash) : this(name, language, HashAlgorithmGuid(hashAlgorithm), hash) { }

	private static Guid LanguageGuid(DocumentLanguageName name) => name switch
	{
		DocumentLanguageName.Unknown => Guid.Empty,
		DocumentLanguageName.VisualCSharp => VISUAL_C_SHARP,
		DocumentLanguageName.VisualBasic => VISUAL_BASIC,
		DocumentLanguageName.VisualFSharp => VISUAL_F_SHARP,
		_ => throw new ArgumentOutOfRangeException(nameof(name), name, "enum out of range"),
	};

	private static Guid HashAlgorithmGuid(HashAlgorithmType name) => name switch
	{
		HashAlgorithmType.None or HashAlgorithmType.Unknown => Guid.Empty,
		HashAlgorithmType.Sha1 => SHA1,
		HashAlgorithmType.Sha256 => SHA256,
		_ => throw new ArgumentOutOfRangeException(nameof(name), name, "enum out of range"),
	};

	public static Document FromMetadata(RM.MetadataReader reader, RM.DocumentHandle handle) => FromMetadata(reader, reader.GetDocument(handle));

	public static Document FromMetadata(RM.MetadataReader reader, RM.Document doc)
	{
		var name = reader.GetDocumentName(doc.Name);
		ImmutableArray<byte> hash = [];
		var hashAlgorithm = Guid.Empty;
		var language = Guid.Empty;

		if (!doc.HashAlgorithm.IsNil && !doc.Hash.IsNil)
		{
			hash = ImmutableArray.Create(reader.GetBlobBytes(doc.Hash));
			hashAlgorithm = reader.GetGuid(doc.HashAlgorithm);
		}

		if (!doc.Language.IsNil)
		{
			language = reader.GetGuid(doc.Language);
		}

		return new(name, language, hashAlgorithm, hash);
	}

}

public readonly struct DocumentName(char separator, ImmutableArray<string> parts)
{
	public readonly char Separator { get; } = separator;
	public readonly ImmutableArray<string> Parts { get; } = parts;

	public override string ToString() => string.Join(Separator, Parts);
}

public enum DocumentLanguageName
{
	Unknown,
	VisualCSharp,
	VisualBasic,
	VisualFSharp,
}

public enum HashAlgorithmType
{
	None = 0,
	Unknown,
	Sha1,
	Sha256,
}