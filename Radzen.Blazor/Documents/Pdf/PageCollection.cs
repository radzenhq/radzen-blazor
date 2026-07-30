using System;
using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// The ordered collection of <see cref="Page"/> instances in a
/// <see cref="PortableDocument"/>.
/// </summary>
public sealed class PageCollection : IReadOnlyList<Page>
{
    private readonly List<Page> pages = [];
    private readonly PortableDocument owner;

    internal PageCollection(PortableDocument owner) => this.owner = owner;

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
        var (width, height) = size.Effective(orientation);

        var page = new Page(width, height);
        Insert(pages.Count, page);
        return page;
    }

    /// <summary>
    /// Adds an existing page to the end of the collection. Adding a page that belongs to
    /// another document moves it: the page keeps its source content and resources and this
    /// document becomes its owner, so the receiving document's conformance and font settings
    /// govern it from then on. The other document must have released the page first
    /// (<see cref="RemoveAt"/> or <see cref="RemoveRange"/>); a page cannot belong to two
    /// documents at once. Use <see cref="PortableDocument.ImportPage"/> to copy a page instead
    /// of moving it.
    /// </summary>
    /// <param name="page">The page to add.</param>
    /// <returns>The added page.</returns>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="page"/> is still listed in another document's pages.
    /// </exception>
    public Page Add(Page page)
    {
        Insert(pages.Count, page);
        return page;
    }

    /// <summary>
    /// Inserts an existing page at the specified index. Inserting a page that
    /// belongs to another document moves it: the page keeps its source content and
    /// resources and this document becomes its owner, so the receiving document's
    /// conformance and font settings govern it from then on. The other document must
    /// have released the page first (<see cref="RemoveAt"/> or <see cref="RemoveRange"/>);
    /// a page cannot belong to two documents at once. Use
    /// <see cref="PortableDocument.ImportPage"/> to copy a page instead of moving it.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert.</param>
    /// <param name="page">The page to insert.</param>
    /// <exception cref="InvalidOperationException">
    /// <paramref name="page"/> is still listed in another document's pages.
    /// </exception>
    public void Insert(int index, Page page)
    {
        ArgumentNullException.ThrowIfNull(page);
        RequireReleasedByDonor(page);
        owner.InvalidateMaterializedGraph();
        pages.Insert(index, page);
        Adopt(page);
    }

    internal bool Holds(Page page) => pages.Contains(page);

    private void RequireReleasedByDonor(Page page)
    {
        if (page.Owner is not { } donor || ReferenceEquals(donor, owner) || !donor.Pages.Holds(page))
        {
            return;
        }

        throw new InvalidOperationException(
            "The page still belongs to another document. Inserting a page from another document moves it, and a page "
            + "cannot belong to two documents at once because each one applies its own conformance, font and content "
            + "settings to it. Remove the page from its current document first, or use PortableDocument.ImportPage to "
            + "insert a copy and leave the original in place.");
    }

    private void Adopt(Page page)
    {
        if (ReferenceEquals(page.Owner, owner))
        {
            return;
        }

        if (page.Owner is { } donor)
        {
            owner.CarryForeignPage(page, donor);
        }

        page.Owner = owner;
    }

    /// <summary>
    /// Removes the page at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the page to remove.</param>
    public void RemoveAt(int index)
    {
        owner.InvalidateMaterializedGraph();
        pages.RemoveAt(index);
    }

    /// <summary>Moves a page from one index to another.</summary>
    /// <param name="from">The zero-based source index.</param>
    /// <param name="to">The zero-based destination index in the resulting collection.</param>
    public void Move(int from, int to)
    {
        owner.InvalidateMaterializedGraph();
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
    public void RemoveRange(int index, int count)
    {
        owner.InvalidateMaterializedGraph();
        pages.RemoveRange(index, count);
    }

    /// <summary>Creates a new document containing deep copies of the selected pages.</summary>
    /// <param name="range">The page range to extract.</param>
    /// <returns>A new document containing the selected pages.</returns>
    public PortableDocument ExtractPages(Range range)
    {
        var result = new PortableDocument();
        result.ImportPages(owner, range);
        return result;
    }

    /// <summary>
    /// Splits the document at zero-based page boundaries. Each boundary starts a new
    /// document; boundaries must be strictly increasing and lie between 1 and Count - 1.
    /// </summary>
    /// <param name="boundaries">The page indexes at which new documents start.</param>
    /// <returns>The documents in original page order.</returns>
    public IReadOnlyList<PortableDocument> Split(params int[] boundaries)
    {
        ArgumentNullException.ThrowIfNull(boundaries);
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
        var result = new List<PortableDocument>(boundaries.Length + 1);
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
