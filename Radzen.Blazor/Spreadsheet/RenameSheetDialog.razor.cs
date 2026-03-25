using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

using Radzen.Blazor;
namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Dialog for renaming a sheet in a spreadsheet.
/// </summary>
public partial class RenameSheetDialog : ComponentBase
{
    /// <summary>
    /// The current name of the sheet.
    /// </summary>
    [Parameter]
    public string Name { get; set; } = "";

    /// <summary>
    /// The names of existing sheets used for duplicate validation.
    /// </summary>
    [Parameter]
    public IReadOnlyList<string> ExistingNames { get; set; } = [];

    /// <summary>
    /// The dialog service instance.
    /// </summary>
    [Inject]
    public DialogService DialogService { get; set; } = default!;

    [Inject]
    Localizer Localizer { get; set; } = default!;

    string L(string key) => Localizer.Get(key, System.Globalization.CultureInfo.CurrentUICulture);

    private string? error;

    private void OnOk()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            error = L(nameof(RadzenStrings.Spreadsheet_NameCannotBeEmpty));
            return;
        }

        foreach (var existing in ExistingNames)
        {
            if (string.Equals(existing, Name, StringComparison.OrdinalIgnoreCase))
            {
                error = string.Format(System.Globalization.CultureInfo.CurrentCulture, L(nameof(RadzenStrings.Spreadsheet_SheetNameAlreadyExists)), Name);
                return;
            }
        }

        DialogService.Close(Name);
    }

    private void OnCancel()
    {
        DialogService.Close();
    }
}
