using System;
using System.Text.RegularExpressions;

#nullable enable

namespace Radzen.Blazor.Spreadsheet;

class SumIfFunction : FormulaFunction
{
    public override FunctionParameter[] Parameters =>
    [
        new("range", ParameterType.Collection, isRequired: true),
        new("criteria", ParameterType.Single, isRequired: true),
        new("sum_range", ParameterType.Collection, isRequired: false)
    ];

    public override CellData Evaluate(FunctionArguments arguments)
    {
        var range = arguments.GetRange("range");

        if (range == null)
        {
            return CellData.FromError(CellError.Value);
        }

        var criteria = arguments.GetSingle("criteria");

        if (criteria == null)
        {
            return CellData.FromError(CellError.Value);
        }

        var sumRange = arguments.GetRange("sum_range");
        
        var actualSumRange = sumRange ?? range;

        if (range.Count != actualSumRange.Count)
        {
            return CellData.FromError(CellError.Value);
        }

        var sum = 0d;

        for (int i = 0; i < range.Count; i++)
        {
            var rangeCell = range[i];
            var sumCell = actualSumRange[i];

            // Skip if range cell has an error
            if (rangeCell.IsError)
            {
                return rangeCell;
            }

            // Skip if sum cell has an error
            if (sumCell.IsError)
            {
                return sumCell;
            }

            // Check if the range cell matches the criteria
            if (MatchesCriteria(rangeCell, criteria))
            {
                // Add the corresponding sum cell value if it's a number
                if (sumCell.Type == CellDataType.Number)
                {
                    sum += sumCell.GetValueOrDefault<double>();
                }
                // If sum cell is empty, treat as 0
                else if (sumCell.IsEmpty)
                {
                    // Do nothing, effectively adding 0
                }
                // If sum cell is not a number and not empty, skip it
            }
        }

        return CellData.FromNumber(sum);
    }

    private bool MatchesCriteria(CellData cellData, CellData criteria)
    {
        // Handle error criteria
        if (criteria.IsError)
        {
            return false;
        }

        // Handle empty criteria - only matches empty cells
        if (criteria.IsEmpty)
        {
            return cellData.IsEmpty;
        }

        // Handle string criteria with wildcards
        if (criteria.Type == CellDataType.String)
        {
            var criteriaString = criteria.GetValueOrDefault<string>() ?? "";
            var cellString = cellData.ToString() ?? "";

            // Check for wildcard patterns
            if (criteriaString.Contains('*') || criteriaString.Contains('?'))
            {
                return MatchesWildcardPattern(cellString, criteriaString);
            }

            // Check for comparison expressions
            if (IsComparisonExpression(criteriaString))
            {
                return EvaluateComparisonExpression(cellData, criteriaString);
            }

            // Direct string comparison
            return string.Equals(cellString, criteriaString, StringComparison.OrdinalIgnoreCase);
        }

        // Handle numeric criteria
        if (criteria.Type == CellDataType.Number)
        {
            if (cellData.Type == CellDataType.Number)
            {
                return cellData.IsEqualTo(criteria);
            }
            return false;
        }

        // Handle date criteria
        if (criteria.Type == CellDataType.Date)
        {
            if (cellData.Type == CellDataType.Date)
            {
                return cellData.IsEqualTo(criteria);
            }
            return false;
        }

        // Handle boolean criteria
        if (criteria.Type == CellDataType.Boolean)
        {
            if (cellData.Type == CellDataType.Boolean)
            {
                return cellData.IsEqualTo(criteria);
            }
            return false;
        }

        // Default comparison
        return cellData.IsEqualTo(criteria);
    }

    private bool IsComparisonExpression(string criteria)
    {
        return criteria.StartsWith(">") || criteria.StartsWith("<") || 
               criteria.StartsWith(">=") || criteria.StartsWith("<=") ||
               criteria.StartsWith("<>") || criteria.StartsWith("!=");
    }

    private bool EvaluateComparisonExpression(CellData cellData, string criteria)
    {
        if (cellData.IsEmpty)
        {
            return false;
        }

        // Extract the operator and value
        string operatorStr;
        string valueStr;

        if (criteria.StartsWith(">="))
        {
            operatorStr = ">=";
            valueStr = criteria.Substring(2).Trim();
        }
        else if (criteria.StartsWith("<="))
        {
            operatorStr = "<=";
            valueStr = criteria.Substring(2).Trim();
        }
        else if (criteria.StartsWith("<>") || criteria.StartsWith("!="))
        {
            operatorStr = "<>";
            valueStr = criteria.Substring(2).Trim();
        }
        else if (criteria.StartsWith(">"))
        {
            operatorStr = ">";
            valueStr = criteria.Substring(1).Trim();
        }
        else if (criteria.StartsWith("<"))
        {
            operatorStr = "<";
            valueStr = criteria.Substring(1).Trim();
        }
        else
        {
            return false;
        }

        // Parse the value
        if (!double.TryParse(valueStr, out var numericValue))
        {
            return false;
        }

        // Convert cell data to number for comparison
        double cellValue;
        if (cellData.Type == CellDataType.Number)
        {
            cellValue = cellData.GetValueOrDefault<double>();
        }
        else if (cellData.Type == CellDataType.Date)
        {
            cellValue = cellData.GetValueOrDefault<DateTime>().ToNumber();
        }
        else
        {
            return false;
        }

        // Perform the comparison
        return operatorStr switch
        {
            ">" => cellValue > numericValue,
            "<" => cellValue < numericValue,
            ">=" => cellValue >= numericValue,
            "<=" => cellValue <= numericValue,
            "<>" or "!=" => cellValue != numericValue,
            _ => false
        };
    }

    private bool MatchesWildcardPattern(string text, string pattern)
    {
        // Handle tilde escape sequences first - convert ~* to literal * and ~? to literal ?
        var processedPattern = pattern.Replace("~*", "LITERAL_ASTERISK")  // Use a temporary marker
                                     .Replace("~?", "LITERAL_QUESTION");  // Use a temporary marker

        // Escape special regex characters except * and ?
        var escapedPattern = Regex.Escape(processedPattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".");

        // Restore the escaped wildcards as literal characters
        escapedPattern = escapedPattern.Replace("LITERAL_ASTERISK", "\\*")
                                     .Replace("LITERAL_QUESTION", "\\?");

        try
        {
            return Regex.IsMatch(text, "^" + escapedPattern + "$", RegexOptions.IgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}