using System;
using System.Collections.Immutable;

namespace Radzen.Documents.Geometry;

internal enum SemanticStructureVisibility
{
    Always,
    WhenTagged,
    WhenFullyAccessible,
}

internal enum SemanticIntent
{
    Document,
    Section,
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

    public required ResolvedParagraphFormat Format { get; init; }
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

    public required SemanticStructureVisibility Visibility { get; init; }

    public required ImmutableArray<int> Children { get; init; }
}

internal readonly record struct SemanticStructureAssociation
{
    public required SourceId Source { get; init; }

    public required int Element { get; init; }

    public int? MarkerElement { get; init; }
}

internal readonly record struct SemanticListOccurrence
{
    public required SemanticStructureVisibility Visibility { get; init; }
}

internal sealed record SemanticStructureTree
{
    public required ImmutableArray<SemanticStructureNode> Nodes { get; init; }

    public required ImmutableArray<SemanticStructureAssociation> Associations { get; init; }

    public required ImmutableArray<SemanticListOccurrence> Lists { get; init; }
}

internal readonly record struct CapturedDocumentInfo
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
