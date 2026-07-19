using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

/// <summary>Stores the modeled annotations of a page in paint order.</summary>
public sealed class AnnotationCollection : IReadOnlyList<Annotation>
{
    private readonly TrackedList<Entry> entries = [];
    private bool loaded;
    private bool rewriteImported;

    /// <summary>Gets the number of modeled annotations.</summary>
    public int Count
    {
        get
        {
            var count = 0;
            foreach (var entry in entries)
            {
                if (entry.Annotation is not null)
                {
                    count++;
                }
            }

            return count;
        }
    }

    /// <summary>Gets the modeled annotation at the specified index.</summary>
    /// <param name="index">The zero-based modeled annotation index.</param>
    /// <returns>The annotation at <paramref name="index"/>.</returns>
    public Annotation this[int index] => entries[EntryIndex(index)].Annotation!;

    /// <summary>Adds an annotation and returns it.</summary>
    /// <typeparam name="T">The annotation type.</typeparam>
    /// <param name="annotation">The annotation to add.</param>
    /// <returns>The supplied annotation.</returns>
    public T Add<T>(T annotation) where T : Annotation
    {
        ArgumentNullException.ThrowIfNull(annotation);
        entries.Add(new Entry(annotation, null, null, null));
        return annotation;
    }

    /// <summary>Inserts an annotation at a modeled annotation index.</summary>
    /// <param name="index">The zero-based modeled annotation index.</param>
    /// <param name="annotation">The annotation to insert.</param>
    public void Insert(int index, Annotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        if (index == Count)
        {
            entries.Add(new Entry(annotation, null, null, null));
        }
        else
        {
            entries.Insert(EntryIndex(index), new Entry(annotation, null, null, null));
        }
    }

    /// <summary>Removes the specified annotation.</summary>
    /// <param name="annotation">The annotation to remove.</param>
    /// <returns><c>true</c> when the annotation was found; otherwise <c>false</c>.</returns>
    public bool Remove(Annotation annotation)
    {
        ArgumentNullException.ThrowIfNull(annotation);
        for (var i = 0; i < entries.Count; i++)
        {
            if (ReferenceEquals(entries[i].Annotation, annotation))
            {
                entries.RemoveAt(i);
                return true;
            }
        }

        return false;
    }

    /// <summary>Removes the annotation at a modeled annotation index.</summary>
    /// <param name="index">The zero-based modeled annotation index.</param>
    public void RemoveAt(int index) => entries.RemoveAt(EntryIndex(index));

    /// <summary>Removes all modeled annotations while retaining unsupported loaded annotations.</summary>
    public void Clear() => entries.RemoveAll(entry => entry.Annotation is not null);

    /// <inheritdoc />
    public IEnumerator<Annotation> GetEnumerator()
    {
        foreach (var entry in entries)
        {
            if (entry.Annotation is { } annotation)
            {
                yield return annotation;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    internal IReadOnlyList<Entry> Entries => entries;

    internal bool WasLoaded => loaded;

    internal bool HasChanges
    {
        get
        {
            if (rewriteImported || entries.StructureChanged)
            {
                return true;
            }

            foreach (var entry in entries)
            {
                if (entry.Annotation is { IsModified: true })
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal void Load(Annotation? annotation, DocumentReader reader, DocumentObject original, DictionaryObject? dictionary)
    {
        loaded = true;
        annotation?.AcceptChanges();
        entries.Add(new Entry(annotation, reader, original, dictionary));
        entries.AcceptStructure();
    }

    internal void RewriteImported() => (loaded, rewriteImported) = (true, true);

    internal void RemoveLoadedSubtype(string subtype)
        => entries.RemoveAll(entry => entry.Dictionary is { } dictionary
            && string.Equals(entry.Reader!.GetName(dictionary, "Subtype"), subtype, StringComparison.Ordinal));

    private int EntryIndex(int modeledIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(modeledIndex);

        var current = 0;
        for (var i = 0; i < entries.Count; i++)
        {
            if (entries[i].Annotation is null)
            {
                continue;
            }

            if (current == modeledIndex)
            {
                return i;
            }

            current++;
        }

        throw new ArgumentOutOfRangeException(nameof(modeledIndex));
    }

    internal sealed record Entry(
        Annotation? Annotation,
        DocumentReader? Reader,
        DocumentObject? Original,
        DictionaryObject? Dictionary);
}
