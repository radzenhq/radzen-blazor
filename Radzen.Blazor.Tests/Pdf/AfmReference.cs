#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Radzen.Blazor.Pdf.Tests;

internal sealed class AfmReference
{
    public string FontName = "";
    public double CapHeight;
    public double XHeight;
    public double Ascender;
    public double Descender;
    public double ItalicAngle;
    public double UnderlinePosition;
    public double UnderlineThickness;
    public bool IsFixedPitch;
    public double BBoxLeft;
    public double BBoxBottom;
    public double BBoxRight;
    public double BBoxTop;

    public readonly Dictionary<string, int> WidthByName = new();
    public readonly Dictionary<int, int> WidthByCode = new();
    public readonly Dictionary<int, string> NameByCode = new();

    public static AfmReference Load(string fontFileBaseName)
    {
        using var stream = PdfTestResources.Open($"Afm/{fontFileBaseName}.afm");
        using var reader = new StreamReader(stream);
        var afm = new AfmReference();
        string? line;
        var inChars = false;
        while ((line = reader.ReadLine()) != null)
        {
            if (line.StartsWith("EndCharMetrics", StringComparison.Ordinal)) { inChars = false; continue; }
            if (line.StartsWith("StartCharMetrics", StringComparison.Ordinal)) { inChars = true; continue; }
            if (inChars) { afm.ParseCharMetric(line); continue; }
            afm.ParseHeader(line);
        }

        return afm;
    }

    private void ParseHeader(string line)
    {
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
        switch (parts[0])
        {
            case "FontName": FontName = parts[1]; break;
            case "CapHeight": CapHeight = D(parts[1]); break;
            case "XHeight": XHeight = D(parts[1]); break;
            case "Ascender": Ascender = D(parts[1]); break;
            case "Descender": Descender = D(parts[1]); break;
            case "ItalicAngle": ItalicAngle = D(parts[1]); break;
            case "UnderlinePosition": UnderlinePosition = D(parts[1]); break;
            case "UnderlineThickness": UnderlineThickness = D(parts[1]); break;
            case "IsFixedPitch": IsFixedPitch = parts[1] == "true"; break;
            case "FontBBox":
                BBoxLeft = D(parts[1]);
                BBoxBottom = D(parts[2]);
                BBoxRight = D(parts[3]);
                BBoxTop = D(parts[4]);
                break;
        }
    }

    private void ParseCharMetric(string line)
    {
        int code = -1;
        int width = 0;
        string? name = null;
        foreach (var field in line.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = field.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (t.Length < 2) continue;
            switch (t[0])
            {
                case "C": code = int.Parse(t[1], CultureInfo.InvariantCulture); break;
                case "WX": width = int.Parse(t[1], CultureInfo.InvariantCulture); break;
                case "N": name = t[1]; break;
            }
        }

        if (name == null) return;
        WidthByName[name] = width;
        if (code >= 0)
        {
            WidthByCode[code] = width;
            NameByCode[code] = name;
        }
    }

    private static double D(string s) => double.Parse(s, CultureInfo.InvariantCulture);
}
