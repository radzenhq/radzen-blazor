using System.Collections.Immutable;
using Radzen.Documents.Fonts;

namespace Radzen.Documents.Geometry;

internal sealed class LaidOutDocument
{
    public required FontCollectionSnapshot Fonts { get; init; }

    public required ImmutableArray<PaginatedPage> Pages { get; init; }

    public required DocumentSemantics Semantics { get; init; }

    public required CapturedDocumentInfo Info { get; init; }
}
