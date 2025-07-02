using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Dialog for filtering data in a spreadsheet.
/// </summary>
public partial class FilterDialog : ComponentBase
{
    enum FilterOperator
    {
        None,
        Equals,
        NotEquals,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        BeginsWith,
        DoesNotBeginWith,
        EndsWith,
        DoesNotEndWith,
        Contains,
        DoesNotContain,
        IsEmpty,
        IsNotEmpty
    }

    class FilterOperatorOption
    {
        public string Text { get; set; } = "";
        public FilterOperator Value { get; set; }
    }

    class LogicalOperatorOption
    {
        public string Text { get; set; } = "";
        public LogicalFilterOperator Value { get; set; }
    }

    /// <summary>
    /// The sheet containing the data to filter.
    /// </summary>
    [Parameter, EditorRequired]
    public Sheet Sheet { get; set; } = default!;

    /// <summary>
    /// The column index to filter.
    /// </summary>
    [Parameter, EditorRequired]
    public int Column { get; set; }

    /// <summary>
    /// The row index where the filter was triggered.
    /// </summary>
    [Parameter, EditorRequired]
    public int Row { get; set; }

    /// <summary>
    /// The dialog service instance.
    /// </summary>
    [Inject]
    public DialogService DialogService { get; set; } = default!;

    /// <summary>
    /// Optional existing filter criterion to populate default values.
    /// </summary>
    [Parameter]
    public FilterCriterion? Filter { get; set; }

    private FilterOperator selectedOperator = FilterOperator.Equals;
    private string filterValue = "";
    private LogicalFilterOperator logicalOperator = LogicalFilterOperator.And;
    private FilterOperator secondOperator = FilterOperator.None;
    private string secondFilterValue = "";
    private List<FilterOperatorOption> availableOperators = [];
    private List<LogicalOperatorOption> logicalOperators = [];
    private string fieldName = "";

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        InitializeFieldName();
        InitializeAvailableOperators();
        InitializeLogicalOperators();

        selectedOperator = FilterOperator.Equals;
        secondOperator = FilterOperator.None;

        if (Filter != null)
        {
            var visitor = new FilterCriterionVisitor(Column);
            Filter.Accept(visitor);
            PopulateFromVisitor(visitor);
        }
    }

    private void InitializeFieldName()
    {
        var dataTable = GetCurrentTable();
        var autoFilter = GetCurrentAutoFilter();
        var rangeToUse = RangeRef.Invalid;
        if (dataTable != null)
        {
            rangeToUse = dataTable.Range;
        }
        else if (autoFilter != null)
        {
            rangeToUse = autoFilter.Range;
        }
        if (rangeToUse != RangeRef.Invalid)
        {
            var headerCell = Sheet.Cells[rangeToUse.Start.Row, Column];
            var headerText = headerCell.GetValueAsString();
            fieldName = string.IsNullOrEmpty(headerText) ? $"Column {Column + 1}" : headerText;
        }
        else
        {
            fieldName = $"Column {Column + 1}";
        }
    }

    private void InitializeAvailableOperators()
    {
        availableOperators = [
            new FilterOperatorOption { Text = "", Value = FilterOperator.None },
            new FilterOperatorOption { Text = "equals", Value = FilterOperator.Equals },
            new FilterOperatorOption { Text = "does not equal", Value = FilterOperator.NotEquals },
            new FilterOperatorOption { Text = "is greater than", Value = FilterOperator.GreaterThan },
            new FilterOperatorOption { Text = "is greater than or equal to", Value = FilterOperator.GreaterThanOrEqual },
            new FilterOperatorOption { Text = "is less than", Value = FilterOperator.LessThan },
            new FilterOperatorOption { Text = "is less than or equal to", Value = FilterOperator.LessThanOrEqual },
            new FilterOperatorOption { Text = "begins with", Value = FilterOperator.BeginsWith },
            new FilterOperatorOption { Text = "does not begin with", Value = FilterOperator.DoesNotBeginWith },
            new FilterOperatorOption { Text = "ends with", Value = FilterOperator.EndsWith },
            new FilterOperatorOption { Text = "does not end with", Value = FilterOperator.DoesNotEndWith },
            new FilterOperatorOption { Text = "contains", Value = FilterOperator.Contains },
            new FilterOperatorOption { Text = "does not contain", Value = FilterOperator.DoesNotContain },
            new FilterOperatorOption { Text = "is empty", Value = FilterOperator.IsEmpty },
            new FilterOperatorOption { Text = "is not empty", Value = FilterOperator.IsNotEmpty }
        ];
    }

    private void InitializeLogicalOperators()
    {
        logicalOperators = [
            new LogicalOperatorOption { Text = "AND", Value = LogicalFilterOperator.And },
            new LogicalOperatorOption { Text = "OR", Value = LogicalFilterOperator.Or }
        ];
    }

    private void OnOk()
    {
        var filter = CreateFilter();
        DialogService.Close(filter);
    }

    private void OnCancel()
    {
        DialogService.Close();
    }

    private SheetFilter? CreateFilter()
    {
        var dataTable = GetCurrentTable();
        var autoFilter = GetCurrentAutoFilter();

        RangeRef rangeToUse = RangeRef.Invalid;

        if (dataTable != null)
        {
            rangeToUse = dataTable.Range;
        }
        else if (autoFilter != null)
        {
            rangeToUse = autoFilter.Range;
        }

        if (rangeToUse == RangeRef.Invalid)
        {
            return null;
        }

        FilterCriterion criterion;

        if (secondOperator != FilterOperator.None)
        {
            var criterion1 = CreateCriterion(Column, selectedOperator, filterValue);
            var criterion2 = CreateCriterion(Column, secondOperator, secondFilterValue);

            if (logicalOperator == LogicalFilterOperator.And)
            {
                criterion = new AndCriterion { Criteria = [criterion1, criterion2] };
            }
            else
            {
                criterion = new OrCriterion { Criteria = [criterion1, criterion2] };
            }
        }
        else
        {
            criterion = CreateCriterion(Column, selectedOperator, filterValue);
        }

        return new SheetFilter(criterion, rangeToUse);
    }

    private static FilterCriterion CreateCriterion(int column, FilterOperator operatorType, string value)
    {
        return operatorType switch
        {
            FilterOperator.Equals => new EqualToCriterion { Column = column, Value = value },
            FilterOperator.NotEquals => new NotEqualToCriterion { Column = column, Value = value },
            FilterOperator.GreaterThan => new GreaterThanCriterion { Column = column, Value = value },
            FilterOperator.GreaterThanOrEqual => new GreaterThanOrEqualCriterion { Column = column, Value = value },
            FilterOperator.LessThan => new LessThanCriterion { Column = column, Value = value },
            FilterOperator.LessThanOrEqual => new LessThanOrEqualCriterion { Column = column, Value = value },
            FilterOperator.BeginsWith => new StartsWithCriterion { Column = column, Value = value },
            FilterOperator.DoesNotBeginWith => new DoesNotStartWithCriterion { Column = column, Value = value },
            FilterOperator.EndsWith => new EndsWithCriterion { Column = column, Value = value },
            FilterOperator.DoesNotEndWith => new DoesNotEndWithCriterion { Column = column, Value = value },
            FilterOperator.Contains => new ContainsCriterion { Column = column, Value = value },
            FilterOperator.DoesNotContain => new DoesNotContainCriterion { Column = column, Value = value },
            FilterOperator.IsEmpty => new IsNullCriterion { Column = column },
            FilterOperator.IsNotEmpty => new NotEqualToCriterion { Column = column, Value = null },
            _ => new EqualToCriterion { Column = column, Value = value }
        };
    }

    private Table? GetCurrentTable()
    {
        foreach (var table in Sheet.Tables)
        {
            if (table.Range.Contains(Row, Column))
            {
                return table;
            }
        }
        return null;
    }

    private AutoFilter? GetCurrentAutoFilter()
    {
        if (Sheet.AutoFilter != null && Sheet.AutoFilter.Range.Contains(Row, Column))
        {
            return Sheet.AutoFilter;
        }
        return null;
    }

    private void PopulateFromVisitor(FilterCriterionVisitor visitor)
    {
        if (visitor.Criteria.Count > 0)
        {
            var firstCriterion = visitor.Criteria[0];
            selectedOperator = firstCriterion.Operator;
            filterValue = firstCriterion.Value ?? "";

            if (visitor.Criteria.Count > 1)
            {
                var secondCriterion = visitor.Criteria[1];
                secondOperator = secondCriterion.Operator;
                secondFilterValue = secondCriterion.Value ?? "";
                logicalOperator = visitor.LogicalOperator;
            }
        }
    }

    private class FilterCriterionVisitor(int column) : IFilterCriterionVisitor
    {
        public List<(FilterOperator Operator, string? Value)> Criteria { get; } = [];
        public LogicalFilterOperator LogicalOperator { get; set; } = LogicalFilterOperator.And;

        public void Visit(OrCriterion criterion)
        {
            LogicalOperator = LogicalFilterOperator.Or;
            foreach (var c in criterion.Criteria)
            {
                c.Accept(this);
            }
        }

        public void Visit(AndCriterion criterion)
        {
            LogicalOperator = LogicalFilterOperator.And;
            foreach (var c in criterion.Criteria)
            {
                c.Accept(this);
            }
        }

        public void Visit(EqualToCriterion criterion)
        {
            if (criterion.Column == column)
            {
                Criteria.Add((FilterOperator.Equals, criterion.Value?.ToString()));
            }
        }

        public void Visit(GreaterThanCriterion criterion)
        {
            if (criterion.Column == column)
            {
                Criteria.Add((FilterOperator.GreaterThan, criterion.Value?.ToString()));
            }
        }

        public void Visit(InListCriterion criterion)
        {
            // Not used for custom filter dialog
        }

        public void Visit(IsNullCriterion criterion)
        {
            if (criterion.Column == column)
            {
                Criteria.Add((FilterOperator.IsEmpty, null));
            }
        }

        public void Visit(LessThanCriterion criterion)
        {
            if (criterion.Column == column)
            {
                Criteria.Add((FilterOperator.LessThan, criterion.Value?.ToString()));
            }
        }

        public void Visit(GreaterThanOrEqualCriterion criterion)
        {
            if (criterion.Column == column)
            {
                Criteria.Add((FilterOperator.GreaterThanOrEqual, criterion.Value?.ToString()));
            }
        }

        public void Visit(LessThanOrEqualCriterion criterion)
        {
            if (criterion.Column == column)
            {
                Criteria.Add((FilterOperator.LessThanOrEqual, criterion.Value?.ToString()));
            }
        }

        public void Visit(NotEqualToCriterion criterion)
        {
            if (criterion.Column == column)
            {
                if (criterion.Value == null)
                {
                    Criteria.Add((FilterOperator.IsNotEmpty, null));
                }
                else
                {
                    Criteria.Add((FilterOperator.NotEquals, criterion.Value?.ToString()));
                }
            }
        }

        public void Visit(StartsWithCriterion criterion)
        {
            if (criterion.Column == column)
            {
                Criteria.Add((FilterOperator.BeginsWith, criterion.Value?.ToString()));
            }
        }

        public void Visit(DoesNotStartWithCriterion criterion)
        {
            if (criterion.Column == column)
            {
                Criteria.Add((FilterOperator.DoesNotBeginWith, criterion.Value?.ToString()));
            }
        }

        public void Visit(EndsWithCriterion criterion)
        {
            if (criterion.Column == column)
            {
                Criteria.Add((FilterOperator.EndsWith, criterion.Value?.ToString()));
            }
        }

        public void Visit(DoesNotEndWithCriterion criterion)
        {
            if (criterion.Column == column)
            {
                Criteria.Add((FilterOperator.DoesNotEndWith, criterion.Value?.ToString()));
            }
        }

        public void Visit(ContainsCriterion criterion)
        {
            if (criterion.Column == column)
            {
                Criteria.Add((FilterOperator.Contains, criterion.Value?.ToString()));
            }
        }

        public void Visit(DoesNotContainCriterion criterion)
        {
            if (criterion.Column == column)
            {
                Criteria.Add((FilterOperator.DoesNotContain, criterion.Value?.ToString()));
            }
        }
    }
}