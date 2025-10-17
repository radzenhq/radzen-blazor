using System;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Blazor.Spreadsheet;

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
        Add<AverageFunction>();
        Add<CountFunction>();
        Add<CountAllFunction>();
        Add<IfFunction>();
        Add<IfErrorFunction>();
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
        Add<RoundUpFunction>();
        Add<RoundDownFunction>();
        Add<RoundFunction>();
        Add<IntFunction>();
        Add<TruncFunction>();
        Add<SubtotalFunction>();
        Add<AggregateFunction>();
        Add<RandFunction>();
        Add<RandBetweenFunction>();
        Add<IndexFunction>();
        Add<ChooseFunction>();
        Add<VerticalLookupFunction>();
        Add<HorizontalLookupFunction>();
        Add<XLookupFunction>();
        Add<LenFunction>();
        Add<TrimFunction>();
        Add<ConcatFunction>();
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
    }

    /// <summary>
    /// Registers a new formula function of type T.
    /// </summary>
    public void Add<T>() where T : FormulaFunction, new()
    {
        var function = new T();

        functions[function.Name] = function;
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

        var candidates = root.Find(node =>
        {
            if (node is  FunctionSyntaxNode function)
            {
                return function.Token.Start <= position;
            }

            return false;
        }).Cast<FunctionSyntaxNode>();

        var candidate = candidates
            .OrderByDescending(x => x.Token.Start)
            .FirstOrDefault(x => x.IsInside(position));

        if (candidate == null)
        {
            return null;
        }

        var function = Get(candidate.Name);

        if (function is ErrorFunction)
        {
            return null;
        }

        return new FunctionHintData(function, candidate.GetArgumentIndexAtPosition(position));
    }
}