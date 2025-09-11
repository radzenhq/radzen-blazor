using Radzen.Blazor.Rendering;

namespace Radzen.Blazor;

/// <summary>
/// RadzenFab component.
/// </summary>
/// <example>
/// <code>
/// &lt;RadzenFab Icon="add" Click=@(args => Console.WriteLine("FAB clicked")) /&gt;
/// &lt;RadzenFab Icon="add" IsBusy="@isLoading" BusyText="Loading..." Click=@OnFabClick /&gt;
/// </code>
/// </example>
public partial class RadzenFab : RadzenButton
{
    /// <inheritdoc />
    public override ButtonSize Size { get; set; } = ButtonSize.Large;

    /// <inheritdoc />
    protected override string GetComponentCssClass()
    {
        return ClassList.Create("rz-button rz-fab")
                       .AddButtonSize(Size)
                       .AddVariant(Variant)
                       .AddButtonStyle(ButtonStyle)
                       .AddDisabled(IsDisabled)
                       .AddShade(Shade)
                       .Add($"rz-button-icon-only", string.IsNullOrEmpty(Text) && !string.IsNullOrEmpty(Icon))
                       .ToString();
    }
}