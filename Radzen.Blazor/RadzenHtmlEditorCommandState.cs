namespace Radzen.Blazor
{
    /// <summary>
    /// Class RadzenHtmlEditorCommandState.
    /// </summary>
    internal class RadzenHtmlEditorCommandState
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenHtmlEditorCommandState"/> is undo.
        /// </summary>
        /// <value><c>true</c> if undo; otherwise, <c>false</c>.</value>
        public bool Undo { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenHtmlEditorCommandState"/> is redo.
        /// </summary>
        /// <value><c>true</c> if redo; otherwise, <c>false</c>.</value>
        public bool Redo { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenHtmlEditorCommandState"/> is bold.
        /// </summary>
        /// <value><c>true</c> if bold; otherwise, <c>false</c>.</value>
        public bool Bold { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenHtmlEditorCommandState"/> is italic.
        /// </summary>
        /// <value><c>true</c> if italic; otherwise, <c>false</c>.</value>
        public bool Italic { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [strike through].
        /// </summary>
        /// <value><c>true</c> if [strike through]; otherwise, <c>false</c>.</value>
        public bool StrikeThrough { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenHtmlEditorCommandState"/> is subscript.
        /// </summary>
        /// <value><c>true</c> if subscript; otherwise, <c>false</c>.</value>
        public bool Subscript { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenHtmlEditorCommandState"/> is superscript.
        /// </summary>
        /// <value><c>true</c> if superscript; otherwise, <c>false</c>.</value>
        public bool Superscript { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenHtmlEditorCommandState"/> is underline.
        /// </summary>
        /// <value><c>true</c> if underline; otherwise, <c>false</c>.</value>
        public bool Underline { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [justify right].
        /// </summary>
        /// <value><c>true</c> if [justify right]; otherwise, <c>false</c>.</value>
        public bool JustifyRight { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [justify left].
        /// </summary>
        /// <value><c>true</c> if [justify left]; otherwise, <c>false</c>.</value>
        public bool JustifyLeft { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [justify center].
        /// </summary>
        /// <value><c>true</c> if [justify center]; otherwise, <c>false</c>.</value>
        public bool JustifyCenter { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether [justify full].
        /// </summary>
        /// <value><c>true</c> if [justify full]; otherwise, <c>false</c>.</value>
        public bool JustifyFull { get; set; }
        /// <summary>
        /// Gets or sets the name of the font.
        /// </summary>
        /// <value>The name of the font.</value>
        public string FontName { get; set; }
        /// <summary>
        /// Gets or sets the size of the font.
        /// </summary>
        /// <value>The size of the font.</value>
        public string FontSize { get; set; }
        /// <summary>
        /// Gets or sets the format block.
        /// </summary>
        /// <value>The format block.</value>
        public string FormatBlock { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="RadzenHtmlEditorCommandState"/> is unlink.
        /// </summary>
        /// <value><c>true</c> if unlink; otherwise, <c>false</c>.</value>
        public bool Unlink { get; set;  }
        /// <summary>
        /// Gets or sets the HTML.
        /// </summary>
        /// <value>The HTML.</value>
        public string Html { get; set; }
    }
}