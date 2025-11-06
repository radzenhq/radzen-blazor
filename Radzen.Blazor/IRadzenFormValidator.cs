namespace Radzen;

/// <summary>
/// The interface that a validator component must implement in order to be supported by <see cref="Radzen.Blazor.RadzenTemplateForm{TItem}" />.
/// </summary>
public interface IRadzenFormValidator
{
    /// <summary>
    /// Returns true if valid.
    /// </summary>
    /// <value><c>true</c> if the validator is valid; otherwise, <c>false</c>.</value>
    bool IsValid { get; }

    /// <summary>
    /// Gets or sets the name of the component which is validated.
    /// </summary>
    /// <value>The component name.</value>
    string Component { get; set; }

    /// <summary>
    /// Gets or sets the validation error displayed when invalid.
    /// </summary>
    /// <value>The text to display when invalid.</value>
    string Text { get; set; }
}

