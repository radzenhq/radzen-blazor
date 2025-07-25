using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable
/// <summary>
/// Represents a workbook in a spreadsheet.
/// </summary>
public class Workbook
{
    private readonly List<Sheet> sheets = [];

    /// <summary>
    /// Gets the collection of sheets in the workbook.
    /// </summary>
    public IReadOnlyList<Sheet> Sheets => sheets;

    internal Workbook(Sheet sheet)
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
    public Sheet AddSheet(string name, int rows, int columns)
    {
        var sheet = new Sheet(rows, columns)
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
    public void AddSheet(Sheet sheet)
    {
        sheets.Add(sheet);
        sheet.Workbook = this;
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
        public Dictionary<(string? Color, bool Bold, bool Italic, bool Underline), int> FontStyles { get; } = new();
        public Dictionary<string, int> FillStyles { get; } = new();
        public Dictionary<(int FontId, int FillId, TextAlign TextAlign, VerticalAlign VerticalAlign), int> CellStyles { get; } = new();
        public XDocument StylesDocument { get; set; } = null!;
        public XElement FontsElement { get; set; } = null!;
        public XElement FillsElement { get; set; } = null!;
        public XElement CellXfsElement { get; set; } = null!;
    }

    private static void SaveContentTypes(ZipArchive archive)
    {
        var contentTypes = new XDocument(
            new XElement(XName.Get("Types", "http://schemas.openxmlformats.org/package/2006/content-types"),
                new XElement(XName.Get("Default", "http://schemas.openxmlformats.org/package/2006/content-types"),
                    new XAttribute("Extension", "rels"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-package.relationships+xml")),
                new XElement(XName.Get("Default", "http://schemas.openxmlformats.org/package/2006/content-types"),
                    new XAttribute("Extension", "xml"),
                    new XAttribute("ContentType", "application/xml")),
                new XElement(XName.Get("Override", "http://schemas.openxmlformats.org/package/2006/content-types"),
                    new XAttribute("PartName", "/xl/workbook.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml")),
                new XElement(XName.Get("Override", "http://schemas.openxmlformats.org/package/2006/content-types"),
                    new XAttribute("PartName", "/xl/sharedStrings.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml")),
                new XElement(XName.Get("Override", "http://schemas.openxmlformats.org/package/2006/content-types"),
                    new XAttribute("PartName", "/xl/styles.xml"),
                    new XAttribute("ContentType", "application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"))));

        using var entry = archive.CreateEntry("[Content_Types].xml").Open();
        contentTypes.Save(entry);
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
            SaveSheet(archive, sheet, sheetName, sheetId, relId, styleTracker, sharedStrings, sharedStringsDoc);
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

    private void SaveSheet(ZipArchive archive, Sheet sheet, string sheetName, int sheetId, string relId, StyleTracker styleTracker, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc)
    {
        var sheetDoc = CreateSheetDocument(sheet, sheetId, relId);
        var sheetData = sheetDoc.Root!.Element(XName.Get("sheetData", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))!;

        // Process all rows and cells
        ProcessSheetData(sheet, sheetData, styleTracker, sharedStrings, sharedStringsDoc);

        // Add merged cells
        AddMergedCells(sheet, sheetDoc);

        // Add auto filter
        AddAutoFilter(sheet, sheetDoc);

        // Save sheet in xl/worksheets/ subdirectory
        using var entry = archive.CreateEntry($"xl/worksheets/{sheetName}").Open();
        sheetDoc.Save(entry);
    }

    private static XDocument CreateSheetDocument(Sheet sheet, int sheetId, string relId)
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

    private static XElement CreateSheetProperties(Sheet sheet)
    {
        return new XElement(XName.Get("sheetPr", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            sheet.Filters.Count > 0 ? new XAttribute("filterMode", "1") : null);
    }

    private static XElement CreateDimension(Sheet sheet)
    {
        return new XElement(XName.Get("dimension", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("ref", $"A1:{new CellRef(sheet.RowCount - 1, sheet.ColumnCount - 1)}"));
    }

    private static XElement CreateSheetViews(Sheet sheet)
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

    private static XElement CreateColumns(Sheet sheet)
    {
        return new XElement(XName.Get("cols", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            Enumerable.Range(0, sheet.ColumnCount).Select(col => new XElement(XName.Get("col", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("min", col + 1),
                new XAttribute("max", col + 1),
                new XAttribute("width", Math.Round(sheet.Columns[col] / 7.0, 8)),
                new XAttribute("customWidth", "1"))));
    }

    private void ProcessSheetData(Sheet sheet, XElement sheetData, StyleTracker styleTracker, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc)
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

    private static XElement CreateRowElement(Sheet sheet, int row)
    {
        var rowElement = new XElement(XName.Get("row", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("r", row + 1),
            new XAttribute("ht", sheet.Rows[row]),
            new XAttribute("customHeight", "1"));

        // Add hidden attribute if row is hidden
        if (sheet.Rows.IsHidden(row))
        {
            rowElement.Add(new XAttribute("hidden", "1"));
        }

        return rowElement;
    }

    private XElement CreateCellElement(Sheet sheet, int row, int col, Cell cell, StyleTracker styleTracker, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc)
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
               cell.Format.TextAlign != TextAlign.Left ||
               cell.Format.VerticalAlign != VerticalAlign.Top;
    }

    private int GetOrCreateCellStyle(Cell cell, StyleTracker styleTracker)
    {
        var fontId = GetOrCreateFontStyle(cell, styleTracker);
        var fillId = GetOrCreateFillStyle(cell, styleTracker);

        var styleKey = (fontId, fillId, cell.Format.TextAlign, cell.Format.VerticalAlign);

        if (!styleTracker.CellStyles.TryGetValue(styleKey, out int styleId))
        {
            styleId = styleTracker.CellStyles.Count + 1;
            styleTracker.CellStyles[styleKey] = styleId;
            CreateCellStyleElement(cell, fontId, fillId, styleId, styleTracker);
        }

        return styleId;
    }

    private int GetOrCreateFontStyle(Cell cell, StyleTracker styleTracker)
    {
        var fontKey = (cell.Format.Color, cell.Format.Bold, cell.Format.Italic, cell.Format.Underline);

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

    private void CreateFontElement(Cell cell, int fontId, StyleTracker styleTracker)
    {
        var fontElement = new XElement(XName.Get("font", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XElement(XName.Get("sz", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("val", "11")),
            new XElement(XName.Get("name", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("val", "Aptos Narrow")));

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

        styleTracker.FontsElement.Add(fontElement);
        styleTracker.FontsElement.Attribute("count")!.Value = (styleTracker.FontStyles.Count + 1).ToString();
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
        styleTracker.FillsElement.Attribute("count")!.Value = (styleTracker.FillStyles.Count + 2).ToString();
    }

    /// <summary>
    /// Creates a cell style element in the styles document.
    /// </summary>
    private void CreateCellStyleElement(Cell cell, int fontId, int fillId, int styleId, StyleTracker styleTracker)
    {
        var xfElement = new XElement(XName.Get("xf", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("numFmtId", "0"),
            new XAttribute("fontId", fontId.ToString()),
            new XAttribute("fillId", fillId.ToString()),
            new XAttribute("borderId", "0"),
            new XAttribute("xfId", "0"),
            new XAttribute("applyFont", fontId > 0 ? "1" : "0"),
            new XAttribute("applyFill", fillId > 0 ? "1" : "0"));

        // Add alignment if not default
        if (cell.Format.TextAlign != TextAlign.Left || cell.Format.VerticalAlign != VerticalAlign.Top)
        {
            var alignmentElement = CreateAlignmentElement(cell);
            xfElement.Add(alignmentElement);
            xfElement.Add(new XAttribute("applyAlignment", "1"));
        }

        styleTracker.CellXfsElement.Add(xfElement);
        styleTracker.CellXfsElement.Attribute("count")!.Value = (styleTracker.CellStyles.Count + 1).ToString();
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

        return alignmentElement;
    }

    private static void AddCellValue(XElement cellElement, Cell cell, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc)
    {
        if (!string.IsNullOrEmpty(cell.Formula))
        {
            cellElement.Add(new XAttribute("t", "str"));
            var formulaValue = cell.Formula.StartsWith("=") ? cell.Formula[1..] : cell.Formula;
            cellElement.Add(new XElement(XName.Get("f", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), formulaValue));
            cellElement.Add(new XElement(XName.Get("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), cell.GetValue()));
        }
        else
        {
            switch (cell.ValueType)
            {
                case CellValueType.Number:
                    cellElement.Add(new XAttribute("t", "n"));
                    cellElement.Add(new XElement(XName.Get("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), cell.GetValue()));
                    break;

                case CellValueType.String:
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

                case CellValueType.Error:
                    cellElement.Add(new XAttribute("t", "e"));
                    cellElement.Add(new XElement(XName.Get("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), cell.GetValue()));
                    break;

                case CellValueType.Empty:
                    break;
            }
        }
    }

    private static void AddMergedCells(Sheet sheet, XDocument sheetDoc)
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

    private static void AddAutoFilter(Sheet sheet, XDocument sheetDoc)
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
            "equal" => value == "" ? new IsNullCriterion { Column = columnIndex } : new EqualToCriterion { Column = columnIndex, Value = value },
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
        var fontStyles = new Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline)>();
        var fillColors = new Dictionary<int, string>();
        var cellStyles = new Dictionary<int, (int FontId, int FillId, TextAlign TextAlign, VerticalAlign VerticalAlign)>(0);

        var stylesEntry = archive.GetEntry("xl/styles.xml");
        if (stylesEntry != null)
        {
            using var s = stylesEntry.Open();
            var stylesDoc = XDocument.Load(s);
            var stylesNs = stylesDoc.Root!.Name.Namespace;

            ParseFonts(stylesDoc, stylesNs, fontStyles);
            ParseFills(stylesDoc, stylesNs, fillColors);
            ParseCellStyles(stylesDoc, stylesNs, fontStyles, fillColors, cellStyles);
        }

        return new StyleInfo(fontStyles, fillColors, cellStyles);
    }

    private static void ParseFonts(XDocument stylesDoc, XNamespace stylesNs, Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline)> fontStyles)
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
            fontStyles[i] = (colorValue, bold, italic, underline);
        }
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

    private static void ParseCellStyles(XDocument stylesDoc, XNamespace stylesNs, 
        Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline)> fontStyles,
        Dictionary<int, string> fillColors,
        Dictionary<int, (int FontId, int FillId, TextAlign TextAlign, VerticalAlign VerticalAlign)> cellStyles)
    {
        var cellXfs = stylesDoc.Descendants(stylesNs + "cellXfs").FirstOrDefault()?.Elements(stylesNs + "xf").ToList() ?? [];
        for (var i = 0; i < cellXfs.Count; i++)
        {
            var fontId = cellXfs[i].Attribute("fontId")?.Value;
            var fillId = cellXfs[i].Attribute("fillId")?.Value;
            var applyFont = cellXfs[i].Attribute("applyFont")?.Value;
            var applyFill = cellXfs[i].Attribute("applyFill")?.Value;
            var applyAlignment = cellXfs[i].Attribute("applyAlignment")?.Value;

            if (fontId != null && fillId != null)
            {
                // Only include styles that are actually applied
                var fontIdValue = int.Parse(fontId);
                var fillIdValue = int.Parse(fillId);

                var (textAlign, verticalAlign) = ParseAlignment(cellXfs[i], stylesNs, applyAlignment);

                if (applyFont == "1" && fontStyles.ContainsKey(fontIdValue) ||
                    applyFill == "1" && fillColors.ContainsKey(fillIdValue))
                {
                    cellStyles[i] = (fontIdValue, fillIdValue, textAlign, verticalAlign);
                }
            }
        }
    }

    private static (TextAlign TextAlign, VerticalAlign VerticalAlign) ParseAlignment(XElement cellXf, XNamespace stylesNs, string? applyAlignment)
    {
        var textAlign = TextAlign.Left;
        var verticalAlign = VerticalAlign.Top;
        
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
            }
        }

        return (textAlign, verticalAlign);
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

    private static List<SheetInfo> ParseSheetDefinitions(ZipArchive archive)
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
            SheetId = sheet.Attribute("sheetId")!.Value,
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
                r => r.Attribute("Target")!.Value.Replace("\\", "/")
            );

        return sheets.Select(sheet => new SheetInfo(sheet.Name, sheet.SheetId, sheet.RelId, relMap))
            .Where(sheet => sheet.HasValidPath)
            .ToList();
    }

    private static Sheet LoadSheet(ZipArchive archive, SheetInfo sheetInfo, StyleInfo styleInfo, List<string> sharedStrings)
    {
        var sheet = new Sheet(100, 100); // adjust size as needed

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

    private static void ParseFrozenPanes(XDocument sheetDoc, XNamespace sNs, Sheet sheet)
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

    private static void ParseColumnWidths(XDocument sheetDoc, XNamespace sNs, Sheet sheet)
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

    private static void ParseRowsAndCells(XDocument sheetDoc, XNamespace sNs, Sheet sheet, StyleInfo styleInfo, List<string> sharedStrings, double defaultRowHeight)
    {
        foreach (var rowElem in sheetDoc.Descendants(sNs + "row"))
        {
            ParseRow(rowElem, sNs, sheet, styleInfo, sharedStrings, defaultRowHeight);
        }
    }

    private static void ParseRow(XElement rowElem, XNamespace sNs, Sheet sheet, StyleInfo styleInfo, List<string> sharedStrings, double defaultRowHeight)
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

    private static void ParseCell(XElement cellElem, XNamespace sNs, Sheet sheet, StyleInfo styleInfo, List<string> sharedStrings)
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
            if (!formulaValue.StartsWith("="))
            {
                formulaValue = "=" + formulaValue;
            }
            sheet.Cells[address.Row, address.Column].Formula = formulaValue;
        }
        else
        {
            var value = cellType switch
            {
                "s" => sharedStrings[Convert.ToInt32(valueElem!.Value)],
                _ => valueElem!.Value
            };

            sheet.Cells[address.Row, address.Column].Value = value;
        }
    }

    private static void ApplyCellStyle(XElement cellElem, Sheet sheet, CellRef address, StyleInfo styleInfo)
    {
        var styleId = cellElem.Attribute("s")?.Value;
        if (styleId != null && styleInfo.CellStyles.TryGetValue(int.Parse(styleId), out var style))
        {
            if (styleInfo.FontStyles.TryGetValue(style.FontId, out var fontStyle))
            {
                if (fontStyle.Color != null)
                {
                    sheet.Cells[address.Row, address.Column].Format.Color = fontStyle.Color;
                }
                sheet.Cells[address.Row, address.Column].Format.Bold = fontStyle.Bold;
                sheet.Cells[address.Row, address.Column].Format.Italic = fontStyle.Italic;
                sheet.Cells[address.Row, address.Column].Format.Underline = fontStyle.Underline;
            }
            sheet.Cells[address.Row, address.Column].Format.TextAlign = style.TextAlign;
            sheet.Cells[address.Row, address.Column].Format.VerticalAlign = style.VerticalAlign;
            if (styleInfo.FillColors.TryGetValue(style.FillId, out var fillColor))
            {
                sheet.Cells[address.Row, address.Column].Format.BackgroundColor = fillColor;
            }
        }
    }

    private static void ParseMergedCells(XDocument sheetDoc, XNamespace sNs, Sheet sheet)
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

    private static void ParseAutoFilter(XDocument sheetDoc, XNamespace sNs, Sheet sheet)
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

    private static void ParseFilterColumn(XElement filterColumn, XNamespace sNs, RangeRef range, Sheet sheet)
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

    private class StyleInfo
    {
        public Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline)> FontStyles { get; }
        public Dictionary<int, string> FillColors { get; }
        public Dictionary<int, (int FontId, int FillId, TextAlign TextAlign, VerticalAlign VerticalAlign)> CellStyles { get; }

        public StyleInfo(
            Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline)> fontStyles,
            Dictionary<int, string> fillColors,
            Dictionary<int, (int FontId, int FillId, TextAlign TextAlign, VerticalAlign VerticalAlign)> cellStyles)
        {
            FontStyles = fontStyles;
            FillColors = fillColors;
            CellStyles = cellStyles;
        }
    }

    private class SheetInfo
    {
        public string Name { get; }
        public string SheetId { get; }
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

        public SheetInfo(string name, string sheetId, string relId, Dictionary<string, string> relMap)
        {
            Name = name;
            SheetId = sheetId;
            RelId = relId;
            RelMap = relMap;
        }
    }

    private static void UpdateAndSaveSharedStrings(ZipArchive archive, Dictionary<string, int> sharedStrings, XDocument sharedStringsDoc)
    {
        var sstElement = sharedStringsDoc.Root!;
        sstElement.Attribute("count")!.Value = sharedStrings.Count.ToString();
        sstElement.Attribute("uniqueCount")!.Value = sharedStrings.Count.ToString();

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