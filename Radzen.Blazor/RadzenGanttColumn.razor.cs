namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenGanttColumn. TItem flows into the DAM-annotated RadzenDataGridColumn&lt;TItem&gt; base; mirror
    /// the annotation here so the Razor-generated partial satisfies IL2091 (@typeparam cannot carry
    /// attributes in this Razor version).
    /// </summary>
    /// <typeparam name="TItem">The type of the data item.</typeparam>
    public partial class RadzenGanttColumn<
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)] TItem>
        where TItem : notnull
    {
    }
}
