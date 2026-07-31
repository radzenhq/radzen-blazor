using System.Collections.Immutable;

namespace Radzen.Documents.LaidOut;

internal interface ILaidOutContent<TTable>
{
    ImmutableArray<LaidOutLine> Lines { get; }

    ImmutableArray<LaidOutImage> Images { get; }

    ImmutableArray<LaidOutCodeSymbol> CodeSymbols { get; }

    ImmutableArray<TTable> Tables { get; }

    ImmutableArray<LaidOutBox> Boxes { get; }
}
