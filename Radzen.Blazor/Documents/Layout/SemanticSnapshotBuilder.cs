using System.Collections.Generic;
using System.Collections.Immutable;
using System;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal readonly record struct SemanticCapture(
    SemanticSnapshotBuilder Builder,
    LoweringResult Lowering);

internal sealed class SemanticSnapshotBuilder
{
    private readonly string? language;
    private readonly LoweringResult resolution;
    private readonly LayoutCaptureContext identities;
    private readonly List<SemanticNode> nodes = [];
    private readonly List<ResolvedParagraphStyle> paragraphStyles = [];
    private readonly List<SemanticStructureAssociation> associations = [];
    private readonly List<(object Source, SemanticArtifactKind Kind)> artifactSources = [];
    private readonly Mapper mapper;
    private readonly SemanticNode document;

    private SemanticSnapshotBuilder(
        Document source,
        LoweringResult resolution,
        LayoutCaptureContext identities)
    {
        language = source.Language;
        this.resolution = resolution;
        this.identities = identities;
        mapper = new Mapper(this);
        document = AddNode(SemanticIntent.Document, SemanticStructureTier.Always);

        foreach (var section in source.Sections)
        {
            var sect = AddChild(document, SemanticIntent.Section, SemanticStructureTier.Always);
            foreach (var block in section.Blocks)
            {
                MapBlock(block, sect, SemanticStructureTier.Always);
            }

            CaptureArtifacts(section.Header.Blocks, SemanticArtifactKind.Pagination);
            CaptureArtifacts(section.Footer.Blocks, SemanticArtifactKind.Pagination);
        }
    }

    public static SemanticCapture Capture(
        Document source,
        StyleResolution styles,
        LayoutCaptureContext identities)
    {
        var lowering = LoweringResult.CreateForDocument(styles);
        return new SemanticCapture(new SemanticSnapshotBuilder(source, lowering, identities), lowering);
    }

    public DocumentSemantics Snapshot()
    {
        var listBlockElements = resolution.Semantics.ListBlockElements();
        var tocParagraphElements = resolution.Semantics.TocParagraphElements();
        var runLinkElements = resolution.Semantics.RunLinkElements();
        var capturedAssociations = ImmutableArray.CreateBuilder<SemanticStructureAssociation>(
            associations.Count + listBlockElements.Length + tocParagraphElements.Length + runLinkElements.Length);
        capturedAssociations.AddRange(associations);
        var lastElementBySource = new Dictionary<SourceId, int>();
        foreach (var association in associations)
        {
            lastElementBySource[association.Source] = association.Element;
        }

        foreach (var (block, label, body) in listBlockElements)
        {
            var source = identities.Source(block);
            var element = lastElementBySource.TryGetValue(source, out var associated)
                ? associated
                : body.Index;

            capturedAssociations.Add(new SemanticStructureAssociation
            {
                Source = source,
                Element = element,
                MarkerElement = label.Index,
            });
        }

        foreach (var (paragraph, reference) in tocParagraphElements)
        {
            capturedAssociations.Add(new SemanticStructureAssociation
            {
                Source = identities.Source(paragraph),
                Element = reference.Index,
            });
        }

        foreach (var (run, link) in runLinkElements)
        {
            capturedAssociations.Add(new SemanticStructureAssociation
            {
                Source = identities.Source(run),
                Element = link.Index,
            });
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
                RowSpan = node.RowSpan,
                ColumnSpan = node.ColumnSpan,
                IsDecorative = node.IsDecorative,
                Tier = node.Tier,
                Children = [.. node.Children],
            });
        }

        var capturedArtifacts = ImmutableArray.CreateBuilder<SemanticArtifactAssociation>(artifactSources.Count);
        foreach (var (source, kind) in artifactSources)
        {
            capturedArtifacts.Add(new SemanticArtifactAssociation
            {
                Source = identities.Source(source),
                Kind = kind,
            });
        }

        foreach (var association in capturedAssociations)
        {
            if (nodes[association.Element].IsDecorative)
            {
                capturedArtifacts.Add(new SemanticArtifactAssociation
                {
                    Source = association.Source,
                    Kind = SemanticArtifactKind.Decorative,
                });
            }
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
                Artifacts = capturedArtifacts.ToImmutable(),
            },
        };
    }

    private void MapBlock(Block block, SemanticNode parent, SemanticStructureTier tier)
        => block.Accept(mapper, new MappingContext(parent, tier));

    private SemanticNode AddNode(
        SemanticIntent intent,
        SemanticStructureTier tier,
        int paragraphStyle = -1,
        string? alternateText = null,
        string? actualText = null,
        SemanticHeaderScope headerScope = SemanticHeaderScope.None,
        int rowSpan = 1,
        int columnSpan = 1,
        bool decorative = false)
    {
        var node = new SemanticNode(
            nodes.Count, intent, tier, paragraphStyle, alternateText, actualText,
            headerScope, rowSpan, columnSpan, decorative);
        nodes.Add(node);
        return node;
    }

    private SemanticNode AddChild(
        SemanticNode parent,
        SemanticIntent intent,
        SemanticStructureTier tier,
        int paragraphStyle = -1,
        string? alternateText = null,
        string? actualText = null,
        SemanticHeaderScope headerScope = SemanticHeaderScope.None,
        int rowSpan = 1,
        int columnSpan = 1,
        bool decorative = false)
    {
        var child = AddNode(
            intent, tier, paragraphStyle, alternateText, actualText, headerScope, rowSpan, columnSpan, decorative);
        parent.Children.Add(child.Index);
        return child;
    }

    private void Associate(object source, SemanticNode element, SemanticNode? link = null)
        => associations.Add(new SemanticStructureAssociation
        {
            Source = identities.Source(source),
            Element = element.Index,
            LinkElement = link?.Index,
        });

    private void AssociateArtifact(object source, SemanticArtifactKind kind)
        => artifactSources.Add((source, kind));

    private void CaptureArtifacts(IEnumerable<Block> blocks, SemanticArtifactKind kind)
    {
        foreach (var block in blocks)
        {
            CaptureArtifact(block, kind);
        }
    }

    private void CaptureArtifact(Block block, SemanticArtifactKind kind)
    {
        AssociateArtifact(block, kind);
        switch (block)
        {
            case Paragraph paragraph:
                foreach (var inline in paragraph.Inlines)
                {
                    AssociateArtifact(inline, kind);
                }

                break;
            case Table table:
                foreach (var placed in resolution.TablePlacement(table).Cells)
                {
                    AssociateArtifact(placed.Cell, kind);
                    CaptureArtifacts(placed.Cell.Blocks, kind);
                }

                break;
            case List list:
                foreach (var item in list.Items)
                {
                    CaptureArtifacts(item.Blocks, kind);
                }

                break;
            case Container container:
                CaptureArtifacts(container.Blocks, kind);
                break;
        }
    }

    private (int Index, SemanticIntent Intent) CaptureParagraphStyle(Paragraph paragraph)
    {
        var level = resolution.HeadingLevel(paragraph);
        var declaredRole = resolution.Role(paragraph);
        var intent = level == 0 ? SemanticIntent.Paragraph : SemanticIntent.Heading;
        paragraphStyles.Add(new ResolvedParagraphStyle
        {
            Intent = intent,
            HeadingLevel = level,
            CustomRole = declaredRole ?? (level == 0 ? paragraph.StyleName : null),
            RoleIsDeclared = declaredRole is not null,
        });
        return (paragraphStyles.Count - 1, intent);
    }

    private void MapList(List list, SemanticNode parent)
    {
        var l = AddChild(parent, SemanticIntent.List, SemanticStructureTier.Structural);
        foreach (var item in list.Items)
        {
            var li = AddChild(l, SemanticIntent.ListItem, SemanticStructureTier.Structural);
            var label = AddChild(li, SemanticIntent.ListLabel, SemanticStructureTier.Structural);
            var body = AddChild(li, SemanticIntent.ListBody, SemanticStructureTier.Structural);
            resolution.Semantics.SetListItemElements(item, label, body);
            foreach (var block in item.Blocks)
            {
                MapBlock(block, body, SemanticStructureTier.Structural);
            }
        }
    }

    private readonly record struct MappingContext(SemanticNode Parent, SemanticStructureTier Tier);

    private sealed class Mapper(SemanticSnapshotBuilder capture)
        : BlockVisitor<MappingContext, Nothing>
    {
        protected override Nothing Default(Block block, MappingContext context)
            => throw LoweredBlockDispatch.Unsupported(block);

        public override Nothing Visit(Paragraph paragraph, MappingContext context)
        {
            var (style, intent) = capture.CaptureParagraphStyle(paragraph);
            var element = capture.AddChild(context.Parent, intent, context.Tier, paragraphStyle: style);
            capture.Associate(paragraph, element);
            var inlineTier = SemanticStructureTier.Structural;
            foreach (var inline in paragraph.Inlines)
            {
                var linked = IsLink(inline);
                if (inline is InlineImage image)
                {
                    var link = linked
                        ? capture.AddChild(element, SemanticIntent.Link, inlineTier)
                        : null;
                    var decorative = IsDecorative(image.AlternateText);
                    capture.Associate(
                        image,
                        capture.AddChild(
                            link ?? element,
                            SemanticIntent.Figure,
                            inlineTier,
                            alternateText: decorative || string.IsNullOrEmpty(image.AlternateText) ? null : image.AlternateText,
                            actualText: decorative || string.IsNullOrEmpty(image.ActualText) ? null : image.ActualText,
                            decorative: decorative),
                        link);
                    continue;
                }

                if (linked)
                {
                    capture.Associate(
                        inline,
                        capture.AddChild(element, SemanticIntent.Link, inlineTier));
                }
            }

            return default;
        }

        private static SemanticHeaderScope HeaderScope(bool headerRow, bool headerColumn)
            => (headerRow, headerColumn) switch
            {
                (true, true) => SemanticHeaderScope.ColumnAndRowHeader,
                (true, false) => SemanticHeaderScope.ColumnHeader,
                (false, true) => SemanticHeaderScope.RowHeader,
                _ => SemanticHeaderScope.None,
            };

        private static bool IsLink(Inline inline)
            => inline.Link is { Length: > 0 } || inline.LinkToAnchor is { Length: > 0 };

        public override Nothing Visit(Table table, MappingContext context)
        {
            var element = capture.AddChild(context.Parent, SemanticIntent.Table, context.Tier);
            var placement = capture.resolution.TablePlacement(table);
            for (var row = 0; row < table.Rows.Count; row++)
            {
                var tr = capture.AddChild(element, SemanticIntent.TableRow, context.Tier);
                foreach (var placed in placement.Cells)
                {
                    if (placed.Row != row)
                    {
                        continue;
                    }

                    var scope = HeaderScope(
                        table.Rows[row].IsHeaderRow,
                        placed.Column < table.Columns.Count && table.Columns[placed.Column].IsHeaderColumn);
                    var td = capture.AddChild(
                        tr,
                        scope == SemanticHeaderScope.None
                            ? SemanticIntent.TableCell
                            : SemanticIntent.TableHeaderCell,
                        context.Tier,
                        headerScope: scope,
                        rowSpan: placed.RowSpan,
                        columnSpan: placed.ColumnSpan);
                    capture.Associate(placed.Cell, td);
                    foreach (var child in placed.Cell.Blocks)
                    {
                        capture.MapBlock(child, td, SemanticStructureTier.Structural);
                    }
                }
            }

            return default;
        }

        public override Nothing Visit(Image image, MappingContext context)
        {
            var decorative = IsDecorative(image.AlternateText);
            var figure = capture.AddChild(
                context.Parent,
                SemanticIntent.Figure,
                context.Tier,
                alternateText: decorative || string.IsNullOrEmpty(image.AlternateText) ? null : image.AlternateText,
                actualText: decorative || string.IsNullOrEmpty(image.ActualText) ? null : image.ActualText,
                decorative: decorative);
            capture.Associate(image, figure);
            return default;
        }

        public override Nothing Visit(List list, MappingContext context)
        {
            capture.MapList(list, context.Parent);
            return default;
        }

        public override Nothing Visit(PageBreak block, MappingContext context) => default;

        public override Nothing Visit(Container block, MappingContext context)
        {
            var tier = SemanticStructureTier.Structural;
            var group = capture.AddChild(context.Parent, SemanticIntent.Group, tier);
            foreach (var child in block.Blocks)
            {
                capture.MapBlock(child, group, tier);
            }

            return default;
        }

        public override Nothing Visit(Barcode block, MappingContext context)
            => MapCode(block, block.AlternateText, context);

        public override Nothing Visit(QrCode block, MappingContext context)
            => MapCode(block, block.AlternateText, context);

        public override Nothing Visit(TableOfContents block, MappingContext context)
        {
            var tier = SemanticStructureTier.Structural;
            var navigation = capture.AddChild(context.Parent, SemanticIntent.Navigation, tier);
            foreach (var entry in block.Entries)
            {
                var item = capture.AddChild(navigation, SemanticIntent.NavigationEntry, tier);
                var reference = capture.AddChild(item, SemanticIntent.CrossReference, tier);
                capture.resolution.Semantics.SetTocEntryElement(entry, reference);
                capture.resolution.Semantics.SetTocLinkElement(
                    entry,
                    capture.AddChild(reference, SemanticIntent.Link, tier));
            }

            return default;
        }

        private static bool IsDecorative(string? alternateText) => alternateText is { Length: 0 };

        private Nothing MapCode(Block block, string? alternateText, MappingContext context)
        {
            var decorative = IsDecorative(alternateText);
            capture.Associate(
                block,
                capture.AddChild(
                    context.Parent,
                    SemanticIntent.Figure,
                    SemanticStructureTier.Structural,
                    alternateText: decorative || string.IsNullOrEmpty(alternateText) ? null : alternateText,
                    decorative: decorative));
            return default;
        }
    }
}
