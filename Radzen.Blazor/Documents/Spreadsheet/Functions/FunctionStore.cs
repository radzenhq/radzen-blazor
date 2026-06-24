using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

internal class FunctionHintData(FormulaFunction function, int argumentIndex)
{
    public FormulaFunction Function { get; } = function;

    public int ArgumentIndex { get; } = argumentIndex;
}

/// <summary>
/// Registry for spreadsheet formula functions.
/// </summary>
public class FunctionStore
{
    private readonly Dictionary<string, FormulaFunction> functions = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="FunctionStore"/> class and registers built-in functions.
    /// </summary>
    public FunctionStore()
    {
        Add<SumFunction>();
        Add<SumIfFunction>();
        Add<CountIfFunction>();
        Add<CountIfsFunction>();
        Add<SumIfsFunction>();
        Add<AverageIfFunction>();
        Add<AverageIfsFunction>();
        Add<MaxIfsFunction>();
        Add<MinIfsFunction>();
        Add<AverageFunction>();
        Add<CountFunction>();
        Add<CountAllFunction>();
        Add<IfFunction>();
        Add<IfErrorFunction>();
        Add<IfsFunction>();
        Add<SwitchFunction>();
        Add<AndFunction>();
        Add<OrFunction>();
        Add<NotFunction>();
        Add<ColumnFunction>();
        Add<ColumnsFunction>();
        Add<RowFunction>();
        Add<RowsFunction>();
        Add<MaxFunction>();
        Add<MaxAllFunction>();
        Add<MinFunction>();
        Add<MinAllFunction>();
        Add<LargeFunction>();
        Add<SmallFunction>();
        Add<MedianFunction>();
        Add<RoundUpFunction>();
        Add<RoundDownFunction>();
        Add<RoundFunction>();
        Add<IntFunction>();
        Add<TruncFunction>();
        Add<AbsFunction>();
        Add<ModFunction>();
        Add<SubtotalFunction>();
        Add<AggregateFunction>();
        Add<RandFunction>();
        Add<RandBetweenFunction>();
        Add<IndexFunction>();
        Add<ChooseFunction>();
        Add<VerticalLookupFunction>();
        Add<HorizontalLookupFunction>();
        Add<XLookupFunction>();
        Add<MatchFunction>();
        Add<LenFunction>();
        Add<TrimFunction>();
        Add<ConcatFunction>();
        Add<ConcatenateFunction>();
        Add<TextJoinFunction>();
        Add<LeftFunction>();
        Add<RightFunction>();
        Add<MidFunction>();
        Add<SearchFunction>();
        Add<FindFunction>();
        Add<ReplaceFunction>();
        Add<SubstituteFunction>();
        Add<ProperFunction>();
        Add<UpperFunction>();
        Add<LowerFunction>();
        Add<ValueFunction>();
        Add<TextFunction>();
        Add<ReptFunction>();
        Add<TodayFunction>();
        Add<NowFunction>();
        Add<DayFunction>();
        Add<MonthFunction>();
        Add<YearFunction>();
        Add<HourFunction>();
        Add<MinuteFunction>();
        Add<SecondFunction>();
        Add<WeekdayFunction>();
        Add<WeeknumFunction>();
        Add<DateFunction>();
        Add<EdateFunction>();
        Add<EomonthFunction>();
        Add<DatedifFunction>();
        Add<TimeFunction>();

        Add<PowerFunction>();
        Add<SqrtFunction>();
        Add<ProductFunction>();
        Add<SumProductFunction>();
        Add<CeilingFunction>();
        Add<FloorFunction>();
        Add<StdevFunction>("STDEV.S");
        Add<StdevpFunction>("STDEV.P");
        Add<VarFunction>("VAR.S");
        Add<VarpFunction>("VAR.P");
        Add<ModeFunction>("MODE.SNGL");
        Add<RankFunction>("RANK.EQ");
        Add<CountBlankFunction>();
        Add<IfNaFunction>();
        Add<IsBlankFunction>();
        Add<IsNumberFunction>();
        Add<IsTextFunction>();
        Add<IsErrorFunction>();
        Add<IsNaFunction>();
        Add<DaysFunction>();
        Add<NetworkDaysFunction>();
        Add<WorkdayFunction>();
        Add<DateValueFunction>();
        Add<XMatchFunction>();
        Add<ExactFunction>();
        Add<CharFunction>();
        Add<CodeFunction>();
    }

    /// <summary>
    /// Registers a new formula function of type T, optionally under additional alias names
    /// (e.g. the modern dotted name STDEV.S for STDEV).
    /// </summary>
    public void Add<T>(params string[] aliases) where T : FormulaFunction, new()
    {
        ArgumentNullException.ThrowIfNull(aliases);

        var function = new T();

        functions[function.Name] = function;

        foreach (var alias in aliases)
        {
            functions[alias] = function;
        }
    }

    internal List<string> GetFunctionsForPrefix(string prefix)
    {
        var result = new List<string>();

        foreach (var function in functions.Keys)
        {
            if (function.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(function);
            }
        }

        result.Sort(StringComparer.OrdinalIgnoreCase);

        return result;
    }

    /// <summary>
    /// Gets a formula function by name, or returns an ErrorFunction if the function is not found.
    /// </summary>
    /// <param name="functionName">The name of the function to retrieve.</param>
    /// <returns>The formula function or an ErrorFunction if not found.</returns>
    public FormulaFunction Get(string functionName)
    {
        return functions.TryGetValue(functionName, out var function) ? function : new ErrorFunction();
    }

    internal FunctionHintData? CreateFunctionHint(string text, int position)
    {
        var syntaxTree = FormulaParser.Parse(text);
        var root = syntaxTree.Root;

        FunctionSyntaxNode? best = null;

        foreach (var node in root.Find(node => node is FunctionSyntaxNode f && f.Token.Start <= position))
        {
            var fn = (FunctionSyntaxNode)node;

            if (fn.IsInside(position) && (best is null || fn.Token.Start > best.Token.Start))
            {
                best = fn;
            }
        }

        if (best is null)
        {
            return null;
        }

        var function = Get(best.Name);

        if (function is ErrorFunction)
        {
            return null;
        }

        return new FunctionHintData(function, best.GetArgumentIndexAtPosition(position));
    }
}