using System.Collections.Generic;
using Radzen.Documents.Spreadsheet;
using Xunit;

namespace Radzen.Blazor.Spreadsheet.Tests;

#nullable enable

// Contract for the public Formula / FormulaEngine API. Each [Fact] documents one
// behavior the headless engine guarantees.
public class FormulaEngineContractTests
{
    // ── Stateless Formula.Evaluate ──────────────────────────────────────────
    [Fact]
    public void Formula_Evaluate_LiteralArithmetic()
    {
        Assert.Equal(5d, Formula.Evaluate("=2+3"));
        Assert.Equal(16d, Formula.Evaluate("=SUM(1,2,3)+IF(2>1,10,20)"));
    }

    [Fact]
    public void Formula_Evaluate_LeadingEqualsIsOptional()
    {
        Assert.Equal(7d, Formula.Evaluate("3+4"));
        Assert.Equal(7d, Formula.Evaluate("=3+4"));
    }

    [Fact]
    public void Formula_Evaluate_StringFunction()
    {
        Assert.Equal("HELLO", Formula.Evaluate("=UPPER(\"hello\")"));
    }

    [Fact]
    public void Formula_Evaluate_WithCells()
    {
        var cells = new Dictionary<string, object?>
        {
            ["A1"] = 2.0,
            ["B1"] = 3.0,
            ["C1"] = 4.0,
        };
        Assert.Equal(14d, Formula.Evaluate("=A1+B1*C1", cells));
    }

    [Fact]
    public void Formula_Evaluate_RangeOverCells()
    {
        var cells = new Dictionary<string, object?>
        {
            ["A1"] = 10.0,
            ["A2"] = 20.0,
            ["A3"] = 30.0,
            ["A4"] = 40.0,
        };
        Assert.Equal(100d, Formula.Evaluate("=SUM(A1:A4)", cells));
        Assert.Equal(25d, Formula.Evaluate("=AVERAGE(A1:A4)", cells));
    }

    // ── Stateful FormulaEngine ──────────────────────────────────────────────
    [Fact]
    public void FormulaEngine_SetGet_LiteralValues()
    {
        var engine = new FormulaEngine();
        engine.Set("A1", 42.0);
        engine.Set("A2", "hello");
        Assert.Equal(42d, engine.Get("A1"));
        Assert.Equal("hello", engine.Get("A2"));
    }

    [Fact]
    public void FormulaEngine_FormulaCellRecalculatesOnDependencyChange()
    {
        var engine = new FormulaEngine();
        engine.Set("A1", 2.0);
        engine.Set("B1", 3.0);
        engine.Set("C1", "=A1+B1");
        Assert.Equal(5d, engine.Get("C1"));

        // Change A1 → C1 should recompute.
        engine.Set("A1", 20.0);
        Assert.Equal(23d, engine.Get("C1"));
    }

    [Fact]
    public void FormulaEngine_Evaluate_UsesCurrentState()
    {
        var engine = new FormulaEngine();
        engine.Set("A1", 5.0);
        engine.Set("B1", 10.0);
        Assert.Equal(15d, engine.Evaluate("=A1+B1"));
    }

    [Fact]
    public void FormulaEngine_Evaluate_DoesNotPersistAcrossCalls()
    {
        var engine = new FormulaEngine();
        engine.Set("A1", 5.0);
        var first = engine.Evaluate("=A1*2");
        var second = engine.Evaluate("=A1+1");
        Assert.Equal(10d, first);
        Assert.Equal(6d, second);
        // The intermediate eval result didn't leak into A1.
        Assert.Equal(5d, engine.Get("A1"));
    }

    // ── Custom function registration ────────────────────────────────────────
    [Fact]
    public void FormulaEngine_Functions_CustomFunctionIsCallable()
    {
        var engine = new FormulaEngine();
        engine.Functions.Add<CompoundFunction>();

        engine.Set("A1", 1000.0);   // principal
        engine.Set("B1", 0.05);     // rate
        engine.Set("C1", 3.0);      // years
        engine.Set("D1", "=COMPOUND(A1, B1, C1)");

        // 1000 * (1.05)^3 = 1157.625
        Assert.Equal(1157.625, (double)engine.Get("D1")!, 3);
    }

    public sealed class CompoundFunction : FormulaFunction
    {
        public override string Name => "COMPOUND";

        public override FunctionParameter[] Parameters =>
        [
            new("principal", ParameterType.Single, isRequired: true),
            new("rate",      ParameterType.Single, isRequired: true),
            new("years",     ParameterType.Single, isRequired: true),
        ];

        public override CellData Evaluate(FunctionArguments arguments)
        {
            System.ArgumentNullException.ThrowIfNull(arguments);
            var p = arguments.GetSingle("principal")?.GetValueOrDefault<double>() ?? 0d;
            var r = arguments.GetSingle("rate")?.GetValueOrDefault<double>() ?? 0d;
            var y = arguments.GetSingle("years")?.GetValueOrDefault<double>() ?? 0d;
            return CellData.FromNumber(p * System.Math.Pow(1d + r, y));
        }
    }
}
