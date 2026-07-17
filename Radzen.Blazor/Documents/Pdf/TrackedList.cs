using System;
using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

internal sealed class TrackedList<T>(Action? changed = null) : IList<T>, IReadOnlyList<T>
{
    private readonly List<T> items = [];
    private bool structureChanged;

    public T this[int index]
    {
        get => items[index];
        set
        {
            items[index] = value;
            Changed();
        }
    }

    public int Count => items.Count;

    public bool IsReadOnly => false;

    public bool StructureChanged => structureChanged;

    public void AcceptStructure() => structureChanged = false;

    public void Add(T item)
    {
        items.Add(item);
        Changed();
    }

    public void Clear()
    {
        if (items.Count > 0)
        {
            items.Clear();
            Changed();
        }
    }

    public bool Contains(T item) => items.Contains(item);

    public void CopyTo(T[] array, int arrayIndex) => items.CopyTo(array, arrayIndex);

    public IEnumerator<T> GetEnumerator() => items.GetEnumerator();

    public int IndexOf(T item) => items.IndexOf(item);

    public void Insert(int index, T item)
    {
        items.Insert(index, item);
        Changed();
    }

    public bool Remove(T item)
    {
        var removed = items.Remove(item);
        if (removed)
        {
            Changed();
        }

        return removed;
    }

    public int RemoveAll(Predicate<T> match)
    {
        var removed = items.RemoveAll(match);
        if (removed > 0)
        {
            Changed();
        }

        return removed;
    }

    public void RemoveAt(int index)
    {
        items.RemoveAt(index);
        Changed();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Changed()
    {
        structureChanged = true;
        changed?.Invoke();
    }
}
