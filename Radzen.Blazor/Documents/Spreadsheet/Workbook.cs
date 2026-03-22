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
/// Represents a workbook in a spreadsheet.
/// </summary>
public class Workbook
{
    private readonly List<Worksheet> sheets = [];

    /// <summary>
    /// Gets the collection of sheets in the workbook.
    /// </summary>
    public IReadOnlyList<Worksheet> Sheets => sheets;

    internal Workbook(Worksheet sheet)
    {
        AddSheet(sheet);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Workbook"/> class.
    /// </summary>
    public Workbook()
    {
    }

    /// <summary>
    /// Adds a new sheet to the workbook with the specified name, rows, and columns.
    /// </summary>
    public Worksheet AddSheet(string name, int rows, int columns)
    {
        var sheet = new Worksheet(rows, columns)
        {
            Name = name
        };
        AddSheet(sheet);
        return sheet;
    }

    /// <summary>
    /// Adds an existing sheet to the workbook.
    /// </summary>
    /// <param name="sheet"></param>
    public void AddSheet(Worksheet sheet)
    {
        ArgumentNullException.ThrowIfNull(sheet);
        sheets.Add(sheet);
        sheet.Workbook = this;
    }

    /// <summary>
    /// Gets the sheet with the specified name or null if not found.
    /// </summary>
    /// <param name="name"></param>
    public Worksheet? GetSheet(string name)
    {
        foreach (var sheet in sheets)
        {
            if (string.Equals(sheet.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                return sheet;
            }
        }

        return null;
    }

    /// <summary>
    /// Saves the workbook to the specified stream in the Open XML Spreadsheet format (XLSX).
    /// </summary>
    /// <param name="stream"></param>
    public void SaveToStream(Stream stream)
    {
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

        // Create style tracking with initialized styles document
        var styleTracker = CreateStylesDocument();

        // Create and save content types
        SaveContentTypes(archive);

        // Create and save relationships
        SaveRelationships(archive);

        // Create and save shared strings
        var sharedStrings = new Dictionary<string, int>();
        var sharedStringsDoc = CreateSharedStringsDocument(sharedStrings);

        // Process and save each sheet
        SaveSheets(archive, styleTracker, sharedStrings, sharedStringsDoc);

        // Save styles (after processing all sheets to collect all styles)
        SaveStyles(archive, styleTracker);

        // Update and save shared strings
        UpdateAndSaveSharedStrings(archive, sharedStrings, sharedStringsDoc);

        // Save workbook
        SaveWorkbook(archive);
    }

    private class StyleTracker
    {
        public Dictionary<(string? Color, bool Bold, bool Italic, bool Underline, bool Strikethrough, string? FontFamily, double? FontSize), int> FontStyles { get; } = new();
        public Dictionary<string, int> FillStyles { get; } = new();
        public Dictionary<(int FontId, int FillId, int BorderId, TextAlign TextAlign, VerticalAlign VerticalAlign, bool WrapText, int NumFmtId), int> CellStyles { get; } = new();
        public Dictionary<string, int> NumberFormats { get; } = new(StringComparer.OrdinalIgnoreCase);
        public Dictionary<(string? TopStyle, string? TopColor, string? RightStyle, string? RightColor, string? BottomStyle, string? BottomColor, string? LeftStyle, string? LeftColor), int> BorderStyles { get; } = new();
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

            // Deduplicate media: use base64 of data as key
            var hash = Convert.ToBase64String(image.Data);
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

            if (image.Description != null)
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

            if (image.AnchorMode == ImageAnchorMode.TwoCellAnchor && image.To != null)
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
            var drawingIndex = sheets.IndexOf(sheet) + 1;
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

        for (var row = 0; row < sheet.RowCount; row++)
        {
            for (var col = 0; col < sheet.ColumnCount; col++)
            {
                var cell = sheet.Cells[row, col];
                if (cell.Hyperlink != null)
                {
                    hyperlinksElement ??= new XElement(XName.Get("hyperlinks", ns));

                    var relId = $"rId{relIndex++}";
                    var cellRef = new CellRef(row, col).ToString();

                    var hyperlinkElement = new XElement(XName.Get("hyperlink", ns),
                        new XAttribute("ref", cellRef),
                        new XAttribute(rNs + "id", relId));

                    if (cell.Hyperlink.DisplayText != null)
                    {
                        hyperlinkElement.Add(new XAttribute("display", cell.Hyperlink.DisplayText));
                    }

                    hyperlinksElement.Add(hyperlinkElement);
                    rels.Add((relId, cell.Hyperlink.Url));
                }
            }
        }

        if (hyperlinksElement != null)
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
        for (var row = 0; row < sheet.RowCount; row++)
        {
            var rowElement = CreateRowElement(sheet, row);

            for (var col = 0; col < sheet.ColumnCount; col++)
            {
                var cell = sheet.Cells[row, col];
                var value = cell.GetValue();

                if (value == null)
                {
                    continue;
                }

                var cellElement = CreateCellElement(sheet, row, col, cell, styleTracker, sharedStrings, sharedStringsDoc);
                rowElement.Add(cellElement);
            }

            // Always add the row element, even if it has no cells, to preserve row height
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
        Workbook.AddCellValue(cellElement, cell, sharedStrings, sharedStringsDoc);

        return cellElement;
    }

    private static bool HasCellFormatting(Cell cell)
    {
        return cell.Format.Color != null ||
               cell.Format.BackgroundColor != null ||
               cell.Format.Bold ||
               cell.Format.Italic ||
               cell.Format.Underline ||
               cell.Format.Strikethrough ||
               cell.Format.WrapText ||
               cell.Format.FontFamily != null ||
               cell.Format.FontSize != null ||
               cell.Format.BorderTop != null ||
               cell.Format.BorderRight != null ||
               cell.Format.BorderBottom != null ||
               cell.Format.BorderLeft != null ||
               cell.Format.TextAlign != TextAlign.Left ||
               cell.Format.VerticalAlign != VerticalAlign.Top ||
               !string.IsNullOrEmpty(cell.Format.NumberFormat) ||
               cell.ValueType == CellDataType.Date;
    }

    private int GetOrCreateCellStyle(Cell cell, StyleTracker styleTracker)
    {
        var fontId = GetOrCreateFontStyle(cell, styleTracker);
        var fillId = GetOrCreateFillStyle(cell, styleTracker);
        var numFmtId = GetOrCreateNumberFormat(cell, styleTracker);
        var borderId = GetOrCreateBorderStyle(cell, styleTracker);

        var styleKey = (fontId, fillId, borderId, cell.Format.TextAlign, cell.Format.VerticalAlign, cell.Format.WrapText, numFmtId);

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
        var fontKey = (cell.Format.Color, cell.Format.Bold, cell.Format.Italic, cell.Format.Underline, cell.Format.Strikethrough, cell.Format.FontFamily, cell.Format.FontSize);

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
        if (cell.Format.BackgroundColor == null)
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

        if (bt == null && br == null && bb == null && bl == null)
        {
            return 0;
        }

        var borderKey = (
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

        if (style != null && style.LineStyle != BorderLineStyle.None)
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
        if (styleTracker.NumFmtsElement == null)
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

        if (cell.Format.Color != null)
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
        if (value == null)
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
        if (sheet.AutoFilter != null)
        {
            var uid = Guid.NewGuid().ToString("B").ToUpperInvariant();
            var autoFilter = new XElement(XName.Get("autoFilter", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("ref", sheet.AutoFilter.Range.ToString()),
                new XAttribute(XName.Get("uid", "http://schemas.microsoft.com/office/spreadsheetml/2014/revision"), uid));

            // Process each filter and create filterColumn elements
            foreach (var filter in sheet.Filters)
            {
                var filterColumn = CreateFilterColumn(filter, sheet.AutoFilter.Range);
                if (filterColumn != null)
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

                    if (errorStyleStr != null)
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

        if (dataValidationsElement != null)
        {
            var count = dataValidationsElement.Elements().Count();
            dataValidationsElement.Add(new XAttribute("count", count.ToString(CultureInfo.InvariantCulture)));
            sheetDoc.Root!.Add(dataValidationsElement);
        }
    }

    private static void ParseDataValidations(XDocument sheetDoc, XNamespace sNs, Worksheet sheet)
    {
        var dataValidationsElement = sheetDoc.Descendants(sNs + "dataValidations").FirstOrDefault();
        if (dataValidationsElement == null)
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
            if (type == DataValidationType.List && formula1 != null)
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
                Prompt = promptAttr,
                PromptTitle = promptTitleAttr,
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
            if (firstLeaf != null)
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
            if (firstLeaf != null)
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

    /// <summary>
    /// Loads a workbook from the specified stream in the Open XML Spreadsheet format (XLSX).
    /// </summary>
    /// <param name="stream"></param>
    /// <returns></returns>
    /// <exception cref="InvalidDataException"></exception>
    public static Workbook LoadFromStream(Stream stream)
    {
        var workbook = new Workbook();
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read, leaveOpen: true);

        // Parse styles
        var styleInfo = ParseStyles(archive);

        // Parse shared strings
        var sharedStrings = ParseSharedStrings(archive);

        // Parse sheet definitions and relationships
        var sheetInfos = ParseSheetDefinitions(archive);

        // Load each sheet
        foreach (var sheetInfo in sheetInfos)
        {
            var sheet = LoadSheet(archive, sheetInfo, styleInfo, sharedStrings);
            workbook.AddSheet(sheet);
        }

        return workbook;
    }

    private static StyleInfo ParseStyles(ZipArchive archive)
    {
        var fontStyles = new Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline, bool Strikethrough, string? FontFamily, double? FontSize)>();
        var fillColors = new Dictionary<int, string>();
        var cellStyles = new Dictionary<int, (int FontId, int FillId, int BorderId, TextAlign TextAlign, VerticalAlign VerticalAlign, bool WrapText, int NumFmtId)>(0);
        var numberFormats = new Dictionary<int, string>();
        var borderStyles = new Dictionary<int, (BorderStyle? Top, BorderStyle? Right, BorderStyle? Bottom, BorderStyle? Left)>();

        var stylesEntry = archive.GetEntry("xl/styles.xml");
        if (stylesEntry != null)
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
            if (color != null)
            {
                colorValue = "#" + color[2..]; // Convert from "FFFF0000" to "#FF0000"
            }
            bool bold = fonts[i].Element(stylesNs + "b") != null;
            bool italic = fonts[i].Element(stylesNs + "i") != null;
            bool underline = fonts[i].Element(stylesNs + "u") != null;
            bool strikethrough = fonts[i].Element(stylesNs + "strike") != null;

            var fontName = fonts[i].Element(stylesNs + "name")?.Attribute("val")?.Value;
            string? fontFamily = fontName != null && fontName != "Aptos Narrow" ? fontName : null;

            var szValue = fonts[i].Element(stylesNs + "sz")?.Attribute("val")?.Value;
            double? fontSize = null;
            if (szValue != null && double.TryParse(szValue, System.Globalization.NumberStyles.Float, CultureInfo.InvariantCulture, out var sz) && sz != 11)
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

            if (top != null || right != null || bottom != null || left != null)
            {
                borderStyles[i] = (top, right, bottom, left);
            }
        }
    }

    private static BorderStyle? ParseBorderSide(XElement? element, XNamespace ns)
    {
        if (element == null)
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
        var color = rgbColor != null ? "#" + rgbColor[2..] : "#000000";

        return new BorderStyle { LineStyle = lineStyle, Color = color };
    }

    private static void ParseFills(XDocument stylesDoc, XNamespace stylesNs, Dictionary<int, string> fillColors)
    {
        var fills = stylesDoc.Descendants(stylesNs + "fill").ToList();
        for (var i = 0; i < fills.Count; i++)
        {
            var fgColor = fills[i].Element(stylesNs + "patternFill")?.Element(stylesNs + "fgColor")?.Attribute("rgb")?.Value;
            if (fgColor != null)
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
            if (numFmtId != null && formatCode != null)
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
        Dictionary<int, (int FontId, int FillId, int BorderId, TextAlign TextAlign, VerticalAlign VerticalAlign, bool WrapText, int NumFmtId)> cellStyles)
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
            var numFmtId = numFmtIdAttr != null ? int.Parse(numFmtIdAttr, CultureInfo.InvariantCulture) : 0;

            if (fontId != null && fillId != null)
            {
                var fontIdValue = int.Parse(fontId, CultureInfo.InvariantCulture);
                var fillIdValue = int.Parse(fillId, CultureInfo.InvariantCulture);
                var borderIdValue = borderId != null ? int.Parse(borderId, CultureInfo.InvariantCulture) : 0;

                var (textAlign, verticalAlign, wrapText) = ParseAlignment(cellXfs[i], stylesNs, applyAlignment);

                if (applyFont == "1" && fontStyles.ContainsKey(fontIdValue) ||
                    applyFill == "1" && fillColors.ContainsKey(fillIdValue) ||
                    applyBorder == "1" && borderStyles.ContainsKey(borderIdValue) ||
                    applyAlignment == "1" ||
                    numFmtId > 0)
                {
                    cellStyles[i] = (fontIdValue, fillIdValue, borderIdValue, textAlign, verticalAlign, wrapText, numFmtId);
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
            if (alignment != null)
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

    private static List<string> ParseSharedStrings(ZipArchive archive)
    {
        var sharedStrings = new List<string>();
        var sharedEntry = archive.GetEntry("xl/sharedStrings.xml");

        if (sharedEntry != null)
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
        if (sheetEntry == null)
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

        sheet.Name = sheetInfo.Name;
        return sheet;
    }

    private static double ParseDefaultRowHeight(XDocument sheetDoc, XNamespace sNs)
    {
        // Parse default row height from sheet format properties
        var defaultRowHeight = 20.0; // Default fallback
        var sheetFormatPr = sheetDoc.Descendants(sNs + "sheetFormatPr").FirstOrDefault();
        if (sheetFormatPr != null)
        {
            var defaultHeight = sheetFormatPr.Attribute("defaultRowHeight")?.Value;
            if (defaultHeight != null && double.TryParse(defaultHeight, NumberStyles.Float, CultureInfo.InvariantCulture, out var heightPoints))
            {
                defaultRowHeight = Math.Round(heightPoints * (96.0 / 72.0));
            }
        }

        return defaultRowHeight;
    }

    private static void ParseFrozenPanes(XDocument sheetDoc, XNamespace sNs, Worksheet sheet)
    {
        var sheetView = sheetDoc.Descendants(sNs + "sheetView").FirstOrDefault();
        if (sheetView != null)
        {
            var pane = sheetView.Element(sNs + "pane");
            if (pane != null)
            {
                var xSplit = pane.Attribute("xSplit")?.Value;
                var ySplit = pane.Attribute("ySplit")?.Value;

                if (xSplit != null && int.TryParse(xSplit, out var frozenColumns))
                {
                    sheet.Columns.Frozen = frozenColumns;
                }

                if (ySplit != null && int.TryParse(ySplit, out var frozenRows))
                {
                    sheet.Rows.Frozen = frozenRows;
                }
            }
        }
    }

    private static void ParseColumnWidths(XDocument sheetDoc, XNamespace sNs, Worksheet sheet)
    {
        var cols = sheetDoc.Descendants(sNs + "cols").FirstOrDefault();
        if (cols != null)
        {
            foreach (var col in cols.Elements(sNs + "col"))
            {
                var min = col.Attribute("min")?.Value;
                var max = col.Attribute("max")?.Value;
                var width = col.Attribute("width")?.Value;

                if (min != null && max != null && width != null &&
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

        if (rowIndex != null && int.TryParse(rowIndex, out var rowNum))
        {
            var actualRowIndex = rowNum - 1; // Convert from 1-based to 0-based
            if (actualRowIndex >= 0 && actualRowIndex < sheet.Rows.Count)
            {
                if (rowHeight != null && double.TryParse(rowHeight, NumberStyles.Float, CultureInfo.InvariantCulture, out var heightPoints))
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

        if (cellRef == null)
        {
            return;
        }

        var address = CellRef.Parse(cellRef.Value);

        var valueElem = cellElem.Element(sNs + "v");
        var formulaElem = cellElem.Element(sNs + "f");

        if (valueElem == null && formulaElem == null)
        {
            return;
        }

        var cellType = (string?)cellElem.Attribute("t") ?? "n";

        // Apply cell style if present
        ApplyCellStyle(cellElem, sheet, address, styleInfo);

        if (formulaElem != null)
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
    }

    private static void ApplyCellStyle(XElement cellElem, Worksheet sheet, CellRef address, StyleInfo styleInfo)
    {
        var styleId = cellElem.Attribute("s")?.Value;
        if (styleId != null &&
            styleInfo.CellStyles.TryGetValue(int.Parse(styleId, CultureInfo.InvariantCulture), out var style))
        {
            var fmt = sheet.Cells[address.Row, address.Column].Format;

            if (styleInfo.FontStyles.TryGetValue(style.FontId, out var fontStyle))
            {
                if (fontStyle.Color != null)
                {
                    fmt.Color = fontStyle.Color;
                }
                fmt.Bold = fontStyle.Bold;
                fmt.Italic = fontStyle.Italic;
                fmt.Underline = fontStyle.Underline;
                fmt.Strikethrough = fontStyle.Strikethrough;
                if (fontStyle.FontFamily != null)
                {
                    fmt.FontFamily = fontStyle.FontFamily;
                }
                if (fontStyle.FontSize != null)
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

                if (formatCode != null && !string.Equals(formatCode, "General", StringComparison.OrdinalIgnoreCase))
                {
                    fmt.NumberFormat = formatCode;
                }
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
        if (autoFilterElement != null)
        {
            var refAttribute = autoFilterElement.Attribute("ref")?.Value;
            if (!string.IsNullOrEmpty(refAttribute))
            {
                var range = RangeRef.Parse(refAttribute);
                sheet.AutoFilter = new AutoFilter(sheet, range);

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

            if (customFiltersElement != null)
            {
                var criterion = DeserializeFilterCriterion(customFiltersElement, actualColumn);
                var sheetFilter = new SheetFilter(criterion, range);
                sheet.AddFilter(sheetFilter);
            }
            else if (filtersElement != null)
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
        if (relsEntry != null)
        {
            using var relsStream = relsEntry.Open();
            var relsDoc = XDocument.Load(relsStream);
            var relsNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
            foreach (var rel in relsDoc.Descendants(relsNs + "Relationship"))
            {
                var id = rel.Attribute("Id")?.Value;
                var target = rel.Attribute("Target")?.Value;
                if (id != null && target != null)
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

            if (cellRef != null && relId != null && relMap.TryGetValue(relId, out var url))
            {
                var address = CellRef.Parse(cellRef);
                if (address.Row < sheet.RowCount && address.Column < sheet.ColumnCount)
                {
                    sheet.Cells[address.Row, address.Column].Hyperlink = new Hyperlink
                    {
                        Url = url,
                        DisplayText = display
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
        if (drawingElement == null)
        {
            return;
        }

        var drawingRelId = drawingElement.Attribute(rNs + "id")?.Value;
        if (drawingRelId == null)
        {
            return;
        }

        // Load sheet relationships to resolve drawing rId
        var sheetFileName = sheetInfo.FullPath.Split('/').Last();
        var relsPath = $"xl/worksheets/_rels/{sheetFileName}.rels";
        var relsEntry = archive.GetEntry(relsPath);
        if (relsEntry == null)
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
                if (id != null && target != null)
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
        if (drawingEntry == null)
        {
            return;
        }

        // Load drawing relationships (for resolving image rIds)
        var drawingFileName = drawingPath.Split('/').Last();
        var drawingDir = drawingPath[..drawingPath.LastIndexOf('/')];
        var drawingRelsPath = $"{drawingDir}/_rels/{drawingFileName}.rels";
        var drawingRelMap = new Dictionary<string, string>();
        var drawingRelsEntry = archive.GetEntry(drawingRelsPath);
        if (drawingRelsEntry != null)
        {
            using var drawingRelsStream = drawingRelsEntry.Open();
            var drawingRelsDoc = XDocument.Load(drawingRelsStream);
            var relsNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");
            foreach (var rel in drawingRelsDoc.Descendants(relsNs + "Relationship"))
            {
                var id = rel.Attribute("Id")?.Value;
                var target = rel.Attribute("Target")?.Value;
                if (id != null && target != null)
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
            ImageAnchorMode mode;
            if (anchor.Name == xdr + "twoCellAnchor")
            {
                mode = ImageAnchorMode.TwoCellAnchor;
            }
            else if (anchor.Name == xdr + "oneCellAnchor")
            {
                mode = ImageAnchorMode.OneCellAnchor;
            }
            else
            {
                continue;
            }

            var pic = anchor.Element(xdr + "pic");
            if (pic == null)
            {
                continue;
            }

            // Parse anchor positions
            var from = ParseCellAnchor(anchor.Element(xdr + "from"), xdr);
            if (from == null)
            {
                continue;
            }

            var image = new SheetImage
            {
                AnchorMode = mode,
                From = from
            };

            if (mode == ImageAnchorMode.TwoCellAnchor)
            {
                var to = ParseCellAnchor(anchor.Element(xdr + "to"), xdr);
                if (to == null)
                {
                    continue;
                }
                image.To = to;
            }
            else
            {
                var ext = anchor.Element(xdr + "ext");
                if (ext != null)
                {
                    if (long.TryParse(ext.Attribute("cx")?.Value, out var cx))
                    {
                        image.Width = cx;
                    }
                    if (long.TryParse(ext.Attribute("cy")?.Value, out var cy))
                    {
                        image.Height = cy;
                    }
                }
            }

            // Get image rId from blip
            var blipFill = pic.Element(xdr + "blipFill");
            var blip = blipFill?.Element(a + "blip");
            var embedId = blip?.Attribute(rNs + "embed")?.Value;
            if (embedId == null || !drawingRelMap.TryGetValue(embedId, out var imageTarget))
            {
                continue;
            }

            // Resolve image path
            var imagePath = ResolvePath(drawingDir + "/", imageTarget);
            var imageEntry = archive.GetEntry(imagePath);
            if (imageEntry == null)
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

    private static CellAnchor? ParseCellAnchor(XElement? element, XNamespace xdr)
    {
        if (element == null)
        {
            return null;
        }

        var col = element.Element(xdr + "col")?.Value;
        var colOff = element.Element(xdr + "colOff")?.Value;
        var row = element.Element(xdr + "row")?.Value;
        var rowOff = element.Element(xdr + "rowOff")?.Value;

        if (col == null || row == null)
        {
            return null;
        }

        return new CellAnchor
        {
            Column = int.Parse(col, System.Globalization.CultureInfo.InvariantCulture),
            ColumnOffset = long.TryParse(colOff, out var co) ? co : 0,
            Row = int.Parse(row, System.Globalization.CultureInfo.InvariantCulture),
            RowOffset = long.TryParse(rowOff, out var ro) ? ro : 0
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

    private class StyleInfo
    {
        public Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline, bool Strikethrough, string? FontFamily, double? FontSize)> FontStyles { get; }
        public Dictionary<int, string> FillColors { get; }
        public Dictionary<int, (int FontId, int FillId, int BorderId, TextAlign TextAlign, VerticalAlign VerticalAlign, bool WrapText, int NumFmtId)> CellStyles { get; }
        public Dictionary<int, string> NumberFormats { get; }
        public Dictionary<int, (BorderStyle? Top, BorderStyle? Right, BorderStyle? Bottom, BorderStyle? Left)> BorderStyles { get; }

        public StyleInfo(
            Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline, bool Strikethrough, string? FontFamily, double? FontSize)> fontStyles,
            Dictionary<int, string> fillColors,
            Dictionary<int, (int FontId, int FillId, int BorderId, TextAlign TextAlign, VerticalAlign VerticalAlign, bool WrapText, int NumFmtId)> cellStyles,
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