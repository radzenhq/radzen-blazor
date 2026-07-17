using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

internal sealed class StructureElement
{
    public required string Type { get; init; }

    public string? Alt { get; init; }

    public string? ActualText { get; init; }

    public List<StructureElement> Children { get; } = [];

    public List<(int PageIndex, int Mcid)> Marks { get; } = [];
}
