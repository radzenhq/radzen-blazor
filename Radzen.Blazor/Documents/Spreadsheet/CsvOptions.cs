using System.Text;

namespace Radzen.Documents.Spreadsheet;

#nullable enable

/// <summary>
/// Controls how CSV writers quote individual fields.
/// </summary>
public enum CsvQuoting
{
    /// <summary>
    /// Quote a field only when it contains the separator, the quote character, or a line break.
    /// This is RFC 4180 behavior and what Excel emits.
    /// </summary>
    Minimal,

    /// <summary>
    /// Always wrap every field in the quote character, regardless of contents.
    /// </summary>
    Always,

    /// <summary>
    /// Never quote fields. The caller is responsible for ensuring values do not contain the
    /// separator or line breaks; otherwise the output is not parseable as CSV.
    /// </summary>
    Never,
}

/// <summary>
/// Options for <see cref="Workbook.SaveAsCsv(System.IO.Stream, CsvExportOptions?)"/>.
/// </summary>
public sealed class CsvExportOptions
{
    /// <summary>The field separator. Defaults to <c>,</c>.</summary>
    public char Separator { get; init; } = ',';

    /// <summary>The character used to quote fields. Defaults to <c>"</c>.</summary>
    public char QuoteChar { get; init; } = '"';

    /// <summary>Quoting strategy. Defaults to <see cref="CsvQuoting.Minimal"/>.</summary>
    public CsvQuoting Quoting { get; init; } = CsvQuoting.Minimal;

    /// <summary>Output encoding. Defaults to UTF-8 with BOM, matching Excel's "Save As CSV".</summary>
    public Encoding Encoding { get; init; } = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);

    /// <summary>Line ending. Defaults to <c>\r\n</c>.</summary>
    public string LineEnding { get; init; } = "\r\n";

    /// <summary>
    /// The sheet to export. When null, the first sheet of the workbook is exported.
    /// </summary>
    public Worksheet? Sheet { get; init; }
}

/// <summary>
/// Options for <see cref="Workbook.LoadFromCsv(System.IO.Stream, CsvImportOptions?)"/>.
/// </summary>
public sealed class CsvImportOptions
{
    /// <summary>The field separator. Defaults to <c>,</c>.</summary>
    public char Separator { get; init; } = ',';

    /// <summary>The character used to quote fields. Defaults to <c>"</c>.</summary>
    public char QuoteChar { get; init; } = '"';

    /// <summary>
    /// Encoding used to decode the input. Byte-order marks at the start of the stream are
    /// auto-detected and consumed regardless of this setting.
    /// </summary>
    public Encoding Encoding { get; init; } = Encoding.UTF8;

    /// <summary>
    /// When true, fields that look like numbers, dates, or booleans are stored as the typed
    /// value. When false, every cell is stored as a string.
    /// </summary>
    public bool ParseValues { get; init; } = true;

    /// <summary>
    /// When true, fields that begin with <c>=</c> are stored as formulas; the cached value is
    /// computed by the formula evaluator. When false, such fields are stored as plain text.
    /// </summary>
    public bool ParseFormulas { get; init; } = true;

    /// <summary>The name assigned to the resulting sheet.</summary>
    public string SheetName { get; init; } = "Sheet1";
}
