using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using System.Threading.Tasks;

namespace Radzen.Blazor;
/// <summary>
/// RadzenFabMenu component.
/// </summary>
/// <example>
/// <code>
/// &lt;RadzenFabMenu Icon="add" ToggleIcon="close" AriaLabel="Open menu"&gt;
///     &lt;RadzenFabMenuItem Text="Folder" Icon="folder" Click=@(args => Console.WriteLine("Folder clicked")) /&gt;
///     &lt;RadzenFabMenuItem Text="Chat" Icon="chat" Click=@(args => Console.WriteLine("Chat clicked")) /&gt;
/// &lt;/RadzenFabMenu&gt;
/// </code>
/// </example>
public partial class RadzenFabMenu : RadzenComponent
{
    /// <summary>
    /// Gets or sets the child content.
    /// </summary>
    /// <value>The child content.</value>
    [Parameter]
    public RenderFragment ChildContent { get; set; }

    private bool isOpen;

    /// <summary>
    /// Gets or sets a value indicating whether the menu is open.
    /// </summary>
    /// <value><c>true</c> if the menu is open; otherwise, <c>false</c>.</value>
    [Parameter]
    public bool IsOpen { get; set; }

    /// <summary>
    /// Gets or sets the IsOpen changed callback.
    /// </summary>
    /// <value>The IsOpen changed callback.</value>
    [Parameter]
    public EventCallback<bool> IsOpenChanged { get; set; }

    /// <summary>
    /// Gets or sets the icon.
    /// </summary>
    /// <value>The icon.</value>
    [Parameter]
    public string Icon { get; set; } = "add";

    /// <summary>
    /// Gets or sets the button toggled style.
    /// </summary>
    /// <value>The button toggled style.</value>
    [Parameter]
    public ButtonStyle ToggleButtonStyle { get; set; } = ButtonStyle.Base;

    /// <summary>
    /// Gets or sets the button toggled shade.
    /// </summary>
    /// <value>The button toggled shade.</value>
    [Parameter]
    public Shade ToggleShade { get; set; } = Shade.Default;

    /// <summary>
    /// Gets or sets the toggle icon.
    /// </summary>
    /// <value>The toggle icon.</value>
    [Parameter]
    public string ToggleIcon { get; set; } = "close";

    /// <summary>
    /// Gets or sets the button style.
    /// </summary>
    /// <value>The button style.</value>
    [Parameter]
    public ButtonStyle ButtonStyle { get; set; } = ButtonStyle.Primary;

    /// <summary>
    /// Gets or sets the size.
    /// </summary>
    /// <value>The size.</value>
    [Parameter]
    public ButtonSize Size { get; set; } = ButtonSize.Large;

    /// <summary>
    /// Gets or sets the variant.
    /// </summary>
    /// <value>The variant.</value>
    [Parameter]
    public Variant Variant { get; set; } = Variant.Filled;

    /// <summary>
    /// Gets or sets the shade.
    /// </summary>
    /// <value>The shade.</value>
    [Parameter]
    public Shade Shade { get; set; } = Shade.Default;

    /// <summary>
    /// Gets or sets a value indicating whether this <see cref="RadzenFabMenu"/> is disabled.
    /// </summary>
    /// <value><c>true</c> if disabled; otherwise, <c>false</c>.</value>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>
    /// Gets or sets the button class.
    /// </summary>
    /// <value>The button class.</value>
    [Parameter]
    public string ButtonClass { get; set; }

    /// <summary>
    /// Gets or sets the button style CSS.
    /// </summary>
    /// <value>The button style CSS.</value>
    [Parameter]
    public string ButtonStyleCss { get; set; }

    /// <summary>
    /// Gets or sets the aria-label for the toggle button.
    /// </summary>
    /// <value>The aria-label for the toggle button.</value>
    [Parameter]
    public string AriaLabel { get; set; }

    /// <summary>
    /// Gets or sets the gap.
    /// </summary>
    /// <value>The gap.</value>
    [Parameter]
    public string Gap { get; set; } = "0.25rem";

    /// <summary>
    /// Gets or sets the items style.
    /// </summary>
    /// <value>The items style.</value>
    [Parameter]
    public string ItemsStyle { get; set; }

    /// <summary>
    /// Gets or sets the direction in which the menu items expand.
    /// </summary>
    /// <value>The expansion direction.</value>
    [Parameter]
    public FabMenuDirection Direction { get; set; } = FabMenuDirection.Top;

    private string ComputedItemsStyle => string.IsNullOrEmpty(ItemsStyle) ? string.Empty : ItemsStyle;

    private Orientation StackOrientation => Direction switch
    {
        FabMenuDirection.Top or FabMenuDirection.Bottom => Orientation.Vertical,
        FabMenuDirection.Left or FabMenuDirection.Right or FabMenuDirection.Start or FabMenuDirection.End => Orientation.Horizontal,
        _ => Orientation.Vertical
    };

    private AlignItems StackAlignment => Direction switch
    {
        FabMenuDirection.Top => AlignItems.End,
        FabMenuDirection.Bottom => AlignItems.End,
        FabMenuDirection.Left => AlignItems.End,
        FabMenuDirection.Right => AlignItems.Start,
        FabMenuDirection.Start => AlignItems.End,
        FabMenuDirection.End => AlignItems.Start,
        _ => AlignItems.End
    };

    private string GetStackClasses()
    {
        var baseClasses = "rz-fabmenu-items rz-open";
        var directionClass = Direction switch
        {
            FabMenuDirection.Top => "rz-flex-column-reverse rz-fabmenu-direction-top",
            FabMenuDirection.Bottom => "rz-flex-column rz-fabmenu-direction-bottom",
            FabMenuDirection.Left => "rz-flex-row-reverse rz-fabmenu-direction-left",
            FabMenuDirection.Right => "rz-flex-row rz-fabmenu-direction-right",
            FabMenuDirection.Start => "rz-flex-row-reverse rz-fabmenu-direction-start",
            FabMenuDirection.End => "rz-flex-row rz-fabmenu-direction-end",
            _ => "rz-flex-column-reverse rz-fabmenu-direction-top"
        };
        return $"{baseClasses} {directionClass}";
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (isOpen)
        {
            JSRuntime.InvokeVoid("Radzen.registerFabMenu", Element, Reference);
        }
        else
        {
            JSRuntime.InvokeVoid("Radzen.unregisterFabMenu", Element);
        }
    }

    /// <inheritdoc />
    public override async Task SetParametersAsync(ParameterView parameters)
    {
        var isOpenChanged = parameters.DidParameterChange(nameof(IsOpen), IsOpen);

        await base.SetParametersAsync(parameters);

        if (isOpenChanged)
        {
            isOpen = IsOpen;
        }
    }

    private async Task OnFabClick(MouseEventArgs args)
    {
        await ToggleAsync();
    }

    private async Task OnToggleChanged(bool value)
    {
        isOpen = value;
        await IsOpenChanged.InvokeAsync(isOpen);
    }

    /// <summary>
    /// Toggles the menu open/closed state.
    /// </summary>
    public async Task ToggleAsync()
    {
        isOpen = !isOpen;
        await IsOpenChanged.InvokeAsync(isOpen);
        StateHasChanged();
    }

    /// <summary>
    /// Closes the menu.
    /// </summary>
    [JSInvokable]
    public async Task CloseAsync()
    {
        if (isOpen)
        {
            isOpen = false;

            await IsOpenChanged.InvokeAsync(isOpen);

            StateHasChanged();
        }
    }

    /// <inheritdoc />
    public override void Dispose()
    {
        try
        {
            if (IsOpen)
            {
                JSRuntime.InvokeVoid("Radzen.unregisterFabMenu", Element);
            }
        }
        catch { }

        base.Dispose();
    }
}