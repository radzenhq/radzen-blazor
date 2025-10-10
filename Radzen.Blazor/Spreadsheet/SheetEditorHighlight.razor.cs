using Microsoft.AspNetCore.Components;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable
/// <summary>
/// Represents a syntax highlighting component for spreadsheet formulas.
/// </summary>
public partial class SheetEditorHighlight : ComponentBase
{
    /// <summary>
    /// Gets or sets the value to be syntax highlighted.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    private List<HighlightToken> GetHighlightTokens()
    {
        if (string.IsNullOrEmpty(Value) || !Value.StartsWith('='))
        {
            return [new() { Text = Value, Class = "rz-default-highlight" }];
        }

        var tokens = FormulaLexer.Scan(Value, strict: false);
        var highlightTokens = new List<HighlightToken>();
        int refCount = 0;

        foreach (var token in tokens)
        {
            foreach (var trivia in token.LeadingTrivia)
            {
                highlightTokens.Add(new (){ Text = trivia.Text, Class = "rz-default-highlight" });
            }

            if (token.Type != FormulaTokenType.Whitespace)
            {
                var highlightToken = new HighlightToken
                {
                    Text = token.Type == FormulaTokenType.CellIdentifier ? token.Address.ToString() : token.Value,
                    Class = GetTokenClassName(token.Type),
                    Style = GetTokenStyle(token.Type, refCount)
                };

                if (token.Type == FormulaTokenType.CellIdentifier)
                {
                    refCount++;
                }

                highlightTokens.Add(highlightToken);
            }

            foreach (var trivia in token.TrailingTrivia)
            {
                highlightTokens.Add(new () { Text = trivia.Text, Class = "rz-default-highlight" });
            }
        }

        return highlightTokens;
    }

    private static string GetTokenClassName(FormulaTokenType tokenType)
    {
        return tokenType switch
        {
            FormulaTokenType.NumericLiteral => "rz-number-highlight",
            FormulaTokenType.StringLiteral => "rz-string-highlight",
            FormulaTokenType.CellIdentifier => "rz-cell-highlight",
            FormulaTokenType.Identifier => "rz-function-highlight",
            FormulaTokenType.Plus or FormulaTokenType.Minus or FormulaTokenType.Star or FormulaTokenType.Slash
                or FormulaTokenType.Equals or FormulaTokenType.EqualsGreaterThan or FormulaTokenType.LessThan
                or FormulaTokenType.LessThanOrEqual or FormulaTokenType.GreaterThan or FormulaTokenType.GreaterThanOrEqual
                or FormulaTokenType.OpenParen or FormulaTokenType.CloseParen or FormulaTokenType.Comma or FormulaTokenType.Colon => "rz-operator-highlight",
            _ => "rz-default-highlight"
        };
    }

    private static string? GetTokenStyle(FormulaTokenType tokenType, int refCount)
    {
        if (tokenType == FormulaTokenType.CellIdentifier)
        {
            var colorIndex = (refCount % 5) + 1;
            return $"color:var(--rz-highlight-color-{colorIndex})";
        }

        return null;
    }

    class HighlightToken
    {
        public string? Text { get; set; }
        public string? Class { get; set; }
        public string? Style { get; set; }
    }
}
