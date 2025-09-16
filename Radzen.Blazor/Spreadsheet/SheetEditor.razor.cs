using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable
/// <summary>
/// Represents a content editable element in a spreadsheet.
/// </summary>
public partial class SheetEditor : ComponentBase, IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the value of the content editable element.
    /// </summary>
    [Parameter]
    public string? Value { get; set; }

    private string? value;

    /// <summary>
    /// Event callback that is invoked when the value of the content editable element changes.
    /// </summary>
    [Parameter]
    public EventCallback<string?> ValueChanged { get; set; }

    /// <summary>
    /// Event callback that is invoked when the content editable element loses focus.
    /// </summary>
    [Parameter]
    public EventCallback Blur { get; set; }

    /// <summary>
    /// Event callback that is invoked when the content editable element gains focus.
    /// </summary>
    [Parameter]
    public EventCallback Focus { get; set; }

    /// <summary>
    /// Gets or sets additional HTML attributes for the content editable element.
    /// </summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public IReadOnlyDictionary<string, object>? Attributes { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the content editable element should automatically receive focus when rendered.
    /// </summary>
    [Parameter]
    public bool AutoFocus { get; set; }

    private ElementReference element;

    private IJSObjectReference? jsRef;

    private DotNetObjectReference<SheetEditor>? dotNetRef;

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            dotNetRef = DotNetObjectReference.Create(this);

            jsRef = await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.createSheetEditor", new { element, value, AutoFocus, dotNetRef });
        }
    }

    /// <inheritdoc/>
    protected override async Task OnParametersSetAsync()
    {
        if (Value != value)
        {
            value = Value;

            await SetValueAsync(value);
        }
    }

    /// <summary>
    /// Sets the value of the content editable element asynchronously.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public async Task SetValueAsync(string? value)
    {
        if (jsRef is not null)
        {
            await jsRef.InvokeVoidAsync("setValue", value);
        }
    }

    /// <summary>
    /// Invoked by JS interop when the content editable element's value changes.
    /// </summary>
    [JSInvokable]
    public async Task OnInputAsync(string value)
    {
        this.value = value;

        await ValueChanged.InvokeAsync(value);
    }

    /// <summary>
    /// Invoked by JS interop when the content editable element loses focus.
    /// </summary>
    [JSInvokable]
    public async Task OnBlurAsync()
    {
        await Blur.InvokeAsync();
    }

    /// <summary>
    /// Invoked by JS interop when the content editable element gains focus.
    /// </summary>
    [JSInvokable]
    public async Task OnFocusAsync()
    {
        await Focus.InvokeAsync();
    }


    private List<HighlightToken> GetHighlightTokens(string? text)
    {
        if (string.IsNullOrEmpty(text) || !text.StartsWith("="))
        {
            return [new() { Text = text, Class = "rz-default-highlight" }];
        }

        try
        {
            var tokens = FormulaLexer.Scan(text);
            var highlightTokens = new List<HighlightToken>();
            int refCount = 0;

            foreach (var token in tokens)
            {
                foreach (var trivia in token.LeadingTrivia)
                {
                    highlightTokens.Add(new (){ Text = trivia.Text, Class = "rz-default-highlight" });
                }

                // Add the main token (skip whitespace-only tokens as they're handled by trivia)
                if (token.Type != FormulaTokenType.Whitespace)
                {
                    var highlightToken = new HighlightToken
                    {
                        Text = token.Type == FormulaTokenType.CellIdentifier ? token.AddressValue.ToString() : token.Value,
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
        catch
        {
            return [new() { Text = text, Class = "rz-default-highlight"} ];
        }
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

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        if (jsRef is not null)
        {
            try
            {
                await jsRef.InvokeVoidAsync("dispose");
                await jsRef.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
        }

        dotNetRef?.Dispose();
    }
}