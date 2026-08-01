using System;
using System.Collections.Generic;

namespace Radzen.Documents.Core;

internal interface ITracksChanges
{
    bool IsModified { get; }

    void AcceptChanges();

    void OwnedBy(Action? changed);
}

internal static class TrackedChanges
{
    public static bool AnyModified<T>(TrackedList<T> list) where T : ITracksChanges
        => AnyModified(list, static item => item.IsModified);

    public static bool AnyModified<T>(TrackedList<T> list, Func<T, bool> isModified)
    {
        if (list.StructureChanged)
        {
            return true;
        }

        foreach (var item in list)
        {
            if (isModified(item))
            {
                return true;
            }
        }

        return false;
    }

    public static void Accept<T>(TrackedList<T> list) where T : ITracksChanges
    {
        list.AcceptStructure();
        foreach (var item in list)
        {
            item.AcceptChanges();
        }
    }
}

internal struct ChangeTracker
{
    private bool touched;
    private Action? changed;

    public readonly bool IsModified => touched;

    public void OwnedBy(Action? owner) => changed = owner;

    public void Set<T>(ref T field, T value)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        Touch();
    }

    public void Touch()
    {
        touched = true;
        changed?.Invoke();
    }

    public void AcceptChanges() => touched = false;
}
