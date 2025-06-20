using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable
/// <summary>
/// Represents a content editable element in a spreadsheet.
/// </summary>
public partial class ContentEditable : ComponentBase, IAsyncDisposable
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

    private DotNetObjectReference<ContentEditable>? dotNetRef;

    /// <inheritdoc/>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            dotNetRef = DotNetObjectReference.Create(this);

            jsRef = await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.createContentEditable", new { element, value, AutoFocus, dotNetRef });
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