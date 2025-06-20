namespace Radzen.Blazor
{
    /// <summary>
    /// Represents the state of various commands available in the Radzen HTML editor.
    /// Used to track the status of editor commands such as formatting, undo/redo, and text alignment.
    /// </summary>
    public class RadzenHtmlEditorCommandState
    {
        /// <summary>
        /// Indicates whether the undo command is currently available.
        /// </summary>
        public bool Undo { get; set; }

        /// <summary>
        /// Indicates whether the redo command is currently available.
        /// </summary>
        public bool Redo { get; set; }

        /// <summary>
        /// Indicates whether bold formatting is currently applied.
        /// </summary>
        public bool Bold { get; set; }

        /// <summary>
        /// Indicates whether italic formatting is currently applied.
        /// </summary>
        public bool Italic { get; set; }

        /// <summary>
        /// Indicates whether strikethrough formatting is currently applied.
        /// </summary>
        public bool StrikeThrough { get; set; }

        /// <summary>
        /// Indicates whether subscript formatting is currently applied.
        /// </summary>
        public bool Subscript { get; set; }

        /// <summary>
        /// Indicates whether superscript formatting is currently applied.
        /// </summary>
        public bool Superscript { get; set; }

        /// <summary>
        /// Indicates whether underline formatting is currently applied.
        /// </summary>
        public bool Underline { get; set; }

        /// <summary>
        /// Indicates whether text is currently right-aligned.
        /// </summary>
        public bool JustifyRight { get; set; }

        /// <summary>
        /// Indicates whether text is currently left-aligned.
        /// </summary>
        public bool JustifyLeft { get; set; }

        /// <summary>
        /// Indicates whether text is currently center-aligned.
        /// </summary>
        public bool JustifyCenter { get; set; }

        /// <summary>
        /// Indicates whether text is currently justified.
        /// </summary>
        public bool JustifyFull { get; set; }

        /// <summary>
        /// Gets or sets the name of the currently selected font.
        /// </summary>
        public string FontName { get; set; }

        /// <summary>
        /// Gets or sets the size of the currently selected font.
        /// </summary>
        public string FontSize { get; set; }

        /// <summary>
        /// Gets or sets the current formatting block (e.g., paragraph, heading).
        /// </summary>
        public string FormatBlock { get; set; }

        /// <summary>
        /// Indicates whether the unlink command is currently available.
        /// </summary>
        public bool Unlink { get; set; }

        /// <summary>
        /// Gets or sets the current HTML content of the editor.
        /// </summary>
        public string Html { get; set; }
    }
}