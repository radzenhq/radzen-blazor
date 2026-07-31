using System.Collections.Generic;
using System.Collections.Immutable;

namespace Radzen.Documents.Layout;

internal sealed class SemanticStructureHandles
{
    private readonly Dictionary<ListItem, (SemanticNode Label, SemanticNode Body)> listItemElements = [];
    private readonly Dictionary<Block, (SemanticNode Label, SemanticNode Body)> listBlockElements = [];
    private readonly Dictionary<TocEntry, SemanticNode> tocEntryElements = [];
    private readonly Dictionary<Paragraph, SemanticNode> tocParagraphElements = [];
    private readonly Dictionary<TocEntry, SemanticNode> tocLinkElements = [];
    private readonly Dictionary<Inline, SemanticNode> runLinkElements = [];

    internal void SetListItemElements(ListItem item, SemanticNode label, SemanticNode body)
        => listItemElements[item] = (label, body);

    internal (SemanticNode Label, SemanticNode Body)? ListItemElements(ListItem item)
        => listItemElements.TryGetValue(item, out var elements) ? elements : null;

    internal void SetListBlockElements(Block block, SemanticNode label, SemanticNode body)
        => listBlockElements[block] = (label, body);

    internal void SetTocEntryElement(TocEntry entry, SemanticNode reference)
        => tocEntryElements[entry] = reference;

    internal SemanticNode? TocEntryElement(TocEntry entry)
        => tocEntryElements.TryGetValue(entry, out var reference) ? reference : null;

    internal void SetTocParagraphElement(Paragraph paragraph, SemanticNode reference)
        => tocParagraphElements[paragraph] = reference;

    internal void SetTocLinkElement(TocEntry entry, SemanticNode link)
        => tocLinkElements[entry] = link;

    internal SemanticNode? TocLinkElement(TocEntry entry)
        => tocLinkElements.TryGetValue(entry, out var link) ? link : null;

    internal void SetRunLinkElement(Inline inline, SemanticNode link)
        => runLinkElements[inline] = link;

    internal ImmutableArray<(Inline Inline, SemanticNode Link)> RunLinkElements()
    {
        var result = ImmutableArray.CreateBuilder<(Inline, SemanticNode)>(runLinkElements.Count);
        foreach (var (run, link) in runLinkElements)
        {
            result.Add((run, link));
        }

        return result.MoveToImmutable();
    }

    internal ImmutableArray<(Paragraph Paragraph, SemanticNode Reference)> TocParagraphElements()
    {
        var result = ImmutableArray.CreateBuilder<(Paragraph, SemanticNode)>(tocParagraphElements.Count);
        foreach (var (paragraph, reference) in tocParagraphElements)
        {
            result.Add((paragraph, reference));
        }

        return result.MoveToImmutable();
    }

    internal ImmutableArray<(Block Block, SemanticNode Label, SemanticNode Body)> ListBlockElements()
    {
        var result = ImmutableArray.CreateBuilder<(Block, SemanticNode, SemanticNode)>(listBlockElements.Count);
        foreach (var (block, elements) in listBlockElements)
        {
            result.Add((block, elements.Label, elements.Body));
        }

        return result.MoveToImmutable();
    }
}
