using System.Collections.Generic;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal sealed class SemanticNode(
    int index,
    SemanticIntent intent,
    SemanticStructureTier tier,
    int paragraphStyle,
    string? alternateText,
    string? actualText,
    SemanticHeaderScope headerScope,
    int rowSpan,
    int columnSpan,
    bool decorative)
{
    public int Index { get; } = index;

    public SemanticHeaderScope HeaderScope { get; } = headerScope;

    public int RowSpan { get; } = rowSpan;

    public int ColumnSpan { get; } = columnSpan;

    public bool IsDecorative { get; } = decorative;

    public SemanticIntent Intent { get; } = intent;

    public SemanticStructureTier Tier { get; } = tier;

    public int ParagraphStyle { get; } = paragraphStyle;

    public string? AlternateText { get; } = alternateText;

    public string? ActualText { get; } = actualText;

    public List<int> Children { get; } = [];
}
