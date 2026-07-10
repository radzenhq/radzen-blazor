using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// An ordered, read-only view of the text runs in a paragraph.
/// </summary>
public class InlineCollection : IReadOnlyList<Run>
{
    private readonly List<Run> items = [];

    /// <inheritdoc/>
    public int Count => items.Count;

    /// <inheritdoc/>
    public Run this[int index] => items[index];

    /// <summary>
    /// Appends a new run with the specified text.
    /// </summary>
    /// <param name="text">The run text.</param>
    /// <returns>The newly created run.</returns>
    public Run Add(string text)
    {
        var run = new Run(text);
        items.Add(run);
        return run;
    }

    /// <summary>
    /// Appends an existing run.
    /// </summary>
    /// <param name="run">The run to append.</param>
    /// <returns>The same <paramref name="run"/> instance.</returns>
    public Run Add(Run run)
    {
        items.Add(run);
        return run;
    }

    internal void Clear() => items.Clear();

    /// <inheritdoc/>
    public IEnumerator<Run> GetEnumerator() => items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
