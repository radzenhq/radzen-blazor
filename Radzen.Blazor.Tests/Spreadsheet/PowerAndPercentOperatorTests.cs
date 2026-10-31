using System;
using System.Globalization;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

public class PowerAndPercentOperatorTests
{
    private static object? Evaluate(string formula)
    {
        var sheet = new Worksheet(30, 10);
        sheet.Cells["A1"].Value = 2d;
        sheet.Cells["A2"].Value = 3d;
        sheet.Cells["A4"].Value = "abc";
        sheet.Cells["A5"].Format.NumberFormat = "@";
        sheet.Cells["A5"].Value = "5";
        sheet.Cells["A6"].Value = true;
        sheet.Cells["A8"].Formula = "=DATE(2020,1,2)";
        sheet.Cells["H20"].Formula = formula;
        return sheet.Cells["H20"].Value;
    }

    [Theory]
    [InlineData("=2^3", 8d)]
    [InlineData("=2^3^2", 64d)]
    [InlineData("=-2^2", 4d)]
    [InlineData("=2^-1", 0.5d)]
    [InlineData("=(-2)^2", 4d)]
    [InlineData("=-(2^2)", -4d)]
    [InlineData("=2^(3^2)", 512d)]
    [InlineData("=(2^3)^2", 64d)]
    [InlineData("=2^0.5", 1.4142135623730951d)]
    [InlineData("=0^0", CellError.Num)]
    [InlineData("=0^-1", CellError.Div0)]
    [InlineData("=0^0.5", 0d)]
    [InlineData("=(-8)^(1/3)", -1.9999999999999998d)]
    [InlineData("=(-1)^0.5", CellError.Num)]
    [InlineData("=(-2)^3", -8d)]
    [InlineData("=10^308*10", CellError.Num)]
    [InlineData("=10^309", CellError.Num)]
    [InlineData("=2^1024", CellError.Num)]
    [InlineData("=(-10)^309", CellError.Num)]
    [InlineData("=A1^A2", 8d)]
    [InlineData("=A3^2", 0d)]
    [InlineData("=2^A3", 1d)]
    [InlineData("=A3^A3", CellError.Num)]
    [InlineData("=A4^2", CellError.Value)]
    [InlineData("=2^A4", CellError.Value)]
    [InlineData("=A5^2", 25d)]
    [InlineData("=A6^2", 1d)]
    [InlineData("=A8^2", 1921244224d)]
    [InlineData("=2*3^2", 18d)]
    [InlineData("=2+3^2", 11d)]
    [InlineData("=2^3*4", 32d)]
    [InlineData("=2^3&\"x\"", "8x")]
    [InlineData("=2^3=8", true)]
    [InlineData("=10%", 0.1d)]
    [InlineData("=5%%", 0.0005d)]
    [InlineData("=1%%%", 1E-06d)]
    [InlineData("=-10%", -0.1d)]
    [InlineData("=+10%", 0.1d)]
    [InlineData("=-(10%)", -0.1d)]
    [InlineData("=(-10)%", -0.1d)]
    [InlineData("=A1%", 0.02d)]
    [InlineData("=A3%", 0d)]
    [InlineData("=A4%", CellError.Value)]
    [InlineData("=A5%", 0.05d)]
    [InlineData("=A6%", 0.01d)]
    [InlineData("=A8%", 438.32d)]
    [InlineData("=(1+2)%", 0.03d)]
    [InlineData("=2^10%", 1.0717734625362931d)]
    [InlineData("=10%^2", 0.010000000000000002d)]
    [InlineData("=50%*200", 100d)]
    [InlineData("=A1%+1", 1.02d)]
    [InlineData("=100*5%", 5d)]
    [InlineData("=\"5\"%", 0.05d)]
    [InlineData("=TRUE%", 0.01d)]
    [InlineData("=1/0%", CellError.Div0)]
    [InlineData("=SUM(10%,20%)", 0.30000000000000004d)]
    [InlineData("=SUM(A1^2,A2^2)", 13d)]
    [InlineData("=\"a\"&2^3", "a8")]
    [InlineData("=#REF!^2", CellError.Ref)]
    [InlineData("=2^#REF!", CellError.Ref)]
    [InlineData("=#REF!%", CellError.Ref)]
    [InlineData("=POWER(0,0)", CellError.Num)]
    [InlineData("=POWER(0,-1)", CellError.Div0)]
    [InlineData("=POWER(2,1024)", CellError.Num)]
    [InlineData("=POWER(2,3)", 8d)]
    [InlineData("=SUM(A1:A2^2)", 13d)]
    [InlineData("=SUM(A1:A2%)", 0.05d)]
    [InlineData("=2^3%", 1.0210121257071934d)]
    [InlineData("=-2^-2", 0.25d)]
    [InlineData("=--2^2", 4d)]
    [InlineData("=-A1^2", 4d)]
    [InlineData("=A1^-A2", 0.125d)]
    [InlineData("=1+2%", 1.02d)]
    [InlineData("=(1+2)^2%", 1.0222154132784771d)]
    [InlineData("=-5%^2", 0.0025000000000000005d)]
    [InlineData("=(-8)^(2/3)", CellError.Num)]
    [InlineData("=(-8)^(-1/3)", -0.50000000000000011d)]
    [InlineData("=(-32)^(1/5)", -2d)]
    [InlineData("=(-32)^0.2", -2d)]
    [InlineData("=(-27)^(1/3)", -2.9999999999999996d)]
    [InlineData("=(-4)^(1/2)", CellError.Num)]
    [InlineData("=(-8)^(1/3+1)", CellError.Num)]
    [InlineData("=(-2)^0.5", CellError.Num)]
    [InlineData("=(-2)^1.5", CellError.Num)]
    [InlineData("=(-8)^0.333", CellError.Num)]
    [InlineData("=(-8)^0.3333333333", CellError.Num)]
    public void Evaluates_LikeExcel(string formula, object expected)
    {
        var actual = Evaluate(formula);

        if (expected is double number)
        {
            var tolerance = 1e-9 * Math.Max(1d, Math.Abs(number));
            Assert.InRange(Convert.ToDouble(actual, CultureInfo.InvariantCulture), number - tolerance, number + tolerance);
        }
        else
        {
            Assert.Equal(expected, actual);
        }
    }

    [Theory]
    [InlineData("=2^3")]
    [InlineData("=2^3^2")]
    [InlineData("=-2^2")]
    [InlineData("=2^-1")]
    [InlineData("=(-2)^2")]
    [InlineData("=-(2^2)")]
    [InlineData("=2^(3^2)")]
    [InlineData("=(2^3)^2")]
    [InlineData("=2^0.5")]
    [InlineData("=0^0")]
    [InlineData("=0^-1")]
    [InlineData("=0^0.5")]
    [InlineData("=(-8)^(1/3)")]
    [InlineData("=(-1)^0.5")]
    [InlineData("=(-2)^3")]
    [InlineData("=10^308*10")]
    [InlineData("=10^309")]
    [InlineData("=2^1024")]
    [InlineData("=(-10)^309")]
    [InlineData("=A1^A2")]
    [InlineData("=A3^2")]
    [InlineData("=2^A3")]
    [InlineData("=A3^A3")]
    [InlineData("=A4^2")]
    [InlineData("=2^A4")]
    [InlineData("=A5^2")]
    [InlineData("=A6^2")]
    [InlineData("=A8^2")]
    [InlineData("=2*3^2")]
    [InlineData("=2+3^2")]
    [InlineData("=2^3*4")]
    [InlineData("=2^3&\"x\"")]
    [InlineData("=2^3=8")]
    [InlineData("=10%")]
    [InlineData("=5%%")]
    [InlineData("=1%%%")]
    [InlineData("=-10%")]
    [InlineData("=-(10%)")]
    [InlineData("=(-10)%")]
    [InlineData("=A1%")]
    [InlineData("=A3%")]
    [InlineData("=A4%")]
    [InlineData("=A5%")]
    [InlineData("=A6%")]
    [InlineData("=A8%")]
    [InlineData("=(1+2)%")]
    [InlineData("=2^10%")]
    [InlineData("=10%^2")]
    [InlineData("=50%*200")]
    [InlineData("=A1%+1")]
    [InlineData("=100*5%")]
    [InlineData("=\"5\"%")]
    [InlineData("=TRUE%")]
    [InlineData("=1/0%")]
    [InlineData("=SUM(10%,20%)")]
    [InlineData("=SUM(A1^2,A2^2)")]
    [InlineData("=\"a\"&2^3")]
    [InlineData("=#REF!^2")]
    [InlineData("=2^#REF!")]
    [InlineData("=#REF!%")]
    [InlineData("=POWER(0,0)")]
    [InlineData("=POWER(0,-1)")]
    [InlineData("=POWER(2,1024)")]
    [InlineData("=POWER(2,3)")]
    [InlineData("=SUM(A1:A2^2)")]
    [InlineData("=SUM(A1:A2%)")]
    [InlineData("=2^3%")]
    [InlineData("=-2^-2")]
    [InlineData("=--2^2")]
    [InlineData("=-A1^2")]
    [InlineData("=A1^-A2")]
    [InlineData("=1+2%")]
    [InlineData("=(1+2)^2%")]
    [InlineData("=-5%^2")]
    [InlineData("=(-8)^(2/3)")]
    [InlineData("=(-8)^(-1/3)")]
    [InlineData("=(-32)^(1/5)")]
    [InlineData("=(-32)^0.2")]
    [InlineData("=(-27)^(1/3)")]
    [InlineData("=(-4)^(1/2)")]
    [InlineData("=(-8)^(1/3+1)")]
    [InlineData("=(-2)^0.5")]
    [InlineData("=(-2)^1.5")]
    [InlineData("=(-8)^0.333")]
    [InlineData("=(-8)^0.3333333333")]
    public void Rewrite_KeepsTheValue(string formula)
    {
        var tree = FormulaParser.Parse(formula);
        var rewritten = FormulaRewriter.Rewrite(formula, tree, token => token.Address);

        Assert.Empty(tree.Errors);
        Assert.Equal(Evaluate(formula), Evaluate(rewritten));
    }

    [Theory]
    [InlineData("=2^3^2")]
    [InlineData("=2^(3^2)")]
    [InlineData("=-(2^2)")]
    [InlineData("=-2^2")]
    [InlineData("=2^-1")]
    [InlineData("=5%%")]
    [InlineData("=-10%")]
    [InlineData("=(1+2)%")]
    [InlineData("=(1+2)^2%")]
    [InlineData("=(A1+A2)^(A1*A2)%")]
    public void Rewrite_KeepsParenthesesTheMeaningNeeds(string formula)
    {
        Assert.Equal(formula, FormulaRewriter.Rewrite(formula, FormulaParser.Parse(formula), token => token.Address));
    }
}
