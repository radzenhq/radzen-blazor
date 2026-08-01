using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Radzen.Documents.Layout;

internal sealed class SemanticStructureHandles
{
    private readonly InsertionOrderedMap<ListItem, (SemanticNode Label, SemanticNode Body)> listItemElements = new();
    private readonly InsertionOrderedMap<Block, (SemanticNode Label, SemanticNode Body)> listBlockElements = new();
    private readonly InsertionOrderedMap<TocEntry, SemanticNode> tocEntryElements = new();
    private readonly InsertionOrderedMap<Paragraph, SemanticNode> tocParagraphElements = new();
    private readonly InsertionOrderedMap<TocEntry, SemanticNode> tocLinkElements = new();
    private readonly InsertionOrderedMap<Inline, SemanticNode> runLinkElements = new();

    private bool sealedForSnapshot;

    internal bool IsSealed => sealedForSnapshot;

    internal void Seal() => sealedForSnapshot = true;

    internal void SetListItemElements(ListItem item, SemanticNode label, SemanticNode body)
        => Set(listItemElements, item, (label, body));

    internal (SemanticNode Label, SemanticNode Body)? ListItemElements(ListItem item)
        => listItemElements.TryGetValue(item, out var elements) ? elements : null;

    internal void SetListBlockElements(Block block, SemanticNode label, SemanticNode body)
        => Set(listBlockElements, block, (label, body));

    internal void SetTocEntryElement(TocEntry entry, SemanticNode reference)
        => Set(tocEntryElements, entry, reference);

    internal SemanticNode? TocEntryElement(TocEntry entry)
        => tocEntryElements.TryGetValue(entry, out var reference) ? reference : null;

    internal void SetTocParagraphElement(Paragraph paragraph, SemanticNode reference)
        => Set(tocParagraphElements, paragraph, reference);

    internal void SetTocLinkElement(TocEntry entry, SemanticNode link)
        => Set(tocLinkElements, entry, link);

    internal SemanticNode? TocLinkElement(TocEntry entry)
        => tocLinkElements.TryGetValue(entry, out var link) ? link : null;

    internal void SetRunLinkElement(Inline inline, SemanticNode link)
        => Set(runLinkElements, inline, link);

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

    private void Set<TKey, TValue>(InsertionOrderedMap<TKey, TValue> map, TKey key, TValue value)
        where TKey : notnull
    {
        if (sealedForSnapshot)
        {
            throw new InvalidOperationException(
                "Semantic structure handles were sealed for the snapshot; lowering may not register further elements.");
        }

        map.Set(key, value);
    }

    private sealed class InsertionOrderedMap<TKey, TValue>
        where TKey : notnull
    {
        private readonly Dictionary<TKey, int> slotByKey = [];
        private readonly List<KeyValuePair<TKey, TValue>> entries = [];

        internal int Count => entries.Count;

        internal void Set(TKey key, TValue value)
        {
            if (slotByKey.TryGetValue(key, out var slot))
            {
                entries[slot] = new KeyValuePair<TKey, TValue>(key, value);
                return;
            }

            slotByKey.Add(key, entries.Count);
            entries.Add(new KeyValuePair<TKey, TValue>(key, value));
        }

        internal bool TryGetValue(TKey key, out TValue value)
        {
            if (slotByKey.TryGetValue(key, out var slot))
            {
                value = entries[slot].Value;
                return true;
            }

            value = default!;
            return false;
        }

        public List<KeyValuePair<TKey, TValue>>.Enumerator GetEnumerator() => entries.GetEnumerator();
    }
}
