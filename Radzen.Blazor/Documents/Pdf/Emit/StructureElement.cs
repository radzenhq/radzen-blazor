using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Emit;

// A node in the logical structure tree emitted for Tagged PDF output. Built from
// the authoring DOM during generation; Marks accumulate the (page, MCID) pairs the
// content emitter assigns, in content-stream order.
internal sealed class StructureElement
{
    public required string Type { get; init; }

    // Accessibility text emitted on the StructElem: /Alt (alternate description) and
    // /ActualText (replacement text). Null leaves the entry off.
    public string? Alt { get; init; }

    public string? ActualText { get; init; }

    public List<StructureElement> Children { get; } = [];

    public List<(int PageIndex, int Mcid)> Marks { get; } = [];
}
