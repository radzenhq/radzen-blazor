using System;

namespace Radzen.Documents.Pdf;

internal interface ITracksChanges
{
    bool IsModified { get; }

    void AcceptChanges();
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

    public readonly bool IsModified => touched;

    public void Set<T>(ref T field, T value)
    {
        field = value;
        touched = true;
    }

    public void Touch() => touched = true;

    public void AcceptChanges() => touched = false;
}
