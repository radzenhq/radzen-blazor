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
class XlsxWriter(Workbook workbook)
{
    private readonly IReadOnlyList<Worksheet> sheets = workbook.Sheets;

    public void Write(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        var styleTracker = CreateStylesDocument();
        SaveContentTypes(archive);
        SaveRelationships(archive);

        var sharedStrings = new Dictionary<string, int>();
        var sharedStringsDoc = CreateSharedStringsDocument(sharedStrings);

        SaveSheets(archive, styleTracker, sharedStrings, sharedStringsDoc);
        SaveStyles(archive, styleTracker);
        UpdateAndSaveSharedStrings(archive, sharedStrings, sharedStringsDoc);
        SaveWorkbook(archive);
    }

    private record struct FontKey(string? Color, bool Bold, bool Italic, bool Underline, bool Strikethrough, string? FontFamily, double? FontSize);
    private record struct CellStyleKey(int FontId, int FillId, int BorderId, TextAlign TextAlign, VerticalAlign VerticalAlign, bool WrapText, int NumFmtId);
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

        var drawingRels = new List<(string Id, string Target)>();
        var imageRelIndex = 1;

        foreach (var image in sheet.Images)
        {
            var imageRelId = $"rId{imageRelIndex++}";

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
            drawingRels.Add((imageRelId, relTarget));

            // Build anchor element
            XElement anchorElement;
            var fromElement = new XElement(xdr + "from",
                new XElement(xdr + "col", image.From.Column.ToString(CultureInfo.InvariantCulture)),
                new XElement(xdr + "colOff", image.From.ColumnOffset.ToString(CultureInfo.InvariantCulture)),
                new XElement(xdr + "row", image.From.Row.ToString(CultureInfo.InvariantCulture)),
                new XElement(xdr + "rowOff", image.From.RowOffset.ToString(CultureInfo.InvariantCulture)));

            var cNvPr = new XElement(xdr + "cNvPr",
                new XAttribute("id", imageRelIndex.ToString(CultureInfo.InvariantCulture)),
                new XAttribute("name", image.Name ?? $"Image {imageRelIndex - 1}"));

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

            if (image.AnchorMode == ImageAnchorMode.TwoCellAnchor && image.To is not null)
            {
                var toElement = new XElement(xdr + "to",
                    new XElement(xdr + "col", image.To.Column.ToString(CultureInfo.InvariantCulture)),
                    new XElement(xdr + "colOff", image.To.ColumnOffset.ToString(CultureInfo.InvariantCulture)),
                    new XElement(xdr + "row", image.To.Row.ToString(CultureInfo.InvariantCulture)),
                    new XElement(xdr + "rowOff", image.To.RowOffset.ToString(CultureInfo.InvariantCulture)));

                anchorElement = new XElement(xdr + "twoCellAnchor",
                    fromElement,
                    toElement,
                    picElement,
                    new XElement(xdr + "clientData"));
            }
            else
            {
                var extElement = new XElement(xdr + "ext",
                    new XAttribute("cx", image.Width.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("cy", image.Height.ToString(CultureInfo.InvariantCulture)));

                anchorElement = new XElement(xdr + "oneCellAnchor",
                    fromElement,
                    extElement,
                    picElement,
                    new XElement(xdr + "clientData"));
            }

            drawingDoc.Root!.Add(anchorElement);
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

            foreach (var (id, target) in drawingRels)
            {
                drawingRelsDoc.Root!.Add(new XElement(XName.Get("Relationship", pkgNs),
                    new XAttribute("Id", id),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/image"),
                    new XAttribute("Target", target)));
            }

            using (var relsEntry = archive.CreateEntry($"xl/drawings/_rels/drawing{drawingIndex}.xml.rels").Open())
            {
                drawingRelsDoc.Save(relsEntry);
            }
        }
    }

    private void SaveContentTypes(ZipArchive archive)
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
                    new XAttribute("PartName", "/xl/sharedStrings.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml")),
                new XElement(XName.Get("Override", ctNs),
                    new XAttribute("PartName", "/xl/styles.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"))));

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

        // Add drawing overrides
        for (var i = 0; i < sheets.Count; i++)
        {
            if (sheets[i].Images.Count > 0)
            {
                contentTypes.Root!.Add(new XElement(XName.Get("Override", ctNs),
                    new XAttribute("PartName", $"/xl/drawings/drawing{i + 1}.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.drawing+xml")));
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
        var rels = new XDocument(
            new XElement(XName.Get("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships"),
                new XElement(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships"),
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                    new XAttribute("Target", "xl/workbook.xml"))));

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

        var stylesDoc = new XDocument(
            new XElement(XName.Get("styleSheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                CreateFontsElement(),
                CreateFillsElement(),
                CreateBordersElement(),
                CreateCellStyleXfsElement(),
                CreateCellXfsElement(),
                CreateCellStylesElement()));

        styleTracker.StylesDocument = stylesDoc;
        styleTracker.FontsElement = stylesDoc.Root!.Element(XName.Get("fonts", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))!;
        styleTracker.FillsElement = stylesDoc.Root!.Element(XName.Get("fills", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))!;
        styleTracker.CellXfsElement = stylesDoc.Root!.Element(XName.Get("cellXfs", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))!;
        styleTracker.BordersElement = stylesDoc.Root!.Element(XName.Get("borders", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))!;

        return styleTracker;
    }

    private static XElement CreateFontsElement()
    {
        return new XElement(XName.Get("fonts", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("count", "1"),
            new XElement(XName.Get("font", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XElement(XName.Get("sz", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("val", "11")),
                new XElement(XName.Get("name", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("val", "Aptos Narrow"))));
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

    private void SaveSheets(ZipArchive archive, StyleTracker styleTracker, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc)
    {
        var workbookRels = CreateWorkbookRelationships();
        var workbookRelsElement = workbookRels.Root!;

        // Add shared strings and styles relationships
        workbookRelsElement.Add(new XElement(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships"),
            new XAttribute("Id", "rId1"),
            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"),
            new XAttribute("Target", "sharedStrings.xml")));

        workbookRelsElement.Add(new XElement(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships"),
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
            workbookRelsElement.Add(new XElement(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships"),
                new XAttribute("Id", relId),
                new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                new XAttribute("Target", $"worksheets/{sheetName}")));

            // Save individual sheet
            SaveSheet(archive, sheet, sheetName, sheetId, relId, styleTracker, sharedStrings, sharedStringsDoc, mediaMap, ref globalMediaIndex);
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

    private void SaveSheet(ZipArchive archive, Worksheet sheet, string sheetName, int sheetId, string relId, StyleTracker styleTracker, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc, Dictionary<string, string> mediaMap, ref int globalMediaIndex)
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

        // Add hyperlinks
        var hyperlinkRels = AddHyperlinks(sheet, sheetDoc);

        // Track all sheet relationship entries
        var sheetRelEntries = new List<(string Id, string Type, string Target, bool External)>();
        foreach (var (id, url) in hyperlinkRels)
        {
            sheetRelEntries.Add((id, "http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink", url, true));
        }

        // Add drawing reference if sheet has images
        if (sheet.Images.Count > 0)
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

                if (cell.Hyperlink.DisplayText is not null)
                {
                    hyperlinkElement.Add(new XAttribute("display", cell.Hyperlink.DisplayText));
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

        return new XDocument(
            new XElement(XName.Get("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute(XName.Get("uid", "http://schemas.microsoft.com/office/spreadsheetml/2014/revision"), uid),
                CreateSheetProperties(sheet),
                CreateDimension(sheet),
                CreateSheetViews(sheet),
                CreateColumns(sheet),
                new XElement(XName.Get("sheetData", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))));
    }

    private static XElement CreateSheetProperties(Worksheet sheet)
    {
        return new XElement(XName.Get("sheetPr", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            sheet.Filters.Count > 0 ? new XAttribute("filterMode", "1") : null);
    }

    private static XElement CreateDimension(Worksheet sheet)
    {
        return new XElement(XName.Get("dimension", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("ref", $"A1:{new CellRef(sheet.RowCount - 1, sheet.ColumnCount - 1)}"));
    }

    private static XElement CreateSheetViews(Worksheet sheet)
    {
        return new XElement(XName.Get("sheetViews", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XElement(XName.Get("sheetView", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("workbookViewId", "0"),
                new XElement(XName.Get("pane", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("xSplit", sheet.Columns.Frozen),
                    new XAttribute("ySplit", sheet.Rows.Frozen),
                    new XAttribute("topLeftCell", new CellRef(sheet.Rows.Frozen, sheet.Columns.Frozen).ToString()),
                    new XAttribute("activePane", "topLeft"),
                    new XAttribute("state", "frozen"))));
    }

    private static XElement CreateColumns(Worksheet sheet)
    {
        return new XElement(XName.Get("cols", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            Enumerable.Range(0, sheet.ColumnCount).Select(col =>
            {
                var colElement = new XElement(XName.Get("col", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("min", col + 1),
                    new XAttribute("max", col + 1));

                // Only persist width if it differs from the default
                if (sheet.Columns[col] != sheet.Columns.Size)
                {
                    colElement.Add(new XAttribute("width", Math.Round(sheet.Columns[col] / 7.0, 8)));
                    colElement.Add(new XAttribute("customWidth", "1"));
                }

                return colElement;
            }));
    }

    private void ProcessSheetData(Worksheet sheet, XElement sheetData, StyleTracker styleTracker, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc)
    {
        var cellsByRow = sheet.Cells.GetPopulatedCells()
            .Where(c => c.GetValue() is not null)
            .GroupBy(c => c.Address.Row)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.Address.Column).ToList());

        // Include rows with data, custom height, or hidden state
        var rowsToProcess = new SortedSet<int>(cellsByRow.Keys);

        foreach (var rowIndex in sheet.Rows.GetCustomSizedIndices())
        {
            rowsToProcess.Add(rowIndex);
        }

        foreach (var rowIndex in sheet.Rows.GetHiddenIndices())
        {
            rowsToProcess.Add(rowIndex);
        }

        foreach (var row in rowsToProcess)
        {
            var rowElement = CreateRowElement(sheet, row);

            if (cellsByRow.TryGetValue(row, out var cells))
            {
                foreach (var cell in cells)
                {
                    var cellElement = CreateCellElement(sheet, row, cell.Address.Column, cell, styleTracker, sharedStrings, sharedStringsDoc);
                    rowElement.Add(cellElement);
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
        return !cell.Format.IsDefault || cell.ValueType == CellDataType.Date;
    }

    private int GetOrCreateCellStyle(Cell cell, StyleTracker styleTracker)
    {
        var fontId = GetOrCreateFontStyle(cell, styleTracker);
        var fillId = GetOrCreateFillStyle(cell, styleTracker);
        var numFmtId = GetOrCreateNumberFormat(cell, styleTracker);
        var borderId = GetOrCreateBorderStyle(cell, styleTracker);

        var styleKey = new CellStyleKey(fontId, fillId, borderId, cell.Format.TextAlign, cell.Format.VerticalAlign, cell.Format.WrapText, numFmtId);

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

    private static void AddCellValue(XElement cellElement, Cell cell, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc)
    {
        if (!string.IsNullOrEmpty(cell.Formula))
        {
            cellElement.Add(new XAttribute("t", "str"));
            var formulaValue = cell.Formula.StartsWith('=') ? cell.Formula[1..] : cell.Formula;
            cellElement.Add(new XElement(XName.Get("f", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), formulaValue));
            cellElement.Add(new XElement(XName.Get("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), FormatValueInvariant(cell.Value)));
        }
        else
        {
            switch (cell.ValueType)
            {
                case CellDataType.Number:
                    cellElement.Add(new XAttribute("t", "n"));
                    cellElement.Add(new XElement(XName.Get("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), FormatValueInvariant(cell.Value)));
                    break;

                case CellDataType.String:
                    var strValue = cell.GetValue() ?? string.Empty;
                    if (!sharedStrings.TryGetValue(strValue, out var index))
                    {
                        index = sharedStrings.Count;
                        sharedStrings[strValue] = index;
                        var sstElement = sharedStringsDoc.Root!;
                        sstElement.Add(new XElement(XName.Get("si", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                            new XElement(XName.Get("t", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), strValue)));
                    }

                    cellElement.Add(new XAttribute("t", "s"));
                    cellElement.Add(new XElement(XName.Get("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), index));
                    break;

                case CellDataType.Date:
                    if (cell.Value is DateTime dateValue)
                    {
                        cellElement.Add(new XAttribute("t", "n"));
                        cellElement.Add(new XElement(XName.Get("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                            dateValue.ToNumber().ToString(CultureInfo.InvariantCulture)));
                    }
                    break;

                case CellDataType.Error:
                    cellElement.Add(new XAttribute("t", "e"));
                    cellElement.Add(new XElement(XName.Get("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), cell.GetValue()));
                    break;

                case CellDataType.Empty:
                    break;
            }
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
        if (sheet.AutoFilter is not null)
        {
            var uid = Guid.NewGuid().ToString("B").ToUpperInvariant();
            var autoFilter = new XElement(XName.Get("autoFilter", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("ref", sheet.AutoFilter.Range.ToString()),
                new XAttribute(XName.Get("uid", "http://schemas.microsoft.com/office/spreadsheetml/2014/revision"), uid));

            // Process each filter and create filterColumn elements
            foreach (var filter in sheet.Filters)
            {
                var filterColumn = CreateFilterColumn(filter, sheet.AutoFilter.Range);
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

                    if (!string.IsNullOrEmpty(rule.PromptTitle))
                    {
                        element.Add(new XAttribute("promptTitle", rule.PromptTitle));
                    }

                    if (!string.IsNullOrEmpty(rule.Prompt))
                    {
                        element.Add(new XAttribute("prompt", rule.Prompt));
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

    private static void UpdateAndSaveSharedStrings(ZipArchive archive, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc)
    {
        var sstElement = sharedStringsDoc.Root!;
        sstElement.Attribute("count")!.Value = sharedStrings.Count.ToString(CultureInfo.InvariantCulture);
        sstElement.Attribute("uniqueCount")!.Value = sharedStrings.Count.ToString(CultureInfo.InvariantCulture);

        using var entry = archive.CreateEntry("xl/sharedStrings.xml").Open();
        sharedStringsDoc.Save(entry);
    }

    private void SaveWorkbook(ZipArchive archive)
    {
        var workbook = new XDocument(
            new XElement(XName.Get("workbook", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XElement(XName.Get("sheets", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))));

        var sheetsElement = workbook.Root!.Element(XName.Get("sheets", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))!;

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

        using var entry = archive.CreateEntry("xl/workbook.xml").Open();
        workbook.Save(entry);
    }
}
