#nullable enable
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

// A node in the logical structure tree emitted for Tagged PDF output. Built from
// the authoring DOM during generation; Marks accumulate the (page, MCID) pairs the
// content emitter assigns, in content-stream order.
internal sealed class StructureElement
{
    public required string Type { get; init; }

    public List<StructureElement> Children { get; } = [];

    public List<(int PageIndex, int Mcid)> Marks { get; } = [];
}
