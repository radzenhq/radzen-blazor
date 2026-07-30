using System.Collections.Generic;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Pdf.Emit;

internal readonly struct StructureKid
{
    public StructureElement? Child { get; init; }

    public int PageIndex { get; init; }

    public int Mcid { get; init; }
}

internal readonly struct StructureAnnotation
{
    public required int PageIndex { get; init; }

    public required DictionaryObject Annotation { get; init; }

    public required ReferenceObject Reference { get; init; }
}

internal sealed class StructureElement : IStructureTag
{
    public required string Type { get; init; }

    public string? Alt { get; init; }

    public string? ActualText { get; init; }

    public SemanticHeaderScope HeaderScope { get; init; }

    public List<StructureElement> Children { get; } = [];

    public List<(int PageIndex, int Mcid)> Marks { get; } = [];

    public List<StructureKid> Kids { get; } = [];

    public List<StructureAnnotation> Annotations { get; } = [];

    public int KidCursor { get; set; }

    public void AddMark(int pageIndex, int mcid)
    {
        Marks.Add((pageIndex, mcid));
        Kids.Insert(KidCursor, new StructureKid { PageIndex = pageIndex, Mcid = mcid });
        KidCursor++;
    }

    public void AdvancePast(StructureElement child)
    {
        for (var i = 0; i < Kids.Count; i++)
        {
            if (ReferenceEquals(Kids[i].Child, child))
            {
                KidCursor = i + 1;
                return;
            }
        }
    }
}
