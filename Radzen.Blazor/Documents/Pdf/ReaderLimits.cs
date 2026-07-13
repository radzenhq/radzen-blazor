namespace Radzen.Documents.Pdf;

/// <summary>
/// Resource limits applied while reading a PDF, to bound work on malformed or hostile input.
/// A general-purpose reader is fed untrusted files; these caps turn attacker-controlled
/// sizes/depths into a recoverable <see cref="Objects.DocumentParseException"/> instead of a hang,
/// out-of-memory, or process-killing stack overflow. All defaults are generous for real
/// documents and configurable via the reading entry points.
/// </summary>
public sealed class ReaderLimits
{
    /// <summary>
    /// Maximum nesting depth for directly-nested arrays and dictionaries during parsing.
    /// Real documents nest a handful of levels inline; deeper structures use indirect
    /// references (bounded separately by cycle detection). Default 512.
    /// </summary>
    public int MaxObjectNestingDepth { get; set; } = 512;

    /// <summary>
    /// Backstop depth cap for the page-tree walk (cycle detection is the primary guard).
    /// Default 1024.
    /// </summary>
    public int MaxPageTreeDepth { get; set; } = 1024;

    /// <summary>
    /// Maximum total decoded bytes produced by the filter chain of a single stream.
    /// The primary decompression-bomb guard. Default 512 MB.
    /// </summary>
    public long MaxDecodedStreamBytes { get; set; } = 512L * 1024 * 1024;

    /// <summary>
    /// Maximum expansion ratio (decoded output vs compressed input) for a single stream,
    /// enforced only once decoded output exceeds <see cref="ExpansionRatioFloorBytes"/> so
    /// small legitimate streams are never rejected. Secondary bomb guard. Default 1000.
    /// </summary>
    public int MaxDecodeExpansionRatio { get; set; } = 1000;

    /// <summary>
    /// Decoded-size floor below which <see cref="MaxDecodeExpansionRatio"/> is not applied.
    /// Default 16 MB.
    /// </summary>
    public long ExpansionRatioFloorBytes { get; set; } = 16L * 1024 * 1024;

    /// <summary>Maximum number of filters that may be chained on a single stream. Default 8.</summary>
    public int MaxFilterChainLength { get; set; } = 8;

    /// <summary>Maximum number of cross-reference entries built from an xref (stream or table). Default 8,000,000.</summary>
    public int MaxXrefEntries { get; set; } = 8_000_000;

    /// <summary>Maximum number of objects declared by a single object stream (/N). Default 1,000,000.</summary>
    public int MaxObjectStreamCount { get; set; } = 1_000_000;

    /// <summary>Maximum number of entries materialized from a /ToUnicode CMap. Default 1,000,000.</summary>
    public int MaxCMapEntries { get; set; } = 1_000_000;

    /// <summary>Maximum decoded image size in pixels (width * height). Default 64M (e.g. 8000 x 8000).</summary>
    public long MaxImagePixels { get; set; } = 64L * 1024 * 1024;

    /// <summary>Maximum size in bytes of a source file buffered while loading. Default 2 GiB.</summary>
    public long MaxFileBytes { get; set; } = 2L * 1024 * 1024 * 1024;

    /// <summary>The default limits used when a caller does not supply their own.</summary>
    public static ReaderLimits Default { get; } = new();
}
