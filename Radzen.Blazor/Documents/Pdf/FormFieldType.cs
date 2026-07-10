namespace Radzen.Documents.Pdf;

#nullable enable

/// <summary>
/// The interactive type of an AcroForm field, taken from its <c>/FT</c> entry.
/// </summary>
public enum FormFieldType
{
    /// <summary>A text field (<c>/Tx</c>).</summary>
    Text,

    /// <summary>A button field: push button, check box, or radio (<c>/Btn</c>).</summary>
    Button,

    /// <summary>A choice field: list box or combo box (<c>/Ch</c>).</summary>
    Choice,

    /// <summary>A digital signature field (<c>/Sig</c>).</summary>
    Signature,
}
