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

    // Field dictionaries on the current root-to-node path, to fail loud on a
    // self-referencing /Kids tree instead of recursing into a StackOverflow.
    private readonly HashSet<DictionaryObject> visiting = [];

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

        if (!visiting.Add(dict))
        {
            throw new DocumentParseException("Cyclic /Kids reference in the field tree.");
        }

        try
        {
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
        finally
        {
            visiting.Remove(dict);
        }
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
        WriteTextAppearance(terminal, value);
    }

    /// <summary>
    /// Sets the selected value of a loaded choice field - a list box or combo box
    /// (<c>/FT /Ch</c>) - to one of its <c>/Opt</c> options. Stores the export
    /// value in <c>/V</c>, its position in <c>/I</c>, and regenerates the normal
    /// appearance (<c>/AP /N</c>) so the selection renders without relying on
    /// <c>/NeedAppearances</c>.
    /// </summary>
    /// <param name="name">The field name (<c>/T</c>).</param>
    /// <param name="value">The export value of the option to select.</param>
    /// <exception cref="ArgumentException">The field carries an <c>/Opt</c> list
    /// that does not contain <paramref name="value"/>.</exception>
    public void SelectOption(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var terminal = Find(name);
        var options = OptionValues(terminal.Field);
        if (options is not null)
        {
            var index = options.IndexOf(value);
            if (index < 0)
            {
                throw new ArgumentException($"Field '{name}' has no option with value '{value}'.", nameof(value));
            }

            terminal.Field["I"] = new ArrayObject { new NumberObject(index) };
        }

        terminal.Field["V"] = new StringObject(value);
        WriteTextAppearance(terminal, value);
    }

    /// <summary>
    /// Selects an option of a loaded radio button group (<c>/FT /Btn</c> with the
    /// Radio flag). Stores the option's on-state name in the group <c>/V</c> and
    /// sets each kid widget's appearance state (<c>/AS</c>) to that name for the
    /// matching kid and to <c>Off</c> for the rest.
    /// </summary>
    /// <param name="name">The field name (<c>/T</c>).</param>
    /// <param name="value">The on-state name of the option to select.</param>
    /// <exception cref="ArgumentException">No kid widget carries an appearance
    /// state named <paramref name="value"/>.</exception>
    public void SelectRadioOption(string name, string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var terminal = Find(name);
        var widgets = RadioWidgets(terminal);
        var matched = false;
        foreach (var widget in widgets)
        {
            if (HasAppearanceState(widget, value))
            {
                matched = true;
                break;
            }
        }

        if (!matched)
        {
            throw new ArgumentException($"Radio field '{name}' has no option with value '{value}'.", nameof(value));
        }

        terminal.Field["V"] = new NameObject(value);
        foreach (var widget in widgets)
        {
            widget["AS"] = new NameObject(HasAppearanceState(widget, value) ? value : "Off");
        }
    }

    private void WriteTextAppearance(Terminal terminal, string value)
    {
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

    // The export values of a choice field's /Opt, or null when it carries none. An
    // /Opt entry is either a text string or a [export, display] pair whose first
    // element is the export value (ISO 32000-1 12.7.4.4).
    private List<string>? OptionValues(DictionaryObject field)
    {
        if (!field.TryGetValue("Opt", out var optObject) || reader.Resolve(optObject!) is not ArrayObject options)
        {
            return null;
        }

        var values = new List<string>(options.Count);
        foreach (var entry in options)
        {
            values.Add(reader.Resolve(entry) switch
            {
                StringObject text => FormField.DecodeTextString(text.Value),
                ArrayObject pair when pair.Count > 0 && reader.Resolve(pair[0]) is StringObject export
                    => FormField.DecodeTextString(export.Value),
                _ => string.Empty,
            });
        }

        return values;
    }

    // The kid widgets of a radio group; when field and its single widget are merged
    // (no /Kids) the group is just that one widget.
    private List<DictionaryObject> RadioWidgets(Terminal terminal)
    {
        var widgets = new List<DictionaryObject>();
        foreach (var kid in Kids(terminal.Field))
        {
            if (reader.Resolve(kid) is DictionaryObject widget && !widget.ContainsKey("T"))
            {
                widgets.Add(widget);
            }
        }

        if (widgets.Count == 0)
        {
            widgets.Add(terminal.Widget);
        }

        return widgets;
    }

    private bool HasAppearanceState(DictionaryObject widget, string state)
        => widget.TryGetValue("AP", out var apObject) && reader.Resolve(apObject!) is DictionaryObject ap
            && ap.TryGetValue("N", out var nObject) && reader.Resolve(nObject!) is DictionaryObject states
            && states.ContainsKey(state);

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
