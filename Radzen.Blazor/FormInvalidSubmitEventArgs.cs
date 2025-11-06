using System.Collections.Generic;

namespace Radzen;

/// <summary>
/// Supplies information about a <see cref="Radzen.Blazor.RadzenTemplateForm{TItem}.InvalidSubmit" /> event that is being raised.
/// </summary>
public class FormInvalidSubmitEventArgs
{
    /// <summary>
    /// Gets the validation errors.
    /// </summary>
    public IEnumerable<string> Errors { get; set; }
}

