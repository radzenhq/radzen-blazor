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
/// Writes a <see cref="Workbook"/> to a stream in the Open XML Spreadsheet format (XLSX).
/// </summary>
class XlsxWriter(Workbook sourceWorkbook)
{
    private const double EmuPerPixel = 9525.0;

    private static string PixelsToEmu(double pixels) =>
        ((long)Math.Round(pixels * EmuPerPixel)).ToString(CultureInfo.InvariantCulture);

    private readonly IReadOnlyList<Worksheet> sheets = sourceWorkbook.Sheets;

    public void Write(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        var styleTracker = CreateStylesDocument();

        var sharedStrings = new Dictionary<string, int>();
        var sharedStringsDoc = CreateSharedStringsDocument(sharedStrings);

        var totalTables = 0;
        SaveSheets(archive, styleTracker, sharedStrings, sharedStringsDoc, ref totalTables);
        SaveStyles(archive, styleTracker);
        UpdateAndSaveSharedStrings(archive, sharedStrings, sharedStringsDoc);
        SaveWorkbook(archive);
        SaveTheme(archive);
        SaveDocPropsCore(archive);
        SaveDocPropsApp(archive);

        SaveContentTypes(archive, includeSharedStrings: sharedStrings.Count > 0, tableCount: totalTables);
        SaveRelationships(archive);
    }

    private static void SaveTable(ZipArchive archive, Table table, int tableId)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var refStr = $"{table.Range.Start}:{table.Range.End}";

        var tableElement = new XElement(ns + "table",
            new XAttribute("id", tableId.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("name", table.Name),
            new XAttribute("displayName", table.DisplayName),
            new XAttribute("ref", refStr),
            new XAttribute("totalsRowShown", table.ShowTotalsRow ? "1" : "0"),
            new XAttribute("totalsRowCount", table.ShowTotalsRow ? "1" : "0"),
            new XAttribute("headerRowCount", table.ShowHeaderRow ? "1" : "0"));

        // <autoFilter> is present when the filter button is shown.
        if (table.ShowFilterButton)
        {
            // AutoFilter range covers headers + data (excludes totals row).
            var afEnd = table.ShowTotalsRow
                ? new CellRef(table.Range.End.Row - 1, table.Range.End.Column)
                : table.Range.End;
            tableElement.Add(new XElement(ns + "autoFilter",
                new XAttribute("ref", $"{table.Range.Start}:{afEnd}")));
        }

        // <tableColumns count="N">
        var tableColumnsElement = new XElement(ns + "tableColumns",
            new XAttribute("count", table.Columns.Count.ToString(CultureInfo.InvariantCulture)));

        for (var i = 0; i < table.Columns.Count; i++)
        {
            var col = table.Columns[i];
            var columnElement = new XElement(ns + "tableColumn",
                new XAttribute("id", (i + 1).ToString(CultureInfo.InvariantCulture)),
                new XAttribute("name", col.Name));

            if (table.ShowTotalsRow && col.TotalsCalculation != TotalsCalculation.None)
            {
                columnElement.Add(new XAttribute("totalsRowFunction",
                    TotalsCalculationToXml(col.TotalsCalculation)));
            }

            if (col.CalculatedFormula is not null)
            {
                columnElement.Add(new XElement(ns + "calculatedColumnFormula", col.CalculatedFormula));
            }

            tableColumnsElement.Add(columnElement);
        }
        tableElement.Add(tableColumnsElement);

        // <tableStyleInfo>
        if (table.TableStyle is not null
            || table.ShowBandedRows || table.ShowBandedColumns
            || table.HighlightFirstColumn || table.HighlightLastColumn)
        {
            var tableStyleInfoElement = new XElement(ns + "tableStyleInfo");
            if (table.TableStyle is not null)
                tableStyleInfoElement.Add(new XAttribute("name", table.TableStyle));
            tableStyleInfoElement.Add(new XAttribute("showFirstColumn", table.HighlightFirstColumn ? "1" : "0"));
            tableStyleInfoElement.Add(new XAttribute("showLastColumn", table.HighlightLastColumn ? "1" : "0"));
            tableStyleInfoElement.Add(new XAttribute("showRowStripes", table.ShowBandedRows ? "1" : "0"));
            tableStyleInfoElement.Add(new XAttribute("showColumnStripes", table.ShowBandedColumns ? "1" : "0"));
            tableElement.Add(tableStyleInfoElement);
        }

        var doc = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"), tableElement);
        using var entry = archive.CreateEntry($"xl/tables/table{tableId}.xml").Open();
        doc.Save(entry);
    }

    private static string TotalsCalculationToXml(TotalsCalculation calc) => calc switch
    {
        TotalsCalculation.None => "none",
        TotalsCalculation.Sum => "sum",
        TotalsCalculation.Average => "average",
        TotalsCalculation.Count => "countNums",   // Excel reverses these — see ECMA-376
        TotalsCalculation.CountNumbers => "count",
        TotalsCalculation.Min => "min",
        TotalsCalculation.Max => "max",
        TotalsCalculation.StdDev => "stdDev",
        TotalsCalculation.Var => "var",
        TotalsCalculation.Custom => "custom",
        _ => "none",
    };

    internal static TotalsCalculation TotalsCalculationFromXml(string? value) => value switch
    {
        "sum" => TotalsCalculation.Sum,
        "average" => TotalsCalculation.Average,
        "countNums" => TotalsCalculation.Count,
        "count" => TotalsCalculation.CountNumbers,
        "min" => TotalsCalculation.Min,
        "max" => TotalsCalculation.Max,
        "stdDev" => TotalsCalculation.StdDev,
        "var" => TotalsCalculation.Var,
        "custom" => TotalsCalculation.Custom,
        _ => TotalsCalculation.None,
    };

    private static void SaveTheme(ZipArchive archive)
    {
        var assembly = typeof(XlsxWriter).Assembly;
        using var resource = assembly.GetManifestResourceStream("Radzen.Blazor.Documents.Spreadsheet.theme1.xml")
            ?? throw new InvalidOperationException("Embedded theme1.xml resource not found.");
        using var entry = archive.CreateEntry("xl/theme/theme1.xml").Open();
        resource.CopyTo(entry);
    }

    private static void SaveDocPropsCore(ZipArchive archive)
    {
        XNamespace cp = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
        XNamespace dc = "http://purl.org/dc/elements/1.1/";
        XNamespace dcterms = "http://purl.org/dc/terms/";
        XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";

        var nowIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(cp + "coreProperties",
                new XAttribute(XNamespace.Xmlns + "cp", cp.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "dc", dc.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "dcterms", dcterms.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "xsi", xsi.NamespaceName),
                new XElement(dcterms + "created",
                    new XAttribute(xsi + "type", "dcterms:W3CDTF"), nowIso),
                new XElement(dcterms + "modified",
                    new XAttribute(xsi + "type", "dcterms:W3CDTF"), nowIso)));

        using var entry = archive.CreateEntry("docProps/core.xml").Open();
        doc.Save(entry);
    }

    private void SaveDocPropsApp(ZipArchive archive)
    {
        XNamespace ep = "http://schemas.openxmlformats.org/officeDocument/2006/extended-properties";
        XNamespace vt = "http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes";

        var titlesOfParts = new XElement(ep + "TitlesOfParts",
            new XElement(vt + "vector",
                new XAttribute("size", sheets.Count.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("baseType", "lpstr"),
                sheets.Select(s => new XElement(vt + "lpstr", s.Name))));

        var headingPairs = new XElement(ep + "HeadingPairs",
            new XElement(vt + "vector",
                new XAttribute("size", "2"),
                new XAttribute("baseType", "variant"),
                new XElement(vt + "variant", new XElement(vt + "lpstr", "Worksheets")),
                new XElement(vt + "variant", new XElement(vt + "i4",
                    sheets.Count.ToString(CultureInfo.InvariantCulture)))));

        var doc = new XDocument(
            new XDeclaration("1.0", "UTF-8", "yes"),
            new XElement(ep + "Properties",
                new XAttribute("xmlns", ep.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "vt", vt.NamespaceName),
                new XElement(ep + "Application", "Radzen.Blazor"),
                headingPairs,
                titlesOfParts));

        using var entry = archive.CreateEntry("docProps/app.xml").Open();
        doc.Save(entry);
    }

    private record struct FontKey(string? Color, bool Bold, bool Italic, bool Underline, bool Strikethrough, string? FontFamily, double? FontSize);
    private record struct CellStyleKey(int FontId, int FillId, int BorderId, TextAlign TextAlign, VerticalAlign VerticalAlign, bool WrapText, int NumFmtId, bool? Locked, bool? FormulaHidden, bool QuotePrefix);
    private record struct BorderKey(string? TopStyle, string? TopColor, string? RightStyle, string? RightColor, string? BottomStyle, string? BottomColor, string? LeftStyle, string? LeftColor);

    private class StyleTracker
    {
        public Dictionary<FontKey, int> FontStyles { get; } = new();
        public Dictionary<string, int> FillStyles { get; } = new();
        public Dictionary<CellStyleKey, int> CellStyles { get; } = new();
        public Dictionary<string, int> NumberFormats { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<BorderKey, int> BorderStyles { get; } = new();
        public XDocument StylesDocument { get; set; } = null!;
        public XElement FontsElement { get; set; } = null!;
        public XElement FillsElement { get; set; } = null!;
        public XElement CellXfsElement { get; set; } = null!;
        public XElement BordersElement { get; set; } = null!;
        public XElement? NumFmtsElement { get; set; }
    }

    private static void SaveDrawing(ZipArchive archive, Worksheet sheet, int drawingIndex, Dictionary<string, string> mediaMap, ref int globalMediaIndex)
    {
        XNamespace xdr = "http://schemas.openxmlformats.org/drawingml/2006/spreadsheetDrawing";
        XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
        XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        var drawingDoc = new XDocument(
            new XElement(xdr + "wsDr",
                new XAttribute(XNamespace.Xmlns + "xdr", xdr.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "a", a.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "r", r.NamespaceName)));

        var drawingRels = new List<(string Id, string Target, string Type)>();
        var relIndex = 1;

        foreach (var image in sheet.Images)
        {
            var imageRelId = $"rId{relIndex++}";

            var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(image.Data));
            if (!mediaMap.TryGetValue(hash, out var mediaPath))
            {
                var ext = ContentTypeToExtension(image.ContentType);
                mediaPath = $"xl/media/image{globalMediaIndex++}.{ext}";
                mediaMap[hash] = mediaPath;

                // Write image bytes to archive
                using (var mediaEntry = archive.CreateEntry(mediaPath).Open())
                {
                    mediaEntry.Write(image.Data, 0, image.Data.Length);
                }
            }

            // Relative path from xl/drawings/ to xl/media/
            var relTarget = "../media/" + mediaPath.Split('/').Last();
            drawingRels.Add((imageRelId, relTarget, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image"));

            // Build anchor element
            XElement anchorElement;
            var fromElement = new XElement(xdr + "from",
                new XElement(xdr + "col", image.From.Column.ToString(CultureInfo.InvariantCulture)),
                new XElement(xdr + "colOff", PixelsToEmu(image.From.ColumnOffset)),
                new XElement(xdr + "row", image.From.Row.ToString(CultureInfo.InvariantCulture)),
                new XElement(xdr + "rowOff", PixelsToEmu(image.From.RowOffset)));

            var cNvPr = new XElement(xdr + "cNvPr",
                new XAttribute("id", relIndex.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("name", image.Name ?? $"Image {relIndex - 1}"));

            if (image.Description is not null)
            {
                cNvPr.Add(new XAttribute("descr", image.Description));
            }

            var picElement = new XElement(xdr + "pic",
                new XElement(xdr + "nvPicPr",
                    cNvPr,
                    new XElement(xdr + "cNvPicPr")),
                new XElement(xdr + "blipFill",
                    new XElement(a + "blip", new XAttribute(r + "embed", imageRelId)),
                    new XElement(a + "stretch",
                        new XElement(a + "fillRect"))),
                new XElement(xdr + "spPr",
                    new XElement(a + "prstGeom", new XAttribute("prst", "rect"),
                        new XElement(a + "avLst"))));

            if (image.AnchorMode == DrawingAnchorMode.TwoCellAnchor && image.To is not null)
            {
                var toElement = new XElement(xdr + "to",
                    new XElement(xdr + "col", image.To.Column.ToString(CultureInfo.InvariantCulture)),
                    new XElement(xdr + "colOff", PixelsToEmu(image.To.ColumnOffset)),
                    new XElement(xdr + "row", image.To.Row.ToString(CultureInfo.InvariantCulture)),
                    new XElement(xdr + "rowOff", PixelsToEmu(image.To.RowOffset)));

                anchorElement = new XElement(xdr + "twoCellAnchor",
                    fromElement,
                    toElement,
                    picElement,
                    new XElement(xdr + "clientData"));
            }
            else
            {
                var extElement = new XElement(xdr + "ext",
                    new XAttribute("cx", PixelsToEmu(image.Width)),
                    new XAttribute("cy", PixelsToEmu(image.Height)));

                anchorElement = new XElement(xdr + "oneCellAnchor",
                    fromElement,
                    extElement,
                    picElement,
                    new XElement(xdr + "clientData"));
            }

            drawingDoc.Root!.Add(anchorElement);
        }

        // Write chart anchors
        var chartIndex = 1;
        foreach (var chart in sheet.Charts)
        {
            var chartRelId = $"rId{relIndex++}";
            var chartFileName = $"chart{drawingIndex}_{chartIndex}.xml";
            var chartPath = $"xl/charts/{chartFileName}";

            drawingRels.Add((chartRelId, $"../charts/{chartFileName}", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/chart"));

            // Build anchor element
            var fromElement = new XElement(xdr + "from",
                new XElement(xdr + "col", chart.From.Column.ToString(CultureInfo.InvariantCulture)),
                new XElement(xdr + "colOff", PixelsToEmu(chart.From.ColumnOffset)),
                new XElement(xdr + "row", chart.From.Row.ToString(CultureInfo.InvariantCulture)),
                new XElement(xdr + "rowOff", PixelsToEmu(chart.From.RowOffset)));

            XNamespace c = "http://schemas.openxmlformats.org/drawingml/2006/chart";

            var graphicFrame = new XElement(xdr + "graphicFrame",
                new XElement(xdr + "nvGraphicFramePr",
                    new XElement(xdr + "cNvPr",
                        new XAttribute("id", relIndex.ToString(CultureInfo.InvariantCulture)),
                        new XAttribute("name", chart.Name ?? $"Chart {chartIndex}")),
                    new XElement(xdr + "cNvGraphicFramePr")),
                new XElement(xdr + "xfrm",
                    new XElement(a + "off", new XAttribute("x", "0"), new XAttribute("y", "0")),
                    new XElement(a + "ext", new XAttribute("cx", "0"), new XAttribute("cy", "0"))),
                new XElement(a + "graphic",
                    new XElement(a + "graphicData",
                        new XAttribute("uri", "http://schemas.openxmlformats.org/drawingml/2006/chart"),
                        new XElement(c + "chart",
                            new XAttribute(XNamespace.Xmlns + "c", c.NamespaceName),
                            new XAttribute(r + "id", chartRelId)))));

            XElement chartAnchor;
            if (chart.AnchorMode == DrawingAnchorMode.TwoCellAnchor && chart.To is not null)
            {
                var toElement = new XElement(xdr + "to",
                    new XElement(xdr + "col", chart.To.Column.ToString(CultureInfo.InvariantCulture)),
                    new XElement(xdr + "colOff", PixelsToEmu(chart.To.ColumnOffset)),
                    new XElement(xdr + "row", chart.To.Row.ToString(CultureInfo.InvariantCulture)),
                    new XElement(xdr + "rowOff", PixelsToEmu(chart.To.RowOffset)));

                chartAnchor = new XElement(xdr + "twoCellAnchor",
                    fromElement, toElement, graphicFrame,
                    new XElement(xdr + "clientData"));
            }
            else
            {
                var extElement = new XElement(xdr + "ext",
                    new XAttribute("cx", PixelsToEmu(chart.Width)),
                    new XAttribute("cy", PixelsToEmu(chart.Height)));

                chartAnchor = new XElement(xdr + "oneCellAnchor",
                    fromElement, extElement, graphicFrame,
                    new XElement(xdr + "clientData"));
            }

            drawingDoc.Root!.Add(chartAnchor);

            // Save chart XML
            SaveChartXml(archive, chart, chartPath);

            chartIndex++;
        }

        // Save drawing XML
        using (var entry = archive.CreateEntry($"xl/drawings/drawing{drawingIndex}.xml").Open())
        {
            drawingDoc.Save(entry);
        }

        // Save drawing relationships
        if (drawingRels.Count > 0)
        {
            var pkgNs = "http://schemas.openxmlformats.org/package/2006/relationships";
            var drawingRelsDoc = new XDocument(
                new XElement(XName.Get("Relationships", pkgNs)));

            foreach (var (id, target, type) in drawingRels)
            {
                drawingRelsDoc.Root!.Add(new XElement(XName.Get("Relationship", pkgNs),
                    new XAttribute("Id", id),
                    new XAttribute("Type", type),
                    new XAttribute("Target", target)));
            }

            using (var relsEntry = archive.CreateEntry($"xl/drawings/_rels/drawing{drawingIndex}.xml.rels").Open())
            {
                drawingRelsDoc.Save(relsEntry);
            }
        }
    }

    private static void SaveChartXml(ZipArchive archive, SheetChart chart, string chartPath)
    {
        XDocument chartDoc;

        // If we have the original raw XML, write it back for lossless round-trip
        if (!string.IsNullOrEmpty(chart.RawChartXml))
        {
            chartDoc = XDocument.Parse(chart.RawChartXml);
        }
        else
        {
            chartDoc = GenerateChartXml(chart);
        }

        using var entry = archive.CreateEntry(chartPath).Open();
        chartDoc.Save(entry);
    }

    private static XDocument GenerateChartXml(SheetChart chart)
    {
        XNamespace c = "http://schemas.openxmlformats.org/drawingml/2006/chart";
        XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
        XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        var plotArea = new XElement(c + "plotArea",
            new XElement(c + "layout"));

        // Create chart type group element
        var groupElement = CreateChartGroupElement(chart, c);
        if (groupElement is not null)
        {
            plotArea.Add(groupElement);
        }

        // Add axes for non-pie/donut charts
        if (chart.ChartType != SpreadsheetChartType.Pie && chart.ChartType != SpreadsheetChartType.Donut)
        {
            plotArea.Add(new XElement(c + "catAx",
                new XElement(c + "axId", new XAttribute("val", "1")),
                new XElement(c + "scaling", new XElement(c + "orientation", new XAttribute("val", "minMax"))),
                new XElement(c + "delete", new XAttribute("val", "0")),
                new XElement(c + "axPos", new XAttribute("val", "b")),
                new XElement(c + "crossAx", new XAttribute("val", "2"))));

            plotArea.Add(new XElement(c + "valAx",
                new XElement(c + "axId", new XAttribute("val", "2")),
                new XElement(c + "scaling", new XElement(c + "orientation", new XAttribute("val", "minMax"))),
                new XElement(c + "delete", new XAttribute("val", "0")),
                new XElement(c + "axPos", new XAttribute("val", "l")),
                new XElement(c + "crossAx", new XAttribute("val", "1"))));
        }

        var chartElement = new XElement(c + "chart");

        if (!string.IsNullOrEmpty(chart.Title))
        {
            chartElement.Add(new XElement(c + "title",
                new XElement(c + "tx",
                    new XElement(c + "rich",
                        new XElement(a + "bodyPr"),
                        new XElement(a + "lstStyle"),
                        new XElement(a + "p",
                            new XElement(a + "r",
                                new XElement(a + "t", chart.Title)))))));
        }

        chartElement.Add(new XElement(c + "autoTitleDeleted", new XAttribute("val", string.IsNullOrEmpty(chart.Title) ? "1" : "0")));
        chartElement.Add(plotArea);

        if (chart.ShowLegend)
        {
            var legendPos = chart.LegendPosition switch
            {
                ChartLegendPosition.Top => "t",
                ChartLegendPosition.Bottom => "b",
                ChartLegendPosition.Left => "l",
                ChartLegendPosition.Right => "r",
                _ => "r"
            };

            chartElement.Add(new XElement(c + "legend",
                new XElement(c + "legendPos", new XAttribute("val", legendPos))));
        }

        chartElement.Add(new XElement(c + "plotVisOnly", new XAttribute("val", "1")));

        return new XDocument(
            new XElement(c + "chartSpace",
                new XAttribute(XNamespace.Xmlns + "c", c.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "a", a.NamespaceName),
                new XAttribute(XNamespace.Xmlns + "r", r.NamespaceName),
                chartElement));
    }

    private static XElement? CreateChartGroupElement(SheetChart chart, XNamespace c)
    {
        var (elementName, barDir, grouping) = chart.ChartType switch
        {
            SpreadsheetChartType.Column => ("barChart", "col", "clustered"),
            SpreadsheetChartType.Bar => ("barChart", "bar", "clustered"),
            SpreadsheetChartType.StackedColumn => ("barChart", "col", "stacked"),
            SpreadsheetChartType.StackedBar => ("barChart", "bar", "stacked"),
            SpreadsheetChartType.FullStackedColumn => ("barChart", "col", "percentStacked"),
            SpreadsheetChartType.FullStackedBar => ("barChart", "bar", "percentStacked"),
            SpreadsheetChartType.Line => ("lineChart", (string?)null, "standard"),
            SpreadsheetChartType.Area => ("areaChart", null, "standard"),
            SpreadsheetChartType.StackedArea => ("areaChart", null, "stacked"),
            SpreadsheetChartType.FullStackedArea => ("areaChart", null, "percentStacked"),
            SpreadsheetChartType.Pie => ("pieChart", null, null),
            SpreadsheetChartType.Donut => ("doughnutChart", null, null),
            SpreadsheetChartType.Scatter => ("scatterChart", null, null),
            _ => (null, null, null)
        };

        if (elementName is null)
        {
            return null;
        }

        var group = new XElement(c + elementName);

        if (barDir is not null)
        {
            group.Add(new XElement(c + "barDir", new XAttribute("val", barDir)));
        }

        if (grouping is not null)
        {
            group.Add(new XElement(c + "grouping", new XAttribute("val", grouping)));
        }

        // Add series
        foreach (var series in chart.Series)
        {
            var ser = new XElement(c + "ser",
                new XElement(c + "idx", new XAttribute("val", series.Index.ToString(CultureInfo.InvariantCulture))),
                new XElement(c + "order", new XAttribute("val", series.Index.ToString(CultureInfo.InvariantCulture))));

            if (!string.IsNullOrEmpty(series.Title))
            {
                ser.Add(new XElement(c + "tx",
                    new XElement(c + "strRef",
                        new XElement(c + "strCache",
                            new XElement(c + "ptCount", new XAttribute("val", "1")),
                            new XElement(c + "pt", new XAttribute("idx", "0"),
                                new XElement(c + "v", series.Title))))));
            }

            if (!string.IsNullOrEmpty(series.CategoryFormula))
            {
                var catRef = new XElement(c + "strRef",
                    new XElement(c + "f", series.CategoryFormula));

                if (series.CategoryCache.Count > 0)
                {
                    var cache = new XElement(c + "strCache",
                        new XElement(c + "ptCount", new XAttribute("val", series.CategoryCache.Count.ToString(CultureInfo.InvariantCulture))));

                    for (var i = 0; i < series.CategoryCache.Count; i++)
                    {
                        cache.Add(new XElement(c + "pt", new XAttribute("idx", i.ToString(CultureInfo.InvariantCulture)),
                            new XElement(c + "v", series.CategoryCache[i])));
                    }

                    catRef.Add(cache);
                }

                ser.Add(new XElement(c + "cat", catRef));
            }

            if (!string.IsNullOrEmpty(series.ValueFormula))
            {
                var numRef = new XElement(c + "numRef",
                    new XElement(c + "f", series.ValueFormula));

                if (series.ValueCache.Count > 0)
                {
                    var cache = new XElement(c + "numCache",
                        new XElement(c + "formatCode", "General"),
                        new XElement(c + "ptCount", new XAttribute("val", series.ValueCache.Count.ToString(CultureInfo.InvariantCulture))));

                    for (var i = 0; i < series.ValueCache.Count; i++)
                    {
                        var v = series.ValueCache[i];
                        if (v.HasValue)
                        {
                            cache.Add(new XElement(c + "pt", new XAttribute("idx", i.ToString(CultureInfo.InvariantCulture)),
                                new XElement(c + "v", v.Value.ToString(CultureInfo.InvariantCulture))));
                        }
                    }

                    numRef.Add(cache);
                }

                ser.Add(new XElement(c + "val", numRef));
            }

            group.Add(ser);
        }

        // Add axis IDs for non-pie/donut
        if (chart.ChartType != SpreadsheetChartType.Pie && chart.ChartType != SpreadsheetChartType.Donut)
        {
            group.Add(new XElement(c + "axId", new XAttribute("val", "1")));
            group.Add(new XElement(c + "axId", new XAttribute("val", "2")));
        }

        return group;
    }

    private void SaveContentTypes(ZipArchive archive, bool includeSharedStrings, int tableCount = 0)
    {
        var ctNs = "http://schemas.openxmlformats.org/package/2006/content-types";
        var contentTypes = new XDocument(
            new XElement(XName.Get("Types", ctNs),
                new XElement(XName.Get("Default", ctNs),
                    new XAttribute("Extension", "rels"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(XName.Get("Default", ctNs),
                    new XAttribute("Extension", "xml"),
                    new XAttribute("ContentType", "application/xml")),
                new XElement(XName.Get("Override", ctNs),
                    new XAttribute("PartName", "/xl/workbook.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(XName.Get("Override", ctNs),
                    new XAttribute("PartName", "/xl/theme/theme1.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.theme+xml")),
                new XElement(XName.Get("Override", ctNs),
                    new XAttribute("PartName", "/xl/styles.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml")),
                new XElement(XName.Get("Override", ctNs),
                    new XAttribute("PartName", "/docProps/core.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-package.core-properties+xml")),
                new XElement(XName.Get("Override", ctNs),
                    new XAttribute("PartName", "/docProps/app.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.extended-properties+xml"))));

        if (includeSharedStrings)
        {
            contentTypes.Root!.Add(new XElement(XName.Get("Override", ctNs),
                new XAttribute("PartName", "/xl/sharedStrings.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml")));
        }

        for (var i = 0; i < sheets.Count; i++)
        {
            contentTypes.Root!.Add(new XElement(XName.Get("Override", ctNs),
                new XAttribute("PartName", $"/xl/worksheets/sheet{i + 1}.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml")));
        }

        for (var i = 1; i <= tableCount; i++)
        {
            contentTypes.Root!.Add(new XElement(XName.Get("Override", ctNs),
                new XAttribute("PartName", $"/xl/tables/table{i}.xml"),
                new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.table+xml")));
        }

        // Add image extension defaults for any sheets with images
        var imageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sheet in sheets)
        {
            foreach (var image in sheet.Images)
            {
                var ext = ContentTypeToExtension(image.ContentType);
                imageExtensions.Add(ext);
            }
        }

        foreach (var ext in imageExtensions)
        {
            contentTypes.Root!.Add(new XElement(XName.Get("Default", ctNs),
                new XAttribute("Extension", ext),
                new XAttribute("ContentType", ExtensionToContentType(ext))));
        }

        // Add drawing and chart overrides
        for (var i = 0; i < sheets.Count; i++)
        {
            if (sheets[i].Images.Count > 0 || sheets[i].Charts.Count > 0)
            {
                contentTypes.Root!.Add(new XElement(XName.Get("Override", ctNs),
                    new XAttribute("PartName", $"/xl/drawings/drawing{i + 1}.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.drawing+xml")));

                for (var j = 0; j < sheets[i].Charts.Count; j++)
                {
                    contentTypes.Root!.Add(new XElement(XName.Get("Override", ctNs),
                        new XAttribute("PartName", $"/xl/charts/chart{i + 1}_{j + 1}.xml"),
                        new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.drawingml.chart+xml")));
                }
            }
        }

        using var entry = archive.CreateEntry("[Content_Types].xml").Open();
        contentTypes.Save(entry);
    }

    private static string ContentTypeToExtension(string contentType)
    {
        return contentType switch
        {
            "image/png" => "png",
            "image/jpeg" => "jpeg",
            "image/gif" => "gif",
            "image/bmp" => "bmp",
            "image/svg+xml" => "svg",
            "image/webp" => "webp",
            "image/tiff" => "tiff",
            _ => "png"
        };
    }

    private static string ExtensionToContentType(string extension)
    {
        return extension switch
        {
            "png" => "image/png",
            "jpeg" or "jpg" => "image/jpeg",
            "gif" => "image/gif",
            "bmp" => "image/bmp",
            "svg" => "image/svg+xml",
            "webp" => "image/webp",
            "tiff" or "tif" => "image/tiff",
            _ => "image/png"
        };
    }

    private static void SaveRelationships(ZipArchive archive)
    {
        var ns = "http://schemas.openxmlformats.org/package/2006/relationships";
        var rels = new XDocument(
            new XElement(XName.Get("Relationships", ns),
                new XElement(XName.Get("Relationship", ns),
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                    new XAttribute("Target", "xl/workbook.xml")),
                new XElement(XName.Get("Relationship", ns),
                    new XAttribute("Id", "rId2"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties"),
                    new XAttribute("Target", "docProps/core.xml")),
                new XElement(XName.Get("Relationship", ns),
                    new XAttribute("Id", "rId3"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties"),
                    new XAttribute("Target", "docProps/app.xml"))));

        using var entry = archive.CreateEntry("_rels/.rels").Open();
        rels.Save(entry);
    }

    private static XDocument CreateSharedStringsDocument(Dictionary<string, int> sharedStrings)
    {
        return new XDocument(
            new XElement(XName.Get("sst", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("count", "0"),
                new XAttribute("uniqueCount", "0")));
    }

    private static void SaveStyles(ZipArchive archive, StyleTracker styleTracker)
    {
        using var entry = archive.CreateEntry("xl/styles.xml").Open();
        styleTracker.StylesDocument.Save(entry);
    }

    private static StyleTracker CreateStylesDocument()
    {
        var styleTracker = new StyleTracker();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var stylesDoc = new XDocument(
            new XElement(ns + "styleSheet",
                CreateFontsElement(),
                CreateFillsElement(),
                CreateBordersElement(),
                CreateCellStyleXfsElement(),
                CreateCellXfsElement(),
                CreateCellStylesElement(),
                new XElement(ns + "dxfs", new XAttribute("count", "0")),
                new XElement(ns + "tableStyles",
                    new XAttribute("count", "0"),
                    new XAttribute("defaultTableStyle", "TableStyleMedium2"),
                    new XAttribute("defaultPivotStyle", "PivotStyleLight16"))));

        styleTracker.StylesDocument = stylesDoc;
        styleTracker.FontsElement = stylesDoc.Root!.Element(ns + "fonts")!;
        styleTracker.FillsElement = stylesDoc.Root!.Element(ns + "fills")!;
        styleTracker.CellXfsElement = stylesDoc.Root!.Element(ns + "cellXfs")!;
        styleTracker.BordersElement = stylesDoc.Root!.Element(ns + "borders")!;

        return styleTracker;
    }

    private static XElement CreateFontsElement()
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        return new XElement(ns + "fonts",
            new XAttribute("count", "1"),
            new XElement(ns + "font",
                new XElement(ns + "sz", new XAttribute("val", "11")),
                new XElement(ns + "color", new XAttribute("theme", "1")),
                new XElement(ns + "name", new XAttribute("val", "Aptos Narrow")),
                new XElement(ns + "family", new XAttribute("val", "2")),
                new XElement(ns + "scheme", new XAttribute("val", "minor"))));
    }

    private static XElement CreateFillsElement()
    {
        return new XElement(XName.Get("fills", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("count", "2"),
            new XElement(XName.Get("fill", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XElement(XName.Get("patternFill", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("patternType", "none"))),
            new XElement(XName.Get("fill", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XElement(XName.Get("patternFill", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("patternType", "gray125"))));
    }

    private static XElement CreateBordersElement()
    {
        return new XElement(XName.Get("borders", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("count", "1"),
            new XElement(XName.Get("border", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XElement(XName.Get("left", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")),
                new XElement(XName.Get("right", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")),
                new XElement(XName.Get("top", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")),
                new XElement(XName.Get("bottom", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")),
                new XElement(XName.Get("diagonal", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))));
    }

    private static XElement CreateCellStyleXfsElement()
    {
        return new XElement(XName.Get("cellStyleXfs", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("count", "1"),
            new XElement(XName.Get("xf", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("numFmtId", "0"),
                new XAttribute("fontId", "0"),
                new XAttribute("fillId", "0"),
                new XAttribute("borderId", "0")));
    }

    private static XElement CreateCellXfsElement()
    {
        return new XElement(XName.Get("cellXfs", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("count", "1"),
            new XElement(XName.Get("xf", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("numFmtId", "0"),
                new XAttribute("fontId", "0"),
                new XAttribute("fillId", "0"),
                new XAttribute("borderId", "0"),
                new XAttribute("xfId", "0")));
    }

    private static XElement CreateCellStylesElement()
    {
        return new XElement(XName.Get("cellStyles", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("count", "1"),
            new XElement(XName.Get("cellStyle", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("name", "Normal"),
                new XAttribute("xfId", "0"),
                new XAttribute("builtinId", "0")));
    }

    private void SaveSheets(ZipArchive archive, StyleTracker styleTracker, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc, ref int globalTableIndex)
    {
        var workbookRels = CreateWorkbookRelationships();
        var workbookRelsElement = workbookRels.Root!;
        var pkgNs = "http://schemas.openxmlformats.org/package/2006/relationships";

        // Styles is always present.
        workbookRelsElement.Add(new XElement(XName.Get("Relationship", pkgNs),
            new XAttribute("Id", "rId2"),
            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),
            new XAttribute("Target", "styles.xml")));

        // Media deduplication map (hash -> media path in archive)
        var mediaMap = new Dictionary<string, string>();
        var globalMediaIndex = 1;

        // Process each sheet
        for (var i = 0; i < sheets.Count; i++)
        {
            var sheet = sheets[i];
            var sheetId = i + 1;
            var sheetName = $"sheet{sheetId}.xml";
            var relId = $"rId{sheetId + 2}";

            // Add sheet relationship (update Target to worksheets subdirectory)
            workbookRelsElement.Add(new XElement(XName.Get("Relationship", pkgNs),
                new XAttribute("Id", relId),
                new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                new XAttribute("Target", $"worksheets/{sheetName}")));

            // Save individual sheet
            SaveSheet(archive, sheet, sheetName, sheetId, relId, styleTracker, sharedStrings, sharedStringsDoc, mediaMap, ref globalMediaIndex, ref globalTableIndex);
        }

        // Theme relationship (shared by every workbook)
        workbookRelsElement.Add(new XElement(XName.Get("Relationship", pkgNs),
            new XAttribute("Id", $"rId{sheets.Count + 3}"),
            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/theme"),
            new XAttribute("Target", "theme/theme1.xml")));

        // sharedStrings only when something was written into it.
        if (sharedStrings.Count > 0)
        {
            workbookRelsElement.Add(new XElement(XName.Get("Relationship", pkgNs),
                new XAttribute("Id", "rId1"),
                new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"),
                new XAttribute("Target", "sharedStrings.xml")));
        }

        // Save workbook relationships
        using var relsEntry = archive.CreateEntry("xl/_rels/workbook.xml.rels").Open();
        workbookRels.Save(relsEntry);
    }

    private static XDocument CreateWorkbookRelationships()
    {
        return new XDocument(
            new XElement(XName.Get("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships")));
    }

    private void SaveSheet(ZipArchive archive, Worksheet sheet, string sheetName, int sheetId, string relId, StyleTracker styleTracker, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc, Dictionary<string, string> mediaMap, ref int globalMediaIndex, ref int globalTableIndex)
    {
        var sheetDoc = CreateSheetDocument(sheet, sheetId, relId);
        var sheetData = sheetDoc.Root!.Element(XName.Get("sheetData", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))!;

        // Process all rows and cells
        ProcessSheetData(sheet, sheetData, styleTracker, sharedStrings, sharedStringsDoc);

        // Add merged cells
        AddMergedCells(sheet, sheetDoc);

        // Add auto filter
        AddAutoFilter(sheet, sheetDoc);

        // Add data validations
        AddDataValidations(sheet, sheetDoc);

        // Add sheet protection
        AddSheetProtection(sheet, sheetDoc);

        // Add hyperlinks
        var hyperlinkRels = AddHyperlinks(sheet, sheetDoc);

        // Track all sheet relationship entries
        var sheetRelEntries = new List<(string Id, string Type, string Target, bool External)>();
        foreach (var (id, url) in hyperlinkRels)
        {
            sheetRelEntries.Add((id, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink", url, true));
        }

        // Add drawing reference if sheet has images or charts
        if (sheet.Images.Count > 0 || sheet.Charts.Count > 0)
        {
            var drawingRelId = $"rId{hyperlinkRels.Count + 1}";
            var drawingIndex = sheetId;
            var ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
            sheetDoc.Root!.Add(new XElement(XName.Get("drawing", ns),
                new XAttribute(rNs + "id", drawingRelId)));

            sheetRelEntries.Add((drawingRelId, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/drawing", $"../drawings/drawing{drawingIndex}.xml", false));

            SaveDrawing(archive, sheet, drawingIndex, mediaMap, ref globalMediaIndex);
        }

        // pageMargins is appended last; ECMA-376 puts it before drawing,
        // but Excel and downstream readers tolerate trailing position too.
        sheetDoc.Root!.Add(CreatePageMargins());

        // Tables: write each table's XML part, register a relationship per table,
        // and emit a <tableParts> block on the sheet.
        if (sheet.Tables.Count > 0)
        {
            var ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            XNamespace rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

            var tablePartsElement = new XElement(XName.Get("tableParts", ns),
                new XAttribute("count", sheet.Tables.Count.ToString(CultureInfo.InvariantCulture)));

            var nextRelIndex = sheetRelEntries.Count + 1;
            foreach (var table in sheet.Tables)
            {
                globalTableIndex++;
                var tableId = globalTableIndex;
                var tableRelId = $"rId{nextRelIndex++}";

                tablePartsElement.Add(new XElement(XName.Get("tablePart", ns),
                    new XAttribute(rNs + "id", tableRelId)));

                sheetRelEntries.Add((tableRelId,
                    "http://schemas.openxmlformats.org/officeDocument/2006/relationships/table",
                    $"../tables/table{tableId}.xml",
                    false));

                SaveTable(archive, table, tableId);
            }

            sheetDoc.Root!.Add(tablePartsElement);
        }

        // Save sheet in xl/worksheets/ subdirectory
        using (var entry = archive.CreateEntry($"xl/worksheets/{sheetName}").Open())
        {
            sheetDoc.Save(entry);
        }

        // Save sheet relationships
        if (sheetRelEntries.Count > 0)
        {
            SaveSheetRelationships(archive, sheetName, sheetRelEntries);
        }
    }

    private static List<(string Id, string Url)> AddHyperlinks(Worksheet sheet, XDocument sheetDoc)
    {
        var rels = new List<(string Id, string Url)>();
        var ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        var relIndex = 1;

        XElement? hyperlinksElement = null;

        foreach (var cell in sheet.Cells.GetPopulatedCells())
        {
            if (cell.Hyperlink is not null)
            {
                hyperlinksElement ??= new XElement(XName.Get("hyperlinks", ns));

                var relId = $"rId{relIndex++}";
                var cellRef = cell.Address.ToString();

                var hyperlinkElement = new XElement(XName.Get("hyperlink", ns),
                    new XAttribute("ref", cellRef),
                    new XAttribute(rNs + "id", relId));

                if (cell.Hyperlink.Text is not null)
                {
                    hyperlinkElement.Add(new XAttribute("display", cell.Hyperlink.Text));
                }

                hyperlinksElement.Add(hyperlinkElement);
                rels.Add((relId, cell.Hyperlink.Url));
            }
        }

        if (hyperlinksElement is not null)
        {
            sheetDoc.Root!.Add(hyperlinksElement);
        }

        return rels;
    }

    private static void SaveSheetRelationships(ZipArchive archive, string sheetName, List<(string Id, string Type, string Target, bool External)> rels)
    {
        var pkgNs = "http://schemas.openxmlformats.org/package/2006/relationships";
        var relsDoc = new XDocument(
            new XElement(XName.Get("Relationships", pkgNs)));

        foreach (var (id, type, target, external) in rels)
        {
            var relElement = new XElement(XName.Get("Relationship", pkgNs),
                new XAttribute("Id", id),
                new XAttribute("Type", type),
                new XAttribute("Target", target));

            if (external)
            {
                relElement.Add(new XAttribute("TargetMode", "External"));
            }

            relsDoc.Root!.Add(relElement);
        }

        using (var entry = archive.CreateEntry($"xl/worksheets/_rels/{sheetName}.rels").Open())
        {
            relsDoc.Save(entry);
        }
    }

    private static XDocument CreateSheetDocument(Worksheet sheet, int sheetId, string relId)
    {
        var uid = Guid.NewGuid().ToString("B").ToUpperInvariant();
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var worksheet = new XElement(ns + "worksheet",
            new XAttribute(XName.Get("uid", "http://schemas.microsoft.com/office/spreadsheetml/2014/revision"), uid));

        var sheetPr = CreateSheetProperties(sheet);
        if (sheetPr is not null) worksheet.Add(sheetPr);
        worksheet.Add(CreateDimension(sheet));
        worksheet.Add(CreateSheetViews(sheet));
        worksheet.Add(CreateSheetFormatPr());
        var cols = CreateColumns(sheet);
        if (cols is not null) worksheet.Add(cols);
        worksheet.Add(new XElement(ns + "sheetData"));

        return new XDocument(worksheet);
    }

    private static XElement? CreateSheetProperties(Worksheet sheet)
    {
        if (sheet.Filters.Count == 0)
        {
            return null;
        }
        return new XElement(XName.Get("sheetPr", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("filterMode", "1"));
    }

    private static XElement CreateSheetFormatPr()
    {
        return new XElement(XName.Get("sheetFormatPr", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("defaultRowHeight", "14.25"));
    }

    private static XElement CreatePageMargins()
    {
        return new XElement(XName.Get("pageMargins", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("left", "0.7"),
            new XAttribute("right", "0.7"),
            new XAttribute("top", "0.75"),
            new XAttribute("bottom", "0.75"),
            new XAttribute("header", "0.3"),
            new XAttribute("footer", "0.3"));
    }

    private static XElement CreateDimension(Worksheet sheet)
    {
        var populated = sheet.Cells.GetPopulatedCells()
            .Where(c => c.GetValue() is not null)
            .ToList();

        string refStr;
        if (populated.Count == 0)
        {
            refStr = "A1";
        }
        else
        {
            var minRow = populated.Min(c => c.Address.Row);
            var maxRow = populated.Max(c => c.Address.Row);
            var minCol = populated.Min(c => c.Address.Column);
            var maxCol = populated.Max(c => c.Address.Column);
            refStr = minRow == maxRow && minCol == maxCol
                ? new CellRef(minRow, minCol).ToString()
                : $"{new CellRef(minRow, minCol)}:{new CellRef(maxRow, maxCol)}";
        }

        return new XElement(XName.Get("dimension", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("ref", refStr));
    }

    private static XElement CreateSheetViews(Worksheet sheet)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var sheetView = new XElement(ns + "sheetView",
            new XAttribute("workbookViewId", "0"));

        if (sheet.Columns.Frozen != 0 || sheet.Rows.Frozen != 0)
        {
            sheetView.Add(new XElement(ns + "pane",
                new XAttribute("xSplit", sheet.Columns.Frozen),
                new XAttribute("ySplit", sheet.Rows.Frozen),
                new XAttribute("topLeftCell", new CellRef(sheet.Rows.Frozen, sheet.Columns.Frozen).ToString()),
                new XAttribute("activePane", "topLeft"),
                new XAttribute("state", "frozen")));
        }

        return new XElement(ns + "sheetViews", sheetView);
    }

    private static XElement? CreateColumns(Worksheet sheet)
    {
        var colElements = Enumerable.Range(0, sheet.ColumnCount)
            .Where(col => sheet.Columns[col] != sheet.Columns.Size)
            .Select(col => new XElement(XName.Get("col", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("min", col + 1),
                new XAttribute("max", col + 1),
                new XAttribute("width", Math.Round(sheet.Columns[col] / 7.0, 8)),
                new XAttribute("customWidth", "1")))
            .ToList();

        if (colElements.Count == 0)
        {
            return null;
        }

        return new XElement(XName.Get("cols", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), colElements);
    }

    private void ProcessSheetData(Worksheet sheet, XElement sheetData, StyleTracker styleTracker, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc)
    {
        // row -> (col -> <c> element). SortedDictionary keeps cells in column
        // order within each row, as required by ECMA-376.
        var rowMap = new SortedDictionary<int, SortedDictionary<int, XElement>>();

        SortedDictionary<int, XElement> EnsureRow(int row) =>
            rowMap.TryGetValue(row, out var existing)
                ? existing
                : (rowMap[row] = new SortedDictionary<int, XElement>());

        // 1. Real data cells.
        foreach (var cell in sheet.Cells.GetPopulatedCells().Where(c => c.GetValue() is not null))
        {
            var rowDict = EnsureRow(cell.Address.Row);
            rowDict[cell.Address.Column] = CreateCellElement(sheet, cell.Address.Row, cell.Address.Column, cell, styleTracker, sharedStrings, sharedStringsDoc);
        }

        // 2. Empty <c> placeholders for cells inside merge ranges. Excel
        // always writes these (carrying the anchor's style) so border/fill
        // styling visually applies across the merged range.
        var ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        foreach (var range in sheet.MergedCells.Ranges)
        {
            var anchor = sheet.Cells[range.Start];
            int? anchorStyleId = HasCellFormatting(anchor)
                ? GetOrCreateCellStyle(anchor, styleTracker)
                : null;

            for (var r = range.Start.Row; r <= range.End.Row; r++)
            {
                for (var c = range.Start.Column; c <= range.End.Column; c++)
                {
                    if (r == range.Start.Row && c == range.Start.Column) continue;

                    var rowDict = EnsureRow(r);
                    if (rowDict.ContainsKey(c)) continue;

                    var placeholder = new XElement(XName.Get("c", ns),
                        new XAttribute("r", new CellRef(r, c).ToString()));
                    if (anchorStyleId is not null)
                    {
                        placeholder.Add(new XAttribute("s", anchorStyleId.Value));
                    }
                    rowDict[c] = placeholder;
                }
            }
        }

        // 3. Rows with custom height or hidden state but no cells.
        foreach (var rowIndex in sheet.Rows.GetCustomSizedIndices()) EnsureRow(rowIndex);
        foreach (var rowIndex in sheet.Rows.GetHiddenIndices()) EnsureRow(rowIndex);

        // 4. Emit.
        foreach (var (row, rowDict) in rowMap)
        {
            var rowElement = CreateRowElement(sheet, row);

            if (rowDict.Count > 0)
            {
                var firstCol = rowDict.Keys.First();
                var lastCol  = rowDict.Keys.Last();
                rowElement.Add(new XAttribute("spans", $"{firstCol + 1}:{lastCol + 1}"));

                foreach (var (_, cellEl) in rowDict)
                {
                    rowElement.Add(cellEl);
                }
            }

            sheetData.Add(rowElement);
        }
    }

    private static XElement CreateRowElement(Worksheet sheet, int row)
    {
        var rowElement = new XElement(XName.Get("row", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("r", row + 1));

        // Only persist height if it differs from the default
        if (sheet.Rows[row] != sheet.Rows.Size)
        {
            rowElement.Add(new XAttribute("ht", sheet.Rows[row]));
            rowElement.Add(new XAttribute("customHeight", "1"));
        }

        // Add hidden attribute if row is hidden
        if (sheet.Rows.IsHidden(row))
        {
            rowElement.Add(new XAttribute("hidden", "1"));
        }

        return rowElement;
    }

    private XElement CreateCellElement(Worksheet sheet, int row, int col, Cell cell, StyleTracker styleTracker, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc)
    {
        var cellElement = new XElement(XName.Get("c", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("r", new CellRef(row, col).ToString()));

        // Add style reference if cell has formatting
        if (HasCellFormatting(cell))
        {
            var styleId = GetOrCreateCellStyle(cell, styleTracker);
            cellElement.Add(new XAttribute("s", styleId));
        }

        // Add cell value based on type
        AddCellValue(cellElement, cell, sharedStrings, sharedStringsDoc);

        return cellElement;
    }

    private static bool HasCellFormatting(Cell cell)
    {
        return !cell.Format.IsDefault || cell.ValueType == CellDataType.Date || cell.QuotePrefix;
    }

    private int GetOrCreateCellStyle(Cell cell, StyleTracker styleTracker)
    {
        var fontId = GetOrCreateFontStyle(cell, styleTracker);
        var fillId = GetOrCreateFillStyle(cell, styleTracker);
        var numFmtId = GetOrCreateNumberFormat(cell, styleTracker);
        var borderId = GetOrCreateBorderStyle(cell, styleTracker);

        var styleKey = new CellStyleKey(fontId, fillId, borderId, cell.Format.TextAlign, cell.Format.VerticalAlign, cell.Format.WrapText, numFmtId, cell.Format.Locked, cell.Format.FormulaHidden, cell.QuotePrefix);

        if (!styleTracker.CellStyles.TryGetValue(styleKey, out int styleId))
        {
            styleId = styleTracker.CellStyles.Count + 1;
            styleTracker.CellStyles[styleKey] = styleId;
            CreateCellStyleElement(cell, fontId, fillId, borderId, numFmtId, styleTracker);
        }

        return styleId;
    }

    private int GetOrCreateFontStyle(Cell cell, StyleTracker styleTracker)
    {
        var fontKey = new FontKey(cell.Format.Color, cell.Format.Bold, cell.Format.Italic, cell.Format.Underline, cell.Format.Strikethrough, cell.Format.FontFamily, cell.Format.FontSize);

        if (!styleTracker.FontStyles.TryGetValue(fontKey, out int fontId))
        {
            fontId = styleTracker.FontStyles.Count + 1;
            styleTracker.FontStyles[fontKey] = fontId;
            CreateFontElement(cell, fontId, styleTracker);
        }

        return fontId;
    }

    private int GetOrCreateFillStyle(Cell cell, StyleTracker styleTracker)
    {
        if (cell.Format.BackgroundColor is null)
        {
            return 0;
        }

        if (!styleTracker.FillStyles.TryGetValue(cell.Format.BackgroundColor, out int fillId))
        {
            fillId = styleTracker.FillStyles.Count + 2; // Start from 2 as 0 and 1 are reserved
            styleTracker.FillStyles[cell.Format.BackgroundColor] = fillId;
            CreateFillElement(cell, fillId, styleTracker);
        }

        return fillId;
    }

    private int GetOrCreateBorderStyle(Cell cell, StyleTracker styleTracker)
    {
        var bt = cell.Format.BorderTop;
        var br = cell.Format.BorderRight;
        var bb = cell.Format.BorderBottom;
        var bl = cell.Format.BorderLeft;

        if (bt is null && br is null && bb is null && bl is null)
        {
            return 0;
        }

        var borderKey = new BorderKey(
            bt?.ToXlsxStyle(), bt?.Color,
            br?.ToXlsxStyle(), br?.Color,
            bb?.ToXlsxStyle(), bb?.Color,
            bl?.ToXlsxStyle(), bl?.Color
        );

        if (!styleTracker.BorderStyles.TryGetValue(borderKey, out int borderId))
        {
            borderId = styleTracker.BorderStyles.Count + 1;
            styleTracker.BorderStyles[borderKey] = borderId;
            CreateBorderElement(cell, styleTracker);
        }

        return borderId;
    }

    private static void CreateBorderElement(Cell cell, StyleTracker styleTracker)
    {
        var ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        var borderElement = new XElement(XName.Get("border", ns));

        AddBorderSide(borderElement, "left", cell.Format.BorderLeft, ns);
        AddBorderSide(borderElement, "right", cell.Format.BorderRight, ns);
        AddBorderSide(borderElement, "top", cell.Format.BorderTop, ns);
        AddBorderSide(borderElement, "bottom", cell.Format.BorderBottom, ns);
        borderElement.Add(new XElement(XName.Get("diagonal", ns)));

        styleTracker.BordersElement.Add(borderElement);
        styleTracker.BordersElement.Attribute("count")!.Value = (styleTracker.BorderStyles.Count + 1).ToString(CultureInfo.InvariantCulture);
    }

    private static void AddBorderSide(XElement borderElement, string side, BorderStyle? style, string ns)
    {
        var sideElement = new XElement(XName.Get(side, ns));

        if (style is not null && style.LineStyle != BorderLineStyle.None)
        {
            sideElement.Add(new XAttribute("style", style.ToXlsxStyle()));
            sideElement.Add(new XElement(XName.Get("color", ns),
                new XAttribute("rgb", style.Color.ToXLSXColor())));
        }

        borderElement.Add(sideElement);
    }

    private static int GetOrCreateNumberFormat(Cell cell, StyleTracker styleTracker)
    {
        var formatCode = cell.Format.NumberFormat;

        // Auto-apply default date format for date values without explicit format
        if (string.IsNullOrEmpty(formatCode) && cell.ValueType == CellDataType.Date)
        {
            return 14; // mm/dd/yyyy
        }

        if (string.IsNullOrEmpty(formatCode) ||
            string.Equals(formatCode, "General", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        // Check built-in formats first
        var builtInId = NumberFormatPresets.GetNumFmtId(formatCode);
        if (builtInId >= 0)
        {
            return builtInId;
        }

        // Custom format - check if already tracked
        if (styleTracker.NumberFormats.TryGetValue(formatCode, out var existingId))
        {
            return existingId;
        }

        // Assign new custom ID (164+)
        var newId = 164 + styleTracker.NumberFormats.Count;
        styleTracker.NumberFormats[formatCode] = newId;

        // Create numFmt element
        if (styleTracker.NumFmtsElement is null)
        {
            styleTracker.NumFmtsElement = new XElement(XName.Get("numFmts", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("count", "0"));
            // Insert before fonts element
            styleTracker.FontsElement.AddBeforeSelf(styleTracker.NumFmtsElement);
        }

        styleTracker.NumFmtsElement.Add(new XElement(XName.Get("numFmt", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("numFmtId", newId.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("formatCode", formatCode)));
        styleTracker.NumFmtsElement.Attribute("count")!.Value = styleTracker.NumberFormats.Count.ToString(CultureInfo.InvariantCulture);

        return newId;
    }

    private void CreateFontElement(Cell cell, int fontId, StyleTracker styleTracker)
    {
        var fontSize = cell.Format.FontSize ?? 11;
        var fontName = cell.Format.FontFamily ?? "Aptos Narrow";

        var fontElement = new XElement(XName.Get("font", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XElement(XName.Get("sz", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("val", fontSize.ToString(CultureInfo.InvariantCulture))),
            new XElement(XName.Get("name", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("val", fontName)));

        if (cell.Format.Color is not null)
        {
            fontElement.Add(new XElement(XName.Get("color", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("rgb", cell.Format.Color.ToXLSXColor())));
        }
        if (cell.Format.Bold)
        {
            fontElement.Add(new XElement(XName.Get("b", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")));
        }
        if (cell.Format.Italic)
        {
            fontElement.Add(new XElement(XName.Get("i", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")));
        }
        if (cell.Format.Underline)
        {
            fontElement.Add(new XElement(XName.Get("u", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")));
        }
        if (cell.Format.Strikethrough)
        {
            fontElement.Add(new XElement(XName.Get("strike", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")));
        }

        styleTracker.FontsElement.Add(fontElement);
        styleTracker.FontsElement.Attribute("count")!.Value = (styleTracker.FontStyles.Count + 1).ToString(CultureInfo.InvariantCulture);
    }

    private void CreateFillElement(Cell cell, int fillId, StyleTracker styleTracker)
    {
        var fillElement = new XElement(XName.Get("fill", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XElement(XName.Get("patternFill", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("patternType", "solid"),
                new XElement(XName.Get("fgColor", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("rgb", cell.Format.BackgroundColor!.ToXLSXColor())),
                new XElement(XName.Get("bgColor", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("indexed", "64"))));

        styleTracker.FillsElement.Add(fillElement);
        styleTracker.FillsElement.Attribute("count")!.Value = (styleTracker.FillStyles.Count + 2).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Creates a cell style element in the styles document.
    /// </summary>
    private void CreateCellStyleElement(Cell cell, int fontId, int fillId, int borderId, int numFmtId, StyleTracker styleTracker)
    {
        var xfElement = new XElement(XName.Get("xf", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("numFmtId", numFmtId.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("fontId", fontId.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("fillId", fillId.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("borderId", borderId.ToString(CultureInfo.InvariantCulture)),
            new XAttribute("xfId", "0"),
            new XAttribute("applyFont", fontId > 0 ? "1" : "0"),
            new XAttribute("applyFill", fillId > 0 ? "1" : "0"),
            new XAttribute("applyNumberFormat", numFmtId > 0 ? "1" : "0"),
            new XAttribute("applyBorder", borderId > 0 ? "1" : "0"));

        // Add alignment if not default
        if (cell.Format.TextAlign != TextAlign.Left || cell.Format.VerticalAlign != VerticalAlign.Top || cell.Format.WrapText)
        {
            var alignmentElement = CreateAlignmentElement(cell);
            xfElement.Add(alignmentElement);
            xfElement.Add(new XAttribute("applyAlignment", "1"));
        }

        // Add protection if not default
        if (cell.Format.Locked is not null || cell.Format.FormulaHidden is not null)
        {
            var protectionElement = new XElement(XName.Get("protection", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"));
            if (cell.Format.Locked is not null)
            {
                protectionElement.Add(new XAttribute("locked", cell.Format.Locked.Value ? "1" : "0"));
            }
            if (cell.Format.FormulaHidden is not null)
            {
                protectionElement.Add(new XAttribute("hidden", cell.Format.FormulaHidden.Value ? "1" : "0"));
            }
            xfElement.Add(protectionElement);
            xfElement.Add(new XAttribute("applyProtection", "1"));
        }

        if (cell.QuotePrefix)
        {
            xfElement.Add(new XAttribute("quotePrefix", "1"));
            xfElement.Add(new XAttribute("applyQuotePrefix", "1"));
        }

        styleTracker.CellXfsElement.Add(xfElement);
        styleTracker.CellXfsElement.Attribute("count")!.Value = (styleTracker.CellStyles.Count + 1).ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Creates an alignment element for cell formatting.
    /// </summary>
    private static XElement CreateAlignmentElement(Cell cell)
    {
        var alignmentElement = new XElement(XName.Get("alignment", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"));

        if (cell.Format.TextAlign != TextAlign.Left)
        {
            var horizontalAlign = cell.Format.TextAlign switch
            {
                TextAlign.Center => "center",
                TextAlign.Right => "right",
                TextAlign.Justify => "justify",
                _ => "left"
            };
            alignmentElement.Add(new XAttribute("horizontal", horizontalAlign));
        }

        if (cell.Format.VerticalAlign != VerticalAlign.Top)
        {
            var verticalAlign = cell.Format.VerticalAlign switch
            {
                VerticalAlign.Middle => "center",
                VerticalAlign.Bottom => "bottom",
                _ => "top"
            };
            alignmentElement.Add(new XAttribute("vertical", verticalAlign));
        }

        if (cell.Format.WrapText)
        {
            alignmentElement.Add(new XAttribute("wrapText", "1"));
        }

        return alignmentElement;
    }

    private static string FormatValueInvariant(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        return value switch
        {
            double d => d.ToString(CultureInfo.InvariantCulture),
            float f => f.ToString(CultureInfo.InvariantCulture),
            decimal m => m.ToString(CultureInfo.InvariantCulture),
            IFormattable fmt => fmt.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static readonly XName VElement = XName.Get("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
    private static readonly XName FElement = XName.Get("f", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
    private static readonly XName SiElement = XName.Get("si", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");
    private static readonly XName TElement = XName.Get("t", "http://schemas.openxmlformats.org/spreadsheetml/2006/main");

    private static string CellErrorToString(CellError error) => error switch
    {
        CellError.Value => "#VALUE!",
        CellError.Div0  => "#DIV/0!",
        CellError.Ref   => "#REF!",
        CellError.Name  => "#NAME?",
        CellError.Num   => "#NUM!",
        CellError.NA    => "#N/A",
        _               => "#NAME?",
    };

    private static void AddCellValue(XElement cellElement, Cell cell, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc)
    {
        if (!string.IsNullOrEmpty(cell.Formula))
        {
            var formulaValue = cell.Formula.StartsWith('=') ? cell.Formula[1..] : cell.Formula;
            cellElement.Add(new XElement(FElement, formulaValue));
            EmitTypedValue(cellElement, cell, isFormula: true);
        }
        else if (cell.ValueType == CellDataType.String)
        {
            var strValue = cell.Value as string ?? string.Empty;
            if (!sharedStrings.TryGetValue(strValue, out var index))
            {
                index = sharedStrings.Count;
                sharedStrings[strValue] = index;
                sharedStringsDoc.Root!.Add(new XElement(SiElement,
                    new XElement(TElement, strValue)));
            }

            cellElement.Add(new XAttribute("t", "s"));
            cellElement.Add(new XElement(VElement, index));
        }
        else
        {
            EmitTypedValue(cellElement, cell, isFormula: false);
        }
    }

    private static void EmitTypedValue(XElement cellElement, Cell cell, bool isFormula)
    {
        switch (cell.ValueType)
        {
            case CellDataType.Number:
                // Default cell type is "n"; do not emit redundant t attribute.
                cellElement.Add(new XElement(VElement, FormatValueInvariant(cell.Value)));
                break;

            case CellDataType.String:
                // Only reachable on formula cells (string-result formulas).
                cellElement.Add(new XAttribute("t", "str"));
                cellElement.Add(new XElement(VElement, FormatValueInvariant(cell.Value)));
                break;

            case CellDataType.Boolean:
                cellElement.Add(new XAttribute("t", "b"));
                cellElement.Add(new XElement(VElement, cell.Value is true ? "1" : "0"));
                break;

            case CellDataType.Date:
                if (cell.Value is DateTime dateValue)
                {
                    cellElement.Add(new XElement(VElement,
                        dateValue.ToNumber().ToString(CultureInfo.InvariantCulture)));
                }
                break;

            case CellDataType.Error:
                cellElement.Add(new XAttribute("t", "e"));
                var errorString = cell.Value is CellError err
                    ? CellErrorToString(err)
                    : "#NAME?";
                cellElement.Add(new XElement(VElement, errorString));
                break;

            case CellDataType.Empty:
                // No cached value to emit. For formulas, leave it absent so
                // Excel recalculates on open.
                break;
        }
    }

    private static void AddMergedCells(Worksheet sheet, XDocument sheetDoc)
    {
        if (sheet.MergedCells.Ranges.Count > 0)
        {
            var mergeCells = new XElement(XName.Get("mergeCells", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("count", sheet.MergedCells.Ranges.Count));

            foreach (var range in sheet.MergedCells.Ranges)
            {
                mergeCells.Add(new XElement(XName.Get("mergeCell", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("ref", range.ToString())));
            }

            sheetDoc.Root!.Add(mergeCells);
        }
    }

    private static void AddAutoFilter(Worksheet sheet, XDocument sheetDoc)
    {
        if (sheet.AutoFilter.Range is not null)
        {
            var uid = Guid.NewGuid().ToString("B").ToUpperInvariant();
            var autoFilter = new XElement(XName.Get("autoFilter", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("ref", sheet.AutoFilter.Range.Value.ToString()),
                new XAttribute(XName.Get("uid", "http://schemas.microsoft.com/office/spreadsheetml/2014/revision"), uid));

            // Process each filter and create filterColumn elements
            foreach (var filter in sheet.Filters)
            {
                var filterColumn = CreateFilterColumn(filter, sheet.AutoFilter.Range.Value);
                if (filterColumn is not null)
                {
                    autoFilter.Add(filterColumn);
                }
            }

            sheetDoc.Root!.Add(autoFilter);
        }
    }

    private static void AddDataValidations(Worksheet sheet, XDocument sheetDoc)
    {
        var ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XElement? dataValidationsElement = null;

        foreach (var range in sheet.Validation.Ranges)
        {
            foreach (var validator in sheet.Validation.GetValidators(range))
            {
                if (validator is DataValidationRule rule)
                {
                    dataValidationsElement ??= new XElement(XName.Get("dataValidations", ns));

                    var typeStr = rule.Type switch
                    {
                        DataValidationType.WholeNumber => "whole",
                        DataValidationType.Decimal => "decimal",
                        DataValidationType.List => "list",
                        DataValidationType.Date => "date",
                        DataValidationType.Time => "time",
                        DataValidationType.TextLength => "textLength",
                        DataValidationType.Custom => "custom",
                        _ => "whole"
                    };

                    var element = new XElement(XName.Get("dataValidation", ns),
                        new XAttribute("type", typeStr),
                        new XAttribute("sqref", range.ToString()),
                        new XAttribute("allowBlank", rule.AllowBlank ? "1" : "0"));

                    if (rule.Type != DataValidationType.List)
                    {
                        var operatorStr = rule.Operator switch
                        {
                            DataValidationOperator.Between => "between",
                            DataValidationOperator.NotBetween => "notBetween",
                            DataValidationOperator.Equal => "equal",
                            DataValidationOperator.NotEqual => "notEqual",
                            DataValidationOperator.GreaterThan => "greaterThan",
                            DataValidationOperator.LessThan => "lessThan",
                            DataValidationOperator.GreaterThanOrEqual => "greaterThanOrEqual",
                            DataValidationOperator.LessThanOrEqual => "lessThanOrEqual",
                            _ => "between"
                        };
                        element.Add(new XAttribute("operator", operatorStr));
                    }

                    if (rule.ShowErrorMessage)
                    {
                        element.Add(new XAttribute("showErrorMessage", "1"));
                    }

                    if (rule.ShowInputMessage)
                    {
                        element.Add(new XAttribute("showInputMessage", "1"));
                    }

                    if (!string.IsNullOrEmpty(rule.ErrorTitle))
                    {
                        element.Add(new XAttribute("errorTitle", rule.ErrorTitle));
                    }

                    if (!string.IsNullOrEmpty(rule.Error))
                    {
                        element.Add(new XAttribute("error", rule.Error));
                    }

                    if (!string.IsNullOrEmpty(rule.InputTitle))
                    {
                        element.Add(new XAttribute("promptTitle", rule.InputTitle));
                    }

                    if (!string.IsNullOrEmpty(rule.InputMessage))
                    {
                        element.Add(new XAttribute("prompt", rule.InputMessage));
                    }

                    var errorStyleStr = rule.ErrorStyle switch
                    {
                        DataValidationErrorStyle.Warning => "warning",
                        DataValidationErrorStyle.Information => "information",
                        _ => (string?)null // "stop" is the default, omit it
                    };

                    if (errorStyleStr is not null)
                    {
                        element.Add(new XAttribute("errorStyle", errorStyleStr));
                    }

                    if (!string.IsNullOrEmpty(rule.Formula1))
                    {
                        if (rule.Type == DataValidationType.List)
                        {
                            // Excel format: each item quoted individually, e.g. "Yes","No","Maybe"
                            var items = rule.Formula1.Split(',');
                            var quoted = string.Join(",", items.Select(i => $"\"{i.Trim()}\""));
                            element.Add(new XElement(XName.Get("formula1", ns), quoted));
                        }
                        else
                        {
                            element.Add(new XElement(XName.Get("formula1", ns), rule.Formula1));
                        }
                    }

                    if (!string.IsNullOrEmpty(rule.Formula2))
                    {
                        element.Add(new XElement(XName.Get("formula2", ns), rule.Formula2));
                    }

                    dataValidationsElement.Add(element);
                }
            }
        }

        if (dataValidationsElement is not null)
        {
            var count = dataValidationsElement.Elements().Count();
            dataValidationsElement.Add(new XAttribute("count", count.ToString(CultureInfo.InvariantCulture)));
            sheetDoc.Root!.Add(dataValidationsElement);
        }
    }


    private static XElement? CreateFilterColumn(SheetFilter filter, RangeRef autoFilterRange)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";

        // New criterion types know their own column directly.
        if (filter.Criterion is TopFilterCriterion topCriterion)
        {
            var colId = topCriterion.Column - autoFilterRange.Start.Column;
            if (colId < 0 || colId > autoFilterRange.End.Column - autoFilterRange.Start.Column)
                return null;
            return new XElement(ns + "filterColumn",
                new XAttribute("colId", colId),
                new XElement(ns + "top10",
                    new XAttribute("val", topCriterion.Count.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("top", topCriterion.Bottom ? "0" : "1"),
                    new XAttribute("percent", topCriterion.Percent ? "1" : "0")));
        }

        if (filter.Criterion is DynamicFilterCriterion dynCriterion)
        {
            var colId = dynCriterion.Column - autoFilterRange.Start.Column;
            if (colId < 0 || colId > autoFilterRange.End.Column - autoFilterRange.Start.Column)
                return null;
            return new XElement(ns + "filterColumn",
                new XAttribute("colId", colId),
                new XElement(ns + "dynamicFilter",
                    new XAttribute("type", DynamicFilterTypeToXml(dynCriterion.Type))));
        }

        if (filter.Criterion is CellColorFilterCriterion colorCriterion)
        {
            var colId = colorCriterion.Column - autoFilterRange.Start.Column;
            if (colId < 0 || colId > autoFilterRange.End.Column - autoFilterRange.Start.Column)
                return null;
            // Color filters in OOXML reference a dxfId in styles.xml's <dxfs> block.
            // We don't yet write dxfs, so encode the color + kind in a custom data attribute
            // we recognize on read. Excel itself ignores unknown attributes and falls back to
            // showing all rows, which is acceptable until we wire dxfs.
            return new XElement(ns + "filterColumn",
                new XAttribute("colId", colId),
                new XElement(ns + "colorFilter",
                    new XAttribute("dxfId", "0"),
                    new XAttribute("cellColor", colorCriterion.FontColor ? "0" : "1"),
                    new XAttribute(XNamespace.Get("urn:schemas-radzen:spreadsheet") + "color",
                        colorCriterion.Color ?? "")));
        }

        if (filter.Criterion is FilterCriterionLeaf leaf)
        {
            var colId = leaf.Column - autoFilterRange.Start.Column;
            if (colId >= 0 && colId <= autoFilterRange.End.Column - autoFilterRange.Start.Column)
            {
                return new XElement(XName.Get("filterColumn", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("colId", colId),
                    SerializeFilterCriterion(filter.Criterion, colId));
            }
        }
        else if (filter.Criterion is OrCriterion orCriterion)
        {
            var firstLeaf = orCriterion.Criteria.OfType<FilterCriterionLeaf>().FirstOrDefault();
            if (firstLeaf is not null)
            {
                var colId = firstLeaf.Column - autoFilterRange.Start.Column;
                if (colId >= 0 && colId <= autoFilterRange.End.Column - autoFilterRange.Start.Column)
                {
                    return new XElement(XName.Get("filterColumn", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                        new XAttribute("colId", colId),
                        SerializeFilterCriterion(filter.Criterion, colId));
                }
            }
        }
        else if (filter.Criterion is AndCriterion andCriterion)
        {
            var firstLeaf = andCriterion.Criteria.OfType<FilterCriterionLeaf>().FirstOrDefault();
            if (firstLeaf is not null)
            {
                var colId = firstLeaf.Column - autoFilterRange.Start.Column;
                if (colId >= 0 && colId <= autoFilterRange.End.Column - autoFilterRange.Start.Column)
                {
                    return new XElement(XName.Get("filterColumn", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                        new XAttribute("colId", colId),
                        SerializeFilterCriterion(filter.Criterion, colId));
                }
            }
        }

        return null;
    }

    private static XElement SerializeFilterCriterion(FilterCriterion criterion, int columnIndex)
    {
        return criterion switch
        {
            OrCriterion orCriterion => new XElement(XName.Get("customFilters", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                orCriterion.Criteria.Select(c => SerializeCustomFilter(c))),

            AndCriterion andCriterion => new XElement(XName.Get("customFilters", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("and", "1"),
                andCriterion.Criteria.Select(c => SerializeCustomFilter(c))),

            InListCriterion inListCriterion => new XElement(XName.Get("filters", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                inListCriterion.Values.Select(v => new XElement(XName.Get("filter", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("val", v?.ToString() ?? "")))),

            _ => new XElement(XName.Get("customFilters", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                SerializeCustomFilter(criterion))
        };
    }

    private static XElement SerializeCustomFilter(FilterCriterion criterion)
    {
        var (operatorName, value) = criterion switch
        {
            EqualToCriterion equalCriterion => ("equal", equalCriterion.Value?.ToString() ?? ""),
            NotEqualToCriterion notEqualCriterion => ("notEqual", notEqualCriterion.Value?.ToString() ?? ""),
            GreaterThanCriterion greaterCriterion => ("greaterThan", greaterCriterion.Value?.ToString() ?? ""),
            LessThanCriterion lessCriterion => ("lessThan", lessCriterion.Value?.ToString() ?? ""),
            GreaterThanOrEqualCriterion greaterEqualCriterion => ("greaterThanOrEqual", greaterEqualCriterion.Value?.ToString() ?? ""),
            LessThanOrEqualCriterion lessEqualCriterion => ("lessThanOrEqual", lessEqualCriterion.Value?.ToString() ?? ""),
            StartsWithCriterion startsWithCriterion => ("beginsWith", startsWithCriterion.Value?.ToString() ?? ""),
            DoesNotStartWithCriterion doesNotStartWithCriterion => ("notBeginsWith", doesNotStartWithCriterion.Value?.ToString() ?? ""),
            EndsWithCriterion endsWithCriterion => ("endsWith", endsWithCriterion.Value?.ToString() ?? ""),
            DoesNotEndWithCriterion doesNotEndWithCriterion => ("notEndsWith", doesNotEndWithCriterion.Value?.ToString() ?? ""),
            ContainsCriterion containsCriterion => ("contains", containsCriterion.Value?.ToString() ?? ""),
            DoesNotContainCriterion doesNotContainCriterion => ("notContains", doesNotContainCriterion.Value?.ToString() ?? ""),
            IsNullCriterion => ("equal", ""),
            InListCriterion inListCriterion => throw new NotSupportedException("InListCriterion must be converted to OrCriterion before serialization"),
            _ => throw new NotSupportedException($"Filter criterion type {criterion.GetType().Name} is not supported for serialization.")
        };

        return new XElement(XName.Get("customFilter", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("operator", operatorName),
            new XAttribute("val", value));
    }

    private static string DynamicFilterTypeToXml(DynamicFilterType type) => type switch
    {
        DynamicFilterType.AboveAverage => "aboveAverage",
        DynamicFilterType.BelowAverage => "belowAverage",
        DynamicFilterType.Today => "today",
        DynamicFilterType.Yesterday => "yesterday",
        DynamicFilterType.Tomorrow => "tomorrow",
        DynamicFilterType.ThisWeek => "thisWeek",
        DynamicFilterType.LastWeek => "lastWeek",
        DynamicFilterType.NextWeek => "nextWeek",
        DynamicFilterType.ThisMonth => "thisMonth",
        DynamicFilterType.LastMonth => "lastMonth",
        DynamicFilterType.NextMonth => "nextMonth",
        DynamicFilterType.ThisQuarter => "thisQuarter",
        DynamicFilterType.LastQuarter => "lastQuarter",
        DynamicFilterType.NextQuarter => "nextQuarter",
        DynamicFilterType.ThisYear => "thisYear",
        DynamicFilterType.LastYear => "lastYear",
        DynamicFilterType.NextYear => "nextYear",
        DynamicFilterType.YearToDate => "yearToDate",
        DynamicFilterType.January => "M1",
        DynamicFilterType.February => "M2",
        DynamicFilterType.March => "M3",
        DynamicFilterType.April => "M4",
        DynamicFilterType.May => "M5",
        DynamicFilterType.June => "M6",
        DynamicFilterType.July => "M7",
        DynamicFilterType.August => "M8",
        DynamicFilterType.September => "M9",
        DynamicFilterType.October => "M10",
        DynamicFilterType.November => "M11",
        DynamicFilterType.December => "M12",
        DynamicFilterType.Quarter1 => "Q1",
        DynamicFilterType.Quarter2 => "Q2",
        DynamicFilterType.Quarter3 => "Q3",
        DynamicFilterType.Quarter4 => "Q4",
        _ => "today",
    };

    internal static DynamicFilterType DynamicFilterTypeFromXml(string? value) => value switch
    {
        "aboveAverage" => DynamicFilterType.AboveAverage,
        "belowAverage" => DynamicFilterType.BelowAverage,
        "today" => DynamicFilterType.Today,
        "yesterday" => DynamicFilterType.Yesterday,
        "tomorrow" => DynamicFilterType.Tomorrow,
        "thisWeek" => DynamicFilterType.ThisWeek,
        "lastWeek" => DynamicFilterType.LastWeek,
        "nextWeek" => DynamicFilterType.NextWeek,
        "thisMonth" => DynamicFilterType.ThisMonth,
        "lastMonth" => DynamicFilterType.LastMonth,
        "nextMonth" => DynamicFilterType.NextMonth,
        "thisQuarter" => DynamicFilterType.ThisQuarter,
        "lastQuarter" => DynamicFilterType.LastQuarter,
        "nextQuarter" => DynamicFilterType.NextQuarter,
        "thisYear" => DynamicFilterType.ThisYear,
        "lastYear" => DynamicFilterType.LastYear,
        "nextYear" => DynamicFilterType.NextYear,
        "yearToDate" => DynamicFilterType.YearToDate,
        "M1" => DynamicFilterType.January,
        "M2" => DynamicFilterType.February,
        "M3" => DynamicFilterType.March,
        "M4" => DynamicFilterType.April,
        "M5" => DynamicFilterType.May,
        "M6" => DynamicFilterType.June,
        "M7" => DynamicFilterType.July,
        "M8" => DynamicFilterType.August,
        "M9" => DynamicFilterType.September,
        "M10" => DynamicFilterType.October,
        "M11" => DynamicFilterType.November,
        "M12" => DynamicFilterType.December,
        "Q1" => DynamicFilterType.Quarter1,
        "Q2" => DynamicFilterType.Quarter2,
        "Q3" => DynamicFilterType.Quarter3,
        "Q4" => DynamicFilterType.Quarter4,
        _ => DynamicFilterType.Today,
    };

    private static void UpdateAndSaveSharedStrings(ZipArchive archive, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc)
    {
        if (sharedStrings.Count == 0)
        {
            // No string cells in the workbook; skip the part entirely. Its
            // relationship and Content_Types Override are also omitted.
            return;
        }

        var sstElement = sharedStringsDoc.Root!;
        sstElement.Attribute("count")!.Value = sharedStrings.Count.ToString(CultureInfo.InvariantCulture);
        sstElement.Attribute("uniqueCount")!.Value = sharedStrings.Count.ToString(CultureInfo.InvariantCulture);

        using var entry = archive.CreateEntry("xl/sharedStrings.xml").Open();
        sharedStringsDoc.Save(entry);
    }

    private void SaveWorkbook(ZipArchive archive)
    {
        XNamespace mainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace rNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        var workbook = new XDocument(
            new XElement(mainNs + "workbook",
                new XAttribute(XNamespace.Xmlns + "r", rNs.NamespaceName),
                new XElement(mainNs + "fileVersion",
                    new XAttribute("appName", "xl"),
                    new XAttribute("lastEdited", "7"),
                    new XAttribute("lowestEdited", "7"),
                    new XAttribute("rupBuild", "27932")),
                new XElement(mainNs + "workbookPr",
                    new XAttribute("defaultThemeVersion", "202300")),
                new XElement(mainNs + "bookViews",
                    new XElement(mainNs + "workbookView",
                        new XAttribute("xWindow", "0"),
                        new XAttribute("yWindow", "0"),
                        new XAttribute("windowWidth", "20000"),
                        new XAttribute("windowHeight", "12000"))),
                new XElement(mainNs + "sheets"),
                new XElement(mainNs + "calcPr",
                    new XAttribute("calcId", "0"))));

        var sheetsElement = workbook.Root!.Element(mainNs + "sheets")!;

        // Add sheet references
        for (var i = 0; i < sheets.Count; i++)
        {
            var sheet = sheets[i];
            var sheetId = i + 1;
            var relId = $"rId{sheetId + 2}";

            sheetsElement.Add(new XElement(XName.Get("sheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("name", sheet.Name),
                new XAttribute("sheetId", sheetId),
                new XAttribute(XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"), relId)));
        }

        // Add workbook protection
        var wp = sourceWorkbook.Protection;
        if (wp.LockStructure || wp.PasswordHash is not null || wp.HashValue is not null)
        {
            var ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
            var wpElement = new XElement(XName.Get("workbookProtection", ns));
            if (wp.LockStructure) wpElement.Add(new XAttribute("lockStructure", "1"));
            if (wp.PasswordHash is not null) wpElement.Add(new XAttribute("workbookPassword", wp.PasswordHash));
            if (wp.AlgorithmName is not null) wpElement.Add(new XAttribute("workbookAlgorithmName", wp.AlgorithmName));
            if (wp.HashValue is not null) wpElement.Add(new XAttribute("workbookHashValue", wp.HashValue));
            if (wp.SaltValue is not null) wpElement.Add(new XAttribute("workbookSaltValue", wp.SaltValue));
            if (wp.SpinCount is not null) wpElement.Add(new XAttribute("workbookSpinCount", wp.SpinCount.Value.ToString(CultureInfo.InvariantCulture)));
            sheetsElement.AddBeforeSelf(wpElement);
        }

        using var entry = archive.CreateEntry("xl/workbook.xml").Open();
        workbook.Save(entry);
    }

    private static void AddSheetProtection(Worksheet sheet, XDocument sheetDoc)
    {
        var p = sheet.Protection;
        if (!p.IsProtected && p.PasswordHash is null && p.HashValue is null)
        {
            return;
        }

        var ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var element = new XElement(XName.Get("sheetProtection", ns));

        if (p.IsProtected) element.Add(new XAttribute("sheet", "1"));

        // XLSX: attribute absent or "1" = forbidden; write "0" to allow
        if (p.AllowFormatCells) element.Add(new XAttribute("formatCells", "0"));
        if (p.AllowFormatRows) element.Add(new XAttribute("formatRows", "0"));
        if (p.AllowFormatColumns) element.Add(new XAttribute("formatColumns", "0"));
        if (p.AllowInsertColumns) element.Add(new XAttribute("insertColumns", "0"));
        if (p.AllowInsertRows) element.Add(new XAttribute("insertRows", "0"));
        if (p.AllowInsertHyperlinks) element.Add(new XAttribute("insertHyperlinks", "0"));
        if (p.AllowDeleteColumns) element.Add(new XAttribute("deleteColumns", "0"));
        if (p.AllowDeleteRows) element.Add(new XAttribute("deleteRows", "0"));
        if (p.AllowSort) element.Add(new XAttribute("sort", "0"));
        if (p.AllowAutoFilter) element.Add(new XAttribute("autoFilter", "0"));

        // selectLockedCells/selectUnlockedCells: write "1" if forbidden
        if (!p.AllowSelectLockedCells) element.Add(new XAttribute("selectLockedCells", "1"));
        if (!p.AllowSelectUnlockedCells) element.Add(new XAttribute("selectUnlockedCells", "1"));

        // Password hashes
        if (p.PasswordHash is not null) element.Add(new XAttribute("password", p.PasswordHash));
        if (p.AlgorithmName is not null) element.Add(new XAttribute("algorithmName", p.AlgorithmName));
        if (p.HashValue is not null) element.Add(new XAttribute("hashValue", p.HashValue));
        if (p.SaltValue is not null) element.Add(new XAttribute("saltValue", p.SaltValue));
        if (p.SpinCount is not null) element.Add(new XAttribute("spinCount", p.SpinCount.Value.ToString(CultureInfo.InvariantCulture)));

        // Insert after sheetData per ECMA-376 element order
        var sheetData = sheetDoc.Root!.Element(XName.Get("sheetData", ns));
        sheetData!.AddAfterSelf(element);
    }
}
