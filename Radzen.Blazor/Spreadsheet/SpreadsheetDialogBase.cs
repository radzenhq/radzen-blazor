using System.Globalization;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Base class for spreadsheet dialogs that provides shared <see cref="DialogService"/> injection,
/// a localizer, and a default cancel handler.
/// </summary>
public abstract class SpreadsheetDialogBase : ComponentBase
{
    /// <summary>
    /// Gets or sets the dialog service used to close the dialog.
    /// </summary>
    [Inject]
    protected DialogService DialogService { get; set; } = default!;

    /// <summary>
    /// Gets or sets the localizer used to resolve resource strings.
    /// </summary>
    [Inject]
    internal Localizer Localizer { get; set; } = default!;

    /// <summary>
    /// Returns the localized string for the specified resource key in the current UI culture.
    /// </summary>
    protected string L(string key) => Localizer.Get(key, CultureInfo.CurrentUICulture);

    /// <summary>
    /// Closes the dialog without returning a result.
    /// </summary>
    protected virtual void OnCancel() => DialogService.Close();
}
