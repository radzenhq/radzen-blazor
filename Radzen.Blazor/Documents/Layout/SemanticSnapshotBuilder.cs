using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Radzen.Documents.Geometry;

namespace Radzen.Documents.Layout;

internal readonly record struct SemanticCapture(
    SemanticSnapshotBuilder Builder,
    LoweringContext Lowering);

internal sealed class SemanticSnapshotBuilder
{
    private readonly string? language;
    private readonly LoweringContext resolution;
    private readonly LayoutCaptureContext identities;
    private readonly List<Node> nodes = [];
    private readonly List<ResolvedParagraphStyle> paragraphStyles = [];
    private readonly List<SemanticStructureAssociation> associations = [];
    private readonly List<SemanticListOccurrence> lists = [];
    private readonly Mapper mapper;
    private readonly Node document;

    private SemanticSnapshotBuilder(
        Document source,
        LoweringContext resolution,
        LayoutCaptureContext identities)
    {
        language = source.Language;
        this.resolution = resolution;
        this.identities = identities;
        mapper = new Mapper(this);
        document = AddNode(SemanticIntent.Document, SemanticStructureVisibility.Always);

        foreach (var section in source.Sections)
        {
            var sect = AddChild(document, SemanticIntent.Section, SemanticStructureVisibility.Always);
            foreach (var block in section.Blocks)
            {
                MapBlock(block, sect, SemanticStructureVisibility.Always);
            }
        }
    }

    public static SemanticCapture Capture(
        Document source,
        StyleResolution styles,
        LayoutCaptureContext identities)
    {
        var lowering = LoweringContext.CreateForDocument(styles);
        return new SemanticCapture(new SemanticSnapshotBuilder(source, lowering, identities), lowering);
    }

    public DocumentSemantics Snapshot()
    {
        var listBlockElements = resolution.ListBlockElements();
        var tocParagraphElements = resolution.TocParagraphElements();
        var runLinkElements = resolution.RunLinkElements();
        var capturedAssociations = ImmutableArray.CreateBuilder<SemanticStructureAssociation>(
            associations.Count + listBlockElements.Length + tocParagraphElements.Length + runLinkElements.Length);
        capturedAssociations.AddRange(associations);
        foreach (var (block, label, body) in listBlockElements)
        {
            if (label is Node labelNode && body is Node bodyNode)
            {
                var element = bodyNode.Index;
                for (var i = associations.Count - 1; i >= 0; i--)
                {
                    if (associations[i].Source == identities.Source(block))
                    {
                        element = associations[i].Element;
                        break;
                    }
                }

                capturedAssociations.Add(new SemanticStructureAssociation
                {
                    Source = identities.Source(block),
                    Element = element,
                    MarkerElement = labelNode.Index,
                });
            }
        }

        foreach (var (paragraph, reference) in tocParagraphElements)
        {
            if (reference is Node referenceNode)
            {
                capturedAssociations.Add(new SemanticStructureAssociation
                {
                    Source = identities.Source(paragraph),
                    Element = referenceNode.Index,
                });
            }
        }

        foreach (var (run, link) in runLinkElements)
        {
            if (link is Node linkNode)
            {
                capturedAssociations.Add(new SemanticStructureAssociation
                {
                    Source = identities.Source(run),
                    Element = linkNode.Index,
                });
            }
        }

        var capturedNodes = ImmutableArray.CreateBuilder<SemanticStructureNode>(nodes.Count);
        foreach (var node in nodes)
        {
            capturedNodes.Add(new SemanticStructureNode
            {
                Intent = node.Intent,
                ParagraphStyle = node.ParagraphStyle >= 0 ? node.ParagraphStyle : null,
                AlternateText = node.AlternateText,
                ActualText = node.ActualText,
                HeaderScope = node.HeaderScope,
                Visibility = node.Visibility,
                Children = [.. node.Children],
            });
        }

        return new DocumentSemantics
        {
            Language = language,
            Styles = new ResolvedStyleEnvironment
            {
                Paragraphs = [.. paragraphStyles],
            },
            Structure = new SemanticStructureTree
            {
                Nodes = capturedNodes.MoveToImmutable(),
                Associations = capturedAssociations.MoveToImmutable(),
                Lists = [.. lists],
            },
        };
    }

    private void MapBlock(Block block, Node parent, SemanticStructureVisibility visibility)
        => block.Accept(mapper, new MappingContext(parent, visibility));

    private Node AddNode(
        SemanticIntent intent,
        SemanticStructureVisibility visibility,
        int paragraphStyle = -1,
        string? alternateText = null,
        string? actualText = null,
        SemanticHeaderScope headerScope = SemanticHeaderScope.None)
    {
        var node = new Node(nodes.Count, intent, visibility, paragraphStyle, alternateText, actualText, headerScope);
        nodes.Add(node);
        return node;
    }

    private Node AddChild(
        Node parent,
        SemanticIntent intent,
        SemanticStructureVisibility visibility,
        int paragraphStyle = -1,
        string? alternateText = null,
        string? actualText = null,
        SemanticHeaderScope headerScope = SemanticHeaderScope.None)
    {
        var child = AddNode(intent, visibility, paragraphStyle, alternateText, actualText, headerScope);
        parent.Children.Add(child.Index);
        return child;
    }

    private void Associate(object source, Node element)
        => associations.Add(new SemanticStructureAssociation
        {
            Source = identities.Source(source),
            Element = element.Index,
        });

    private (int Index, SemanticIntent Intent) CaptureParagraphStyle(Paragraph paragraph)
    {
        var level = resolution.HeadingLevel(paragraph);
        var intent = level == 0 ? SemanticIntent.Paragraph : SemanticIntent.Heading;
        paragraphStyles.Add(new ResolvedParagraphStyle
        {
            Intent = intent,
            HeadingLevel = level,
            CustomRole = level == 0 ? paragraph.StyleName : null,
            Format = resolution.Format(paragraph),
        });
        return (paragraphStyles.Count - 1, intent);
    }

    private void MapList(List list, Node parent)
    {
        var l = AddChild(parent, SemanticIntent.List, SemanticStructureVisibility.WhenFullyAccessible);
        foreach (var item in list.Items)
        {
            var li = AddChild(l, SemanticIntent.ListItem, SemanticStructureVisibility.WhenFullyAccessible);
            var label = AddChild(li, SemanticIntent.ListLabel, SemanticStructureVisibility.WhenFullyAccessible);
            var body = AddChild(li, SemanticIntent.ListBody, SemanticStructureVisibility.WhenFullyAccessible);
            resolution.SetListItemElements(item, label, body);
            foreach (var block in item.Blocks)
            {
                MapBlock(block, body, SemanticStructureVisibility.WhenFullyAccessible);
            }
        }
    }

    private static SemanticStructureVisibility RequireTagged(SemanticStructureVisibility visibility)
        => visibility == SemanticStructureVisibility.WhenFullyAccessible
            ? SemanticStructureVisibility.WhenFullyAccessible
            : SemanticStructureVisibility.WhenTagged;

    private readonly record struct MappingContext(Node Parent, SemanticStructureVisibility Visibility);

    private sealed class Node(
        int index,
        SemanticIntent intent,
        SemanticStructureVisibility visibility,
        int paragraphStyle,
        string? alternateText,
        string? actualText,
        SemanticHeaderScope headerScope) : IStructureTag
    {
        public int Index { get; } = index;

        public SemanticHeaderScope HeaderScope { get; } = headerScope;

        public SemanticIntent Intent { get; } = intent;

        public SemanticStructureVisibility Visibility { get; } = visibility;

        public int ParagraphStyle { get; } = paragraphStyle;

        public string? AlternateText { get; } = alternateText;

        public string? ActualText { get; } = actualText;

        public List<int> Children { get; } = [];
    }

    private sealed class Mapper(SemanticSnapshotBuilder capture)
        : BlockVisitor<MappingContext, Nothing>
    {
        protected override Nothing Default(Block block, MappingContext context)
            => throw new NotSupportedException(
                $"Block type '{block.GetType().FullName}' is not mapped into the tagged structure tree. "
                + "Add a Visit overload for it to this block visitor so it cannot silently vanish from accessible output.");

        public override Nothing Visit(Paragraph paragraph, MappingContext context)
        {
            var (style, intent) = capture.CaptureParagraphStyle(paragraph);
            var element = capture.AddChild(context.Parent, intent, context.Visibility, paragraphStyle: style);
            capture.Associate(paragraph, element);
            var inlineVisibility = RequireTagged(context.Visibility);
            foreach (var inline in paragraph.Inlines)
            {
                if (inline is InlineImage image)
                {
                    if (!string.IsNullOrEmpty(image.AlternateText))
                    {
                        capture.Associate(
                            image,
                            capture.AddChild(
                                element,
                                SemanticIntent.Figure,
                                inlineVisibility,
                                alternateText: image.AlternateText));
                    }

                    continue;
                }

                if (IsLink(inline))
                {
                    capture.Associate(
                        inline,
                        capture.AddChild(element, SemanticIntent.Link, inlineVisibility));
                }
            }

            return default;
        }

        private static bool IsLink(Run run)
            => run.Link is { Length: > 0 } || run.LinkToAnchor is { Length: > 0 };

        public override Nothing Visit(Table table, MappingContext context)
        {
            var element = capture.AddChild(context.Parent, SemanticIntent.Table, context.Visibility);
            foreach (var row in table.Rows)
            {
                var tr = capture.AddChild(element, SemanticIntent.TableRow, context.Visibility);
                foreach (var cell in row.Cells)
                {
                    var td = capture.AddChild(
                        tr,
                        row.IsHeaderRow ? SemanticIntent.TableHeaderCell : SemanticIntent.TableCell,
                        context.Visibility,
                        headerScope: row.IsHeaderRow ? SemanticHeaderScope.ColumnHeader : SemanticHeaderScope.None);
                    capture.Associate(cell, td);
                    foreach (var child in cell.Blocks)
                    {
                        capture.MapBlock(child, td, RequireTagged(context.Visibility));
                    }
                }
            }

            return default;
        }

        public override Nothing Visit(Image image, MappingContext context)
        {
            var figure = capture.AddChild(
                context.Parent,
                SemanticIntent.Figure,
                context.Visibility,
                alternateText: image.AlternateText,
                actualText: image.ActualText);
            capture.Associate(image, figure);
            return default;
        }

        public override Nothing Visit(List list, MappingContext context)
        {
            capture.lists.Add(new SemanticListOccurrence { Visibility = context.Visibility });
            capture.MapList(list, context.Parent);
            return default;
        }

        public override Nothing Visit(PageBreak block, MappingContext context) => default;

        public override Nothing Visit(Container block, MappingContext context)
        {
            foreach (var child in block.Blocks)
            {
                capture.MapBlock(child, context.Parent, RequireTagged(context.Visibility));
            }

            return default;
        }

        public override Nothing Visit(Barcode block, MappingContext context)
            => MapCode(block, block.AlternateText, context);

        public override Nothing Visit(QrCode block, MappingContext context)
            => MapCode(block, block.AlternateText, context);

        public override Nothing Visit(TableOfContents block, MappingContext context)
        {
            var visibility = RequireTagged(context.Visibility);
            var navigation = capture.AddChild(context.Parent, SemanticIntent.Navigation, visibility);
            foreach (var entry in block.Entries)
            {
                var item = capture.AddChild(navigation, SemanticIntent.NavigationEntry, visibility);
                var reference = capture.AddChild(item, SemanticIntent.CrossReference, visibility);
                capture.resolution.SetTocEntryElement(entry, reference);
                capture.resolution.SetTocLinkElement(
                    entry,
                    capture.AddChild(reference, SemanticIntent.Link, visibility));
            }

            return default;
        }

        private Nothing MapCode(Block block, string? alternateText, MappingContext context)
        {
            if (!string.IsNullOrEmpty(alternateText))
            {
                capture.Associate(
                    block,
                    capture.AddChild(
                        context.Parent,
                        SemanticIntent.Figure,
                        RequireTagged(context.Visibility),
                        alternateText: alternateText));
            }

            return default;
        }
    }
}
