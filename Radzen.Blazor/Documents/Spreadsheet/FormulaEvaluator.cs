using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace Radzen.Documents.Spreadsheet;

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

class FormulaEvaluator(Worksheet sheet, Cell currentCell, Dictionary<Cell, CellData>? evaluated = null) : IFormulaSyntaxNodeVisitor
{
    private object? value;
    private readonly HashSet<Cell> evaluationStack = [];
    private readonly HashSet<string> nameStack = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Cell, CellData> evaluated = evaluated ?? [];
    private Cell formulaCell = currentCell;
    private bool arrayContext;

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

    public void VisitBooleanLiteral(BooleanLiteralSyntaxNode booleanLiteralSyntaxNode)
    {
        value = CellData.FromBoolean(booleanLiteralSyntaxNode.Token.ValueKind == ValueKind.True);
    }

    public void VisitErrorLiteral(ErrorLiteralSyntaxNode errorLiteralSyntaxNode)
    {
        value = CellData.FromError(errorLiteralSyntaxNode.Token.ErrorValue);
    }

    public void VisitBinaryExpression(BinaryExpressionSyntaxNode binaryExpressionSyntaxNode)
    {
        binaryExpressionSyntaxNode.Left.Accept(this);
        var left = value;
        binaryExpressionSyntaxNode.Right.Accept(this);
        var right = value;
        var op = binaryExpressionSyntaxNode.Operator;

        if (arrayContext && (left is RangeList || right is RangeList))
        {
            value = Broadcast([left, right], operands => ApplyBinary(op, operands[0], operands[1]));
            return;
        }

        value = ApplyBinary(op, Intersect(left, formulaCell), Intersect(right, formulaCell));
    }

    private static CellData ApplyBinary(BinaryOperator op, CellData left, CellData right)
    {
        if (left.IsError)
        {
            return left;
        }

        if (right.IsError)
        {
            return right;
        }

        if (op == BinaryOperator.Concat)
        {
            return CellData.FromString(ToText(left) + ToText(right));
        }

        // For comparison operators, we don't need both sides to be numeric
        var isComparisonOperator = op is
            BinaryOperator.Equals or BinaryOperator.NotEquals or
            BinaryOperator.LessThan or BinaryOperator.LessThanOrEqual or
            BinaryOperator.GreaterThan or BinaryOperator.GreaterThanOrEqual;

        if (left.IsEmpty)
        {
            left = isComparisonOperator ? EmptyAs(right) : CellData.FromNumber(0d);
        }

        if (right.IsEmpty)
        {
            right = isComparisonOperator ? EmptyAs(left) : CellData.FromNumber(0d);
        }

        // Coerce Date and Boolean to Number for arithmetic operations (Excel semantics)
        if (!isComparisonOperator)
        {
            if (left.Type == CellDataType.Date)
            {
                var ln = left.GetValueOrDefault<DateTime>().ToNumber();
                left = CellData.FromNumber(ln);
            }
            if (right.Type == CellDataType.Date)
            {
                var rn = right.GetValueOrDefault<DateTime>().ToNumber();
                right = CellData.FromNumber(rn);
            }
            if (left.Type == CellDataType.Boolean)
            {
                left = CellData.FromNumber(left.GetValueOrDefault<bool>() ? 1d : 0d);
            }
            if (right.Type == CellDataType.Boolean)
            {
                right = CellData.FromNumber(right.GetValueOrDefault<bool>() ? 1d : 0d);
            }
        }

        if (!isComparisonOperator)
        {
            if (!TryCoerceOperand(ref left) || !TryCoerceOperand(ref right))
            {
                return CellData.FromError(CellError.Value);
            }
        }

        if (op == BinaryOperator.Divide)
        {
            var rnum = right.GetValueOrDefault<double>();

            if (Math.Abs(rnum) == 0d)
            {
                return CellData.FromError(CellError.Div0);
            }
        }

        if (isComparisonOperator)
        {
            switch (op)
            {
                case BinaryOperator.Equals:
                    return CellData.FromBoolean(left.IsEqualTo(right));
                case BinaryOperator.NotEquals:
                    return CellData.FromBoolean(!left.IsEqualTo(right));
                case BinaryOperator.LessThan:
                    return CellData.FromBoolean(left.IsLessThan(right));
                case BinaryOperator.LessThanOrEqual:
                    return CellData.FromBoolean(left.IsLessThanOrEqualTo(right));
                case BinaryOperator.GreaterThan:
                    return CellData.FromBoolean(left.IsGreaterThan(right));
                case BinaryOperator.GreaterThanOrEqual:
                    return CellData.FromBoolean(left.IsGreaterThanOrEqualTo(right));
            }
        }
        else
        {
            var l = left.GetValueOrDefault<double>();
            var r = right.GetValueOrDefault<double>();
            double res = 0d;
            switch (op)
            {
                case BinaryOperator.Plus:
                    res = l + r;
                    break;
                case BinaryOperator.Minus:
                    res = l - r;
                    break;
                case BinaryOperator.Multiply:
                    res = l * r;
                    break;
                case BinaryOperator.Divide:
                    res = l / r;
                    break;
            }
            return CellData.FromNumber(res);
        }

        return CellData.FromError(CellError.Value);
    }

    public void VisitUnaryExpression(UnaryExpressionSyntaxNode unaryExpressionSyntaxNode)
    {
        unaryExpressionSyntaxNode.Operand.Accept(this);
        var operand = value;
        var op = unaryExpressionSyntaxNode.Operator;

        if (arrayContext && operand is RangeList)
        {
            value = Broadcast([operand], operands => ApplyUnary(op, operands[0]));
            return;
        }

        value = ApplyUnary(op, Intersect(operand, formulaCell));
    }

    private static CellData ApplyUnary(UnaryOperator op, CellData operand)
    {
        if (operand.IsError)
        {
            return operand;
        }

        if (op == UnaryOperator.Plus)
        {
            return operand;
        }

        if (operand.IsEmpty)
        {
            operand = CellData.FromNumber(0d);
        }

        if (operand.Type == CellDataType.Date)
        {
            operand = CellData.FromNumber(operand.GetValueOrDefault<DateTime>().ToNumber());
        }

        if (!TryCoerceOperand(ref operand))
        {
            return CellData.FromError(CellError.Value);
        }

        return CellData.FromNumber(-operand.GetValueOrDefault<double>());
    }

    private static RangeList Broadcast(IReadOnlyList<object?> operands, Func<CellData[], CellData> apply)
    {
        var arrays = operands.OfType<RangeList>().ToList();
        var rows = arrays.Aggregate(1, (size, array) => BroadcastSize(size, array.Rows));
        var columns = arrays.Aggregate(1, (size, array) => BroadcastSize(size, array.Columns));
        var rowSource = arrays.First(array => array.Rows == rows);
        var columnSource = arrays.First(array => array.Columns == columns);
        var result = new RangeList(rows, columns, rowSource.StartRow, columnSource.StartColumn, rowSource.Worksheet);
        var elements = new CellData[operands.Count];

        for (var row = 0; row < rows; row++)
        {
            for (var column = 0; column < columns; column++)
            {
                for (var i = 0; i < operands.Count; i++)
                {
                    elements[i] = ElementAt(operands[i], row, column);
                }

                result.Add(apply(elements));
            }
        }

        return result;
    }

    private static int BroadcastSize(int size, int other) => size == 1 ? other : other == 1 ? size : Math.Max(size, other);

    private static CellData ElementAt(object? operand, int row, int column)
    {
        if (operand is not RangeList array)
        {
            return (CellData)operand!;
        }

        var arrayRow = array.Rows == 1 ? 0 : row;
        var arrayColumn = array.Columns == 1 ? 0 : column;

        if (arrayRow >= array.Rows || arrayColumn >= array.Columns)
        {
            return CellData.FromError(CellError.NA);
        }

        return array[arrayRow * array.Columns + arrayColumn];
    }

    private static CellData Intersect(object? operand, Cell cell)
    {
        if (operand is not RangeList range)
        {
            return (CellData)operand!;
        }

        if (range.Count == 1)
        {
            return range[0];
        }

        var row = cell.Address.Row;
        var column = cell.Address.Column;

        if (range.Columns == 1 && row >= range.StartRow && row < range.StartRow + range.Rows)
        {
            return range[row - range.StartRow];
        }

        if (range.Rows == 1 && column >= range.StartColumn && column < range.StartColumn + range.Columns)
        {
            return range[column - range.StartColumn];
        }

        return CellData.FromError(CellError.Value);
    }

    private static CellData EmptyAs(CellData other)
    {
        return other.Type switch
        {
            CellDataType.String => CellData.FromString(string.Empty),
            CellDataType.Boolean => CellData.FromBoolean(false),
            _ => CellData.FromNumber(0d)
        };
    }

    private static bool TryCoerceOperand(ref CellData operand)
    {
        if (operand.Type == CellDataType.Number)
        {
            return true;
        }

        if (operand.TryCoerceToNumber(out var number, allowBooleans: true, nonNumericTextAsZero: false))
        {
            operand = CellData.FromNumber(number);
            return true;
        }

        return false;
    }

    private static string ToText(CellData data)
    {
        return data.Type switch
        {
            CellDataType.Empty => string.Empty,
            CellDataType.Number => data.GetValueOrDefault<double>().ToString(CultureInfo.InvariantCulture),
            CellDataType.Date => data.GetValueOrDefault<DateTime>().ToNumber().ToString(CultureInfo.InvariantCulture),
            _ => data.ToString()
        };
    }

    private CellData EvaluateCell(Cell cell)
    {
        if (cell.FormulaSyntaxTree is null)
        {
            return cell.Data;
        }

        if (evaluated.TryGetValue(cell, out var cached))
        {
            return cached;
        }

        if (cell.FormulaSyntaxTree.Errors.Count > 0)
        {
            return CellData.FromError(CellError.Name);
        }

        if (!evaluationStack.Add(cell))
        {
            return CellData.FromError(CellError.Circular);
        }

        var outerCell = formulaCell;
        var outerArrayContext = arrayContext;
        formulaCell = cell;
        arrayContext = false;

        cell.FormulaSyntaxTree.Root.Accept(this);
        var result = Intersect(value, cell);

        formulaCell = outerCell;
        arrayContext = outerArrayContext;
        evaluationStack.Remove(cell);
        evaluated[cell] = result;
        return result;
    }

    public void VisitCell(CellSyntaxNode cellSyntaxNode)
    {
        var address = cellSyntaxNode.Token.Address;
        var targetSheet = sheet;
        if (!string.IsNullOrEmpty(address.Worksheet))
        {
            var wb = sheet.Workbook;
            targetSheet = wb.GetSheet(address.Worksheet) ?? targetSheet;
            if (!string.Equals(targetSheet.Name, address.Worksheet, StringComparison.OrdinalIgnoreCase))
            {
                value = CellData.FromError(CellError.Ref);
                return;
            }
        }

        // If the row/column was deleted, set whole formula to =#REF!
        if (targetSheet.IsDeletedRow(address.Row) || targetSheet.IsDeletedColumn(address.Column))
        {
            value = CellData.FromError(CellError.Ref);
            return;
        }

        if (address.Row < 0 || address.Column < 0)
        {
            value = CellData.FromError(CellError.Ref);
            return;
        }

        if (address.Row >= Worksheet.MaxRows || address.Column >= Worksheet.MaxColumns)
        {
            value = CellData.FromError(CellError.Name);
            return;
        }

        if (address.Row >= targetSheet.RowCount || address.Column >= targetSheet.ColumnCount)
        {
            value = CellData.Empty;
            return;
        }

        if (!targetSheet.Cells.TryGet(address.Row, address.Column, out var cell))
        {
            // Cell is in bounds but not populated - treat as empty
            value = new CellData(null);
            return;
        }

        value = EvaluateCell(cell);
    }

    public void VisitName(NameSyntaxNode nameSyntaxNode)
    {
        var tree = sheet.Workbook.ResolveDefinedName(nameSyntaxNode.Name);

        if (tree is null || tree.Errors.Count > 0)
        {
            value = CellData.FromError(CellError.Name);
            return;
        }

        if (!nameStack.Add(nameSyntaxNode.Name))
        {
            value = CellData.FromError(CellError.Circular);
            return;
        }

        tree.Root.Accept(this);
        nameStack.Remove(nameSyntaxNode.Name);
    }

    public CellData Evaluate(FormulaSyntaxNode node)
    {
        node.Accept(this);

        return Intersect(value, formulaCell);
    }


    public void VisitFunction(FunctionSyntaxNode functionSyntaxNode)
    {
        var function = sheet.FunctionRegistry.Get(functionSyntaxNode.Name);

        var functionArguments = ProcessArguments(function, functionSyntaxNode.Arguments, out var arrayArguments);
        
        if (functionArguments is null)
        {
            return; // Error already set in ProcessArguments
        }

        if (arrayArguments is null)
        {
            value = function.Evaluate(functionArguments);
            return;
        }

        value = Broadcast([.. arrayArguments.Select(argument => argument.Values)], elements => EvaluateElement(function, functionArguments, arrayArguments, elements));
    }

    private static CellData EvaluateElement(FormulaFunction function, FunctionArguments functionArguments, List<(string Name, RangeList Values)> arrayArguments, CellData[] elements)
    {
        for (var i = 0; i < arrayArguments.Count; i++)
        {
            if (elements[i].IsError && !function.CanHandleErrors)
            {
                return elements[i];
            }

            functionArguments.Set(arrayArguments[i].Name, elements[i]);
        }

        return function.Evaluate(functionArguments);
    }

    private FunctionArguments? ProcessArguments(FormulaFunction function, List<FormulaSyntaxNode> argumentNodes, out List<(string Name, RangeList Values)>? arrayArguments)
    {
        arrayArguments = null;
        var parameterDefinitions = function.Parameters;
        var functionArguments = new FunctionArguments(formulaCell);
        var argumentIndex = 0;

        for (int paramIndex = 0; paramIndex < parameterDefinitions.Length; paramIndex++)
        {
            var paramDef = parameterDefinitions[paramIndex];
            
            // Check if we have enough arguments for required parameters
            // But only if this is not a repeating parameter (repeating parameters can handle empty lists)
            if (paramDef.IsRequired &&
                paramDef.Type != ParameterType.Sequence &&
                paramDef.Type != ParameterType.Group &&
                argumentIndex >= argumentNodes.Count)
            {
                value = CellData.FromError(CellError.Value);
                return null;
            }

            if (paramDef.Type == ParameterType.Sequence)
            {
                var allArguments = new List<CellData>();
                while (argumentIndex < argumentNodes.Count)
                {
                    var argumentNode = argumentNodes[argumentIndex];
                    var argument = ProcessArgument(argumentNode, function, paramDef.Type);
                    if (argument is null)
                    {
                        return null; // Error already set
                    }

                    // Excel coerces literal boolean constants to numbers in aggregate functions
                    // (e.g. SUM(TRUE,1)→2) but skips booleans from cell references.
                    if (function.CoerceLiteralBooleans && argumentNode is BooleanLiteralSyntaxNode)
                    {
                        for (int i = 0; i < argument.Count; i++)
                        {
                            if (argument[i].Type == CellDataType.Boolean)
                            {
                                argument[i] = CellData.FromNumber(argument[i].GetValueOrDefault<bool>() ? 1d : 0d);
                            }
                        }
                    }

                    allArguments.AddRange(argument);
                    argumentIndex++;
                }
                functionArguments.Set(paramDef.Name, allArguments);
            }
            else if (paramDef.Type == ParameterType.Group)
            {
                // Collect all remaining arguments WITHOUT flattening - each stays its own group so
                // (range, criteria) pairs and array shapes survive. A single cell is a one-element list
                // (not a RangeList); consumers use the `as RangeList` / default-1xN convention.
                var groups = new List<List<CellData>>();
                while (argumentIndex < argumentNodes.Count)
                {
                    var argument = ProcessArgument(argumentNodes[argumentIndex], function, paramDef.Type);
                    if (argument is null)
                    {
                        return null; // Error already set
                    }

                    groups.Add(argument);
                    argumentIndex++;
                }
                functionArguments.Set(paramDef.Name, groups);
            }
            else
            {
                if (argumentIndex < argumentNodes.Count)
                {
                    var argument = ProcessArgument(argumentNodes[argumentIndex], function, paramDef.Type);
                    if (argument is null)
                    {
                        return null; // Error already set
                    }
                    
                    if (paramDef.Type == ParameterType.Collection)
                    {
                        if (argumentNodes[argumentIndex] is CellSyntaxNode cellNode)
                        {
                            argument = new RangeList(1, 1, cellNode.Token.Address.Row, cellNode.Token.Address.Column, sheet) { argument[0] };
                        }
                        functionArguments.Set(paramDef.Name, argument);
                    }
                    else
                    {
                        if (argument is RangeList { Count: > 1 } values)
                        {
                            (arrayArguments ??= []).Add((paramDef.Name, values));
                        }

                        functionArguments.Set(paramDef.Name, argument[0]);
                    }
                    argumentIndex++;
                }
                else if (!paramDef.IsRequired)
                {
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

    private List<CellData>? ProcessArgument(FormulaSyntaxNode argumentNode, FormulaFunction function, ParameterType parameterType)
    {
        var outerArrayContext = arrayContext;
        arrayContext = outerArrayContext || parameterType != ParameterType.Single;
        argumentNode.Accept(this);
        arrayContext = outerArrayContext;

        if (!arrayContext && parameterType == ParameterType.Single && value is RangeList)
        {
            value = Intersect(value, formulaCell);
        }

        var hasError = value is CellData cellData && cellData.IsError;

        // Only short-circuit on errors if the function cannot handle them
        if (hasError && !function.CanHandleErrors)
        {
            return null;
        }

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
        var start = rangeSyntaxNode.Start.Token.Address;
        var end = rangeSyntaxNode.End.Token.Address;

        var startSheet = sheet;
        if (!string.IsNullOrEmpty(start.Worksheet))
        {
            var wb = sheet.Workbook;
            startSheet = wb.GetSheet(start.Worksheet) ?? startSheet;
            if (!string.Equals(startSheet.Name, start.Worksheet, StringComparison.OrdinalIgnoreCase))
            {
                value = CellData.FromError(CellError.Ref);
                return;
            }
        }

        // If end has no sheet specified, assume same as start; otherwise they must match
        var endSheet = startSheet;
        if (!string.IsNullOrEmpty(end.Worksheet))
        {
            endSheet = sheet.Workbook.GetSheet(end.Worksheet) ?? endSheet;
            if (!string.Equals(endSheet.Name, end.Worksheet, StringComparison.OrdinalIgnoreCase) || endSheet != startSheet)
            {
                value = CellData.FromError(CellError.Ref);
                return;
            }
        }

        if (start.Row > end.Row || (start.Row == end.Row && start.Column > end.Column))
        {
            value = CellData.FromError(CellError.Value);
            return;
        }

        if (start.Row >= Worksheet.MaxRows || end.Row >= Worksheet.MaxRows || start.Column >= Worksheet.MaxColumns || end.Column >= Worksheet.MaxColumns)
        {
            value = CellData.FromError(CellError.Name);
            return;
        }

        var rows = end.Row - start.Row + 1;
        var columns = end.Column - start.Column + 1;

        if ((long)rows * columns > Worksheet.MaxRows)
        {
            rows = Math.Clamp(startSheet.RowCount - start.Row, 1, rows);
            columns = Math.Clamp(startSheet.ColumnCount - start.Column, 1, columns);
        }

        var cells = new RangeList(rows, columns, start.Row, start.Column, startSheet);

        for (var row = start.Row; row < start.Row + rows; row++)
        {
            for (var column = start.Column; column < start.Column + columns; column++)
            {
                if (startSheet.IsDeletedRow(row) || startSheet.IsDeletedColumn(column))
                {
                    value = CellData.FromError(CellError.Ref);
                    return;
                }

                if (row < 0 || column < 0)
                {
                    value = CellData.FromError(CellError.Ref);
                    return;
                }

                CellData cellValue;

                if (row >= startSheet.RowCount || column >= startSheet.ColumnCount)
                {
                    cellValue = CellData.Empty;
                }
                else if (!startSheet.Cells.TryGet(row, column, out var cell))
                {
                    // Cell is in bounds but not populated - treat as empty
                    cellValue = new CellData(null);
                }
                else
                {
                    cellValue = EvaluateCell(cell);
                }
                // Do not short-circuit on errors here; include them in the range so
                // functions like SUBTOTAL/AGGREGATE can decide how to handle them.
                cells.Add(cellValue);
            }
        }

        value = cells;
    }
}