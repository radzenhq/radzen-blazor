using System;
using System.Collections.Generic;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Visual presets for the built-in Excel table styles. Each preset captures the colors
/// and emphasis used by <see cref="Documents.Spreadsheet.Table.TableStyle"/> when rendering
/// inside <see cref="RadzenSpreadsheet"/>. Presets are generated for the full Excel
/// palette (Light1..21, Medium1..28, Dark1..11) so every named style produces a
/// distinct visual output. Colors approximate the Office default theme; precise
/// Excel theming would require reading <c>theme1.xml</c> from the package.
/// </summary>
internal static class TableStylePresets
{
    /// <summary>Color set for one named table style.</summary>
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

    // Office default theme accents (Excel 2016+).
    private static readonly string[] Accents =
    [
        "#5B9BD5", // Accent 1 - blue
        "#ED7D31", // Accent 2 - orange
        "#A5A5A5", // Accent 3 - gray
        "#FFC000", // Accent 4 - gold
        "#4472C4", // Accent 5 - dark blue
        "#70AD47", // Accent 6 - green
    ];

    // Lighter tint for banded data rows.
    private static readonly string[] LightTints =
    [
        "#DDEBF7", "#FCE4D6", "#EDEDED", "#FFF2CC", "#D9E1F2", "#E2EFDA",
    ];

    // Stronger tint for the Light15..21 / Medium15..21 series.
    private static readonly string[] StrongTints =
    [
        "#BDD7EE", "#F8CBAD", "#D9D9D9", "#FFE699", "#B4C7E7", "#C6E0B4",
    ];

    // Darker variants for the Dark2..7 series.
    private static readonly string[] DarkAccents =
    [
        "#1F3864", "#843C0C", "#525252", "#806000", "#1F3864", "#375623",
    ];

    private static readonly Preset Default = BuildMediumWithBands(0); // TableStyleMedium2 equivalent

    private static readonly Dictionary<string, Preset> presets = BuildAll();

    /// <summary>Looks up a preset by name; falls back to a sensible default.</summary>
    internal static Preset Get(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return Default;
        }

        return presets.TryGetValue(name, out var p) ? p : Default;
    }

    private static Dictionary<string, Preset> BuildAll()
    {
        var d = new Dictionary<string, Preset>(StringComparer.OrdinalIgnoreCase)
        {
            ["None"] = new Preset(null, "#000000", null, null, null, null, "#000000"),
        };

        // ── Light series ──────────────────────────────────────────────────
        // Light1: simple black border, no fills.
        d["TableStyleLight1"] = new Preset(null, "#000000", null, null, null, null, "#000000");

        // Light2..7: accent-colored top/bottom bands, no body fill.
        for (var i = 0; i < 6; i++)
        {
            d[$"TableStyleLight{2 + i}"] = new Preset(
                HeaderBackground: null,
                HeaderColor: "#000000",
                RowBackground: null,
                AltRowBackground: null,
                TotalsBackground: null,
                TotalsColor: null,
                BorderColor: Accents[i]);
        }

        // Light8..14: accent border + light banded rows.
        for (var i = 0; i < 6; i++)
        {
            d[$"TableStyleLight{8 + i}"] = new Preset(
                HeaderBackground: null,
                HeaderColor: "#000000",
                RowBackground: null,
                AltRowBackground: LightTints[i],
                TotalsBackground: null,
                TotalsColor: null,
                BorderColor: Accents[i]);
        }

        // Light15..21: stronger banded rows.
        for (var i = 0; i < 6; i++)
        {
            d[$"TableStyleLight{15 + i}"] = new Preset(
                HeaderBackground: null,
                HeaderColor: "#000000",
                RowBackground: null,
                AltRowBackground: StrongTints[i],
                TotalsBackground: null,
                TotalsColor: null,
                BorderColor: Accents[i]);
        }

        // ── Medium series ─────────────────────────────────────────────────
        // Medium1: black header, gray banding.
        d["TableStyleMedium1"] = new Preset(
            HeaderBackground: "#000000",
            HeaderColor: "#FFFFFF",
            RowBackground: "#FFFFFF",
            AltRowBackground: "#F2F2F2",
            TotalsBackground: "#000000",
            TotalsColor: "#FFFFFF",
            BorderColor: "#000000");

        // Medium2..7: accent header, no banding.
        for (var i = 0; i < 6; i++)
        {
            d[$"TableStyleMedium{2 + i}"] = BuildMediumWithBands(i, banded: false);
        }

        // Medium8..14: accent header with banded rows.
        for (var i = 0; i < 6; i++)
        {
            d[$"TableStyleMedium{8 + i}"] = BuildMediumWithBands(i, banded: true);
        }

        // Medium15..21: accent header with stronger banded rows.
        for (var i = 0; i < 6; i++)
        {
            d[$"TableStyleMedium{15 + i}"] = new Preset(
                HeaderBackground: Accents[i],
                HeaderColor: "#FFFFFF",
                RowBackground: "#FFFFFF",
                AltRowBackground: StrongTints[i],
                TotalsBackground: Accents[i],
                TotalsColor: "#FFFFFF",
                BorderColor: Accents[i]);
        }

        // Medium22..28: dark accent header with banded rows (7 styles).
        for (var i = 0; i < 7; i++)
        {
            var accent = i < 6 ? DarkAccents[i] : "#000000";
            var tint = i < 6 ? LightTints[i] : "#F2F2F2";
            d[$"TableStyleMedium{22 + i}"] = new Preset(
                HeaderBackground: accent,
                HeaderColor: "#FFFFFF",
                RowBackground: "#FFFFFF",
                AltRowBackground: tint,
                TotalsBackground: accent,
                TotalsColor: "#FFFFFF",
                BorderColor: accent);
        }

        // ── Dark series ───────────────────────────────────────────────────
        // Dark1: black on white.
        d["TableStyleDark1"] = new Preset(
            HeaderBackground: "#000000",
            HeaderColor: "#FFFFFF",
            RowBackground: "#F2F2F2",
            AltRowBackground: "#FFFFFF",
            TotalsBackground: "#000000",
            TotalsColor: "#FFFFFF",
            BorderColor: "#000000");

        // Dark2..7: dark accents with light banded rows.
        for (var i = 0; i < 6; i++)
        {
            d[$"TableStyleDark{2 + i}"] = new Preset(
                HeaderBackground: DarkAccents[i],
                HeaderColor: "#FFFFFF",
                RowBackground: LightTints[i],
                AltRowBackground: "#FFFFFF",
                TotalsBackground: DarkAccents[i],
                TotalsColor: "#FFFFFF",
                BorderColor: DarkAccents[i]);
        }

        // Dark8..11: pure accent fills (full-color body).
        for (var i = 0; i < 4; i++)
        {
            d[$"TableStyleDark{8 + i}"] = new Preset(
                HeaderBackground: Accents[i],
                HeaderColor: "#FFFFFF",
                RowBackground: Accents[i],
                AltRowBackground: StrongTints[i],
                TotalsBackground: DarkAccents[i],
                TotalsColor: "#FFFFFF",
                BorderColor: DarkAccents[i]);
        }

        return d;
    }

    private static Preset BuildMediumWithBands(int accentIndex, bool banded = true)
    {
        var accent = Accents[accentIndex];
        var tint = LightTints[accentIndex];
        return new Preset(
            HeaderBackground: accent,
            HeaderColor: "#FFFFFF",
            RowBackground: "#FFFFFF",
            AltRowBackground: banded ? tint : "#FFFFFF",
            TotalsBackground: accent,
            TotalsColor: "#FFFFFF",
            BorderColor: accent);
    }
}
