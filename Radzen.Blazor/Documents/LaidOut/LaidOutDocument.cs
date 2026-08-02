using System.Collections.Immutable;
using Radzen.Documents.Fonts;

namespace Radzen.Documents.LaidOut;

internal sealed class LaidOutDocument
{
    public required FontCollectionSnapshot Fonts { get; init; }

    public required ImmutableArray<LaidOutPage> Pages { get; init; }

    public required DocumentSemantics Semantics { get; init; }

    public required LaidOutDocumentInfo Info { get; init; }
}
