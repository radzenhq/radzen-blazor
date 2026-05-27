using System;

namespace Radzen.Blazor.Spreadsheet;

#nullable enable

internal sealed class EventBinding<TSource> where TSource : class
{
    private TSource? source;
    private readonly Action<TSource> attach;
    private readonly Action<TSource> detach;

    public EventBinding(Action<TSource> attach, Action<TSource> detach)
    {
        this.attach = attach;
        this.detach = detach;
    }

    public void Bind(TSource? next)
    {
        if (ReferenceEquals(source, next))
        {
            return;
        }

        if (source is not null)
        {
            detach(source);
        }

        source = next;

        if (source is not null)
        {
            attach(source);
        }
    }

    public void Dispose() => Bind(null);
}
