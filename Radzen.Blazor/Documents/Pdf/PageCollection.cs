using System;
using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// The ordered collection of <see cref="Page"/> instances in a
/// <see cref="Document"/>.
/// </summary>
public sealed class PageCollection : IReadOnlyList<Page>
{
    private readonly List<Page> pages = [];
    private readonly Document? owner;

    /// <summary>Initializes an independent page collection.</summary>
    public PageCollection()
    {
    }

    internal PageCollection(Document owner) => this.owner = owner;

    /// <summary>Gets the number of pages in the collection.</summary>
    public int Count => pages.Count;

    /// <summary>Gets the page at the specified index.</summary>
    /// <param name="index">The zero-based page index.</param>
    /// <returns>The page at <paramref name="index"/>.</returns>
    public Page this[int index] => pages[index];

    /// <summary>
    /// Adds a new A4 portrait page to the end of the collection.
    /// </summary>
    /// <returns>The newly added page.</returns>
    public Page Add() => Add(PageSizes.A4, PageOrientation.Portrait);

    /// <summary>
    /// Adds a new portrait page of the given size to the end of the collection.
    /// </summary>
    /// <param name="size">The page size.</param>
    /// <returns>The newly added page.</returns>
    public Page Add(PageSize size) => Add(size, PageOrientation.Portrait);

    /// <summary>
    /// Adds a new page of the given size and orientation to the end of the
    /// collection. Landscape orientation swaps the width and height.
    /// </summary>
    /// <param name="size">The page size.</param>
    /// <param name="orientation">The page orientation.</param>
    /// <returns>The newly added page.</returns>
    public Page Add(PageSize size, PageOrientation orientation)
    {
        var (width, height) = orientation == PageOrientation.Landscape
            ? (size.Height, size.Width)
            : (size.Width, size.Height);

        var page = new Page(width, height);
        Insert(pages.Count, page);
        return page;
    }

    /// <summary>
    /// Inserts an existing page at the specified index. A page taken from another
    /// document keeps its source content and resources; the other document is left
    /// unchanged and keeps the page too.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert.</param>
    /// <param name="page">The page to insert.</param>
    public void Insert(int index, Page page)
    {
        ArgumentNullException.ThrowIfNull(page);
        pages.Insert(index, page);
        Adopt(page);
    }

    // A page from another document keeps its source node, contents and resources in the
    // donor's LoadedState, which this document's save path cannot see; carry those entries
    // over, or the page emits no /Resources for a content stream that still names them.
    private void Adopt(Page page)
    {
        if (owner is null || ReferenceEquals(page.Owner, owner))
        {
            return;
        }

        if (page.Owner is { } donor)
        {
            owner.CarryForeignPage(page, donor);
        }
        else
        {
            page.Owner = owner;
        }
    }

    /// <summary>
    /// Removes the page at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the page to remove.</param>
    public void RemoveAt(int index) => pages.RemoveAt(index);

    /// <summary>Moves a page from one index to another.</summary>
    /// <param name="from">The zero-based source index.</param>
    /// <param name="to">The zero-based destination index in the resulting collection.</param>
    public void Move(int from, int to)
    {
        var page = pages[from];
        pages.RemoveAt(from);
        try
        {
            pages.Insert(to, page);
        }
        catch
        {
            pages.Insert(from, page);
            throw;
        }
    }

    /// <summary>Removes a contiguous range of pages.</summary>
    /// <param name="index">The zero-based index of the first page to remove.</param>
    /// <param name="count">The number of pages to remove.</param>
    public void RemoveRange(int index, int count) => pages.RemoveRange(index, count);

    /// <summary>Creates a new document containing deep copies of the selected pages.</summary>
    /// <param name="range">The page range to extract.</param>
    /// <returns>A new document containing the selected pages.</returns>
    public Document ExtractPages(Range range)
    {
        if (owner is null)
        {
            throw new InvalidOperationException("Pages can be extracted only from a collection owned by a Document.");
        }

        var result = new Document();
        result.ImportPages(owner, range);
        return result;
    }

    /// <summary>
    /// Splits the document at zero-based page boundaries. Each boundary starts a new
    /// document; boundaries must be strictly increasing and lie between 1 and Count - 1.
    /// </summary>
    /// <param name="boundaries">The page indexes at which new documents start.</param>
    /// <returns>The documents in original page order.</returns>
    public IReadOnlyList<Document> Split(params int[] boundaries)
    {
        ArgumentNullException.ThrowIfNull(boundaries);
        if (owner is null)
        {
            throw new InvalidOperationException("Pages can be split only from a collection owned by a Document.");
        }

        var previous = 0;
        foreach (var boundary in boundaries)
        {
            if (boundary <= previous || boundary >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(boundaries), boundary, "Split boundaries must be strictly increasing and between 1 and Count - 1.");
            }

            previous = boundary;
        }

        var snapshot = PageOperations.Snapshot(owner);
        var result = new List<Document>(boundaries.Length + 1);
        previous = 0;
        foreach (var boundary in boundaries)
        {
            result.Add(PageOperations.Extract(snapshot, previous, boundary - previous));
            previous = boundary;
        }

        result.Add(PageOperations.Extract(snapshot, previous, Count - previous));
        return result;
    }

    /// <inheritdoc />
    public IEnumerator<Page> GetEnumerator() => pages.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
