using System;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class FunctionRegistryTests
{
    private readonly FunctionRegistry functionRegistry = new();

    [Theory]
    [InlineData(2, -1)]
    [InlineData(4, -1)]
    [InlineData(5, 0)]
    [InlineData(6, 0)]
    [InlineData(7, 1)]
    [InlineData(8, 1)]
    [InlineData(9, 2)]
    [InlineData(10, 2)]
    public void Basic_Function_Provides_Correct_Arg_Index(int cursorPosition, int expectedArgIndex)
    {
        var func = "=SUM(1,2,3)";
        var result = functionRegistry.CreateFunctionHint(func, cursorPosition);

        Assert.NotNull(result);

        Assert.Equal(expectedArgIndex, result.ArgumentIndex);
        Assert.IsType<SumFunction>(result.Function);
    }
    [Theory]
    [InlineData(5, 0, "=SUM(")]
    [InlineData(4, -1, "=SUM(")]
    public void Basic_Function_Provides_Correct_Arg_Index_With_IncompleteFormula(int cursorPosition, int expectedArgIndex, string formula)
    {
        var result = functionRegistry.CreateFunctionHint(formula, cursorPosition);

        Assert.NotNull(result);

        Assert.Equal(expectedArgIndex, result.ArgumentIndex);
        Assert.IsType<SumFunction>(result.Function);
    }

    [Fact]
    public void Position_Outside_Of_Formula_Returns_null()
    {
        var func = "=1 + SUM(1,2, 3) + 2";
        var result = functionRegistry.CreateFunctionHint(func, 0);

        Assert.Null(result);

        result = functionRegistry.CreateFunctionHint(func, 5);
        Assert.Null(result);

        result = functionRegistry.CreateFunctionHint(func, 16);
        Assert.Null(result);
    }

    [Theory]
    [InlineData(5, 0, typeof(SumFunction))]
    [InlineData(7, 1, typeof(SumFunction))]
    [InlineData(8, -1, typeof(CountFunction))]
    [InlineData(13, 0, typeof(CountFunction))]
    [InlineData(15, 1, typeof(CountFunction))]
    [InlineData(17, 1, typeof(SumFunction))]
    public void Nested_Function_Produces_Correct_ArgIndex(int cursorPosition, int expectedArgIndex, Type expectedFunction)
    {
        var func = "=SUM(1,COUNT(1,2),3)";
        var result = functionRegistry.CreateFunctionHint(func, cursorPosition);

        Assert.NotNull(result);

        Assert.Equal(expectedArgIndex, result.ArgumentIndex);
        Assert.IsType(expectedFunction, result.Function);
    }
}