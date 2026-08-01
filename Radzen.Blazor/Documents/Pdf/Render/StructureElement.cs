using System.Collections.Generic;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Pdf.Render;

internal sealed class StructureElement
{
    public required int Id { get; init; }

    public required string Type { get; init; }

    public string? Alt { get; init; }

    public string? ActualText { get; init; }

    public string? Language { get; init; }

    public SemanticHeaderScope HeaderScope { get; init; }

    public int RowSpan { get; init; }

    public int ColumnSpan { get; init; }

    public List<StructureElement> Children { get; } = [];
}
