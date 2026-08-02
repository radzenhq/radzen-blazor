using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf;

/// <summary>
/// Resource limits applied while reading a PDF, to bound work on malformed or hostile input.
/// A general-purpose reader is fed untrusted files; these caps turn attacker-controlled
/// sizes/depths into a recoverable <see cref="DocumentParseException"/> instead of a hang,
/// out-of-memory, or process-killing stack overflow. All defaults are generous for real
/// documents and configurable via the reading entry points. The inherited
/// <see cref="ResourceLimits"/> caps bound authored input as well, and govern the image decoders
/// used during rendering.
/// </summary>
public sealed class ReaderLimits : ResourceLimits
{
    /// <summary>
    /// Maximum nesting depth for directly-nested arrays and dictionaries during parsing.
    /// Real documents nest a handful of levels inline; deeper structures use indirect
    /// references (bounded separately by cycle detection). Default 512.
    /// </summary>
    public int MaxObjectNestingDepth { get; internal init; } = 512;

    /// <summary>
    /// Backstop depth cap for the page-tree walk (cycle detection is the primary guard).
    /// Default 1024.
    /// </summary>
    public int MaxPageTreeDepth { get; internal init; } = 1024;

    /// <summary>
    /// Maximum total decoded bytes produced by the filter chain of a single stream.
    /// The primary decompression-bomb guard. Default 512 MB.
    /// </summary>
    public long MaxDecodedStreamBytes { get; init; } = 512L * 1024 * 1024;

    /// <summary>Maximum bytes produced when multiple decoded streams are concatenated. Default 512 MB.</summary>
    public long MaxAggregateDecodedBytes { get; init; } = 512L * 1024 * 1024;

    /// <summary>
    /// Maximum expansion ratio (decoded output vs compressed input) for a single stream,
    /// enforced only once decoded output exceeds <see cref="ExpansionRatioFloorBytes"/> so
    /// small legitimate streams are never rejected. Secondary bomb guard. Default 1000.
    /// </summary>
    public int MaxDecodeExpansionRatio { get; internal init; } = 1000;

    /// <summary>
    /// Decoded-size floor below which <see cref="MaxDecodeExpansionRatio"/> is not applied.
    /// Default 16 MB.
    /// </summary>
    public long ExpansionRatioFloorBytes { get; internal init; } = 16L * 1024 * 1024;

    /// <summary>Maximum number of filters that may be chained on a single stream. Default 8.</summary>
    public int MaxFilterChainLength { get; internal init; } = 8;

    /// <summary>Maximum number of cross-reference entries built from an xref (stream or table). Default 8,000,000.</summary>
    public int MaxXrefEntries { get; internal init; } = 8_000_000;

    /// <summary>Maximum number of objects declared by a single object stream (/N). Default 1,000,000.</summary>
    public int MaxObjectStreamCount { get; internal init; } = 1_000_000;

    /// <summary>Maximum number of entries materialized from a /ToUnicode CMap. Default 1,000,000.</summary>
    public int MaxCMapEntries { get; internal init; } = 1_000_000;

    /// <summary>
    /// Maximum number of per-code width entries materialized from a font's width table
    /// (a CID font /W array). Default 1,000,000.
    /// </summary>
    public int MaxFontWidthEntries { get; internal init; } = 1_000_000;

    /// <summary>
    /// Maximum number of Type 2 charstring operations interpreted while reading one glyph of a
    /// CFF font, counted across the glyph's charstring and every subr it calls. Subr calls
    /// multiply: a chain of subrs that each call the next k times performs k^depth operations
    /// while holding one operand and staying inside the nesting limit, so neither the 48-entry
    /// argument stack nor the depth cap bounds it. This is the only cap on that product.
    /// Default 1,000,000.
    /// </summary>
    public int MaxCharstringOperations { get; internal init; } = 1_000_000;

    /// <summary>The default limits used when a caller does not supply their own.</summary>
    public static new ReaderLimits Default => new();

    internal static ReaderLimits From(ResourceLimits limits)
        => limits as ReaderLimits
            ?? new ReaderLimits { MaxImagePixels = limits.MaxImagePixels, MaxFileBytes = limits.MaxFileBytes };

    internal override ReaderLimits Snapshot()
    {
        RequirePositive(MaxObjectNestingDepth, nameof(MaxObjectNestingDepth));
        RequirePositive(MaxPageTreeDepth, nameof(MaxPageTreeDepth));
        RequirePositive(MaxDecodedStreamBytes, nameof(MaxDecodedStreamBytes));
        RequirePositive(MaxAggregateDecodedBytes, nameof(MaxAggregateDecodedBytes));
        RequirePositive(MaxDecodeExpansionRatio, nameof(MaxDecodeExpansionRatio));
        RequirePositive(ExpansionRatioFloorBytes, nameof(ExpansionRatioFloorBytes));
        RequirePositive(MaxFilterChainLength, nameof(MaxFilterChainLength));
        RequirePositive(MaxXrefEntries, nameof(MaxXrefEntries));
        RequirePositive(MaxObjectStreamCount, nameof(MaxObjectStreamCount));
        RequirePositive(MaxCMapEntries, nameof(MaxCMapEntries));
        RequirePositive(MaxFontWidthEntries, nameof(MaxFontWidthEntries));
        RequirePositive(MaxCharstringOperations, nameof(MaxCharstringOperations));
        Validate();

        return new ReaderLimits
        {
            MaxObjectNestingDepth = MaxObjectNestingDepth,
            MaxPageTreeDepth = MaxPageTreeDepth,
            MaxDecodedStreamBytes = MaxDecodedStreamBytes,
            MaxAggregateDecodedBytes = MaxAggregateDecodedBytes,
            MaxDecodeExpansionRatio = MaxDecodeExpansionRatio,
            ExpansionRatioFloorBytes = ExpansionRatioFloorBytes,
            MaxFilterChainLength = MaxFilterChainLength,
            MaxXrefEntries = MaxXrefEntries,
            MaxObjectStreamCount = MaxObjectStreamCount,
            MaxCMapEntries = MaxCMapEntries,
            MaxFontWidthEntries = MaxFontWidthEntries,
            MaxCharstringOperations = MaxCharstringOperations,
            MaxImagePixels = MaxImagePixels,
            MaxFileBytes = MaxFileBytes,
        };
    }
}
