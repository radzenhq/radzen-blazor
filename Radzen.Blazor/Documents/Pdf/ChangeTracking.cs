namespace Radzen.Documents.Pdf;

internal interface ITracksChanges
{
    bool IsModified { get; }

    void AcceptChanges();
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
