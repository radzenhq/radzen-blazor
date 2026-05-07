using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Visual presets for the built-in Excel table styles. Each preset captures the colors
/// and emphasis used by <see cref="Documents.Spreadsheet.Table.TableStyle"/> when rendering
/// inside <see cref="RadzenSpreadsheet"/>. The set is intentionally small — Excel ships
/// 60 built-in styles and we map a representative few; unknown names fall back to
/// <c>TableStyleMedium2</c>.
/// </summary>
internal static class TableStylePresets
{
    /// <summary>One row of computed colors for a cell inside a table.</summary>
    internal sealed record Preset(
        string? HeaderBackground,
        string? HeaderColor,
        string? RowBackground,
        string? AltRowBackground,
        string? TotalsBackground,
        string? TotalsColor,
        string? BorderColor,
        bool BoldHeader = true,
        bool BoldTotals = true,
        bool BoldFirstColumn = true,
        bool BoldLastColumn = true);

    private static readonly Preset Default = new(
        HeaderBackground: "#5B9BD5",
        HeaderColor: "#FFFFFF",
        RowBackground: "#FFFFFF",
        AltRowBackground: "#DDEBF7",
        TotalsBackground: null,
        TotalsColor: null,
        BorderColor: "#5B9BD5");

    private static readonly Dictionary<string, Preset> presets = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TableStyleLight1"] = new Preset(
            HeaderBackground: null,
            HeaderColor: "#000000",
            RowBackground: null,
            AltRowBackground: null,
            TotalsBackground: null,
            TotalsColor: null,
            BorderColor: "#000000"),

        ["TableStyleLight9"] = new Preset(
            HeaderBackground: null,
            HeaderColor: "#000000",
            RowBackground: null,
            AltRowBackground: "#F2F2F2",
            TotalsBackground: null,
            TotalsColor: null,
            BorderColor: "#5B9BD5"),

        ["TableStyleLight15"] = new Preset(
            HeaderBackground: null,
            HeaderColor: "#000000",
            RowBackground: null,
            AltRowBackground: "#FFF2CC",
            TotalsBackground: null,
            TotalsColor: null,
            BorderColor: "#FFC000"),

        ["TableStyleMedium2"] = Default,

        ["TableStyleMedium7"] = new Preset(
            HeaderBackground: "#A5A5A5",
            HeaderColor: "#FFFFFF",
            RowBackground: "#FFFFFF",
            AltRowBackground: "#EDEDED",
            TotalsBackground: "#A5A5A5",
            TotalsColor: "#FFFFFF",
            BorderColor: "#A5A5A5"),

        ["TableStyleMedium9"] = new Preset(
            HeaderBackground: "#4472C4",
            HeaderColor: "#FFFFFF",
            RowBackground: "#FFFFFF",
            AltRowBackground: "#D9E1F2",
            TotalsBackground: "#4472C4",
            TotalsColor: "#FFFFFF",
            BorderColor: "#4472C4"),

        ["TableStyleMedium14"] = new Preset(
            HeaderBackground: "#70AD47",
            HeaderColor: "#FFFFFF",
            RowBackground: "#FFFFFF",
            AltRowBackground: "#E2EFDA",
            TotalsBackground: "#70AD47",
            TotalsColor: "#FFFFFF",
            BorderColor: "#70AD47"),

        ["TableStyleMedium21"] = new Preset(
            HeaderBackground: "#ED7D31",
            HeaderColor: "#FFFFFF",
            RowBackground: "#FFFFFF",
            AltRowBackground: "#FCE4D6",
            TotalsBackground: "#ED7D31",
            TotalsColor: "#FFFFFF",
            BorderColor: "#ED7D31"),

        ["TableStyleDark1"] = new Preset(
            HeaderBackground: "#000000",
            HeaderColor: "#FFFFFF",
            RowBackground: "#F2F2F2",
            AltRowBackground: "#FFFFFF",
            TotalsBackground: "#000000",
            TotalsColor: "#FFFFFF",
            BorderColor: "#000000"),

        ["TableStyleDark2"] = new Preset(
            HeaderBackground: "#1F3864",
            HeaderColor: "#FFFFFF",
            RowBackground: "#D9E1F2",
            AltRowBackground: "#A4B6E0",
            TotalsBackground: "#1F3864",
            TotalsColor: "#FFFFFF",
            BorderColor: "#1F3864"),
    };

    /// <summary>Looks up a preset by name; falls back to a sensible default.</summary>
    internal static Preset Get(string? name)
    {
        if (name is null) return Default;
        return presets.TryGetValue(name, out var p) ? p : Default;
    }
}
