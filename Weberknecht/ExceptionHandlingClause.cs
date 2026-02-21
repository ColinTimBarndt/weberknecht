using System.Reflection;

namespace Weberknecht;

using Options = ExceptionHandlingClauseOptions;

internal readonly struct ExceptionHandlingClause
{

	Options Flags { get; }

	Type? Exception { get; }

	LabelRange Try { get; }

	LabelRange Handler { get; }

	Label FilterStart { get; }

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

}