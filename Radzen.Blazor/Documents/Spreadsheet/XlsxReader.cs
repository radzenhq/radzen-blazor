using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Reads a <see cref="Workbook"/> from a stream in the Open XML Spreadsheet format (XLSX).
/// </summary>
static class XlsxReader
{
    private const double EmuPerPixel = 9525.0;

    public static Workbook Read(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var workbook = new Workbook();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

        var styleInfo = ParseStyles(archive);
        var sharedStrings = ParseSharedStrings(archive);
        var sheetInfos = ParseSheetDefinitions(archive);

        foreach (var sheetInfo in sheetInfos)
        {
            var sheet = LoadSheet(archive, sheetInfo, styleInfo, sharedStrings);
            workbook.AddSheet(sheet);
        }

        ParseWorkbookProtection(archive, workbook);

        return workbook;
    }

    private static StyleInfo ParseStyles(ZipArchive archive)
    {
        var fontStyles = new Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline, bool Strikethrough, string? FontFamily, double? FontSize)>();
        var fillColors = new Dictionary<int, string>();
        var cellStyles = new Dictionary<int, (int FontId, int FillId, int BorderId, TextAlign TextAlign, VerticalAlign VerticalAlign, bool WrapText, int NumFmtId, bool? Locked, bool? FormulaHidden, bool QuotePrefix)>(0);
        var numberFormats = new Dictionary<int, string>();
        var borderStyles = new Dictionary<int, (BorderStyle? Top, BorderStyle? Right, BorderStyle? Bottom, BorderStyle? Left)>();

        var stylesEntry = archive.GetEntry("xl/styles.xml");
        if (stylesEntry is not null)
        {
            using var s = stylesEntry.Open();
            var stylesDoc = XDocument.Load(s);
            var stylesNs = stylesDoc.Root!.Name.Namespace;

            ParseFonts(stylesDoc, stylesNs, fontStyles);
            ParseFills(stylesDoc, stylesNs, fillColors);
            ParseNumberFormats(stylesDoc, stylesNs, numberFormats);
            ParseBorders(stylesDoc, stylesNs, borderStyles);
            ParseCellStyles(stylesDoc, stylesNs, fontStyles, fillColors, numberFormats, borderStyles, cellStyles);
        }

        return new StyleInfo(fontStyles, fillColors, cellStyles, numberFormats, borderStyles);
    }

    private static void ParseFonts(XDocument stylesDoc, XNamespace stylesNs, Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline, bool Strikethrough, string? FontFamily, double? FontSize)> fontStyles)
    {
        var fonts = stylesDoc.Descendants(stylesNs + "font").ToList();
        for (var i = 0; i < fonts.Count; i++)
        {
            var color = fonts[i].Element(stylesNs + "color")?.Attribute("rgb")?.Value;
            string? colorValue = null;
            if (color is not null)
            {
                colorValue = "#" + color[2..]; // Convert from "FFFF0000" to "#FF0000"
            }
            bool bold = fonts[i].Element(stylesNs + "b") is not null;
            bool italic = fonts[i].Element(stylesNs + "i") is not null;
            bool underline = fonts[i].Element(stylesNs + "u") is not null;
            bool strikethrough = fonts[i].Element(stylesNs + "strike") is not null;

            var fontName = fonts[i].Element(stylesNs + "name")?.Attribute("val")?.Value;
            string? fontFamily = fontName is not null && fontName != "Aptos Narrow" ? fontName : null;

            var szValue = fonts[i].Element(stylesNs + "sz")?.Attribute("val")?.Value;
            double? fontSize = null;
            if (szValue is not null && double.TryParse(szValue, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out var sz) && sz != 11)
            {
                fontSize = sz;
            }

            fontStyles[i] = (colorValue, bold, italic, underline, strikethrough, fontFamily, fontSize);
        }
    }

    private static void ParseBorders(XDocument stylesDoc, XNamespace stylesNs, Dictionary<int, (BorderStyle? Top, BorderStyle? Right, BorderStyle? Bottom, BorderStyle? Left)> borderStyles)
    {
        var borders = stylesDoc.Descendants(stylesNs + "border").ToList();
        for (var i = 0; i < borders.Count; i++)
        {
            var top = ParseBorderSide(borders[i].Element(stylesNs + "top"), stylesNs);
            var right = ParseBorderSide(borders[i].Element(stylesNs + "right"), stylesNs);
            var bottom = ParseBorderSide(borders[i].Element(stylesNs + "bottom"), stylesNs);
            var left = ParseBorderSide(borders[i].Element(stylesNs + "left"), stylesNs);

            if (top is not null || right is not null || bottom is not null || left is not null)
            {
                borderStyles[i] = (top, right, bottom, left);
            }
        }
    }

    private static BorderStyle? ParseBorderSide(XElement? element, XNamespace ns)
    {
        if (element is null)
        {
            return null;
        }

        var styleAttr = element.Attribute("style")?.Value;
        if (string.IsNullOrEmpty(styleAttr))
        {
            return null;
        }

        var lineStyle = BorderStyle.FromXlsxStyle(styleAttr);
        if (lineStyle == BorderLineStyle.None)
        {
            return null;
        }

        var colorElement = element.Element(ns + "color");
        var rgbColor = colorElement?.Attribute("rgb")?.Value;
        var color = rgbColor is not null ? "#" + rgbColor[2..] : "#000000";

        return new BorderStyle { LineStyle = lineStyle, Color = color };
    }

    private static void ParseFills(XDocument stylesDoc, XNamespace stylesNs, Dictionary<int, string> fillColors)
    {
        var fills = stylesDoc.Descendants(stylesNs + "fill").ToList();
        for (var i = 0; i < fills.Count; i++)
        {
            var fgColor = fills[i].Element(stylesNs + "patternFill")?.Element(stylesNs + "fgColor")?.Attribute("rgb")?.Value;
            if (fgColor is not null)
            {
                fillColors[i] = "#" + fgColor[2..]; // Convert from "FFFF0000" to "#FF0000"
            }
        }
    }

    private static void ParseNumberFormats(XDocument stylesDoc, XNamespace stylesNs, Dictionary<int, string> numberFormats)
    {
        var numFmts = stylesDoc.Descendants(stylesNs + "numFmt").ToList();
        foreach (var numFmt in numFmts)
        {
            var numFmtId = numFmt.Attribute("numFmtId")?.Value;
            var formatCode = numFmt.Attribute("formatCode")?.Value;
            if (numFmtId is not null && formatCode is not null)
            {
                numberFormats[int.Parse(numFmtId, CultureInfo.InvariantCulture)] = formatCode;
            }
        }
    }

    private static void ParseCellStyles(XDocument stylesDoc, XNamespace stylesNs,
        Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline, bool Strikethrough, string? FontFamily, double? FontSize)> fontStyles,
        Dictionary<int, string> fillColors,
        Dictionary<int, string> numberFormats,
        Dictionary<int, (BorderStyle? Top, BorderStyle? Right, BorderStyle? Bottom, BorderStyle? Left)> borderStyles,
        Dictionary<int, (int FontId, int FillId, int BorderId, TextAlign TextAlign, VerticalAlign VerticalAlign, bool WrapText, int NumFmtId, bool? Locked, bool? FormulaHidden, bool QuotePrefix)> cellStyles)
    {
        var cellXfs = stylesDoc.Descendants(stylesNs + "cellXfs").FirstOrDefault()?.Elements(stylesNs + "xf").ToList() ?? [];
        for (var i = 0; i < cellXfs.Count; i++)
        {
            var fontId = cellXfs[i].Attribute("fontId")?.Value;
            var fillId = cellXfs[i].Attribute("fillId")?.Value;
            var borderId = cellXfs[i].Attribute("borderId")?.Value;
            var applyFont = cellXfs[i].Attribute("applyFont")?.Value;
            var applyFill = cellXfs[i].Attribute("applyFill")?.Value;
            var applyBorder = cellXfs[i].Attribute("applyBorder")?.Value;
            var applyAlignment = cellXfs[i].Attribute("applyAlignment")?.Value;
            var numFmtIdAttr = cellXfs[i].Attribute("numFmtId")?.Value;
            var numFmtId = numFmtIdAttr is not null ? int.Parse(numFmtIdAttr, CultureInfo.InvariantCulture) : 0;
            var quotePrefix = cellXfs[i].Attribute("quotePrefix")?.Value == "1";

            if (fontId is not null && fillId is not null)
            {
                var fontIdValue = int.Parse(fontId, CultureInfo.InvariantCulture);
                var fillIdValue = int.Parse(fillId, CultureInfo.InvariantCulture);
                var borderIdValue = borderId is not null ? int.Parse(borderId, CultureInfo.InvariantCulture) : 0;

                var (textAlign, verticalAlign, wrapText) = ParseAlignment(cellXfs[i], stylesNs, applyAlignment);

                var applyProtection = cellXfs[i].Attribute("applyProtection")?.Value;
                var (locked, formulaHidden) = ParseProtection(cellXfs[i], stylesNs, applyProtection);

                if (applyFont == "1" && fontStyles.ContainsKey(fontIdValue) ||
                    applyFill == "1" && fillColors.ContainsKey(fillIdValue) ||
                    applyBorder == "1" && borderStyles.ContainsKey(borderIdValue) ||
                    applyAlignment == "1" ||
                    applyProtection == "1" ||
                    numFmtId > 0 ||
                    quotePrefix)
                {
                    cellStyles[i] = (fontIdValue, fillIdValue, borderIdValue, textAlign, verticalAlign, wrapText, numFmtId, locked, formulaHidden, quotePrefix);
                }
            }
        }
    }

    private static (TextAlign TextAlign, VerticalAlign VerticalAlign, bool WrapText) ParseAlignment(XElement cellXf, XNamespace stylesNs, string? applyAlignment)
    {
        var textAlign = TextAlign.Left;
        var verticalAlign = VerticalAlign.Top;
        var wrapText = false;

        if (applyAlignment == "1")
        {
            var alignment = cellXf.Element(stylesNs + "alignment");
            if (alignment is not null)
            {
                var horizontal = alignment.Attribute("horizontal")?.Value;
                textAlign = horizontal switch
                {
                    "center" => TextAlign.Center,
                    "right" => TextAlign.Right,
                    "justify" => TextAlign.Justify,
                    _ => TextAlign.Left
                };
                var vertical = alignment.Attribute("vertical")?.Value;
                verticalAlign = vertical switch
                {
                    "center" => VerticalAlign.Middle,
                    "bottom" => VerticalAlign.Bottom,
                    _ => VerticalAlign.Top
                };
                wrapText = alignment.Attribute("wrapText")?.Value == "1";
            }
        }

        return (textAlign, verticalAlign, wrapText);
    }

    private static (bool? Locked, bool? FormulaHidden) ParseProtection(XElement cellXf, XNamespace stylesNs, string? applyProtection)
    {
        bool? locked = null;
        bool? formulaHidden = null;

        if (applyProtection == "1")
        {
            var protection = cellXf.Element(stylesNs + "protection");
            if (protection is not null)
            {
                var lockedAttr = protection.Attribute("locked")?.Value;
                if (lockedAttr is not null)
                {
                    locked = lockedAttr != "0";
                }

                var hiddenAttr = protection.Attribute("hidden")?.Value;
                if (hiddenAttr is not null)
                {
                    formulaHidden = hiddenAttr == "1";
                }
            }
        }

        return (locked, formulaHidden);
    }

    private static List<string> ParseSharedStrings(ZipArchive archive)
    {
        var sharedStrings = new List<string>();
        var sharedEntry = archive.GetEntry("xl/sharedStrings.xml");

        if (sharedEntry is not null)
        {
            using var s = sharedEntry.Open();
            var doc = XDocument.Load(s);
            sharedStrings = doc.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value).ToList();
        }

        return sharedStrings;
    }

    private static List<WorksheetInfo> ParseSheetDefinitions(ZipArchive archive)
    {
        // Parse sheet definitions (sheet names + target file paths)
        var workbookEntry = archive.GetEntry("xl/workbook.xml") ?? throw new InvalidDataException("workbook.xml not found");

        using var wbStream = workbookEntry.Open();
        var wbDoc = XDocument.Load(wbStream);
        var ns = wbDoc.Root!.Name.Namespace;
        XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        var sheets = wbDoc.Descendants(ns + "sheet")
        .Select(sheet => new
        {
            Name = sheet.Attribute("name")!.Value,
            WorksheetId = sheet.Attribute("sheetId")!.Value,
            RelId = sheet.Attribute(r + "id")!.Value
        })
        .ToList();

        // Parse workbook relationships to resolve actual .xml file for each sheet
        var relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels") ?? throw new InvalidDataException("workbook.xml.rels not found");
        using var relsStream = relsEntry.Open();
        var relsDoc = XDocument.Load(relsStream);
        var relsNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");

        var relMap = relsDoc.Descendants(relsNs + "Relationship")
            .ToDictionary(
                r => r.Attribute("Id")!.Value,
                r => r.Attribute("Target")!.Value.Replace("\\", "/", StringComparison.Ordinal)
            );

        return sheets.Select(sheet => new WorksheetInfo(sheet.Name, sheet.WorksheetId, sheet.RelId, relMap))
            .Where(sheet => sheet.HasValidPath)
            .ToList();
    }

    private static Worksheet LoadSheet(ZipArchive archive, WorksheetInfo sheetInfo, StyleInfo styleInfo, List<string> sharedStrings)
    {
        var sheet = new Worksheet(100, 100); // adjust size as needed

        var sheetEntry = archive.GetEntry(sheetInfo.FullPath);
        if (sheetEntry is null)
        {
            return sheet;
        }

        using var sheetStream = sheetEntry.Open();
        var sheetDoc = XDocument.Load(sheetStream);
        var sNs = sheetDoc.Root!.Name.Namespace;

        // Parse default row height
        var defaultRowHeight = ParseDefaultRowHeight(sheetDoc, sNs);

        // Parse frozen panes
        ParseFrozenPanes(sheetDoc, sNs, sheet);

        // Parse column widths
        ParseColumnWidths(sheetDoc, sNs, sheet);

        // Parse rows and cells
        ParseRowsAndCells(sheetDoc, sNs, sheet, styleInfo, sharedStrings, defaultRowHeight);

        // Parse merged cells
        ParseMergedCells(sheetDoc, sNs, sheet);

        // Parse auto filter
        ParseAutoFilter(sheetDoc, sNs, sheet);

        // Parse data validations
        ParseDataValidations(sheetDoc, sNs, sheet);

        // Parse hyperlinks
        ParseHyperlinks(archive, sheetInfo, sheetDoc, sNs, sheet);

        // Parse drawings (images)
        ParseDrawings(archive, sheetInfo, sheetDoc, sNs, sheet);

        // Parse tables (must run after rows/cells so column-name fallback can read header values)
        ParseTables(archive, sheetInfo, sheetDoc, sNs, sheet);

        // Parse sheet protection
        ParseSheetProtection(sheetDoc, sNs, sheet);

        sheet.Name = sheetInfo.Name;
        return sheet;
    }

    private static double ParseDefaultRowHeight(XDocument sheetDoc, XNamespace sNs)
    {
        // Parse default row height from sheet format properties
        var defaultRowHeight = 20.0; // Default fallback
        var sheetFormatPr = sheetDoc.Descendants(sNs + "sheetFormatPr").FirstOrDefault();
        if (sheetFormatPr is not null)
        {
            var defaultHeight = sheetFormatPr.Attribute("defaultRowHeight")?.Value;
            if (defaultHeight is not null && double.TryParse(defaultHeight, NumberStyles.Float, CultureInfo.InvariantCulture, out var heightPoints))
            {
                defaultRowHeight = Math.Round(heightPoints * (96.0 / 72.0));
            }
        }

        return defaultRowHeight;
    }

    private static void ParseFrozenPanes(XDocument sheetDoc, XNamespace sNs, Worksheet sheet)
    {
        var sheetView = sheetDoc.Descendants(sNs + "sheetView").FirstOrDefault();
        if (sheetView is not null)
        {
            var pane = sheetView.Element(sNs + "pane");
            if (pane is not null)
            {
                var xSplit = pane.Attribute("xSplit")?.Value;
                var ySplit = pane.Attribute("ySplit")?.Value;

                if (xSplit is not null && int.TryParse(xSplit, out var frozenColumns))
                {
                    sheet.Columns.Frozen = frozenColumns;
                }

                if (ySplit is not null && int.TryParse(ySplit, out var frozenRows))
                {
                    sheet.Rows.Frozen = frozenRows;
                }
            }
        }
    }

    private static void ParseColumnWidths(XDocument sheetDoc, XNamespace sNs, Worksheet sheet)
    {
        var cols = sheetDoc.Descendants(sNs + "cols").FirstOrDefault();
        if (cols is not null)
        {
            foreach (var col in cols.Elements(sNs + "col"))
            {
                var min = col.Attribute("min")?.Value;
                var max = col.Attribute("max")?.Value;
                var width = col.Attribute("width")?.Value;

                if (min is not null && max is not null && width is not null &&
                    int.TryParse(min, out var minCol) &&
                    int.TryParse(max, out var maxCol) &&
                    double.TryParse(width, NumberStyles.Float, CultureInfo.InvariantCulture, out var colWidth))
                {
                    // Excel column width to pixels conversion:
                    // pixels = Truncate(((256 * width + Truncate(128/7)) / 256) * 7)
                    var pixelWidth = (256 * colWidth + Math.Truncate(128 / 7.0)) / 256 * 7;

                    for (var i = minCol - 1; i <= maxCol - 1 && i < sheet.Columns.Count; i++)
                    {
                        sheet.Columns[i] = pixelWidth;
                    }
                }
            }
        }
    }

    private static void ParseRowsAndCells(XDocument sheetDoc, XNamespace sNs, Worksheet sheet, StyleInfo styleInfo, List<string> sharedStrings, double defaultRowHeight)
    {
        foreach (var rowElem in sheetDoc.Descendants(sNs + "row"))
        {
            ParseRow(rowElem, sNs, sheet, styleInfo, sharedStrings, defaultRowHeight);
        }
    }

    private static void ParseRow(XElement rowElem, XNamespace sNs, Worksheet sheet, StyleInfo styleInfo, List<string> sharedStrings, double defaultRowHeight)
    {
        var rowIndex = rowElem.Attribute("r")?.Value;
        var rowHeight = rowElem.Attribute("ht")?.Value;
        var customHeight = rowElem.Attribute("customHeight")?.Value;

        if (rowIndex is not null && int.TryParse(rowIndex, out var rowNum))
        {
            var actualRowIndex = rowNum - 1; // Convert from 1-based to 0-based
            if (actualRowIndex >= 0 && actualRowIndex < sheet.Rows.Count)
            {
                if (rowHeight is not null && double.TryParse(rowHeight, NumberStyles.Float, CultureInfo.InvariantCulture, out var heightPoints))
                {
                    // Excel row height is in points, convert to pixels
                    // 1 point = 1/72 inch, 1 inch = 96 pixels (standard DPI)
                    // So: pixels = points * (96/72) = points * 1.333...
                    var pixelHeight = Math.Round(heightPoints * (96.0 / 72.0));
                    sheet.Rows[actualRowIndex] = pixelHeight;
                }
                else
                {
                    // Use default row height if no custom height specified
                    sheet.Rows[actualRowIndex] = defaultRowHeight;
                }
            }
        }

        foreach (var cellElem in rowElem.Elements(sNs + "c"))
        {
            ParseCell(cellElem, sNs, sheet, styleInfo, sharedStrings);
        }
    }

    private static void ParseCell(XElement cellElem, XNamespace sNs, Worksheet sheet, StyleInfo styleInfo, List<string> sharedStrings)
    {
        var cellRef = cellElem.Attribute("r")!;

        if (cellRef is null)
        {
            return;
        }

        var address = CellRef.Parse(cellRef.Value);

        var valueElem = cellElem.Element(sNs + "v");
        var formulaElem = cellElem.Element(sNs + "f");

        if (valueElem is null && formulaElem is null)
        {
            return;
        }

        var cellType = (string?)cellElem.Attribute("t") ?? "n";

        if (formulaElem is not null)
        {
            var formulaValue = formulaElem.Value;
            if (!formulaValue.StartsWith('='))
            {
                formulaValue = "=" + formulaValue;
            }
            sheet.Cells[address.Row, address.Column].Formula = formulaValue;
        }
        else
        {
            var value = cellType switch
            {
                "s" => sharedStrings[Convert.ToInt32(valueElem!.Value, CultureInfo.InvariantCulture)],
                _ => valueElem!.Value
            };

            sheet.Cells[address.Row, address.Column].Value = value;
        }

        ApplyCellStyle(cellElem, sheet, address, styleInfo);
    }

    private static void ApplyCellStyle(XElement cellElem, Worksheet sheet, CellRef address, StyleInfo styleInfo)
    {
        var styleId = cellElem.Attribute("s")?.Value;
        if (styleId is not null &&
            styleInfo.CellStyles.TryGetValue(int.Parse(styleId, CultureInfo.InvariantCulture), out var style))
        {
            var fmt = sheet.Cells[address.Row, address.Column].Format;

            if (styleInfo.FontStyles.TryGetValue(style.FontId, out var fontStyle))
            {
                if (fontStyle.Color is not null)
                {
                    fmt.Color = fontStyle.Color;
                }
                fmt.Bold = fontStyle.Bold;
                fmt.Italic = fontStyle.Italic;
                fmt.Underline = fontStyle.Underline;
                fmt.Strikethrough = fontStyle.Strikethrough;
                if (fontStyle.FontFamily is not null)
                {
                    fmt.FontFamily = fontStyle.FontFamily;
                }
                if (fontStyle.FontSize is not null)
                {
                    fmt.FontSize = fontStyle.FontSize;
                }
            }
            fmt.TextAlign = style.TextAlign;
            fmt.VerticalAlign = style.VerticalAlign;
            fmt.WrapText = style.WrapText;
            if (styleInfo.FillColors.TryGetValue(style.FillId, out var fillColor))
            {
                fmt.BackgroundColor = fillColor;
            }
            if (styleInfo.BorderStyles.TryGetValue(style.BorderId, out var borderStyle))
            {
                fmt.BorderTop = borderStyle.Top?.Clone();
                fmt.BorderRight = borderStyle.Right?.Clone();
                fmt.BorderBottom = borderStyle.Bottom?.Clone();
                fmt.BorderLeft = borderStyle.Left?.Clone();
            }
            if (style.NumFmtId > 0)
            {
                var formatCode = styleInfo.NumberFormats.TryGetValue(style.NumFmtId, out var custom)
                    ? custom
                    : NumberFormatPresets.GetFormatCode(style.NumFmtId);

                if (formatCode is not null && !string.Equals(formatCode, "General", StringComparison.OrdinalIgnoreCase))
                {
                    fmt.NumberFormat = formatCode;
                }
            }
            if (style.Locked is not null)
            {
                fmt.Locked = style.Locked;
            }
            if (style.FormulaHidden is not null)
            {
                fmt.FormulaHidden = style.FormulaHidden;
            }
            if (style.QuotePrefix)
            {
                sheet.Cells[address.Row, address.Column].QuotePrefix = true;
            }
        }
    }

    private static void ParseMergedCells(XDocument sheetDoc, XNamespace sNs, Worksheet sheet)
    {
        var mergeCells = sheetDoc.Descendants(sNs + "mergeCell")
            .Select(m => m.Attribute("ref")?.Value)
            .ToList();

        foreach (var mergedRange in mergeCells)
        {
            if (string.IsNullOrEmpty(mergedRange))
            {
                continue;
            }

            var range = RangeRef.Parse(mergedRange);
            sheet.MergedCells.Add(range);
        }
    }

    private static void ParseAutoFilter(XDocument sheetDoc, XNamespace sNs, Worksheet sheet)
    {
        var autoFilterElement = sheetDoc.Descendants(sNs + "autoFilter").FirstOrDefault();
        if (autoFilterElement is not null)
        {
            var refAttribute = autoFilterElement.Attribute("ref")?.Value;
            if (!string.IsNullOrEmpty(refAttribute))
            {
                var range = RangeRef.Parse(refAttribute);
                sheet.AutoFilter.Range = range;

                // Load filter columns
                var filterColumns = autoFilterElement.Elements(sNs + "filterColumn").ToList();
                foreach (var filterColumn in filterColumns)
                {
                    ParseFilterColumn(filterColumn, sNs, range, sheet);
                }
            }
        }
    }

    private static void ParseFilterColumn(XElement filterColumn, XNamespace sNs, RangeRef range, Worksheet sheet)
    {
        var colIdAttribute = filterColumn.Attribute("colId")?.Value;
        if (!string.IsNullOrEmpty(colIdAttribute) && int.TryParse(colIdAttribute, out var colId))
        {
            var actualColumn = range.Start.Column + colId;

            // Find customFilters or filters element
            var customFiltersElement = filterColumn.Element(sNs + "customFilters");
            var filtersElement = filterColumn.Element(sNs + "filters");

            if (customFiltersElement is not null)
            {
                var criterion = DeserializeFilterCriterion(customFiltersElement, actualColumn);
                var sheetFilter = new SheetFilter(criterion, range);
                sheet.AddFilter(sheetFilter);
            }
            else if (filtersElement is not null)
            {
                var criterion = DeserializeFilterCriterion(filtersElement, actualColumn);
                var sheetFilter = new SheetFilter(criterion, range);
                sheet.AddFilter(sheetFilter);
            }
        }
    }

    private static void ParseHyperlinks(ZipArchive archive, WorksheetInfo sheetInfo, XDocument sheetDoc, XNamespace sNs, Worksheet sheet)
    {
        var hyperlinks = sheetDoc.Descendants(sNs + "hyperlink").ToList();
        if (hyperlinks.Count == 0)
        {
            return;
        }

        // Load sheet relationships
        var relMap = new Dictionary<string, string>();
        var sheetFileName = sheetInfo.FullPath.Split('/').Last();
        var relsPath = $"xl/worksheets/_rels/{sheetFileName}.rels";
        var relsEntry = archive.GetEntry(relsPath);
        if (relsEntry is not null)
        {
            using var relsStream = relsEntry.Open();
            var relsDoc = XDocument.Load(relsStream);
            var relsNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
            foreach (var rel in relsDoc.Descendants(relsNs + "Relationship"))
            {
                var id = rel.Attribute("Id")?.Value;
                var target = rel.Attribute("Target")?.Value;
                if (id is not null && target is not null)
                {
                    relMap[id] = target;
                }
            }
        }

        XNamespace rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        foreach (var hyperlink in hyperlinks)
        {
            var cellRef = hyperlink.Attribute("ref")?.Value;
            var relId = hyperlink.Attribute(rNs + "id")?.Value;
            var display = hyperlink.Attribute("display")?.Value;

            if (cellRef is not null && relId is not null && relMap.TryGetValue(relId, out var url))
            {
                var address = CellRef.Parse(cellRef);
                if (address.Row < sheet.RowCount && address.Column < sheet.ColumnCount)
                {
                    sheet.Cells[address.Row, address.Column].Hyperlink = new Hyperlink
                    {
                        Url = url,
                        Text = display
                    };
                }
            }
        }
    }

    private static void ParseDrawings(ZipArchive archive, WorksheetInfo sheetInfo, XDocument sheetDoc, XNamespace sNs, Worksheet sheet)
    {
        // Find <drawing r:id="..."/> element in sheet XML
        XNamespace rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var drawingElement = sheetDoc.Descendants(sNs + "drawing").FirstOrDefault();
        if (drawingElement is null)
        {
            return;
        }

        var drawingRelId = drawingElement.Attribute(rNs + "id")?.Value;
        if (drawingRelId is null)
        {
            return;
        }

        // Load sheet relationships to resolve drawing rId
        var sheetFileName = sheetInfo.FullPath.Split('/').Last();
        var relsPath = $"xl/worksheets/_rels/{sheetFileName}.rels";
        var relsEntry = archive.GetEntry(relsPath);
        if (relsEntry is null)
        {
            return;
        }

        var sheetRelMap = new Dictionary<string, string>();
        using (var relsStream = relsEntry.Open())
        {
            var relsDoc = XDocument.Load(relsStream);
            var relsNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
            foreach (var rel in relsDoc.Descendants(relsNs + "Relationship"))
            {
                var id = rel.Attribute("Id")?.Value;
                var target = rel.Attribute("Target")?.Value;
                if (id is not null && target is not null)
                {
                    sheetRelMap[id] = target;
                }
            }
        }

        if (!sheetRelMap.TryGetValue(drawingRelId, out var drawingTarget))
        {
            return;
        }

        // Resolve drawing path relative to xl/worksheets/
        var drawingPath = ResolvePath("xl/worksheets/", drawingTarget);
        var drawingEntry = archive.GetEntry(drawingPath);
        if (drawingEntry is null)
        {
            return;
        }

        // Load drawing relationships (for resolving image rIds)
        var drawingFileName = drawingPath.Split('/').Last();
        var drawingDir = drawingPath[..drawingPath.LastIndexOf('/')];
        var drawingRelsPath = $"{drawingDir}/_rels/{drawingFileName}.rels";
        var drawingRelMap = new Dictionary<string, string>();
        var drawingRelsEntry = archive.GetEntry(drawingRelsPath);
        if (drawingRelsEntry is not null)
        {
            using var drawingRelsStream = drawingRelsEntry.Open();
            var drawingRelsDoc = XDocument.Load(drawingRelsStream);
            var relsNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
            foreach (var rel in drawingRelsDoc.Descendants(relsNs + "Relationship"))
            {
                var id = rel.Attribute("Id")?.Value;
                var target = rel.Attribute("Target")?.Value;
                if (id is not null && target is not null)
                {
                    drawingRelMap[id] = target;
                }
            }
        }

        // Parse drawing XML
        XNamespace xdr = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
        XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";

        using var drawingStream = drawingEntry.Open();
        var drawingDoc = XDocument.Load(drawingStream);

        foreach (var anchor in drawingDoc.Root!.Elements())
        {
            DrawingAnchorMode mode;
            if (anchor.Name == xdr + "twoCellAnchor")
            {
                mode = DrawingAnchorMode.TwoCellAnchor;
            }
            else if (anchor.Name == xdr + "oneCellAnchor")
            {
                mode = DrawingAnchorMode.OneCellAnchor;
            }
            else
            {
                continue;
            }

            // Parse anchor positions
            var from = ParseCellAnchor(anchor.Element(xdr + "from"), xdr);
            if (from is null)
            {
                continue;
            }

            CellAnchor? to = null;
            double anchorWidth = 0, anchorHeight = 0;

            if (mode == DrawingAnchorMode.TwoCellAnchor)
            {
                to = ParseCellAnchor(anchor.Element(xdr + "to"), xdr);
                if (to is null)
                {
                    continue;
                }
            }
            else
            {
                var ext = anchor.Element(xdr + "ext");
                if (ext is not null)
                {
                    if (long.TryParse(ext.Attribute("cx")?.Value, out var cx))
                    {
                        anchorWidth = cx / EmuPerPixel;
                    }
                    if (long.TryParse(ext.Attribute("cy")?.Value, out var cy))
                    {
                        anchorHeight = cy / EmuPerPixel;
                    }
                }
            }

            // Check for chart (graphicFrame)
            var graphicFrame = anchor.Element(xdr + "graphicFrame");
            if (graphicFrame is not null)
            {
                ParseChartFromGraphicFrame(archive, graphicFrame, drawingRelMap, drawingDir, rNs, a, sheet, mode, from, to, anchorWidth, anchorHeight);
                continue;
            }

            // Check for image (pic)
            var pic = anchor.Element(xdr + "pic");
            if (pic is null)
            {
                continue;
            }

            var image = new SheetImage
            {
                AnchorMode = mode,
                From = from
            };

            if (mode == DrawingAnchorMode.TwoCellAnchor)
            {
                image.To = to;
            }
            else
            {
                image.Width = anchorWidth;
                image.Height = anchorHeight;
            }

            // Get image rId from blip
            var blipFill = pic.Element(xdr + "blipFill");
            var blip = blipFill?.Element(a + "blip");
            var embedId = blip?.Attribute(rNs + "embed")?.Value;
            if (embedId is null || !drawingRelMap.TryGetValue(embedId, out var imageTarget))
            {
                continue;
            }

            // Resolve image path
            var imagePath = ResolvePath(drawingDir + "/", imageTarget);
            var imageEntry = archive.GetEntry(imagePath);
            if (imageEntry is null)
            {
                continue;
            }

            // Read image data
            using var imageStream = imageEntry.Open();
            using var ms = new System.IO.MemoryStream();
            imageStream.CopyTo(ms);
            image.Data = ms.ToArray();

            // Determine content type from extension
            var ext2 = imagePath.Split('.').Last().ToLowerInvariant();
            image.ContentType = ext2 switch
            {
                "png" => "image/png",
                "jpg" or "jpeg" => "image/jpeg",
                "gif" => "image/gif",
                "bmp" => "image/bmp",
                "svg" => "image/svg+xml",
                "webp" => "image/webp",
                "tiff" or "tif" => "image/tiff",
                _ => "image/png"
            };

            // Parse name/description
            var nvPicPr = pic.Element(xdr + "nvPicPr");
            var cNvPr = nvPicPr?.Element(xdr + "cNvPr");
            image.Name = cNvPr?.Attribute("name")?.Value;
            image.Description = cNvPr?.Attribute("descr")?.Value;

            sheet.AddImage(image);
        }
    }

    private static void ParseChartFromGraphicFrame(
        ZipArchive archive, XElement graphicFrame, Dictionary<string, string> drawingRelMap,
        string drawingDir, XNamespace rNs, XNamespace a,
        Worksheet sheet, DrawingAnchorMode mode, CellAnchor from, CellAnchor? to, double width, double height)
    {
        // Look for <a:graphic><a:graphicData uri="...chart..."><c:chart r:id="..."/></a:graphicData></a:graphic>
        var graphic = graphicFrame.Element(a + "graphic");
        var graphicData = graphic?.Element(a + "graphicData");
        var uri = graphicData?.Attribute("uri")?.Value;

        if (uri != "http://schemas.openxmlformats.org/drawingml/2006/chart")
        {
            return;
        }

        XNamespace c = "http://schemas.openxmlformats.org/drawingml/2006/chart";
        var chartElement = graphicData?.Element(c + "chart");
        var chartRelId = chartElement?.Attribute(rNs + "id")?.Value;

        if (chartRelId is null || !drawingRelMap.TryGetValue(chartRelId, out var chartTarget))
        {
            return;
        }

        var chartPath = ResolvePath(drawingDir + "/", chartTarget);
        var chartEntry = archive.GetEntry(chartPath);
        if (chartEntry is null)
        {
            return;
        }

        using var chartStream = chartEntry.Open();
        var chartDoc = XDocument.Load(chartStream);

        var chart = ParseChart(chartDoc, c, a);
        if (chart is null)
        {
            return;
        }

        chart.AnchorMode = mode;
        chart.From = from;
        chart.To = to;
        chart.Width = width;
        chart.Height = height;

        // Parse name from graphicFrame
        XNamespace xdr = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
        var nvGraphicFramePr = graphicFrame.Element(xdr + "nvGraphicFramePr");
        var cNvPr = nvGraphicFramePr?.Element(xdr + "cNvPr");
        chart.Name = cNvPr?.Attribute("name")?.Value;

        sheet.AddChart(chart);
    }

    private static SheetChart? ParseChart(XDocument chartDoc, XNamespace c, XNamespace a)
    {
        var chartSpace = chartDoc.Root;
        if (chartSpace is null)
        {
            return null;
        }

        var chart = new SheetChart
        {
            RawChartXml = chartDoc.ToString()
        };

        var chartElement = chartSpace.Element(c + "chart");
        if (chartElement is null)
        {
            chart.ChartType = SpreadsheetChartType.Unsupported;
            return chart;
        }

        // Parse title
        var titleElement = chartElement.Element(c + "title");
        if (titleElement is not null)
        {
            chart.Title = ParseChartTitle(titleElement, c, a);
        }

        // Parse legend
        var legendElement = chartElement.Element(c + "legend");
        if (legendElement is not null)
        {
            chart.ShowLegend = true;
            var legendPos = legendElement.Element(c + "legendPos")?.Attribute("val")?.Value;
            chart.LegendPosition = legendPos switch
            {
                "t" => ChartLegendPosition.Top,
                "b" => ChartLegendPosition.Bottom,
                "l" => ChartLegendPosition.Left,
                "r" or "tr" => ChartLegendPosition.Right,
                _ => ChartLegendPosition.Right
            };
        }

        // Parse plot area
        var plotArea = chartElement.Element(c + "plotArea");
        if (plotArea is null)
        {
            chart.ChartType = SpreadsheetChartType.Unsupported;
            return chart;
        }

        // Find chart type group element
        foreach (var groupElement in plotArea.Elements())
        {
            var localName = groupElement.Name.LocalName;
            var chartType = localName switch
            {
                "barChart" => ParseBarChartType(groupElement, c),
                "lineChart" => SpreadsheetChartType.Line,
                "areaChart" => ParseAreaChartType(groupElement, c),
                "pieChart" => SpreadsheetChartType.Pie,
                "doughnutChart" => SpreadsheetChartType.Donut,
                "scatterChart" => SpreadsheetChartType.Scatter,
                _ => (SpreadsheetChartType?)null
            };

            if (chartType is null)
            {
                continue;
            }

            chart.ChartType = chartType.Value;

            // Parse series
            foreach (var ser in groupElement.Elements(c + "ser"))
            {
                var series = ParseChartSeries(ser, c, a);
                if (series is not null)
                {
                    chart.Series.Add(series);
                }
            }

            break; // Use the first recognized chart type group
        }

        if (chart.Series.Count == 0 && chart.ChartType != SpreadsheetChartType.Unsupported)
        {
            chart.ChartType = SpreadsheetChartType.Unsupported;
        }

        return chart;
    }

    private static SpreadsheetChartType ParseBarChartType(XElement barChart, XNamespace c)
    {
        var barDir = barChart.Element(c + "barDir")?.Attribute("val")?.Value ?? "col";
        var grouping = barChart.Element(c + "grouping")?.Attribute("val")?.Value ?? "clustered";

        return (barDir, grouping) switch
        {
            ("bar", "stacked") => SpreadsheetChartType.StackedBar,
            ("bar", "percentStacked") => SpreadsheetChartType.FullStackedBar,
            ("bar", _) => SpreadsheetChartType.Bar,
            (_, "stacked") => SpreadsheetChartType.StackedColumn,
            (_, "percentStacked") => SpreadsheetChartType.FullStackedColumn,
            _ => SpreadsheetChartType.Column
        };
    }

    private static SpreadsheetChartType ParseAreaChartType(XElement areaChart, XNamespace c)
    {
        var grouping = areaChart.Element(c + "grouping")?.Attribute("val")?.Value ?? "standard";

        return grouping switch
        {
            "stacked" => SpreadsheetChartType.StackedArea,
            "percentStacked" => SpreadsheetChartType.FullStackedArea,
            _ => SpreadsheetChartType.Area
        };
    }

    private static string? ParseChartTitle(XElement titleElement, XNamespace c, XNamespace a)
    {
        // c:title > c:tx > c:rich > a:p > a:r > a:t
        var tx = titleElement.Element(c + "tx");
        var rich = tx?.Element(c + "rich");
        if (rich is not null)
        {
            var result = new System.Text.StringBuilder();
            foreach (var p in rich.Elements(a + "p"))
            {
                foreach (var r in p.Elements(a + "r"))
                {
                    var t = r.Element(a + "t")?.Value;
                    if (t is not null)
                    {
                        result.Append(t);
                    }
                }
            }
            return result.Length > 0 ? result.ToString() : null;
        }

        // c:title > c:tx > c:strRef > c:strCache > c:pt > c:v
        var strRef = tx?.Element(c + "strRef");
        var strCache = strRef?.Element(c + "strCache");
        var pt = strCache?.Element(c + "pt");
        return pt?.Element(c + "v")?.Value;
    }

    private static ChartSeriesDefinition? ParseChartSeries(XElement ser, XNamespace c, XNamespace a)
    {
        var series = new ChartSeriesDefinition();

        // Parse index
        var idx = ser.Element(c + "idx")?.Attribute("val")?.Value;
        if (idx is not null && int.TryParse(idx, out var index))
        {
            series.Index = index;
        }

        // Parse title
        var tx = ser.Element(c + "tx");
        if (tx is not null)
        {
            var strRef = tx.Element(c + "strRef");
            if (strRef is not null)
            {
                series.Title = strRef.Element(c + "strCache")?.Element(c + "pt")?.Element(c + "v")?.Value;
            }
            else
            {
                series.Title = tx.Element(c + "v")?.Value;
            }
        }

        // Parse categories
        var cat = ser.Element(c + "cat");
        if (cat is not null)
        {
            var strRef = cat.Element(c + "strRef");
            var numRef = cat.Element(c + "numRef");
            var refElement = strRef ?? numRef;

            if (refElement is not null)
            {
                series.CategoryFormula = refElement.Element(c + "f")?.Value;

                var cache = refElement.Element(c + "strCache") ?? refElement.Element(c + "numCache");
                if (cache is not null)
                {
                    foreach (var pt in cache.Elements(c + "pt"))
                    {
                        series.CategoryCache.Add(pt.Element(c + "v")?.Value ?? "");
                    }
                }
            }
        }

        // Parse values
        var val = ser.Element(c + "val");
        if (val is not null)
        {
            var numRef = val.Element(c + "numRef");
            if (numRef is not null)
            {
                series.ValueFormula = numRef.Element(c + "f")?.Value;

                var numCache = numRef.Element(c + "numCache");
                if (numCache is not null)
                {
                    foreach (var pt in numCache.Elements(c + "pt"))
                    {
                        var v = pt.Element(c + "v")?.Value;
                        if (v is not null && double.TryParse(v, System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var dv))
                        {
                            series.ValueCache.Add(dv);
                        }
                        else
                        {
                            series.ValueCache.Add(null);
                        }
                    }
                }
            }
        }

        // Parse color from spPr > solidFill > srgbClr
        var spPr = ser.Element(c + "spPr");
        var solidFill = spPr?.Element(a + "solidFill");
        var srgbClr = solidFill?.Element(a + "srgbClr");
        var colorVal = srgbClr?.Attribute("val")?.Value;
        if (colorVal is not null)
        {
            series.Color = $"#{colorVal}";
        }

        return series;
    }

    private static CellAnchor? ParseCellAnchor(XElement? element, XNamespace xdr)
    {
        if (element is null)
        {
            return null;
        }

        var col = element.Element(xdr + "col")?.Value;
        var colOff = element.Element(xdr + "colOff")?.Value;
        var row = element.Element(xdr + "row")?.Value;
        var rowOff = element.Element(xdr + "rowOff")?.Value;

        if (col is null || row is null)
        {
            return null;
        }

        return new CellAnchor
        {
            Column = int.Parse(col, System.Globalization.CultureInfo.InvariantCulture),
            ColumnOffset = long.TryParse(colOff, out var co) ? co / EmuPerPixel : 0,
            Row = int.Parse(row, System.Globalization.CultureInfo.InvariantCulture),
            RowOffset = long.TryParse(rowOff, out var ro) ? ro / EmuPerPixel : 0
        };
    }

    private static string ResolvePath(string basePath, string relativePath)
    {
        // Handle relative paths like ../media/image1.png
        var parts = (basePath.TrimEnd('/') + "/" + relativePath).Split('/');
        var stack = new System.Collections.Generic.Stack<string>();
        foreach (var part in parts)
        {
            if (part == "..")
            {
                if (stack.Count > 0)
                {
                    stack.Pop();
                }
            }
            else if (part != "." && part.Length > 0)
            {
                stack.Push(part);
            }
        }
        var result = new string[stack.Count];
        for (var i = stack.Count - 1; i >= 0; i--)
        {
            result[i] = stack.Pop();
        }
        return string.Join("/", result);
    }

    private static void ParseTables(ZipArchive archive, WorksheetInfo sheetInfo, XDocument sheetDoc, XNamespace sNs, Worksheet sheet)
    {
        XNamespace rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var tableParts = sheetDoc.Descendants(sNs + "tablePart").ToList();
        if (tableParts.Count == 0) return;

        // Resolve sheet rels into a map
        var sheetFileName = sheetInfo.FullPath.Split('/').Last();
        var relsPath = $"xl/worksheets/_rels/{sheetFileName}.rels";
        var relsEntry = archive.GetEntry(relsPath);
        if (relsEntry is null) return;

        var sheetRelMap = new Dictionary<string, string>();
        using (var relsStream = relsEntry.Open())
        {
            var relsDoc = XDocument.Load(relsStream);
            var relsNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
            foreach (var rel in relsDoc.Descendants(relsNs + "Relationship"))
            {
                var id = rel.Attribute("Id")?.Value;
                var target = rel.Attribute("Target")?.Value;
                if (id is not null && target is not null)
                {
                    sheetRelMap[id] = target;
                }
            }
        }

        foreach (var tablePart in tableParts)
        {
            var relId = tablePart.Attribute(rNs + "id")?.Value;
            if (relId is null || !sheetRelMap.TryGetValue(relId, out var target)) continue;

            var tablePath = ResolvePath("xl/worksheets/", target);
            var tableEntry = archive.GetEntry(tablePath);
            if (tableEntry is null) continue;

            using var tableStream = tableEntry.Open();
            var tableDoc = XDocument.Load(tableStream);
            var tNs = tableDoc.Root!.Name.Namespace;
            var root = tableDoc.Root!;

            var name = root.Attribute("name")?.Value ?? "Table";
            var displayName = root.Attribute("displayName")?.Value;
            var refStr = root.Attribute("ref")?.Value;
            var headerRowCount = root.Attribute("headerRowCount")?.Value;
            var totalsRowCount = root.Attribute("totalsRowCount")?.Value;

            if (refStr is null || !TryParseRange(refStr, out var range)) continue;

            var hasHeaders = headerRowCount != "0";
            var table = sheet.AddTable(name, range, hasHeaders);
            if (displayName is not null && displayName != name) table.DisplayName = displayName;
            table.ShowTotalsRow = totalsRowCount == "1";

            // Filter button: presence of <autoFilter> child indicates ShowFilterButton=true
            var hasAutoFilter = root.Element(tNs + "autoFilter") is not null;
            table.ShowFilterButton = hasAutoFilter;

            // Style + striping
            var styleInfo = root.Element(tNs + "tableStyleInfo");
            if (styleInfo is not null)
            {
                table.TableStyle = styleInfo.Attribute("name")?.Value;
                table.HighlightFirstColumn = styleInfo.Attribute("showFirstColumn")?.Value == "1";
                table.HighlightLastColumn = styleInfo.Attribute("showLastColumn")?.Value == "1";
                table.ShowBandedRows = styleInfo.Attribute("showRowStripes")?.Value != "0";
                table.ShowBandedColumns = styleInfo.Attribute("showColumnStripes")?.Value == "1";
            }

            // Columns: rename + totals function + calculated formula
            var tableColumns = root.Element(tNs + "tableColumns");
            if (tableColumns is not null)
            {
                var i = 0;
                foreach (var colElem in tableColumns.Elements(tNs + "tableColumn"))
                {
                    if (i >= table.Columns.Count) break;
                    var colName = colElem.Attribute("name")?.Value;
                    if (colName is not null) table.Columns[i].Name = colName;

                    var totalsFunction = colElem.Attribute("totalsRowFunction")?.Value;
                    if (totalsFunction is not null)
                    {
                        table.Columns[i].TotalsCalculation = XlsxWriter.TotalsCalculationFromXml(totalsFunction);
                    }

                    var calcFormula = colElem.Element(tNs + "calculatedColumnFormula")?.Value;
                    if (!string.IsNullOrEmpty(calcFormula))
                    {
                        table.Columns[i].CalculatedFormula = calcFormula;
                    }
                    i++;
                }
            }
        }
    }

    private static bool TryParseRange(string s, out RangeRef range)
    {
        range = default;
        var parts = s.Split(':');
        if (parts.Length == 1)
        {
            if (CellRef.TryParse(parts[0], out var only))
            {
                range = new RangeRef(only, only);
                return true;
            }
            return false;
        }
        if (parts.Length == 2
            && CellRef.TryParse(parts[0], out var start)
            && CellRef.TryParse(parts[1], out var end))
        {
            range = new RangeRef(start, end);
            return true;
        }
        return false;
    }

    private static void ParseSheetProtection(XDocument sheetDoc, XNamespace sNs, Worksheet sheet)
    {
        var protElement = sheetDoc.Descendants(sNs + "sheetProtection").FirstOrDefault();
        if (protElement is null)
        {
            return;
        }

        var p = sheet.Protection;
        p.IsProtected = protElement.Attribute("sheet")?.Value == "1";

        // XLSX: attribute absent or "1" = forbidden; "0" = allowed
        p.AllowFormatCells = protElement.Attribute("formatCells")?.Value == "0";
        p.AllowFormatRows = protElement.Attribute("formatRows")?.Value == "0";
        p.AllowFormatColumns = protElement.Attribute("formatColumns")?.Value == "0";
        p.AllowInsertColumns = protElement.Attribute("insertColumns")?.Value == "0";
        p.AllowInsertRows = protElement.Attribute("insertRows")?.Value == "0";
        p.AllowInsertHyperlinks = protElement.Attribute("insertHyperlinks")?.Value == "0";
        p.AllowDeleteColumns = protElement.Attribute("deleteColumns")?.Value == "0";
        p.AllowDeleteRows = protElement.Attribute("deleteRows")?.Value == "0";
        p.AllowSort = protElement.Attribute("sort")?.Value == "0";
        p.AllowAutoFilter = protElement.Attribute("autoFilter")?.Value == "0";

        // selectLockedCells/selectUnlockedCells: "1" means forbidden, absent means allowed
        p.AllowSelectLockedCells = protElement.Attribute("selectLockedCells")?.Value != "1";
        p.AllowSelectUnlockedCells = protElement.Attribute("selectUnlockedCells")?.Value != "1";

        // Password hashes (preserved for round-trip)
        p.PasswordHash = protElement.Attribute("password")?.Value;
        p.AlgorithmName = protElement.Attribute("algorithmName")?.Value;
        p.HashValue = protElement.Attribute("hashValue")?.Value;
        p.SaltValue = protElement.Attribute("saltValue")?.Value;
        var spinCountAttr = protElement.Attribute("spinCount")?.Value;
        if (spinCountAttr is not null && int.TryParse(spinCountAttr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sc))
        {
            p.SpinCount = sc;
        }
    }

    private static void ParseWorkbookProtection(ZipArchive archive, Workbook workbook)
    {
        var entry = archive.GetEntry("xl/workbook.xml");
        if (entry is null)
        {
            return;
        }

        using var stream = entry.Open();
        var doc = XDocument.Load(stream);
        var ns = doc.Root!.Name.Namespace;

        var protElement = doc.Descendants(ns + "workbookProtection").FirstOrDefault();
        if (protElement is null)
        {
            return;
        }

        var p = workbook.Protection;
        p.LockStructure = protElement.Attribute("lockStructure")?.Value == "1";
        p.PasswordHash = protElement.Attribute("workbookPassword")?.Value ?? protElement.Attribute("password")?.Value;
        p.AlgorithmName = protElement.Attribute("workbookAlgorithmName")?.Value;
        p.HashValue = protElement.Attribute("workbookHashValue")?.Value;
        p.SaltValue = protElement.Attribute("workbookSaltValue")?.Value;
        var spinCountAttr = protElement.Attribute("workbookSpinCount")?.Value;
        if (spinCountAttr is not null && int.TryParse(spinCountAttr, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sc))
        {
            p.SpinCount = sc;
        }
    }

    private class StyleInfo
    {
        public Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline, bool Strikethrough, string? FontFamily, double? FontSize)> FontStyles { get; }
        public Dictionary<int, string> FillColors { get; }
        public Dictionary<int, (int FontId, int FillId, int BorderId, TextAlign TextAlign, VerticalAlign VerticalAlign, bool WrapText, int NumFmtId, bool? Locked, bool? FormulaHidden, bool QuotePrefix)> CellStyles { get; }
        public Dictionary<int, string> NumberFormats { get; }
        public Dictionary<int, (BorderStyle? Top, BorderStyle? Right, BorderStyle? Bottom, BorderStyle? Left)> BorderStyles { get; }

        public StyleInfo(
            Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline, bool Strikethrough, string? FontFamily, double? FontSize)> fontStyles,
            Dictionary<int, string> fillColors,
            Dictionary<int, (int FontId, int FillId, int BorderId, TextAlign TextAlign, VerticalAlign VerticalAlign, bool WrapText, int NumFmtId, bool? Locked, bool? FormulaHidden, bool QuotePrefix)> cellStyles,
            Dictionary<int, string> numberFormats,
            Dictionary<int, (BorderStyle? Top, BorderStyle? Right, BorderStyle? Bottom, BorderStyle? Left)> borderStyles)
        {
            FontStyles = fontStyles;
            FillColors = fillColors;
            CellStyles = cellStyles;
            NumberFormats = numberFormats;
            BorderStyles = borderStyles;
        }
    }

    private class WorksheetInfo
    {
        public string Name { get; }
        public string WorksheetId { get; }
        public string RelId { get; }
        public Dictionary<string, string> RelMap { get; }
        public bool HasValidPath => RelMap.TryGetValue(RelId, out _);
        public string FullPath
        {
            get
            {
                if (RelMap.TryGetValue(RelId, out var sheetPath))
                {
                    return $"xl/{sheetPath.TrimStart('/')}"; // Excel paths are relative
                }
                return string.Empty;
            }
        }

        public WorksheetInfo(string name, string sheetId, string relId, Dictionary<string, string> relMap)
        {
            Name = name;
            WorksheetId = sheetId;
            RelId = relId;
            RelMap = relMap;
        }
    }

    private static void ParseDataValidations(XDocument sheetDoc, XNamespace sNs, Worksheet sheet)
    {
        var dataValidationsElement = sheetDoc.Descendants(sNs + "dataValidations").FirstOrDefault();
        if (dataValidationsElement is null)
        {
            return;
        }

        foreach (var dvElement in dataValidationsElement.Elements(sNs + "dataValidation"))
        {
            var typeAttr = dvElement.Attribute("type")?.Value;
            var operatorAttr = dvElement.Attribute("operator")?.Value;
            var sqrefAttr = dvElement.Attribute("sqref")?.Value;
            var allowBlankAttr = dvElement.Attribute("allowBlank")?.Value;
            var errorAttr = dvElement.Attribute("error")?.Value;
            var errorTitleAttr = dvElement.Attribute("errorTitle")?.Value;
            var promptAttr = dvElement.Attribute("prompt")?.Value;
            var promptTitleAttr = dvElement.Attribute("promptTitle")?.Value;
            var showErrorMessageAttr = dvElement.Attribute("showErrorMessage")?.Value;
            var showInputMessageAttr = dvElement.Attribute("showInputMessage")?.Value;
            var errorStyleAttr = dvElement.Attribute("errorStyle")?.Value;

            if (string.IsNullOrEmpty(sqrefAttr))
            {
                continue;
            }

            var type = typeAttr switch
            {
                "whole" => DataValidationType.WholeNumber,
                "decimal" => DataValidationType.Decimal,
                "list" => DataValidationType.List,
                "date" => DataValidationType.Date,
                "time" => DataValidationType.Time,
                "textLength" => DataValidationType.TextLength,
                "custom" => DataValidationType.Custom,
                _ => DataValidationType.WholeNumber
            };

            var op = operatorAttr switch
            {
                "between" => DataValidationOperator.Between,
                "notBetween" => DataValidationOperator.NotBetween,
                "equal" => DataValidationOperator.Equal,
                "notEqual" => DataValidationOperator.NotEqual,
                "greaterThan" => DataValidationOperator.GreaterThan,
                "lessThan" => DataValidationOperator.LessThan,
                "greaterThanOrEqual" => DataValidationOperator.GreaterThanOrEqual,
                "lessThanOrEqual" => DataValidationOperator.LessThanOrEqual,
                _ => DataValidationOperator.Between
            };

            var errorStyle = errorStyleAttr switch
            {
                "warning" => DataValidationErrorStyle.Warning,
                "information" => DataValidationErrorStyle.Information,
                _ => DataValidationErrorStyle.Stop
            };

            var formula1 = dvElement.Element(sNs + "formula1")?.Value;
            var formula2 = dvElement.Element(sNs + "formula2")?.Value;

            // For list type, formula1 contains individually quoted items: "Yes","No","Maybe"
            // Or may be wrapped as a single string: "Yes,No,Maybe"
            if (type == DataValidationType.List && formula1 is not null)
            {
                // Remove surrounding quotes from each item and rejoin with commas
                var parts = formula1.Split(',');
                formula1 = string.Join(",", parts.Select(p => p.Trim().Trim('"')));
            }

            var rule = new DataValidationRule
            {
                Type = type,
                Operator = op,
                Formula1 = formula1,
                Formula2 = formula2,
                AllowBlank = allowBlankAttr != "0",
                Error = errorAttr ?? "The value you entered is not valid.",
                ErrorTitle = errorTitleAttr,
                InputMessage = promptAttr,
                InputTitle = promptTitleAttr,
                ShowErrorMessage = showErrorMessageAttr == "1",
                ShowInputMessage = showInputMessageAttr == "1",
                ErrorStyle = errorStyle
            };

            // Handle multiple sqref ranges (space-separated)
            var sqrefs = sqrefAttr.Split(' ');
            foreach (var sqref in sqrefs)
            {
                if (!string.IsNullOrEmpty(sqref))
                {
                    var range = RangeRef.Parse(sqref);
                    sheet.Validation.Add(range, rule);
                }
            }
        }
    }


    private static FilterCriterion DeserializeFilterCriterion(XElement element, int columnIndex)
    {
        var ns = element.Name.Namespace;

        if (element.Name.LocalName == "customFilters")
        {
            var customFilters = element.Elements(ns + "customFilter").ToList();
            if (customFilters.Count == 0)
            {
                throw new NotSupportedException("No customFilter elements found in customFilters");
            }

            if (customFilters.Count == 1)
            {
                return DeserializeCustomFilter(customFilters[0], columnIndex);
            }

            // Multiple filters - check if it's AND or OR logic
            var isAnd = element.Attribute("and")?.Value == "1";

            if (isAnd)
            {
                var criteria = customFilters.Select(f => DeserializeCustomFilter(f, columnIndex)).ToArray();
                return new AndCriterion { Criteria = criteria };
            }
            else
            {
                var criteria = customFilters.Select(f => DeserializeCustomFilter(f, columnIndex)).ToArray();
                return new OrCriterion { Criteria = criteria };
            }
        }
        else if (element.Name.LocalName == "filters")
        {
            var filters = element.Elements(ns + "filter").ToList();
            if (filters.Count == 0)
            {
                throw new NotSupportedException("No filter elements found in filters");
            }

            var values = filters.Select(f => f.Attribute("val")?.Value).ToArray();
            return new InListCriterion { Column = columnIndex, Values = values };
        }

        throw new NotSupportedException($"Filter element type {element.Name.LocalName} is not supported for deserialization.");
    }

    private static FilterCriterion DeserializeCustomFilter(XElement element, int columnIndex)
    {
        var operatorName = element.Attribute("operator")?.Value ?? "";
        var value = element.Attribute("val")?.Value ?? "";

        return operatorName switch
        {
            "equal" => string.IsNullOrEmpty(value)
                ? new IsNullCriterion { Column = columnIndex }
                : new EqualToCriterion { Column = columnIndex, Value = value },
            "notEqual" => new NotEqualToCriterion { Column = columnIndex, Value = value },
            "greaterThan" => new GreaterThanCriterion { Column = columnIndex, Value = value },
            "lessThan" => new LessThanCriterion { Column = columnIndex, Value = value },
            "greaterThanOrEqual" => new GreaterThanOrEqualCriterion { Column = columnIndex, Value = value },
            "lessThanOrEqual" => new LessThanOrEqualCriterion { Column = columnIndex, Value = value },
            "beginsWith" => new StartsWithCriterion { Column = columnIndex, Value = value },
            "notBeginsWith" => new DoesNotStartWithCriterion { Column = columnIndex, Value = value },
            "endsWith" => new EndsWithCriterion { Column = columnIndex, Value = value },
            "notEndsWith" => new DoesNotEndWithCriterion { Column = columnIndex, Value = value },
            "contains" => new ContainsCriterion { Column = columnIndex, Value = value },
            "notContains" => new DoesNotContainCriterion { Column = columnIndex, Value = value },
            _ => throw new NotSupportedException($"Filter operator {operatorName} is not supported for deserialization.")
        };
    }
}
