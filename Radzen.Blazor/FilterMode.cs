namespace Radzen;

/// <summary>
/// Specifies the filtering mode of <see cref="Radzen.Blazor.RadzenDataGrid{TItem}" />.
/// </summary>
public enum FilterMode
{
    /// <summary>
    /// The component displays inline filtering UI and filters as you type.
    /// </summary>
    Simple,

    /// <summary>
    /// The component displays inline filtering UI and filters as you type combined with filter operator menu.
    /// </summary>
    SimpleWithMenu,

    /// <summary>
    /// The component displays a popup filtering UI and allows you to pick filtering operator and or filter by multiple values.
    /// </summary>
    Advanced,

    /// <summary>
    /// The component displays a popup filtering UI and allows you to pick multiple values from list of all values.
    /// </summary>
    CheckBoxList
}

