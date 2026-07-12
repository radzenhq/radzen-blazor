using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// The interactive form of a loaded <see cref="Document"/>. Exposes the terminal
/// fields and lets a caller fill text fields (regenerating their normal
/// appearance) and toggle button fields on. Edits mutate the live field
/// dictionaries and are preserved when the document is saved.
/// </summary>
public sealed class AcroForm
{
    // Chosen on-state for a checkbox whose fixture carries no explicit /AP states.
    private const string OnState = "Yes";

    private readonly DocumentReader reader;
    private readonly List<FormField> fields = [];
    private readonly List<string> fieldNames = [];

    // Terminal fields keyed by their fully qualified dotted name, each paired with
    // the widget annotation that renders it (the field dict itself when merged).
    private readonly Dictionary<string, Terminal> terminals = new(StringComparer.Ordinal);

    internal AcroForm(DocumentReader reader, DictionaryObject dictionary)
    {
        this.reader = reader;
        Dictionary = dictionary;

        if (dictionary.TryGetValue("Fields", out var fieldsObject)
            && reader.Resolve(fieldsObject!) is ArrayObject entries)
        {
            foreach (var entry in entries)
            {
                Collect(entry, string.Empty);
            }
        }
    }

    internal DictionaryObject Dictionary { get; }

    /// <summary>Gets the terminal fields of the form.</summary>
    public IReadOnlyList<FormField> Fields => fields;

    /// <summary>
    /// Gets the fully qualified dotted names of the terminal fields (parent
    /// <c>/T</c> joined to each descendant <c>/T</c> with <c>'.'</c>), the names
    /// accepted by <see cref="FillField"/> and <see cref="CheckField"/>.
    /// </summary>
    public IReadOnlyList<string> FieldNames => fieldNames;

    private readonly record struct Terminal(DictionaryObject Field, DictionaryObject Widget);

    // Walks the field tree, recording each terminal under its qualified name. A node
    // whose /Kids are field dictionaries (they carry /T) is non-terminal; a node with
    // no /Kids, or whose /Kids are only widget annotations, is itself the terminal.
    private void Collect(DocumentObject entry, string prefix)
    {
        if (reader.Resolve(entry) is not DictionaryObject dict)
        {
            return;
        }

        var partial = PartialName(dict);
        var qualified = prefix.Length == 0 ? partial : prefix + "." + partial;

        var fieldKids = new List<DocumentObject>();
        foreach (var kid in Kids(dict))
        {
            if (reader.Resolve(kid) is DictionaryObject kidDict && kidDict.ContainsKey("T"))
            {
                fieldKids.Add(kid);
            }
        }

        if (fieldKids.Count > 0)
        {
            foreach (var kid in fieldKids)
            {
                Collect(kid, qualified);
            }

            return;
        }

        terminals[qualified] = new Terminal(dict, WidgetOf(dict));
        fieldNames.Add(qualified);
        fields.Add(new FormField(reader, dict));
    }

    private IEnumerable<DocumentObject> Kids(DictionaryObject dict)
        => dict.TryGetValue("Kids", out var kidsObject) && reader.Resolve(kidsObject!) is ArrayObject kids
            ? kids
            : [];

    // The annotation that renders a terminal: its first widget-only kid (a separate
    // widget carries no field /T), or the field dict itself when field and widget merge.
    private DictionaryObject WidgetOf(DictionaryObject dict)
    {
        foreach (var kid in Kids(dict))
        {
            if (reader.Resolve(kid) is DictionaryObject kidDict && !kidDict.ContainsKey("T"))
            {
                return kidDict;
            }
        }

        return dict;
    }

    private string PartialName(DictionaryObject dict)
        => dict.TryGetValue("T", out var value) && reader.Resolve(value!) is StringObject text
            ? FormField.DecodeTextString(text.Value)
            : string.Empty;

    /// <summary>
    /// Sets the text value of a field and regenerates its normal appearance
    /// (<c>/AP /N</c>) so the value renders without relying on
    /// <c>/NeedAppearances</c>.
    /// </summary>
    /// <param name="name">The field name (<c>/T</c>).</param>
    /// <param name="value">The text to store.</param>
    public void FillField(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var terminal = Find(name);
        terminal.Field["V"] = new StringObject(value);

        if (FieldAppearances.CanEncode(value))
        {
            // Write the appearance onto the widget so a separate-widget kid's stale /AP
            // does not override the new value in a viewer; when merged this is the field.
            terminal.Widget["AP"] = new DictionaryObject { ["N"] = BuildTextAppearance(terminal, value) };
        }
        else
        {
            // A base-14 WinAnsi appearance would silently drop these glyphs, so let viewers
            // regenerate from /V with a capable font instead of emitting missing glyphs.
            Dictionary["NeedAppearances"] = new BooleanObject(true);
            if (terminal.Widget.ContainsKey("AP"))
            {
                terminal.Widget["AP"] = new NullObject();
            }
        }
    }

    /// <summary>
    /// Turns a button (check box) field on: its value and appearance state
    /// (<c>/V</c> and <c>/AS</c>) are set to the same on-state name.
    /// </summary>
    /// <param name="name">The field name (<c>/T</c>).</param>
    public void CheckField(string name)
    {
        var terminal = Find(name);
        var on = OnStateName(terminal.Widget);
        terminal.Field["V"] = new NameObject(on);
        terminal.Widget["AS"] = new NameObject(on);
    }

    // The on-state is the first non-/Off appearance in the widget's /AP /N; only when
    // the widget has no /AP states do we fall back to the conventional "Yes".
    private string OnStateName(DictionaryObject widget)
    {
        if (widget.TryGetValue("AP", out var apObject) && reader.Resolve(apObject!) is DictionaryObject ap
            && ap.TryGetValue("N", out var nObject) && reader.Resolve(nObject!) is DictionaryObject states)
        {
            foreach (var key in states.Keys)
            {
                if (!string.Equals(key, "Off", StringComparison.Ordinal))
                {
                    return key;
                }
            }
        }

        return OnState;
    }

    private Terminal Find(string name)
        => terminals.TryGetValue(name, out var terminal)
            ? terminal
            : throw new ArgumentException($"Field '{name}' not found.", nameof(name));

    private StreamObject BuildTextAppearance(Terminal terminal, string value)
    {
        var (width, height) = RectSize(terminal.Widget);
        var (daFont, daSize) = DefaultAppearance(terminal);
        var fontSize = daSize > 0.0 ? daSize : FieldAppearances.DefaultFontSize;
        return FieldAppearances.BuildText(value, width, height, FieldAppearances.AppearanceFont(daFont, fontSize));
    }

    private (double Width, double Height) RectSize(DictionaryObject field)
    {
        if (field.TryGetValue("Rect", out var rectObject) && reader.Resolve(rectObject!) is ArrayObject rect
            && rect.Count >= 4)
        {
            var x0 = Number(rect[0]);
            var y0 = Number(rect[1]);
            var x1 = Number(rect[2]);
            var y1 = Number(rect[3]);
            return (Math.Abs(x1 - x0), Math.Abs(y1 - y0));
        }

        return (200.0, 14.0);
    }

    private static double Number(DocumentObject value) => value is NumberObject number ? number.DoubleValue : 0.0;

    // Resolves the /DA to draw the value with: the field's own /DA wins, else the widget's,
    // else the form default /DA. A /DA size of 0 (auto) is reported as 0 for the caller to map.
    private (string? Font, double Size) DefaultAppearance(Terminal terminal)
        => FieldAppearances.ParseDefaultAppearance(
            DaString(terminal.Field) ?? DaString(terminal.Widget) ?? DaString(Dictionary));

    private string? DaString(DictionaryObject dict)
        => dict.TryGetValue("DA", out var da) && reader.Resolve(da!) is StringObject text ? text.Value : null;
}
