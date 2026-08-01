using System;
using System.Collections;
using System.Collections.Generic;

namespace Radzen.Documents.Core;

internal sealed class TrackedList<T> : IList<T>, IReadOnlyList<T>
{
    private readonly List<T> items = [];
    private Action? changed;
    private bool structureChanged;

    public TrackedList()
    {
    }

    public TrackedList(Action? changed) => this.changed = changed;

    public void OwnedBy(Action? owner)
    {
        changed = owner;
        foreach (var item in items)
        {
            Attach(item);
        }
    }

    public T this[int index]
    {
        get => items[index];
        set
        {
            if (ReferenceEquals(items[index], value) || EqualityComparer<T>.Default.Equals(items[index], value))
            {
                return;
            }

            Release(items[index]);
            items[index] = value;
            Attach(value);
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
        Attach(item);
        Changed();
    }

    public void Clear()
    {
        if (items.Count > 0)
        {
            foreach (var item in items)
            {
                Release(item);
            }

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
        Attach(item);
        Changed();
    }

    public bool Remove(T item)
    {
        var removed = items.Remove(item);
        if (removed)
        {
            Release(item);
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
        Release(items[index]);
        items.RemoveAt(index);
        Changed();
    }

    public void Move(int fromIndex, int toIndex)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(fromIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(fromIndex, items.Count);
        ArgumentOutOfRangeException.ThrowIfNegative(toIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(toIndex, items.Count);

        if (fromIndex == toIndex)
        {
            return;
        }

        var item = items[fromIndex];
        items.RemoveAt(fromIndex);
        items.Insert(toIndex, item);
        Changed();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private void Attach(T item)
    {
        if (item is ITracksChanges tracked)
        {
            tracked.OwnedBy(changed);
        }
    }

    private static void Release(T item)
    {
        if (item is ITracksChanges tracked)
        {
            tracked.OwnedBy(null);
        }
    }

    private void Changed()
    {
        structureChanged = true;
        changed?.Invoke();
    }
}
