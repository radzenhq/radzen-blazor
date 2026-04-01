namespace Radzen;

/// <summary>
/// Specifies the ways a <see cref="Radzen.Blazor.RadzenAccordion" /> component renders its items.
/// </summary>
public enum AccordionRenderMode
{
    /// <summary>
    /// The RadzenAccordion component switches its items server side. The component re-renders on every expand/collapse.
    /// </summary>
    Server,

    /// <summary>
    /// The RadzenAccordion component switches its items client-side. All items are rendered and the expand/collapse is handled with JavaScript.
    /// </summary>
    Client
}
