using System.Collections.Generic;
using Radzen.Documents.LaidOut;

namespace Radzen.Documents.Layout;

internal sealed class LayoutCaptureContext
{
    private readonly Dictionary<object, SourceId> sources = new(ReferenceEqualityComparer.Instance);
    private readonly List<object> sourceValues = [];
    private readonly Dictionary<byte[], SceneImageData> images = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<object, LaidOutNodeId> paints = new(ReferenceEqualityComparer.Instance);
    private int nextNodeId;

    public LaidOutNodeId Node() => new(nextNodeId++);

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

    public SceneImageData Image(byte[] data)
    {
        if (!images.TryGetValue(data, out var captured))
        {
            captured = new SceneImageData(data);
            images.Add(data, captured);
        }

        return captured;
    }

    public LaidOutNodeId Paint(object source)
    {
        if (!paints.TryGetValue(source, out var id))
        {
            id = Node();
            paints.Add(source, id);
        }

        return id;
    }
}
