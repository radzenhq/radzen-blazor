namespace Radzen.Blazor.Spreadsheet.Tests;

using System;
using Xunit;

public class FormulaParserTests
{
    [Fact]
    public void FormulaParser_ShouldRequireEqualsAtStart()
    {
        var formula = "A1";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.NotEmpty(syntaxTree.Errors);
        Assert.Contains("Unexpected token", syntaxTree.Errors[0]);
    }

    [Fact]
    public void FormulaParser_ShouldParseNumberLiteral()
    {
        var formula = "=123";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        Assert.IsType<NumberLiteralSyntaxNode>(syntaxTree.Root);
        var numberNode = (NumberLiteralSyntaxNode)syntaxTree.Root;
        Assert.Equal(123, numberNode.Token.IntValue);
    }

    [Fact]
    public void FormulaParser_ShouldParseAdditionOfTwoNumberLiterals()
    {
        var formula = "=123+456";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        Assert.IsType<BinaryExpressionSyntaxNode>(syntaxTree.Root);
        var binaryNode = (BinaryExpressionSyntaxNode)syntaxTree.Root;
        Assert.Equal(BinaryOperator.Plus, binaryNode.Operator);
        Assert.IsType<NumberLiteralSyntaxNode>(binaryNode.Left);
        Assert.IsType<NumberLiteralSyntaxNode>(binaryNode.Right);
        Assert.Equal(123, ((NumberLiteralSyntaxNode)binaryNode.Left).Token.IntValue);
        Assert.Equal(456, ((NumberLiteralSyntaxNode)binaryNode.Right).Token.IntValue);
    }

    [Fact]
    public void FormulaParser_ShouldParseAdditionOfMultipleNumberLiterals()
    {
        var formula = "=123+456+789";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<BinaryExpressionSyntaxNode>(node);
        var binaryNode = (BinaryExpressionSyntaxNode)node;
        Assert.Equal(BinaryOperator.Plus, binaryNode.Operator);
        Assert.IsType<BinaryExpressionSyntaxNode>(binaryNode.Left);
        var leftBinaryNode = (BinaryExpressionSyntaxNode)binaryNode.Left;
        Assert.Equal(BinaryOperator.Plus, leftBinaryNode.Operator);
        Assert.Equal(123, ((NumberLiteralSyntaxNode)leftBinaryNode.Left).Token.IntValue);
        Assert.Equal(456, ((NumberLiteralSyntaxNode)leftBinaryNode.Right).Token.IntValue);
        Assert.Equal(789, ((NumberLiteralSyntaxNode)binaryNode.Right).Token.IntValue);
    }

    [Fact]
    public void FormulaParser_ShouldParseSubtractionOfTwoNumberLiterals()
    {
        var formula = "=123-456";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<BinaryExpressionSyntaxNode>(node);
        var binaryNode = (BinaryExpressionSyntaxNode)node;
        Assert.Equal(BinaryOperator.Minus, binaryNode.Operator);
        Assert.IsType<NumberLiteralSyntaxNode>(binaryNode.Left);
        Assert.IsType<NumberLiteralSyntaxNode>(binaryNode.Right);
        Assert.Equal(123, ((NumberLiteralSyntaxNode)binaryNode.Left).Token.IntValue);
        Assert.Equal(456, ((NumberLiteralSyntaxNode)binaryNode.Right).Token.IntValue);
    }

    [Fact]
    public void FormulaParser_ShouldParseSubtractionOfMultipleNumberLiterals()
    {
        var formula = "=123-456-789";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<BinaryExpressionSyntaxNode>(node);
        var binaryNode = (BinaryExpressionSyntaxNode)node;
        Assert.Equal(BinaryOperator.Minus, binaryNode.Operator);
        Assert.IsType<BinaryExpressionSyntaxNode>(binaryNode.Left);
        var leftBinaryNode = (BinaryExpressionSyntaxNode)binaryNode.Left;
        Assert.Equal(BinaryOperator.Minus, leftBinaryNode.Operator);
        Assert.Equal(123, ((NumberLiteralSyntaxNode)leftBinaryNode.Left).Token.IntValue);
        Assert.Equal(456, ((NumberLiteralSyntaxNode)leftBinaryNode.Right).Token.IntValue);
        Assert.Equal(789, ((NumberLiteralSyntaxNode)binaryNode.Right).Token.IntValue);
    }

    [Fact]
    public void FormulaParser_ShouldParseMultiplicationOfTwoNumberLiterals()
    {
        var formula = "=123*456";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<BinaryExpressionSyntaxNode>(node);
        var binaryNode = (BinaryExpressionSyntaxNode)node;
        Assert.Equal(BinaryOperator.Multiply, binaryNode.Operator);
        Assert.IsType<NumberLiteralSyntaxNode>(binaryNode.Left);
        Assert.IsType<NumberLiteralSyntaxNode>(binaryNode.Right);
        Assert.Equal(123, ((NumberLiteralSyntaxNode)binaryNode.Left).Token.IntValue);
        Assert.Equal(456, ((NumberLiteralSyntaxNode)binaryNode.Right).Token.IntValue);
    }

    [Fact]
    public void FormulaParse_MultiplicationPrecedence()
    {
        var formula = "=123+456*789";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<BinaryExpressionSyntaxNode>(node);
        var binaryNode = (BinaryExpressionSyntaxNode)node;
        Assert.Equal(BinaryOperator.Plus, binaryNode.Operator);
        Assert.IsType<NumberLiteralSyntaxNode>(binaryNode.Left);
        Assert.IsType<BinaryExpressionSyntaxNode>(binaryNode.Right);
        var rightBinaryNode = (BinaryExpressionSyntaxNode)binaryNode.Right;
        Assert.Equal(BinaryOperator.Multiply, rightBinaryNode.Operator);
        Assert.Equal(456, ((NumberLiteralSyntaxNode)rightBinaryNode.Left).Token.IntValue);
        Assert.Equal(789, ((NumberLiteralSyntaxNode)rightBinaryNode.Right).Token.IntValue);
    }

    [Fact]
    public void FormulaParser_ShouldParseDivisionOfTwoNumberLiterals()
    {
        var formula = "=123/456";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<BinaryExpressionSyntaxNode>(node);
        var binaryNode = (BinaryExpressionSyntaxNode)node;
        Assert.Equal(BinaryOperator.Divide, binaryNode.Operator);
        Assert.IsType<NumberLiteralSyntaxNode>(binaryNode.Left);
        Assert.IsType<NumberLiteralSyntaxNode>(binaryNode.Right);
        Assert.Equal(123, ((NumberLiteralSyntaxNode)binaryNode.Left).Token.IntValue);
        Assert.Equal(456, ((NumberLiteralSyntaxNode)binaryNode.Right).Token.IntValue);
    }

    [Fact]
    public void FormulaParser_ShouldParseParentheses()
    {
        var formula = "=(123+456)*789";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<BinaryExpressionSyntaxNode>(node);
        var binaryNode = (BinaryExpressionSyntaxNode)node;
        Assert.Equal(BinaryOperator.Multiply, binaryNode.Operator);
        Assert.IsType<BinaryExpressionSyntaxNode>(binaryNode.Left);
        Assert.IsType<NumberLiteralSyntaxNode>(binaryNode.Right);
        var leftBinaryNode = (BinaryExpressionSyntaxNode)binaryNode.Left;
        Assert.Equal(789, ((NumberLiteralSyntaxNode)binaryNode.Right).Token.IntValue);
        Assert.Equal(BinaryOperator.Plus, leftBinaryNode.Operator);
        Assert.Equal(123, ((NumberLiteralSyntaxNode)leftBinaryNode.Left).Token.IntValue);
        Assert.Equal(456, ((NumberLiteralSyntaxNode)leftBinaryNode.Right).Token.IntValue);
    }

    [Fact]
    public void FormulaParser_ShouldParseNestedParentheses()
    {
        var formula = "=((123+456)*789)/101112";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<BinaryExpressionSyntaxNode>(node);
        var binaryNode = (BinaryExpressionSyntaxNode)node;
        Assert.Equal(BinaryOperator.Divide, binaryNode.Operator);
        Assert.IsType<BinaryExpressionSyntaxNode>(binaryNode.Left);
        Assert.IsType<NumberLiteralSyntaxNode>(binaryNode.Right);
        var leftBinaryNode = (BinaryExpressionSyntaxNode)binaryNode.Left;
        Assert.Equal(101112, ((NumberLiteralSyntaxNode)binaryNode.Right).Token.IntValue);
        Assert.Equal(BinaryOperator.Multiply, leftBinaryNode.Operator);
        Assert.IsType<BinaryExpressionSyntaxNode>(leftBinaryNode.Left);
        var leftLeftBinaryNode = (BinaryExpressionSyntaxNode)leftBinaryNode.Left;
        Assert.Equal(123, ((NumberLiteralSyntaxNode)leftLeftBinaryNode.Left).Token.IntValue);
        Assert.Equal(456, ((NumberLiteralSyntaxNode)leftLeftBinaryNode.Right).Token.IntValue);
        Assert.Equal(789, ((NumberLiteralSyntaxNode)leftBinaryNode.Right).Token.IntValue);
    }

    [Fact]
    public void FormulaParser_ShouldParseCellIndentifer()
    {
        var formula = "=A1";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<CellSyntaxNode>(node);
        var cellIdentifierNode = (CellSyntaxNode)node;
        Assert.Equal("A1", cellIdentifierNode.Token.Address.ToString());
    }

    [Fact]
    public void FormulaParser_ShouldParseFunction()
    {
        var formula = "=SUM(A1,1)";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<FunctionSyntaxNode>(node);
        var functionNode = (FunctionSyntaxNode)node;
        Assert.Equal("SUM", functionNode.Name);
        Assert.Equal(2, functionNode.Arguments.Count);
        Assert.IsType<CellSyntaxNode>(functionNode.Arguments[0]);
        Assert.IsType<NumberLiteralSyntaxNode>(functionNode.Arguments[1]);

        var cellIdentifierNode = (CellSyntaxNode)functionNode.Arguments[0];
        Assert.Equal("A1", cellIdentifierNode.Token.Address.ToString());
        var numberLiteralNode = (NumberLiteralSyntaxNode)functionNode.Arguments[1];
        Assert.Equal(1, numberLiteralNode.Token.IntValue);
    }

    [Fact]
    public void FormulaParser_ShouldParseNestedFunctions()
    {
        var formula = "=SUM(A1,MAX(B1,C1))";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<FunctionSyntaxNode>(node);
        var functionNode = (FunctionSyntaxNode)node;
        Assert.Equal("SUM", functionNode.Name);
        Assert.Equal(2, functionNode.Arguments.Count);
        Assert.IsType<CellSyntaxNode>(functionNode.Arguments[0]);
        Assert.IsType<FunctionSyntaxNode>(functionNode.Arguments[1]);

        var cellIdentifierNode = (CellSyntaxNode)functionNode.Arguments[0];
        Assert.Equal("A1", cellIdentifierNode.Token.Address.ToString());

        var nestedFunctionNode = (FunctionSyntaxNode)functionNode.Arguments[1];
        Assert.Equal("MAX", nestedFunctionNode.Name);
        Assert.Equal(2, nestedFunctionNode.Arguments.Count);
        Assert.IsType<CellSyntaxNode>(nestedFunctionNode.Arguments[0]);
        Assert.IsType<CellSyntaxNode>(nestedFunctionNode.Arguments[1]);

        var firstCellIdentifierNode = (CellSyntaxNode)nestedFunctionNode.Arguments[0];
        var secondCellIdentifierNode = (CellSyntaxNode)nestedFunctionNode.Arguments[1];
        Assert.Equal("B1", firstCellIdentifierNode.Token.Address.ToString());
        Assert.Equal("C1", secondCellIdentifierNode.Token.Address.ToString());
    }

    [Fact]
    public void FormulaParser_ShouldParseFunctionWithNoArguments()
    {
        var formula = "=SUM()";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<FunctionSyntaxNode>(node);
        var functionNode = (FunctionSyntaxNode)node;
        Assert.Equal("SUM", functionNode.Name);
        Assert.Empty(functionNode.Arguments);
    }

    [Fact]
    public void FormulaParser_ShouldParseCellRange()
    {
        var formula = "=A1:A2";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<RangeSyntaxNode>(node);
        var rangeNode = (RangeSyntaxNode)node;
        Assert.Equal("A1", rangeNode.Start.Token.Address.ToString());
        Assert.Equal("A2", rangeNode.End.Token.Address.ToString());
    }

    [Fact]
    public void FormulaParser_ShouldParseCellRangeInFunction()
    {
        var formula = "=SUM(A1:A2)";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<FunctionSyntaxNode>(node);
        var functionNode = (FunctionSyntaxNode)node;
        Assert.Equal("SUM", functionNode.Name);
        Assert.Single(functionNode.Arguments);
        Assert.IsType<RangeSyntaxNode>(functionNode.Arguments[0]);
        var rangeNode = (RangeSyntaxNode)functionNode.Arguments[0];
        Assert.Equal("A1", rangeNode.Start.Token.Address.ToString());
        Assert.Equal("A2", rangeNode.End.Token.Address.ToString());
    }

    [Fact]
    public void FormulaParser_ShouldHandleInvalidRange()
    {
        var formula = "=A2:A1";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<RangeSyntaxNode>(node);
        var rangeNode = (RangeSyntaxNode)node;
        Assert.Equal("A2", rangeNode.Start.Token.Address.ToString());
        Assert.Equal("A1", rangeNode.End.Token.Address.ToString());
    }

    [Fact]
    public void FormulaParser_ShouldHandleSingleCellRange()
    {
        var formula = "=A1:A1";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<RangeSyntaxNode>(node);
        var rangeNode = (RangeSyntaxNode)node;
        Assert.Equal("A1", rangeNode.Start.Token.Address.ToString());
        Assert.Equal("A1", rangeNode.End.Token.Address.ToString());
    }

    [Fact]
    public void FormulaParser_ShouldHandleMultiColumnRange()
    {
        var formula = "=A1:B1";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<RangeSyntaxNode>(node);
        var rangeNode = (RangeSyntaxNode)node;
        Assert.Equal("A1", rangeNode.Start.Token.Address.ToString());
        Assert.Equal("B1", rangeNode.End.Token.Address.ToString());
    }

    [Fact]
    public void FormulaParser_ShouldHandleMultiRowRange()
    {
        var formula = "=A1:A2";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<RangeSyntaxNode>(node);
        var rangeNode = (RangeSyntaxNode)node;
        Assert.Equal("A1", rangeNode.Start.Token.Address.ToString());
        Assert.Equal("A2", rangeNode.End.Token.Address.ToString());
    }

    [Fact]
    public void FormulaParser_ShouldHandleMultiCellRange()
    {
        var formula = "=A1:B2";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.IsType<RangeSyntaxNode>(node);
        var rangeNode = (RangeSyntaxNode)node;
        Assert.Equal("A1", rangeNode.Start.Token.Address.ToString());
        Assert.Equal("B2", rangeNode.End.Token.Address.ToString());
    }

    [Fact]
    public void FormulaParser_ShouldAddErrorOnInvalidFormula()
    {
        var formula = "A1"; // Missing equals sign
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.NotEmpty(syntaxTree.Errors);
        Assert.Contains("Unexpected token", syntaxTree.Errors[0]);
    }

    [Fact]
    public void FormulaParser_ShouldReturnPartialExpressionOnIncompleteExpression()
    {
        var formula = "=123+"; // Incomplete expression
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.NotEmpty(syntaxTree.Errors);
        Assert.IsType<BinaryExpressionSyntaxNode>(syntaxTree.Root);
        if (syntaxTree.Root is BinaryExpressionSyntaxNode binaryNode)
        {
            Assert.Equal(BinaryOperator.Plus, binaryNode.Operator);
            Assert.IsType<NumberLiteralSyntaxNode>(binaryNode.Left);
            Assert.Equal(123, ((NumberLiteralSyntaxNode)binaryNode.Left).Token.IntValue);
        }
    }

    [Fact]
    public void FormulaParser_ShouldAddErrorOnIncompleteExpression()
    {
        var formula = "=123+"; // Incomplete expression
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.NotEmpty(syntaxTree.Errors);
    }

    [Fact]
    public void FormulaParser_ShouldReturnPartialFunctionOnMissingCloseParen()
    {
        var formula = "=SUM(A1"; // Missing closing parenthesis
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.NotEmpty(syntaxTree.Errors);
        Assert.IsType<FunctionSyntaxNode>(syntaxTree.Root);
        var functionNode = (FunctionSyntaxNode)syntaxTree.Root;
        Assert.Equal("SUM", functionNode.Name);
        Assert.Single(functionNode.Arguments);
        Assert.IsType<CellSyntaxNode>(functionNode.Arguments[0]);
    }

    [Fact]
    public void FormulaParser_ShouldAddErrorOnInvalidFunctionSyntax()
    {
        var formula = "=SUM(A1"; // Missing closing parenthesis
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.NotEmpty(syntaxTree.Errors);
    }

    [Fact]
    public void FormulaParser_ShouldParseGroupedExpression()
    {
        var formula = "=(A1)"; // Parentheses without function name should parse as grouped expression
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors); // This should actually succeed as it's a valid grouped expression
        Assert.IsType<CellSyntaxNode>(syntaxTree.Root);
    }

    [Fact]
    public void FormulaParser_ShouldReturnPartialRangeOnIncompleteRange()
    {
        var formula = "=A1:"; // Incomplete range
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.NotEmpty(syntaxTree.Errors);
        Assert.NotNull(syntaxTree.Root);
    }

    [Fact]
    public void FormulaParser_ShouldAddErrorOnInvalidRange()
    {
        var formula = "=A1:"; // Incomplete range
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.NotEmpty(syntaxTree.Errors);
    }

    [Fact]
    public void FormulaParser_ShouldHandleUnterminatedString()
    {
        var formula = "=\"hello"; // Unterminated string literal
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors); // Should succeed as lexer handles unterminated strings
        Assert.IsType<StringLiteralSyntaxNode>(syntaxTree.Root);
        var stringNode = (StringLiteralSyntaxNode)syntaxTree.Root;
        Assert.Equal("hello", stringNode.Token.Value);
    }

    [Fact]
    public void FormulaParser_ShouldHandleMissingOperand()
    {
        var formula = "=*5"; // Missing left operand
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.NotEmpty(syntaxTree.Errors); // Should have errors
        Assert.NotNull(syntaxTree.Root);
    }

    [Fact]
    public void FormulaParser_ShouldReturnPartialExpressionOnUnbalancedParentheses()
    {
        var formula = "=(A1+B1"; // Missing closing parenthesis
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.NotEmpty(syntaxTree.Errors);
        Assert.IsType<BinaryExpressionSyntaxNode>(syntaxTree.Root); // Should return the binary expression inside
        var binaryNode = (BinaryExpressionSyntaxNode)syntaxTree.Root;
        Assert.Equal(BinaryOperator.Plus, binaryNode.Operator);
        Assert.IsType<CellSyntaxNode>(binaryNode.Left);
        Assert.IsType<CellSyntaxNode>(binaryNode.Right);
    }

    [Fact]
    public void FormulaParser_ShouldAddErrorOnUnbalancedParentheses()
    {
        var formula = "=(A1+B1"; // Missing closing parenthesis
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.NotEmpty(syntaxTree.Errors);
    }

    [Fact]
    public void FormulaParser_ShouldReturnPartialFunctionOnIncompleteArguments()
    {
        var formula = "=SUM(A1,"; // Incomplete function arguments
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.NotEmpty(syntaxTree.Errors);
        Assert.IsType<FunctionSyntaxNode>(syntaxTree.Root);
        var functionNode = (FunctionSyntaxNode)syntaxTree.Root;
        Assert.Equal("SUM", functionNode.Name);
        Assert.True(functionNode.Arguments.Count >= 1); // Should have at least the first argument
        Assert.IsType<CellSyntaxNode>(functionNode.Arguments[0]);
    }

    [Fact]
    public void FormulaParser_DefaultBehavior_ShouldStillWork()
    {
        // Test that default behavior still works as before
        var formula = "=123+456";
        var syntaxTree = FormulaParser.Parse(formula);
        Assert.Empty(syntaxTree.Errors);
        var node = syntaxTree.Root;
        Assert.NotNull(node);
        Assert.IsType<BinaryExpressionSyntaxNode>(node);
    }
}