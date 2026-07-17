namespace Radzen.Documents.Pdf;

/// <summary>
/// A change-tracked model object: it can answer whether it has been modified since it was
/// materialized from a loaded document, and re-baseline itself once the loaded state is read.
/// </summary>
internal interface ITracksChanges
{
    bool IsModified { get; }

    void AcceptChanges();
}

// The one implementation of the Set/touched/AcceptChanges triple, composed as a field by every
// per-object tracker instead of copied into each. Tracking is unconditional: whether a new value
// is "equal" to the old is not the same question as whether it emits the same bytes, and for a
// mutable Font or a float it is not even stable, so an assignment always marks the object modified.
// AcceptChanges re-baselines a freshly loaded object, which was built through these same setters.
// A mutable struct so it costs no allocation; only ever held as a class field and mutated in place.
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
