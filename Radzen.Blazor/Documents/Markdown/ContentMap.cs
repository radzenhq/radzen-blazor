using System.Collections.Generic;

namespace Radzen.Documents.Markdown;

internal sealed class ContentMap
{
    private readonly List<ContentSegment> segments = [];

    public IReadOnlyList<ContentSegment> Segments => segments;

    public int ContentLength => segments.Count > 0 ? segments[^1].ContentEnd : 0;

    public void Append(int contentLength, int sourceStart, int sourceLength)
    {
        var contentStart = ContentLength;
        segments.Add(new ContentSegment(contentStart, contentStart + contentLength, sourceStart, sourceStart + sourceLength));
    }

    public void Append(IEnumerable<ContentSegment> source)
    {
        foreach (var segment in source)
        {
            Append(segment.ContentEnd - segment.ContentStart, segment.SourceStart, segment.SourceEnd - segment.SourceStart);
        }
    }

    public void Clear() => segments.Clear();

    public void TrimStart(int count)
    {
        var index = 0;

        while (index < segments.Count && segments[index].ContentEnd <= count)
        {
            index++;
        }

        segments.RemoveRange(0, index);

        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];
            var sourceStart = segment.SourceStart;

            if (i == 0 && segment.ContentStart < count)
            {
                sourceStart = ToSource(count);
            }

            segments[i] = new ContentSegment(
                i == 0 ? 0 : segment.ContentStart - count,
                segment.ContentEnd - count,
                sourceStart,
                segment.SourceEnd);
        }
    }

    public void TrimEnd(int length)
    {
        while (segments.Count > 0 && segments[^1].ContentStart >= length)
        {
            segments.RemoveAt(segments.Count - 1);
        }

        if (segments.Count > 0 && segments[^1].ContentEnd > length)
        {
            var last = segments[^1];
            segments[^1] = new ContentSegment(last.ContentStart, length, last.SourceStart, ToSource(length));
        }
    }

    public int ToContent(int sourceOffset)
    {
        foreach (var segment in segments)
        {
            if (sourceOffset < segment.SourceStart)
            {
                return segment.ContentStart;
            }

            if (sourceOffset <= segment.SourceEnd)
            {
                return segment.ContentStart + System.Math.Min(sourceOffset - segment.SourceStart, segment.ContentEnd - segment.ContentStart);
            }
        }

        return ContentLength;
    }

    public int ToSource(int contentOffset)
    {
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (contentOffset < segment.ContentStart)
            {
                return segment.SourceStart;
            }

            if (contentOffset < segment.ContentEnd || (contentOffset == segment.ContentEnd && i == segments.Count - 1))
            {
                var contentLength = segment.ContentEnd - segment.ContentStart;
                var sourceLength = segment.SourceEnd - segment.SourceStart;
                var delta = contentOffset - segment.ContentStart;

                if (contentLength == sourceLength)
                {
                    return segment.SourceStart + delta;
                }

                return delta == 0 ? segment.SourceStart : delta >= contentLength ? segment.SourceEnd : segment.SourceStart;
            }
        }

        return segments.Count > 0 ? segments[^1].SourceEnd : 0;
    }

    public int ToSourceEnd(int contentOffset)
    {
        for (var i = 0; i < segments.Count; i++)
        {
            var segment = segments[i];

            if (contentOffset <= segment.ContentEnd && (contentOffset > segment.ContentStart || i == 0 || contentOffset == segment.ContentStart && segment.ContentStart == segment.ContentEnd))
            {
                return contentOffset >= segment.ContentEnd ? segment.SourceEnd : ToSource(contentOffset);
            }
        }

        return segments.Count > 0 ? segments[^1].SourceEnd : 0;
    }


}
