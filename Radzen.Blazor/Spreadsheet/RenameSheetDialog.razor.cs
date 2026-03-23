using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;

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

    private string? error;

    private void OnOk()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            error = "Name cannot be empty.";
            return;
        }

        foreach (var existing in ExistingNames)
        {
            if (string.Equals(existing, Name, StringComparison.OrdinalIgnoreCase))
            {
                error = $"A sheet named '{Name}' already exists.";
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
