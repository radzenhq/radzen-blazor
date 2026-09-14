using Radzen.Blazor;

namespace Radzen.Blazor.Tests;

internal sealed class SourceEngine(string? text)
{
    public MarkdownEditorEngine Engine { get; } = new(text);

    public string Text => Engine.Text;

    public bool CanUndo => Engine.CanUndo;

    public bool CanRedo => Engine.CanRedo;

    private int At(int offset) => Engine.ToPosition(offset);

    private MarkdownEditorUpdate? Back(MarkdownEditorUpdate? update)
    {
        if (update != null)
        {
            update.SelectionStart = Engine.ToSource(update.SelectionStart, preferNext: update.SelectionStart < update.SelectionEnd);
            update.SelectionEnd = Engine.ToSource(update.SelectionEnd);
        }

        return update;
    }

    public MarkdownEditorUpdate InsertText(int start, int end, string text, bool literal, string? key = null, bool merge = false, bool paragraphs = false, bool selection = false) =>
        Back(Engine.InsertText(At(start), At(end), text, literal, key, merge, paragraphs, selection))!;

    public MarkdownEditorUpdate? Delete(int start, int end, bool forward = false, string? key = null, bool merge = false, bool selection = false) =>
        Back(Engine.Delete(At(start), At(end), forward, key, merge, selection));

    public MarkdownEditorUpdate? InsertParagraph(int start, int end) => Back(Engine.InsertParagraph(At(start), At(end)));

    public MarkdownEditorUpdate? InsertLineBreak(int start, int end) => Back(Engine.InsertLineBreak(At(start), At(end)));

    public MarkdownEditorUpdate? AppendRow(int caret) => Back(Engine.AppendRow(At(caret)));

    public MarkdownEditorUpdate? Indent(int start, int end, bool outdent) => Back(Engine.Indent(At(start), At(end), outdent));

    public MarkdownEditorUpdate? Command(string name, int start, int end, string? value, string? label) => Back(Engine.Command(name, At(start), At(end), value, label));

    public MarkdownEditorUpdate? Undo() => Back(Engine.Undo());

    public MarkdownEditorUpdate? Redo() => Back(Engine.Redo());

    public MarkdownEditorUpdate Apply(int start, int end, string inserted, (int Start, int End) after, string? key = null, bool merge = false) => Engine.Apply(start, end, inserted, after, key, merge);

    public MarkdownEditorToolState State(int start, int end) => Engine.State(At(start), At(end));
}
