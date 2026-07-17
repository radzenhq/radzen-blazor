using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf.Content;

internal readonly record struct ContentEdit(int Start, int End, byte[] Bytes);

internal static class ContentEdits
{
    public static byte[] Apply(byte[] source, List<ContentEdit> edits)
    {
        if (edits.Count == 0)
        {
            return source;
        }

        edits.Sort(static (a, b) => a.Start.CompareTo(b.Start));

        var length = source.Length;
        var previousEnd = 0;
        foreach (var edit in edits)
        {
            if (edit.Start < previousEnd || edit.End < edit.Start || edit.End > source.Length)
            {
                throw new InvalidOperationException("Overlapping content edits cannot be applied safely.");
            }

            length += edit.Bytes.Length - (edit.End - edit.Start);
            previousEnd = edit.End;
        }

        var result = new byte[length];
        var read = 0;
        var write = 0;
        foreach (var edit in edits)
        {
            source.AsSpan(read, edit.Start - read).CopyTo(result.AsSpan(write));
            write += edit.Start - read;
            edit.Bytes.CopyTo(result, write);
            write += edit.Bytes.Length;
            read = edit.End;
        }

        source.AsSpan(read).CopyTo(result.AsSpan(write));
        return result;
    }
}
