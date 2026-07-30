using System.Collections.Immutable;

namespace Radzen.Documents.Geometry;

internal readonly struct LaidOutBoxContent
{
    public required double Height { get; init; }

    public required ImmutableArray<LaidOutLine> Lines { get; init; }

    public required ImmutableArray<LaidOutImage> Images { get; init; }

    public required ImmutableArray<LaidOutCodeSymbol> CodeSymbols { get; init; }

    public required ImmutableArray<LaidOutTablePlacement> Tables { get; init; }

    public required ImmutableArray<LaidOutBox> Boxes { get; init; }
}
