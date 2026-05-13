namespace Radzen.Blazor.Spreadsheet;

#nullable enable

/// <summary>
/// Sentinel result returned from a Spreadsheet dialog when the user engages a
/// range picker icon to pick a range visually from the sheet.
/// </summary>
public sealed record RangePickRequest(string FieldId, string Value);
