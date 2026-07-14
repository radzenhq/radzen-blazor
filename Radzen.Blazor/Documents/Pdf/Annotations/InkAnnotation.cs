using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace Radzen.Documents.Pdf;

/// <summary>Represents one continuous stroke in an ink annotation.</summary>
public sealed class InkStroke : IList<AnnotationPoint>
{
    private readonly List<AnnotationPoint> points = [];

    /// <inheritdoc />
    public AnnotationPoint this[int index] { get => points[index]; set => points[index] = value; }

    /// <inheritdoc />
    public int Count => points.Count;

    /// <inheritdoc />
    public bool IsReadOnly => false;

    /// <inheritdoc />
    public void Add(AnnotationPoint item) => points.Add(item);

    /// <inheritdoc />
    public void Clear() => points.Clear();

    /// <inheritdoc />
    public bool Contains(AnnotationPoint item) => points.Contains(item);

    /// <inheritdoc />
    public void CopyTo(AnnotationPoint[] array, int arrayIndex) => points.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public IEnumerator<AnnotationPoint> GetEnumerator() => points.GetEnumerator();

    /// <inheritdoc />
    public int IndexOf(AnnotationPoint item) => points.IndexOf(item);

    /// <inheritdoc />
    public void Insert(int index, AnnotationPoint item) => points.Insert(index, item);

    /// <inheritdoc />
    public bool Remove(AnnotationPoint item) => points.Remove(item);

    /// <inheritdoc />
    public void RemoveAt(int index) => points.RemoveAt(index);

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary>Represents one or more freehand ink strokes.</summary>
/// <param name="bounds">The annotation bounds.</param>
public sealed class InkAnnotation(Rect bounds) : Annotation(bounds)
{
    /// <summary>Gets the freehand strokes.</summary>
    public IList<InkStroke> Strokes { get; } = [];

    /// <summary>Gets or sets the stroke width in points.</summary>
    public double StrokeWidth { get; set; } = 1;

    internal override string Subtype => "Ink";

    internal override void AppendState(StringBuilder value)
    {
        Append(value, StrokeWidth);
        foreach (var stroke in Strokes)
        {
            value.Append('|').Append(stroke.Count);
            foreach (var point in stroke)
            {
                Append(value, point.X);
                Append(value, point.Y);
            }
        }
    }
}
