using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Radzen.Blazor.Rendering;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Radzen.Blazor;

/// <summary>
/// Represents a dialog component in the Radzen framework.
/// RadzenDialog is a reusable component that facilitates the creation of modal dialogs.
/// It can be used to display custom content, collect user input or confirm/cancel operations.
/// </summary>
public partial class RadzenDialog
{
    private string SideDialogContentCssClass
    {
        get
        {
            var baseCss = "rz-dialog-side-content";
            return string.IsNullOrEmpty(sideDialogOptions?.ContentCssClass)
                ? baseCss
                : $"{baseCss} {sideDialogOptions.ContentCssClass}";
        }
    }

    [Inject] private DialogService Service { get; set; }

    /// <summary>
    /// Gets or sets the close side dialog aria label text.
    /// </summary>
    /// <value>The close side dialog aria label text.</value>
    [Parameter]
    public string CloseSideDialogAriaLabel { get; set; } = "Close side dialog";

    private List<Dialog> dialogs = new List<Dialog>();
    private bool isSideDialogOpen = false;
    private RenderFragment sideDialogContent;
    private SideDialogOptions sideDialogOptions;
    private Dialog sideDialog;
    private ElementReference? resizeBarElement;
    private ElementReference? sideDialogElement;
    private IJSObjectReference sideDialogResizeHandleJsModule;

    /// <summary>
    /// Opens a new dialog with specified parameters and options.
    /// </summary>
    /// <param name="title">The title of the dialog.</param>
    /// <param name="type">The content type of the dialog, usually a component type.</param>
    /// <param name="parameters">A dictionary of parameters to pass to the dialog content.</param>
    /// <param name="options">Additional configuration options for the dialog, such as size or behavior.</param>
    /// <returns>A Task that represents the asynchronous operation of opening the dialog.</returns>
    public async Task Open(string title,
        Type type,
        Dictionary<string, object> parameters,
        DialogOptions options)
    {
        dialogs.Add(new Dialog() { Title = title, Type = type, Parameters = parameters, Options = options });

        await InvokeAsync(() => { StateHasChanged(); });
    }

    /// <summary>
    /// Closes the currently open dialog and returns the specified result.
    /// </summary>
    /// <param name="result">The result to return when the dialog is closed. This can contain any data that needs to be passed back to the caller.</param>
    /// <returns>A Task that represents the asynchronous operation of closing the dialog.</returns>
    public async Task Close(dynamic result)
    {
        var lastDialog = dialogs.LastOrDefault();
        if (lastDialog != null)
        {
            dialogs.Remove(lastDialog);
            if (dialogs.Count == 0) await JSRuntime.InvokeAsync<string>("Radzen.closeDialog");
        }

        await InvokeAsync(() =>
        {
            StateHasChanged();
        });
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (Service is null) return;
        Service.OnOpen -= OnOpen;
        Service.OnClose -= OnClose;
        Service.OnSideOpen -= OnSideOpen;
        Service.OnSideClose -= OnSideClose;
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (Service is null) return;
        Service.OnOpen += OnOpen;
        Service.OnClose += OnClose;
        Service.OnSideOpen += OnSideOpen;
        Service.OnSideClose += OnSideClose;
    }

    private void OnSideOpen(Type sideComponent, Dictionary<string, object> parameters, SideDialogOptions options)
    {
        sideDialogOptions = options;

        // Create a DialogOptions from SideDialogOptions by copying common properties
        var dialogOptions = new DialogOptions()
        {
            ShowTitle = options.ShowTitle,
            ShowClose = options.ShowClose,
            Width = options.Width,
            Height = options.Height,
            Style = options.Style,
            CloseDialogOnOverlayClick = options.CloseDialogOnOverlayClick,
            CssClass = options.CssClass,
            WrapperCssClass = options.WrapperCssClass,
            ContentCssClass = options.ContentCssClass,
            CloseTabIndex = options.CloseTabIndex,
            TitleContent = options.TitleContent
        };

        // Create a Dialog object for the side dialog to support cascading parameter
        sideDialog = new Dialog()
        {
            Title = options.Title, Type = sideComponent, Parameters = parameters, Options = dialogOptions
        };

        // Wrap the content in a CascadingValue to provide the Dialog object to child components
        sideDialogContent = new RenderFragment(builder =>
        {
            // Open CascadingValue
            builder.OpenComponent<CascadingValue<Dialog>>(0);
            builder.AddAttribute(1, "Value", sideDialog);
            builder.AddAttribute(2, "IsFixed", true);
            builder.AddAttribute(3, "ChildContent", (RenderFragment)((builder2) =>
            {
                // Open the actual component
                builder2.OpenComponent(0, sideComponent);
                foreach (var parameter in parameters)
                {
                    builder2.AddAttribute(1, parameter.Key, parameter.Value);
                }

                builder2.CloseComponent();
            }));
            builder.CloseComponent(); // Close CascadingValue
        });
        isSideDialogOpen = true;
        StateHasChanged();
    }

    private bool sideDialogClosing = false;

    private async Task OnSideCloseAsync()
    {
        sideDialogClosing = true;

        StateHasChanged();

        await Task.Delay(300);

        isSideDialogOpen = false;
        sideDialogClosing = false;

        StateHasChanged();

        Service?.OnSideCloseComplete();
    }

    private void OnSideClose(dynamic _)
    {
        if (isSideDialogOpen)
        {
            InvokeAsync(OnSideCloseAsync);
        }
    }

    private void OnOpen(string title,
        Type type,
        Dictionary<string, object> parameters,
        DialogOptions options)
    {
        Open(title, type, parameters, options).ConfigureAwait(false);
    }

    private void OnClose(dynamic result)
    {
        Close(result).ConfigureAwait(false);
    }

    private string GetSideDialogCssClass() => ClassList.Create("rz-dialog-side")
        .Add($"rz-dialog-side-position-{sideDialogOptions?.Position.ToString().ToLower()}")
        .Add(sideDialogOptions?.CssClass)
        .Add("rz-open", !sideDialogClosing)
        .Add("rz-close", sideDialogClosing)
        .ToString();

    private string GetResizeBarCssClass() => ClassList.Create("rz-dialog-resize-bar")
        .ToString();

    private string GetSideDialogStyle()
    {
        string widthStyle = string.IsNullOrEmpty(sideDialogOptions?.Width)
            ? string.Empty
            : $"width: {sideDialogOptions.Width};";
        string heightStyle = string.IsNullOrEmpty(sideDialogOptions?.Height)
            ? string.Empty
            : $"height: {sideDialogOptions.Height};";

        return $"{widthStyle}{heightStyle}{sideDialogOptions?.Style}";
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (isSideDialogOpen)
        {
            await JSRuntime.InvokeAsync<string>("Radzen.openSideDialog", sideDialogOptions);

            if (sideDialogOptions is { Resizable: true } && resizeBarElement.HasValue)
            {
                sideDialogResizeHandleJsModule =
                    await JSRuntime.InvokeAsync<IJSObjectReference>("Radzen.initSideDialogResize",
                        resizeBarElement, sideDialogElement);
            }
        }
    }

    /// <summary>
    /// Disposes resources used by the dialog asynchronously.
    /// This method is typically called to clean up unmanaged resources or
    /// perform cleanup tasks for JavaScript interop involved in the dialog component.
    /// </summary>
    /// <returns>A ValueTask that represents the asynchronous disposal operation.</returns>
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (sideDialogResizeHandleJsModule != null)
            {
                await sideDialogResizeHandleJsModule.InvokeVoidAsync("dispose");
            }
        }
        catch
        {
            /* Ignore */
        }
    }
}