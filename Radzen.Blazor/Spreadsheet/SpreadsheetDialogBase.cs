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
    /// The UI culture cascaded from a parent component. Dialogs render outside the component that
    /// opened them, so this resolves only when a <c>DefaultUICulture</c> cascade is set above the
    /// dialog host; otherwise <see cref="L"/> falls back to <see cref="CultureInfo.CurrentUICulture"/>.
    /// </summary>
    [CascadingParameter(Name = "DefaultUICulture")]
    protected CultureInfo? DefaultUICulture { get; set; }

    /// <summary>
    /// Returns the localized string for the specified resource key in the current UI culture.
    /// </summary>
    protected string L(string key) => Localizer.Get(key, DefaultUICulture ?? CultureInfo.CurrentUICulture);

    /// <summary>
    /// The culture used to parse and format values entered in the dialog. The spreadsheet passes
    /// its workbook culture when opening the dialog.
    /// </summary>
    [Parameter]
    public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;

    /// <summary>
    /// Converts a numeric string entered in <see cref="Culture"/> to the canonical invariant form
    /// stored by the engine. Non-numeric text is returned verbatim.
    /// </summary>
    protected string ToInvariantNumber(string value) =>
        double.TryParse(value, NumberStyles.Any, Culture, out var number)
            ? number.ToString(CultureInfo.InvariantCulture)
            : value;

    /// <summary>
    /// Converts a canonical invariant numeric string to <see cref="Culture"/> for display.
    /// Non-numeric text is returned verbatim.
    /// </summary>
    protected string ToLocalNumber(string value) =>
        double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var number)
            ? number.ToString(Culture)
            : value;

    /// <summary>
    /// Closes the dialog without returning a result.
    /// </summary>
    protected virtual void OnCancel() => DialogService.Close();
}
