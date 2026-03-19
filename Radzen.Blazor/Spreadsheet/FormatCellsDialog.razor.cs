using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Dialog for formatting cells in a spreadsheet.
/// </summary>
public partial class FormatCellsDialog : ComponentBase
{
    /// <summary>
    /// The current format of the selected cell.
    /// </summary>
    [Parameter]
    public string? CurrentFormat { get; set; }

    /// <summary>
    /// A sample value to preview formatting.
    /// </summary>
    [Parameter]
    public object? SampleValue { get; set; }

    /// <summary>
    /// The data type of the sample value.
    /// </summary>
    [Parameter]
    public CellDataType ValueType { get; set; }

    /// <summary>
    /// The dialog service instance.
    /// </summary>
    [Inject]
    public DialogService DialogService { get; set; } = default!;

    private NumberFormatCategory selectedCategory = NumberFormatCategory.General;
    private string customFormatCode = "General";
    private int decimalPlaces = 2;
    private bool useThousandsSeparator = true;
    private string currencySymbol = "$";
    private string? selectedPreset;
    private string preview = "";

    private static readonly NumberFormatCategory[] categories =
    [
        NumberFormatCategory.General,
        NumberFormatCategory.Number,
        NumberFormatCategory.Currency,
        NumberFormatCategory.Accounting,
        NumberFormatCategory.Percentage,
        NumberFormatCategory.Scientific,
        NumberFormatCategory.Date,
        NumberFormatCategory.Time,
        NumberFormatCategory.Text
    ];

    private IReadOnlyList<(string, string)> presets = [];

    private bool ShowDecimalPlaces => selectedCategory is NumberFormatCategory.Number
        or NumberFormatCategory.Currency or NumberFormatCategory.Accounting
        or NumberFormatCategory.Percentage or NumberFormatCategory.Scientific;

    private bool ShowThousandsSeparator => selectedCategory == NumberFormatCategory.Number;

    private bool ShowCurrencySymbol => selectedCategory is NumberFormatCategory.Currency
        or NumberFormatCategory.Accounting;

    private bool ShowPresetList => selectedCategory is NumberFormatCategory.Date
        or NumberFormatCategory.Time;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (!string.IsNullOrEmpty(CurrentFormat) &&
            !string.Equals(CurrentFormat, "General", StringComparison.OrdinalIgnoreCase))
        {
            customFormatCode = CurrentFormat;
            selectedCategory = NumberFormatPresets.GetCategory(CurrentFormat);
        }

        LoadPresetsForCategory();
        UpdatePreview();
    }

    private void OnCategoryChanged()
    {
        LoadPresetsForCategory();
        RebuildFormatCode();
        UpdatePreview();
    }

    private void OnOptionChanged()
    {
        RebuildFormatCode();
        UpdatePreview();
    }

    private void OnPresetChanged()
    {
        customFormatCode = selectedPreset ?? customFormatCode;
        UpdatePreview();
    }

    private void OnFormatCodeChanged()
    {
        UpdatePreview();
    }

    private void LoadPresetsForCategory()
    {
        if (selectedCategory is NumberFormatCategory.Date or NumberFormatCategory.Time)
        {
            presets = NumberFormatPresets.GetPresets(selectedCategory);
            selectedPreset = presets.Count > 0 ? presets[0].Item2 : null;
        }
        else
        {
            presets = [];
            selectedPreset = null;
        }
    }

    private void RebuildFormatCode()
    {
        customFormatCode = FormatCodeBuilder.Build(
            selectedCategory, decimalPlaces, useThousandsSeparator,
            currencySymbol, selectedPreset);
    }

    private void UpdatePreview()
    {
        if (SampleValue == null)
        {
            preview = "";
            return;
        }

        var result = NumberFormat.Apply(customFormatCode, SampleValue, ValueType);
        preview = result ?? SampleValue.ToString() ?? "";
    }

    private void OnOk()
    {
        DialogService.Close(customFormatCode);
    }

    private void OnCancel()
    {
        DialogService.Close();
    }
}
