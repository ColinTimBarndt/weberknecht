using RM = System.Reflection.Metadata;

namespace Weberknecht.Metadata;

public readonly struct SequencePoint(
	Document document,
	int startLine, int startColumn,
	int endLine, int endColumn
)
{

	public Document Document { get; } = document;

	public int StartLine { get; } = startLine;

	public int StartColumn { get; } = startColumn;

	public int EndLine { get; } = endLine;

	public int EndColumn { get; } = endColumn;

	public bool IsHidden => StartLine == RM.SequencePoint.HiddenLine;

	public SequencePoint(Document document) : this(document, RM.SequencePoint.HiddenLine, 0, RM.SequencePoint.HiddenLine, 0) { }

	public override string ToString() => IsHidden ? $"{Document.Name}:<hidden>" : $"{Document.Name}:{StartLine}:{StartColumn}";

}