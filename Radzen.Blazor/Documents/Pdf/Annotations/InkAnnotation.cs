using System.Collections;
using System.Collections.Generic;
using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf;

/// <summary>Represents one continuous stroke in an ink annotation.</summary>
public sealed class InkStroke : IList<AnnotationPoint>
{
    private readonly TrackedList<AnnotationPoint> points = [];

    /// <inheritdoc />
    public AnnotationPoint this[int index]
    {
        get => points[index];
        set => points[index] = value;
    }

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

    internal bool IsModified => points.StructureChanged;

    internal void AcceptChanges() => points.AcceptStructure();
}

/// <summary>Represents one or more freehand ink strokes.</summary>
public sealed class InkAnnotation : Annotation
{
    private double strokeWidth = 1;

    /// <summary>Initializes a new instance of the <see cref="InkAnnotation"/> class.</summary>
    /// <param name="bounds">The annotation bounds.</param>
    public InkAnnotation(PdfRect bounds) : base(bounds) => Strokes = new TrackedList<InkStroke>(Touch);

    /// <summary>Gets the freehand strokes.</summary>
    public IList<InkStroke> Strokes { get; }

    /// <summary>Gets or sets the stroke width in points.</summary>
    public double StrokeWidth
    {
        get => strokeWidth;
        set => Set(ref strokeWidth, value);
    }

    internal override bool IsModified
    {
        get
        {
            if (base.IsModified)
            {
                return true;
            }

            foreach (var stroke in Strokes)
            {
                if (stroke.IsModified)
                {
                    return true;
                }
            }

            return false;
        }
    }

    internal override string Subtype => "Ink";

    internal override void AcceptChanges()
    {
        base.AcceptChanges();
        foreach (var stroke in Strokes)
        {
            stroke.AcceptChanges();
        }
    }
}
