using Radzen.Blazor;

namespace Radzen.Blazor.Tests;

internal sealed class SourceEngine(string? text)
{
    private readonly MarkdownEditorEngine engine = new(text);

    public string Text => engine.Text;

    private int At(int offset) => engine.ToPosition(offset);

    private MarkdownEditorUpdate? Back(MarkdownEditorUpdate? update)
    {
        if (update != null)
        {
            update.SelectionStart = engine.ToSource(update.SelectionStart, preferNext: update.SelectionStart < update.SelectionEnd);
            update.SelectionEnd = engine.ToSource(update.SelectionEnd);
        }

        return update;
    }

    public MarkdownEditorUpdate InsertText(int start, int end, string text, bool literal, string? key = null, bool merge = false, bool selection = false) =>
        Back(engine.InsertText(At(start), At(end), text, literal, key, merge, selection))!;

    public MarkdownEditorUpdate? Delete(int start, int end, bool forward = false, string? key = null, bool merge = false, bool selection = false) =>
        Back(engine.Delete(At(start), At(end), forward, key, merge, selection));

    public MarkdownEditorUpdate? InsertParagraph(int start, int end) => Back(engine.InsertParagraph(At(start), At(end)));

    public MarkdownEditorUpdate? InsertLineBreak(int start, int end) => Back(engine.InsertLineBreak(At(start), At(end)));

    public MarkdownEditorUpdate? AppendRow(int caret) => Back(engine.AppendRow(At(caret)));

    public MarkdownEditorUpdate? Indent(int start, int end, bool outdent) => Back(engine.Indent(At(start), At(end), outdent));

    public MarkdownEditorUpdate? Command(string name, int start, int end, string? value, string? label) => Back(engine.Command(name, At(start), At(end), value, label));

    public MarkdownEditorUpdate? Undo() => Back(engine.Undo());

    public MarkdownEditorUpdate? Redo() => Back(engine.Redo());

    public MarkdownEditorUpdate Apply(int start, int end, string inserted, (int Start, int End) after, string? key = null, bool merge = false) => engine.Apply(start, end, inserted, after, key, merge);

    public MarkdownEditorToolState State(int start, int end) => engine.State(At(start), At(end));
}
