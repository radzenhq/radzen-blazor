using System;
using System.Collections.Immutable;

namespace Radzen.Documents.Geometry;

internal enum SemanticStructureTier
{
    Always,
    Structural,
    Assistive,
}

internal enum SemanticArtifactKind
{
    LayoutDecoration,
    Pagination,
    Decorative,
    RepeatedContent,
    Watermark,
}

internal enum SemanticIntent
{
    Document,
    Section,
    Group,
    Paragraph,
    Heading,
    List,
    ListItem,
    ListLabel,
    ListBody,
    Table,
    TableRow,
    TableHeaderCell,
    TableCell,
    Figure,
    Navigation,
    NavigationEntry,
    CrossReference,
    Link,
}

internal enum SemanticHeaderScope
{
    None,
    ColumnHeader,
    RowHeader,
    ColumnAndRowHeader,
}

internal readonly record struct ResolvedParagraphFormat
{
    public required HorizontalAlignment Alignment { get; init; }

    public required Unit SpacingBefore { get; init; }

    public required Unit SpacingAfter { get; init; }

    public required Unit LeftIndent { get; init; }

    public required bool KeepTogether { get; init; }

    public required bool KeepWithNext { get; init; }
}

internal readonly record struct ResolvedParagraphStyle
{
    public required SemanticIntent Intent { get; init; }

    public int HeadingLevel { get; init; }

    public string? CustomRole { get; init; }
}

internal sealed record ResolvedStyleEnvironment
{
    public required ImmutableArray<ResolvedParagraphStyle> Paragraphs { get; init; }
}

internal readonly record struct SemanticStructureNode
{
    public required SemanticIntent Intent { get; init; }

    public int? ParagraphStyle { get; init; }

    public string? AlternateText { get; init; }

    public string? ActualText { get; init; }

    public SemanticHeaderScope HeaderScope { get; init; }

    public int RowSpan { get; init; }

    public int ColumnSpan { get; init; }

    public bool IsDecorative { get; init; }

    public required SemanticStructureTier Tier { get; init; }

    public required ImmutableArray<int> Children { get; init; }
}

internal readonly record struct SemanticStructureAssociation
{
    public required SourceId Source { get; init; }

    public required int Element { get; init; }

    public int? MarkerElement { get; init; }
}

internal readonly record struct SemanticArtifactAssociation
{
    public required SourceId Source { get; init; }

    public required SemanticArtifactKind Kind { get; init; }
}

internal sealed record SemanticStructureTree
{
    public required ImmutableArray<SemanticStructureNode> Nodes { get; init; }

    public required ImmutableArray<SemanticStructureAssociation> Associations { get; init; }

    public required ImmutableArray<SemanticArtifactAssociation> Artifacts { get; init; }
}

internal readonly record struct LaidOutDocumentInfo
{
    public string? Title { get; init; }

    public string? Author { get; init; }

    public string? Subject { get; init; }

    public string? Keywords { get; init; }

    public string? Creator { get; init; }

    public DateTimeOffset? CreationDate { get; init; }

    public DateTimeOffset? ModificationDate { get; init; }

}

internal sealed record DocumentSemantics
{
    public string? Language { get; init; }

    public required ResolvedStyleEnvironment Styles { get; init; }

    public required SemanticStructureTree Structure { get; init; }
}
