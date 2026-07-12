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
        pages.Add(page);
        return page;
    }

    /// <summary>
    /// Inserts an existing page at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which to insert.</param>
    /// <param name="page">The page to insert.</param>
    public void Insert(int index, Page page)
    {
        System.ArgumentNullException.ThrowIfNull(page);
        pages.Insert(index, page);
    }

    /// <summary>
    /// Removes the page at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the page to remove.</param>
    public void RemoveAt(int index) => pages.RemoveAt(index);

    /// <inheritdoc />
    public IEnumerator<Page> GetEnumerator() => pages.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
