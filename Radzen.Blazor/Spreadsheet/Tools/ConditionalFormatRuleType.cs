namespace Radzen.Blazor.Spreadsheet.Tools;

/// <summary>
/// Represents the type of conditional formatting rule in the dialog.
/// </summary>
public enum ConditionalFormatRuleType
{
    /// <summary>Greater than a value.</summary>
    GreaterThan,
    /// <summary>Less than a value.</summary>
    LessThan,
    /// <summary>Between two values.</summary>
    Between,
    /// <summary>Equal to a value.</summary>
    EqualTo,
    /// <summary>Text contains a string.</summary>
    TextContains
}
