using Radzen.Blazor;

namespace Radzen;

/// <summary>
/// Supplies information about RadzenDropDown ItemRender event.
/// </summary>
public class DropDownItemRenderEventArgs<
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)] TValue> : DropDownBaseItemRenderEventArgs<TValue>
{
    /// <summary>
    /// Gets the DropDown.
    /// </summary>
    public RadzenDropDown<TValue>? DropDown { get; internal set; }
}

