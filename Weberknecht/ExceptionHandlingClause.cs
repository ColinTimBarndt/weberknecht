using System.Buffers.Binary;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using Weberknecht.Metadata;

namespace Weberknecht;

using Options = ExceptionHandlingClauseOptions;

internal readonly struct ExceptionHandlingClause
{

	public Options Flags { get; }

	public Type? Exception { get; }

	public LabelRange Try { get; }

	public LabelRange Handler { get; }

	public Label FilterStart { get; }

	private ExceptionHandlingClause(Options flags, Type? exception, LabelRange @try, LabelRange handler, Label filterStart)
	{
		Flags = flags;
		Exception = exception;
		Try = @try;
		Handler = handler;
		FilterStart = filterStart;
	}

	public override readonly string ToString() => Flags switch
	{
		Options.Clause => $".try {Try} catch {Exception!.Name} handler {Handler}",
		Options.Filter => $".try {Try} filter {FilterStart} handler {Handler}",
		Options.Finally => $".try {Try} finally handler {Handler}",
		Options.Fault => $".try {Try} fault handler {Handler}",
		_ => ".try <unknown>",
	};

	public static ExceptionHandlingClause Clause(Type exception, LabelRange @try, LabelRange handler)
		=> new(Options.Clause, exception, @try, handler, default);

	public static ExceptionHandlingClause Filter(Label filterStart, LabelRange @try, LabelRange handler)
		=> new(Options.Filter, null, @try, handler, filterStart);

	public static ExceptionHandlingClause Finally(LabelRange @try, LabelRange handler)
		=> new(Options.Finally, null, @try, handler, default);

	public static ExceptionHandlingClause Fault(LabelRange @try, LabelRange handler)
		=> new(Options.Fault, null, @try, handler, default);

	public static byte[] EncodeExceptionHandlers<T>(ReadOnlySpan<ExceptionHandlingClause> clauses, LabelAddressMap labels, T tokens)
	where T : ITokenSource
	{
		bool small = true;
		foreach (var clause in clauses)
			small &= clause.CanUseSmallEncoding(labels);

		int size = 4 + clauses.Length * (small ? SMALL_SIZE : FAT_SIZE);
		byte[] buffer = new byte[size];
		buffer[0] = small ? (byte)1 : (byte)(1 | 0x40); // 0x01 = Exception handlers, 0x40 = fat flag
		BinaryPrimitives.WriteInt32LittleEndian(buffer.AsSpan(1, 4), size); // size fits in 24 bits => last byte is zero

		Span<byte> slice = buffer.AsSpan(4);
		if (small)
		{
			for (int i = 0; i < clauses.Length; i++)
				clauses[i].WriteSmall(slice[(SMALL_SIZE * i)..], labels, tokens);
		}
		else
		{
			for (int i = 0; i < clauses.Length; i++)
				clauses[i].WriteFat(slice[(FAT_SIZE * i)..], labels, tokens);
		}

		return buffer;
	}

	internal bool CanUseSmallEncoding(LabelAddressMap labels)
	{
		var (tryStart, tryEnd) = labels[Try];
		int tryLength = tryEnd - tryStart;
		var (handlerStart, handlerEnd) = labels[Handler];
		int handlerLength = handlerEnd - handlerStart;

		// Starts must fit in a ushort, lengths must fit in a byte
		return (((tryStart | handlerStart) & ~0xffff) | ((tryLength | handlerLength) & ~0xff)) == 0;
	}

	private const int SMALL_SIZE = 12;

	internal void WriteSmall<T>(Span<byte> buffer, LabelAddressMap labels, T tokens)
	where T : ITokenSource
	{
		var (tryStart, tryEnd) = labels[Try];
		int tryLength = tryEnd - tryStart;
		var (handlerStart, handlerEnd) = labels[Handler];
		int handlerLength = handlerEnd - handlerStart;

		BinaryPrimitives.WriteUInt16LittleEndian(buffer, (ushort)Flags);
		BinaryPrimitives.WriteUInt16LittleEndian(buffer[2..], (ushort)tryStart);
		buffer[4] = (byte)tryLength;
		BinaryPrimitives.WriteUInt16LittleEndian(buffer[5..], (ushort)handlerStart);
		buffer[7] = (byte)handlerLength;

		int tokenOrFilter;
		switch (Flags)
		{
			case Options.Clause:
				tokenOrFilter = tokens.GetToken(Exception!);
				break;

			case Options.Filter:
				tokenOrFilter = labels[FilterStart];
				break;

			default:
				return;
		}
		BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], tokenOrFilter);
	}

	private const int FAT_SIZE = 24;

	internal void WriteFat<T>(Span<byte> buffer, LabelAddressMap labels, T tokens)
	where T : ITokenSource
	{
		var (tryStart, tryEnd) = labels[Try];
		int tryLength = tryEnd - tryStart;
		var (handlerStart, handlerEnd) = labels[Handler];
		int handlerLength = handlerEnd - handlerStart;

		BinaryPrimitives.WriteInt32LittleEndian(buffer, (int)Flags);
		BinaryPrimitives.WriteInt32LittleEndian(buffer[4..], tryStart);
		BinaryPrimitives.WriteInt32LittleEndian(buffer[8..], tryLength);
		BinaryPrimitives.WriteInt32LittleEndian(buffer[12..], handlerStart);
		BinaryPrimitives.WriteInt32LittleEndian(buffer[16..], handlerLength);

		int tokenOrFilter;
		switch (Flags)
		{
			case Options.Clause:
				tokenOrFilter = tokens.GetToken(Exception!);
				break;

			case Options.Filter:
				tokenOrFilter = labels[FilterStart];
				break;

			default:
				return;
		}
		BinaryPrimitives.WriteUInt16LittleEndian(buffer[20..], (ushort)tokenOrFilter);
	}

}

/// <summary>
/// Data structure to help with emitting exception handling clauses as defined in IL using the ILGenerator API.
/// <summary>
internal struct ExceptionHandlingClauseHelper()
{

	// Try start -> stack of blocks
	private readonly Dictionary<Label, TryStack> _blocks = [];
	private readonly List<TryBlock> _active = [];
	private readonly Stack<TryBlock> _stack = [];
	private Label _endOfFilter = default;

	internal ExceptionHandlingClauseHelper(ReadOnlySpan<ExceptionHandlingClause> clauses) : this()
	{
		foreach (var clause in clauses)
		{
			if (clause.Flags is Options.Finally or Options.Fault)
				continue;

			if (!_blocks.TryGetValue(clause.Try.Start, out var stack))
			{
				stack = new();
				_blocks[clause.Try.Start] = stack;
			}

			stack.AddCatchClause(clause);
		}

		foreach (var stack in _blocks.Values)
			stack.FindEnds();

		foreach (var clause in clauses)
		{
			if (clause.Flags is not Options.Finally and not Options.Fault)
				continue;

			if (!_blocks.TryGetValue(clause.Try.Start, out var stack))
			{
				stack = new();
				_blocks[clause.Try.Start] = stack;
			}

			stack.AddFinalClause(clause);
		}
	}

	public void OnMarkLabel(Label label, ILGenerator gen)
	{
		if (!_endOfFilter.IsNull && label == _endOfFilter)
		{
			_endOfFilter = default;
			gen.BeginCatchBlock(null);
			return;
		}

		// Try get clause from current try block
		if (_stack.TryPeek(out var top))
		{
			if (top.IsEmpty && top.ReachedEnd(label))
			{
				_stack.Pop();
				Console.WriteLine("End Try");
				gen.EndExceptionBlock();
				return;
			}

			if (top.TryTakeClause(label) is ExceptionHandlingClause clause)
			{
				TriggerClause(gen, clause);
				return;
			}
		}

		// Search for next handler in active try blocks
		for (int i = 0; i < _active.Count; i++)
		{
			var block = _active[i];
			if (block.TryTakeClause(label) is not ExceptionHandlingClause clause2)
				continue;

			_active.RemoveAt(i);
			TriggerClause(gen, clause2);
			_stack.Push(block);

			return;
		}

		// Try get next stack of try blocks
		if (_blocks.TryGetValue(label, out var stack))
		{
			_blocks.Remove(label);
			for (int i = 0; i < stack.Blocks.Count; i++)
			{
				Console.WriteLine("Begin Try");
				gen.BeginExceptionBlock();
			}

			_active.AddRange(stack.Blocks);
		}
	}

	private void TriggerClause(ILGenerator gen, ExceptionHandlingClause clause)
	{
		Console.WriteLine($"TriggerClause {clause}");
		switch (clause.Flags)
		{
			case Options.Clause:
				gen.BeginCatchBlock(clause.Exception);
				return;

			case Options.Filter:
				gen.BeginExceptFilterBlock();
				if (!_endOfFilter.IsNull)
					throw new UnreachableException();
				_endOfFilter = clause.Handler.Start;
				return;

			case Options.Finally:
				gen.BeginFinallyBlock();
				return;

			case Options.Fault:
				gen.BeginFaultBlock();
				return;

			default:
				throw new NotImplementedException(Enum.GetName(clause.Flags));
		}
	}

	private sealed class TryStack()
	{

		// TryEnd -> Block
		private readonly Dictionary<Label, TryBlock> _blocks = [];

		// Used for finding the corresponding finally/fault
		private readonly Dictionary<Label, TryBlock> _blocksByEnd = [];

		public void AddCatchClause(ExceptionHandlingClause clause)
		{
			if (!_blocks.TryGetValue(clause.Try.End, out var block))
			{
				block = new();
				_blocks[clause.Try.End] = block;
			}

			block.AddClause(clause);
		}

		public void AddFinalClause(ExceptionHandlingClause clause)
		{
			if (!_blocksByEnd.TryGetValue(clause.Try.End, out var block))
			{
				block = new();
				_blocks.Add(clause.Try.End, block);
			}

			block.AddClause(clause);
		}

		public void FindEnds()
		{
			foreach (var block in _blocks.Values)
			{
				block.FindEnd();
				_blocksByEnd.Add(block.ClausesEnd, block);
				Console.WriteLine($"Found try end: {block.ClausesEnd}");
			}
		}

		public ICollection<TryBlock> Blocks => _blocks.Values;

	}

	private sealed class TryBlock()
	{

		// handler / filter start -> clause
		private readonly Dictionary<Label, ExceptionHandlingClause> _clauses = [];

		public Label ClausesEnd { get; private set; } = default;

		private readonly List<Label> _ends = [];

		public void AddClause(ExceptionHandlingClause clause)
		{
			if (clause.Flags is Options.Finally or Options.Fault)
				_ends.Add(clause.Handler.End);

			Label catchStart = clause.Flags == Options.Filter ? clause.FilterStart : clause.Handler.Start;
			_clauses.Add(catchStart, clause);
		}

		public void FindEnd()
		{
			// Handler end is the handler / filter start of the next catch/filter
			foreach (var clause in _clauses.Values)
			{
				if (!_clauses.ContainsKey(clause.Handler.End))
				{
					ClausesEnd = clause.Handler.End;
					_ends.Add(ClausesEnd);
					return;
				}
			}
			throw new UnreachableException();
		}

		public bool ReachedEnd(Label label)
		{
			_ends.Remove(label);
			return _ends.Count == 0;
		}

		public ExceptionHandlingClause? TryTakeClause(Label label)
		{
			if (_clauses.TryGetValue(label, out var clause))
			{
				_clauses.Remove(label);
				return clause;
			}
			return null;
		}

		public bool IsEmpty => _clauses.Count == 0;

	}

}