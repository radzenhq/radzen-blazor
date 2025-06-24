using System;
using System.Collections.Generic;
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

        // Track unique styles
        var fontStyles = new Dictionary<(string? Color, bool Bold, bool Italic, bool Underline), int>();
        var fillStyles = new Dictionary<string, int>();
        var cellStyles = new Dictionary<(int FontId, int FillId, TextAlign TextAlign, VerticalAlign VerticalAlign), int>();

        // Create styles.xml
        var stylesDoc = new XDocument(
            new XElement(XName.Get("styleSheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XElement(XName.Get("fonts", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("count", "1"),
                    // Default font
                    new XElement(XName.Get("font", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                        new XElement(XName.Get("sz", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                            new XAttribute("val", "11")),
                        new XElement(XName.Get("name", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                            new XAttribute("val", "Aptos Narrow"))))));

        // Add fills section
        var fillsElement = new XElement(XName.Get("fills", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("count", "2"),
            // Default fills
            new XElement(XName.Get("fill", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XElement(XName.Get("patternFill", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("patternType", "none"))),
            new XElement(XName.Get("fill", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XElement(XName.Get("patternFill", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("patternType", "gray125"))));

        stylesDoc.Root!.Add(fillsElement);

        // Add borders section
        stylesDoc.Root.Add(new XElement(XName.Get("borders", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("count", "1"),
            new XElement(XName.Get("border", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XElement(XName.Get("left", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")),
                new XElement(XName.Get("right", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")),
                new XElement(XName.Get("top", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")),
                new XElement(XName.Get("bottom", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")),
                new XElement(XName.Get("diagonal", "http://schemas.openxmlformats.org/spreadsheetml/2006/main")))));

        // Add cellStyleXfs section
        stylesDoc.Root.Add(new XElement(XName.Get("cellStyleXfs", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("count", "1"),
            new XElement(XName.Get("xf", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("numFmtId", "0"),
                new XAttribute("fontId", "0"),
                new XAttribute("fillId", "0"),
                new XAttribute("borderId", "0"))));

        // Add cellXfs section
        var cellXfsElement = new XElement(XName.Get("cellXfs", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("count", "1"),
            // Default style
            new XElement(XName.Get("xf", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("numFmtId", "0"),
                new XAttribute("fontId", "0"),
                new XAttribute("fillId", "0"),
                new XAttribute("borderId", "0"),
                new XAttribute("xfId", "0")));

        stylesDoc.Root.Add(cellXfsElement);

        // Add cellStyles section
        stylesDoc.Root.Add(new XElement(XName.Get("cellStyles", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
            new XAttribute("count", "1"),
            new XElement(XName.Get("cellStyle", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("name", "Normal"),
                new XAttribute("xfId", "0"),
                new XAttribute("builtinId", "0"))));

        // 1. Create [Content_Types].xml
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

        using (var entry = archive.CreateEntry("[Content_Types].xml").Open())
        {
            contentTypes.Save(entry);
        }

        // 2. Create _rels/.rels
        var rels = new XDocument(
            new XElement(XName.Get("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships"),
                new XElement(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships"),
                    new XAttribute("Id", "rId1"),
                    new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument"),
                    new XAttribute("Target", "xl/workbook.xml"))));

        using (var entry = archive.CreateEntry("_rels/.rels").Open())
        {
            rels.Save(entry);
        }

        // 3. Create xl/workbook.xml
        var workbook = new XDocument(
            new XElement(XName.Get("workbook", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XElement(XName.Get("sheets", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))));

        var sheetsElement = workbook.Root!.Element(XName.Get("sheets", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))!;

        // 4. Create xl/_rels/workbook.xml.rels
        var workbookRels = new XDocument(
            new XElement(XName.Get("Relationships", "http://schemas.openxmlformats.org/package/2006/relationships")));

        var workbookRelsElement = workbookRels.Root!;

        // Add shared strings relationship
        workbookRelsElement.Add(new XElement(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships"),
            new XAttribute("Id", "rId1"),
            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings"),
            new XAttribute("Target", "sharedStrings.xml")));

        // Add styles relationship
        workbookRelsElement.Add(new XElement(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships"),
            new XAttribute("Id", "rId2"),
            new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles"),
            new XAttribute("Target", "styles.xml")));

        // 5. Create shared strings
        var sharedStrings = new Dictionary<string, int>();
        var sharedStringsDoc = new XDocument(
            new XElement(XName.Get("sst", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("count", "0"),
                new XAttribute("uniqueCount", "0")));

        var sstElement = sharedStringsDoc.Root!;

        // 6. Process each sheet
        for (var i = 0; i < sheets.Count; i++)
        {
            var sheet = sheets[i];
            var sheetId = i + 1;
            var sheetName = $"sheet{sheetId}.xml";
            var relId = $"rId{sheetId + 2}"; // +2 because we have sharedStrings and styles relationships

            // Add sheet to workbook.xml
            sheetsElement.Add(new XElement(XName.Get("sheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                new XAttribute("name", sheet.Name),
                new XAttribute("sheetId", sheetId),
                new XAttribute(XName.Get("id", "http://schemas.openxmlformats.org/officeDocument/2006/relationships"), relId)));

            // Add relationship
            workbookRelsElement.Add(new XElement(XName.Get("Relationship", "http://schemas.openxmlformats.org/package/2006/relationships"),
                new XAttribute("Id", relId),
                new XAttribute("Type", "http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet"),
                new XAttribute("Target", sheetName)));

            // Create sheet XML
            var sheetDoc = new XDocument(
                new XElement(XName.Get("worksheet", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XElement(XName.Get("dimension", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                        new XAttribute("ref", $"A1:{new CellRef(sheet.RowCount - 1, sheet.ColumnCount - 1)}")),
                    new XElement(XName.Get("sheetData", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))));

            var sheetData = sheetDoc.Root!.Element(XName.Get("sheetData", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))!;

            // Process cells
            for (var row = 0; row < sheet.RowCount; row++)
            {
                var rowElement = new XElement(XName.Get("row", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("r", row + 1));

                for (var col = 0; col < sheet.ColumnCount; col++)
                {
                    var cell = sheet.Cells[row, col];
                    var value = cell.GetValue();

                    if (value == null)
                    {
                        continue;
                    }

                    var cellElement = new XElement(XName.Get("c", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                        new XAttribute("r", new CellRef(row, col).ToString()));

                    // Add style reference if cell has formatting
                    if (cell.Format.Color != null || cell.Format.BackgroundColor != null || cell.Format.Bold || cell.Format.Italic || cell.Format.Underline || cell.Format.TextAlign != TextAlign.Left || cell.Format.VerticalAlign != VerticalAlign.Top)
                    {
                        var fontKey = (cell.Format.Color, cell.Format.Bold, cell.Format.Italic, cell.Format.Underline);
                        var fontId = 0;
                        if (!fontStyles.TryGetValue(fontKey, out fontId))
                        {
                            fontId = fontStyles.Count + 1;
                            fontStyles[fontKey] = fontId;
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
                            stylesDoc.Root!.Element(XName.Get("fonts", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))!.Add(fontElement);
                            stylesDoc.Root!.Element(XName.Get("fonts", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"))!.Attribute("count")!.Value = (fontStyles.Count + 1).ToString();
                        }

                        var fillId = 0;
                        if (cell.Format.BackgroundColor != null)
                        {
                            if (!fillStyles.TryGetValue(cell.Format.BackgroundColor, out fillId))
                            {
                                fillId = fillStyles.Count + 2; // Start from 2 as 0 and 1 are reserved
                                fillStyles[cell.Format.BackgroundColor] = fillId;
                                fillsElement.Add(new XElement(XName.Get("fill", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                                    new XElement(XName.Get("patternFill", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                                        new XAttribute("patternType", "solid"),
                                        new XElement(XName.Get("fgColor", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                                            new XAttribute("rgb", cell.Format.BackgroundColor.ToXLSXColor())),
                                        new XElement(XName.Get("bgColor", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                                            new XAttribute("indexed", "64")))));
                                fillsElement.Attribute("count")!.Value = (fillStyles.Count + 2).ToString();
                            }
                        }

                        var styleKey = (fontId, fillId, cell.Format.TextAlign, cell.Format.VerticalAlign);
                        if (!cellStyles.TryGetValue(styleKey, out int styleId))
                        {
                            styleId = cellStyles.Count + 1;
                            cellStyles[styleKey] = styleId;
                            var xfElement = new XElement(XName.Get("xf", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                                new XAttribute("numFmtId", "0"),
                                new XAttribute("fontId", fontId.ToString()),
                                new XAttribute("fillId", fillId.ToString()),
                                new XAttribute("borderId", "0"),
                                new XAttribute("xfId", "0"),
                                new XAttribute("applyFont", fontId > 0 ? "1" : "0"),
                                new XAttribute("applyFill", fillId > 0 ? "1" : "0"));

                            // Add alignment if not default (left-aligned and top-aligned)
                            if (cell.Format.TextAlign != TextAlign.Left || cell.Format.VerticalAlign != VerticalAlign.Top)
                            {
                                var horizontalAlign = cell.Format.TextAlign switch
                                {
                                    TextAlign.Center => "center",
                                    TextAlign.Right => "right",
                                    TextAlign.Justify => "justify",
                                    _ => "left"
                                };
                                var verticalAlign = cell.Format.VerticalAlign switch
                                {
                                    VerticalAlign.Middle => "center",
                                    VerticalAlign.Bottom => "bottom",
                                    _ => "top"
                                };
                                
                                var alignmentElement = new XElement(XName.Get("alignment", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"));
                                if (cell.Format.TextAlign != TextAlign.Left)
                                {
                                    alignmentElement.Add(new XAttribute("horizontal", horizontalAlign));
                                }
                                if (cell.Format.VerticalAlign != VerticalAlign.Top)
                                {
                                    alignmentElement.Add(new XAttribute("vertical", verticalAlign));
                                }
                                xfElement.Add(alignmentElement);
                                xfElement.Add(new XAttribute("applyAlignment", "1"));
                            }

                            cellXfsElement.Add(xfElement);
                            cellXfsElement.Attribute("count")!.Value = (cellStyles.Count + 1).ToString();
                        }

                        cellElement.Add(new XAttribute("s", styleId));
                    }

                    if (!string.IsNullOrEmpty(cell.Formula))
                    {
                        cellElement.Add(new XAttribute("t", "str"));
                        var formulaValue = cell.Formula.StartsWith("=") ? cell.Formula[1..] : cell.Formula;
                        cellElement.Add(new XElement(XName.Get("f", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), formulaValue));
                        cellElement.Add(new XElement(XName.Get("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), value));
                    }
                    else
                    {
                        switch (cell.ValueType)
                        {
                            case CellValueType.Number:
                                cellElement.Add(new XAttribute("t", "n"));
                                cellElement.Add(new XElement(XName.Get("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), value));
                                break;

                            case CellValueType.String:
                                var strValue = cell.GetValue() ?? string.Empty;
                                if (!sharedStrings.TryGetValue(strValue, out var index))
                                {
                                    index = sharedStrings.Count;
                                    sharedStrings[strValue] = index;
                                    sstElement.Add(new XElement(XName.Get("si", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                                        new XElement(XName.Get("t", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), strValue)));
                                }

                                cellElement.Add(new XAttribute("t", "s"));
                                cellElement.Add(new XElement(XName.Get("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), index));
                                break;

                            case CellValueType.Error:
                                cellElement.Add(new XAttribute("t", "e"));
                                cellElement.Add(new XElement(XName.Get("v", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"), value));
                                break;

                            case CellValueType.Empty:
                                break;
                        }
                    }

                    rowElement.Add(cellElement);
                }

                if (rowElement.HasElements)
                {
                    sheetData.Add(rowElement);
                }
            }

            // Add merged cells
            if (sheet.MergedCells.Ranges.Count > 0)
            {
                var mergeCells = new XElement(XName.Get("mergeCells", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                    new XAttribute("count", sheet.MergedCells.Ranges.Count));

                foreach (var range in sheet.MergedCells.Ranges)
                {
                    mergeCells.Add(new XElement(XName.Get("mergeCell", "http://schemas.openxmlformats.org/spreadsheetml/2006/main"),
                        new XAttribute("ref", range.ToString())));
                }

                sheetDoc.Root.Add(mergeCells);
            }

            // Save sheet
            using var entry = archive.CreateEntry($"xl/{sheetName}").Open();
            sheetDoc.Save(entry);
        }

        // Update shared strings count
        sstElement.Attribute("count")!.Value = sharedStrings.Count.ToString();
        sstElement.Attribute("uniqueCount")!.Value = sharedStrings.Count.ToString();

        // Save shared strings
        using (var entry = archive.CreateEntry("xl/sharedStrings.xml").Open())
        {
            sharedStringsDoc.Save(entry);
        }

        // Save workbook.xml
        using (var entry = archive.CreateEntry("xl/workbook.xml").Open())
        {
            workbook.Save(entry);
        }

        // Save workbook.xml.rels
        using (var entry = archive.CreateEntry("xl/_rels/workbook.xml.rels").Open())
        {
            workbookRels.Save(entry);
        }

        // Save styles.xml
        using (var entry = archive.CreateEntry("xl/styles.xml").Open())
        {
            stylesDoc.Save(entry);
        }
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
        var fontStyles = new Dictionary<int, (string? Color, bool Bold, bool Italic, bool Underline)>();
        var fillColors = new Dictionary<int, string>();
        var cellStyles = new Dictionary<int, (int FontId, int FillId, TextAlign TextAlign, VerticalAlign VerticalAlign)>(0);

        var stylesEntry = archive.GetEntry("xl/styles.xml");
        if (stylesEntry != null)
        {
            using var s = stylesEntry.Open();
            var stylesDoc = XDocument.Load(s);
            var stylesNs = stylesDoc.Root!.Name.Namespace;

            // Parse fonts
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

            // Parse fills
            var fills = stylesDoc.Descendants(stylesNs + "fill").ToList();
            for (var i = 0; i < fills.Count; i++)
            {
                var fgColor = fills[i].Element(stylesNs + "patternFill")?.Element(stylesNs + "fgColor")?.Attribute("rgb")?.Value;
                if (fgColor != null)
                {
                    fillColors[i] = "#" + fgColor[2..]; // Convert from "FFFF0000" to "#FF0000"
                }
            }

            // Parse cell styles
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

                    var textAlign = TextAlign.Left;
                    var verticalAlign = VerticalAlign.Top;
                    if (applyAlignment == "1")
                    {
                        var alignment = cellXfs[i].Element(stylesNs + "alignment");
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

                    if (applyFont == "1" && fontStyles.ContainsKey(fontIdValue) ||
                        applyFill == "1" && fillColors.ContainsKey(fillIdValue))
                    {
                        cellStyles[i] = (fontIdValue, fillIdValue, textAlign, verticalAlign);
                    }
                }
            }
        }

        // 1. Parse shared strings (used in all sheets)
        var sharedStrings = new List<string>();
        var sharedEntry = archive.GetEntry("xl/sharedStrings.xml");

        if (sharedEntry != null)
        {
            using var s = sharedEntry.Open();
            var doc = XDocument.Load(s);
            sharedStrings = doc.Descendants().Where(e => e.Name.LocalName == "t").Select(e => e.Value).ToList();
        }

        // 2. Parse sheet definitions (sheet names + target file paths)
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

        // 3. Parse workbook relationships to resolve actual .xml file for each sheet
        var relsEntry = archive.GetEntry("xl/_rels/workbook.xml.rels") ?? throw new InvalidDataException("workbook.xml.rels not found");
        using var relsStream = relsEntry.Open();
        var relsDoc = XDocument.Load(relsStream);
        var relsNs = XNamespace.Get("http://schemas.openxmlformats.org/package/2006/relationships");

        var relMap = relsDoc.Descendants(relsNs + "Relationship")
            .ToDictionary(
                r => r.Attribute("Id")!.Value,
                r => r.Attribute("Target")!.Value.Replace("\\", "/")
            );

        // 4. Load each sheet
        foreach (var sheetInfo in sheets)
        {
            if (!relMap.TryGetValue(sheetInfo.RelId, out var sheetPath))
            {
                continue;
            }

            var fullPath = $"xl/{sheetPath.TrimStart('/')}"; // Excel paths are relative

            var sheetEntry = archive.GetEntry(fullPath);

            if (sheetEntry == null)
            {
                continue;
            }

            var sheet = new Sheet(100, 100); // adjust size as needed

            using var sheetStream = sheetEntry.Open();
            var sheetDoc = XDocument.Load(sheetStream);
            var sNs = sheetDoc.Root!.Name.Namespace;

            foreach (var rowElem in sheetDoc.Descendants(sNs + "row"))
            {
                foreach (var cellElem in rowElem.Elements(sNs + "c"))
                {
                    var cellRef = cellElem.Attribute("r")!;

                    if (cellRef == null)
                    {
                        continue;
                    }

                    var address = CellRef.Parse(cellRef.Value);

                    var valueElem = cellElem.Element(sNs + "v");
                    var formulaElem = cellElem.Element(sNs + "f");

                    if (valueElem == null && formulaElem == null)
                    {
                        continue;
                    }

                    var cellType = (string?)cellElem.Attribute("t") ?? "n";

                    // Apply cell style if present
                    var styleId = cellElem.Attribute("s")?.Value;
                    if (styleId != null && cellStyles.TryGetValue(int.Parse(styleId), out var style))
                    {
                        if (fontStyles.TryGetValue(style.FontId, out var fontStyle))
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
                        if (fillColors.TryGetValue(style.FillId, out var fillColor))
                        {
                            sheet.Cells[address.Row, address.Column].Format.BackgroundColor = fillColor;
                        }
                    }

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
            }

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

            sheet.Name = sheetInfo.Name;

            workbook.AddSheet(sheet);
        }

        return workbook;
    }
}