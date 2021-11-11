namespace Radzen.Blazor
{
    internal class RadzenHtmlEditorCommandState
    {
        public bool Undo { get; set; }
        public bool Redo { get; set; }
        public bool Bold { get; set; }
        public bool Italic { get; set; }
        public bool StrikeThrough { get; set; }
        public bool Subscript { get; set; }
        public bool Superscript { get; set; }
        public bool Underline { get; set; }
        public bool JustifyRight { get; set; }
        public bool JustifyLeft { get; set; }
        public bool JustifyCenter { get; set; }
        public bool JustifyFull { get; set; }
        public string FontName { get; set; }
        public string FontSize { get; set; }
        public string FormatBlock { get; set; }
        public bool Unlink { get; set; }
        public string Html { get; set; }
    }
}