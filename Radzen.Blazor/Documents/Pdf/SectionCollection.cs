using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// An ordered, read-only view of the sections in a document. Use <see cref="Add"/> to append a section.
/// </summary>
public class SectionCollection : IReadOnlyList<Section>
{
    private readonly List<Section> items = [];

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public Section this[int index] => items[index];

    /// <summary>
    /// Appends a new section to the document.
    /// </summary>
    /// <returns>The newly created section.</returns>
    public Section Add()
    {
        var section = new Section
        {
            HeaderDistance = Unit.FromCentimeter(1.25),
            FooterDistance = Unit.FromCentimeter(1.25),
        };
        items.Add(section);
        return section;
    }

    /// <inheritdoc/>
    public IEnumerator<Section> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
