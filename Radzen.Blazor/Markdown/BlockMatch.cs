namespace Radzen.Blazor.Markdown;

/// <summary>
/// Represents the result of matching a block during parsing.
/// </summary>
internal enum BlockMatch
{
    /// <summary>
    /// The block matches.
    /// </summary>
    Match,

    /// <summary>
    /// Skip the block.
    /// </summary>
    Skip,

    /// <summary>
    /// Break block parsing.
    /// </summary>
    Break
}

