using System;
using System.Collections.Generic;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal sealed class LayoutCaptureContext(ImageProbes probes)
{
    private readonly Dictionary<object, SourceId> sources = new(ReferenceEqualityComparer.Instance);
    private readonly List<object> sourceValues = [];
    private readonly Dictionary<byte[], SceneImageData> images = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, PaintId> paints = new(ReferenceEqualityComparer.Instance);
    private int nextPaintId;

    public ImageProbes Probes { get; } = probes ?? throw new ArgumentNullException(nameof(probes));

    public SourceId Source(object source)
    {
        if (!sources.TryGetValue(source, out var id))
        {
            id = new SourceId(sourceValues.Count);
            sources.Add(source, id);
            sourceValues.Add(source);
        }

        return id;
    }

    public T Resolve<T>(SourceId id) where T : class
        => (T)sourceValues[id.Value];

    public Lazy<SourceResolver> DeferredSources() => new(() => new SourceResolver(sourceValues));

    public SceneImageData Image(byte[] data)
    {
        if (!images.TryGetValue(data, out var captured))
        {
            captured = new SceneImageData(data, ImageMetrics.MediaType(Probes.Format(data)));
            images.Add(data, captured);
        }

        return captured;
    }

    public PaintId Paint(object source)
    {
        if (!paints.TryGetValue(source, out var id))
        {
            id = new PaintId(nextPaintId++);
            paints.Add(source, id);
        }

        return id;
    }
}
