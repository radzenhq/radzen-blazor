using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Represents errors that can occur during formula evaluation in a spreadsheet.
/// </summary>
public enum CellError
{
    /// <summary>
    /// Indicates a value error, such as type mismatch or invalid argument.
    /// </summary>
    Value,  // #VALUE! - Type mismatch or invalid argument
    /// <summary>
    /// Indicates a division by zero error.
    /// </summary>
    Div0,   // #DIV/0! - Division by zero
    /// <summary>
    /// Indicates an invalid cell reference error, such as referencing a cell that does not exist.
    /// </summary>
    Ref,    // #REF! - Invalid cell reference
    /// <summary>
    /// Indicates an invalid name error, such as using an undefined function or variable name.
    /// </summary>
    Name,   // #NAME? - Invalid function name
    /// <summary>
    /// Indicates an invalid number error, such as using a number that is too large or too small for the context.
    /// </summary>
    Num,    // #NUM! - Invalid number
    /// <summary>
    /// Indicates that a value is not available, such as when a formula cannot return a result.
    /// </summary>
    NA,      // #N/A - Value not available
    /// <summary>
    /// Indicates a circular reference error, which occurs when a formula refers back to its own cell either directly or indirectly.
    /// </summary>
    Circular // #CIRCULAR - Circular reference
}

class FormulaEvaluator(Sheet sheet) : IFormulaSyntaxNodeVisitor
{
    private readonly Sheet sheet = sheet;
    private object? value;
    private readonly HashSet<Cell> evaluationStack = [];

    public void VisitNumberLiteral(NumberLiteralSyntaxNode numberLiteralSyntaxNode)
    {
        var token = numberLiteralSyntaxNode.Token;

        value = token.ValueKind switch
        {
            ValueKind.Int => CellData.FromNumber(token.IntValue),
            ValueKind.UInt => CellData.FromNumber(token.UintValue),
            ValueKind.Long => CellData.FromNumber(token.LongValue),
            ValueKind.ULong => CellData.FromNumber(token.UlongValue),
            ValueKind.Float => CellData.FromNumber(token.FloatValue),
            ValueKind.Double => CellData.FromNumber(token.DoubleValue),
            ValueKind.Decimal => CellData.FromNumber((double)token.DecimalValue),
            _ => throw new InvalidOperationException($"Unsupported value kind: {token.ValueKind}")
        };
    }

    public void VisitStringLiteral(StringLiteralSyntaxNode stringLiteralSyntaxNode)
    {
        if (stringLiteralSyntaxNode.Token.Value is not null)
        {
            value = CellData.FromString(stringLiteralSyntaxNode.Token.Value);
        }
    }

    public void VisitErrorLiteral(ErrorLiteralSyntaxNode errorLiteralSyntaxNode)
    {
        value = CellData.FromError(errorLiteralSyntaxNode.Token.ErrorValue);
    }

    public void VisitBinaryExpression(BinaryExpressionSyntaxNode binaryExpressionSyntaxNode)
    {
        binaryExpressionSyntaxNode.Left.Accept(this);
        var left = (CellData)value!;
        binaryExpressionSyntaxNode.Right.Accept(this);
        var right = (CellData)value!;

        if (left.IsError)
        {
            value = left;
            return;
        }

        if (right.IsError)
        {
            value = right;
            return;
        }

        // Treat empty values as zero for arithmetic
        if (left.IsEmpty)
        {
            left = CellData.FromNumber(0d);
        }

        if (right.IsEmpty)
        {
            right = CellData.FromNumber(0d);
        }

        // For comparison operators, we don't need both sides to be numeric
        var isComparisonOperator = binaryExpressionSyntaxNode.Operator is
            BinaryOperator.Equals or BinaryOperator.NotEquals or
            BinaryOperator.LessThan or BinaryOperator.LessThanOrEqual or
            BinaryOperator.GreaterThan or BinaryOperator.GreaterThanOrEqual;

        if (!isComparisonOperator && (left.Type != CellDataType.Number || right.Type != CellDataType.Number))
        {
            value = CellData.FromError(CellError.Value);
            return;
        }

        if (binaryExpressionSyntaxNode.Operator == BinaryOperator.Divide)
        {
            var rnum = right.GetValueOrDefault<double>();

            if (Math.Abs(rnum) == 0d)
            {
                value = CellData.FromError(CellError.Div0);
                return;
            }
        }

        if (isComparisonOperator)
        {
            switch (binaryExpressionSyntaxNode.Operator)
            {
                case BinaryOperator.Equals:
                    value = CellData.FromBoolean(left.IsEqualTo(right));
                    return;
                case BinaryOperator.NotEquals:
                    value = CellData.FromBoolean(!left.IsEqualTo(right));
                    return;
                case BinaryOperator.LessThan:
                    value = CellData.FromBoolean(left.IsLessThan(right));
                    return;
                case BinaryOperator.LessThanOrEqual:
                    value = CellData.FromBoolean(left.IsLessThanOrEqualTo(right));
                    return;
                case BinaryOperator.GreaterThan:
                    value = CellData.FromBoolean(left.IsGreaterThan(right));
                    return;
                case BinaryOperator.GreaterThanOrEqual:
                    value = CellData.FromBoolean(left.IsGreaterThanOrEqualTo(right));
                    return;
            }
        }
        else
        {
            var l = left.GetValueOrDefault<double>();
            var r = right.GetValueOrDefault<double>();
            switch (binaryExpressionSyntaxNode.Operator)
            {
                case BinaryOperator.Plus: value = CellData.FromNumber(l + r); return;
                case BinaryOperator.Minus: value = CellData.FromNumber(l - r); return;
                case BinaryOperator.Multiply: value = CellData.FromNumber(l * r); return;
                case BinaryOperator.Divide: value = CellData.FromNumber(l / r); return;
            }
        }

        throw new InvalidOperationException($"Unsupported operator: {binaryExpressionSyntaxNode.Operator}");
    }

    private CellData EvaluateCell(Cell cell)
    {
        if (!evaluationStack.Add(cell))
        {
            return CellData.FromError(CellError.Circular);
        }

        CellData result;
        if (cell.FormulaSyntaxTree != null)
        {
            cell.FormulaSyntaxTree.Root.Accept(this);
            result = (CellData)value!;
        }
        else
        {
            result = cell.Data;
        }
        evaluationStack.Remove(cell);
        return result;
    }

    public void VisitCell(CellSyntaxNode cellSyntaxNode)
    {
        var address = cellSyntaxNode.Token.AddressValue;
        // If the row/column was deleted, set whole formula to =#REF!
        if (sheet.IsDeletedRow(address.Row) || sheet.IsDeletedColumn(address.Column))
        {
            value = CellData.FromError(CellError.Ref);
            return;
        }

        // If out of bounds, return #REF!
        if ((address.Row < 0 || address.Row >= sheet.RowCount) || (address.Column < 0 || address.Column >= sheet.ColumnCount))
        {
            value = CellData.FromError(CellError.Ref);
            return;
        }

        if (!sheet.Cells.TryGet(address.Row, address.Column, out var cell))
        {
            value = CellData.FromError(CellError.Ref);
            return;
        }

        value = EvaluateCell(cell);
    }

    public CellData Evaluate(FormulaSyntaxNode node)
    {
        node.Accept(this);

        return (CellData)value!;
    }


    public void VisitFunction(FunctionSyntaxNode functionSyntaxNode)
    {
        // Get the function to check if it can handle errors
        var function = sheet.FunctionRegistry.Get(functionSyntaxNode.Name);

        // Process arguments according to parameter definitions
        var functionArguments = ProcessArguments(function, functionSyntaxNode.Arguments);
        
        if (functionArguments == null)
        {
            return; // Error already set in ProcessArguments
        }

        value = function.Evaluate(functionArguments);
    }

    private FunctionArguments? ProcessArguments(FormulaFunction function, List<FormulaSyntaxNode> argumentNodes)
    {
        var parameterDefinitions = function.Parameters;
        var functionArguments = new FunctionArguments();
        var argumentIndex = 0;

        for (int paramIndex = 0; paramIndex < parameterDefinitions.Length; paramIndex++)
        {
            var paramDef = parameterDefinitions[paramIndex];
            
            // Check if we have enough arguments for required parameters
            // But only if this is not a repeating parameter (repeating parameters can handle empty lists)
            if (paramDef.IsRequired && 
                paramDef.Type != ParameterType.Sequence && 
                argumentIndex >= argumentNodes.Count)
            {
                value = CellData.FromError(CellError.Value);
                return null;
            }

            // Handle repeating single parameters
            if (paramDef.Type == ParameterType.Sequence)
            {
                // Collect all remaining arguments for repeating single parameters
                var allArguments = new List<CellData>();
                while (argumentIndex < argumentNodes.Count)
                {
                    var argument = ProcessArgument(argumentNodes[argumentIndex], function);
                    if (argument == null)
                    {
                        return null; // Error already set
                    }
                    allArguments.AddRange(argument);
                    argumentIndex++;
                }
                functionArguments.Set(paramDef.Name, allArguments);
            }
            else
            {
                // Handle single parameter
                if (argumentIndex < argumentNodes.Count)
                {
                    var argument = ProcessArgument(argumentNodes[argumentIndex], function);
                    if (argument == null)
                    {
                        return null; // Error already set
                    }
                    
                    // Set the argument based on parameter type
                    if (paramDef.Type == ParameterType.Collection)
                    {
                        // For collections, pass the list directly
                        functionArguments.Set(paramDef.Name, argument);
                    }
                    else
                    {
                        // For single values, pass the first item
                        functionArguments.Set(paramDef.Name, argument[0]);
                    }
                    argumentIndex++;
                }
                else if (!paramDef.IsRequired)
                {
                    // Optional parameter not provided - skip it
                    continue;
                }
            }
        }

        // Check if we have too many arguments
        // But only if this is not an ErrorFunction (ErrorFunction can handle any number of arguments)
        if (argumentIndex < argumentNodes.Count && function is not ErrorFunction)
        {
            value = CellData.FromError(CellError.Value);
            return null;
        }

        return functionArguments;
    }

    private List<CellData>? ProcessArgument(FormulaSyntaxNode argumentNode, FormulaFunction function)
    {
        argumentNode.Accept(this);

        var hasError = value is CellData cellData && cellData.IsError;

        // Only short-circuit on errors if the function cannot handle them
        if (hasError && !function.CanHandleErrors)
        {
            return null;
        }

        // Convert the evaluated value to a list of CellData
        if (value is List<CellData> list)
        {
            return list;
        }
        else
        {
            return [(CellData)value!];
        }
    }

    public void VisitRange(RangeSyntaxNode rangeSyntaxNode)
    {
        var start = rangeSyntaxNode.Start.Token.AddressValue;
        var end = rangeSyntaxNode.End.Token.AddressValue;

        if (start.Row > end.Row || (start.Row == end.Row && start.Column > end.Column))
        {
            value = CellData.FromError(CellError.Value);
            return;
        }

        var cells = new List<CellData>();

        for (var row = start.Row; row <= end.Row; row++)
        {
            for (var column = start.Column; column <= end.Column; column++)
            {
                if (sheet.IsDeletedRow(row) || sheet.IsDeletedColumn(column))
                {
                    value = CellData.FromError(CellError.Ref);
                    return;
                }
                if (row < 0 || row >= sheet.RowCount || column < 0 || column >= sheet.ColumnCount)
                {
                    value = CellData.FromError(CellError.Ref);
                    return;
                }
                if (!sheet.Cells.TryGet(row, column, out var cell))
                {
                    value = CellData.FromError(CellError.Ref);
                    return;
                }

                var cellValue = EvaluateCell(cell);

                if (cellValue.IsError)
                {
                    return;
                }

                cells.Add(cellValue);
            }
        }

        value = cells;
    }
}