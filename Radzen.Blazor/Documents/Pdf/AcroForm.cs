using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

using Radzen.Documents.Pdf.Emit;
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

    // Text-field /Ff flags (ISO 32000-1 table 228): bit 13 multiline, bit 14 password,
    // bit 25 comb. A baked single left-aligned line is faithful for none of these.
    private const int MultilineFlag = 1 << 12;
    private const int PasswordFlag = 1 << 13;
    private const int CombFlag = 1 << 24;

    private readonly DocumentReader reader;
    private readonly Document owner;
    private readonly List<FormField> fields = [];
    private readonly List<string> fieldNames = [];

    // The field/widget/form dictionaries a caller has mutated through this form,
    // recorded for the incremental save path (Document.SaveIncremental) so it can
    // re-emit only the objects that actually changed. Inert on the full-save path,
    // which re-imports the whole form graph regardless.
    internal HashSet<DocumentObject> ChangedObjects { get; } = new(ReferenceEqualityComparer.Instance);

    // Terminal fields keyed by their fully qualified dotted name, each paired with
    // the widget annotation that renders it (the field dict itself when merged).
    private readonly Dictionary<string, Terminal> terminals = new(StringComparer.Ordinal);

    // Field dictionaries on the current root-to-node path, to fail loud on a
    // self-referencing /Kids tree instead of recursing into a StackOverflow.
    private readonly HashSet<DictionaryObject> visiting = [];

    internal AcroForm(DocumentReader reader, DictionaryObject dictionary, Document owner)
    {
        this.reader = reader;
        this.owner = owner;
        Dictionary = dictionary;

        if (reader.GetArray(dictionary, "Fields") is { } entries)
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

    // A terminal and every annotation that renders it: one field can be shown by several
    // widget kids (the same field repeated on each page), all of which an edit must refresh.
    private readonly record struct Terminal(DictionaryObject Field, IReadOnlyList<DictionaryObject> Widgets);

    // Walks the field tree, recording each terminal under its qualified name. A node
    // whose /Kids are field dictionaries (they carry /T) is non-terminal; a node with
    // no /Kids, or whose /Kids are only widget annotations, is itself the terminal.
    private void Collect(DocumentObject entry, string prefix)
    {
        if (reader.AsDictionary(entry) is not { } dict)
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
                if (reader.AsDictionary(kid) is { } kidDict && kidDict.ContainsKey("T"))
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

            // Two root fields legally may share a /T in malformed-but-real PDFs; keep both
            // reachable by suffixing the collision so terminals, FieldNames and Fields agree.
            var key = qualified;
            for (var index = 2; terminals.ContainsKey(key); index++)
            {
                key = qualified + "_" + index;
            }

            terminals[key] = new Terminal(dict, WidgetsOf(dict));
            fieldNames.Add(key);
            fields.Add(new FormField(reader, dict, key));
        }
        finally
        {
            visiting.Remove(dict);
        }
    }

    private IEnumerable<DocumentObject> Kids(DictionaryObject dict)
        => reader.GetArray(dict, "Kids") is { } kids ? kids : [];

    // The annotations that render a terminal: its widget-only kids (a separate widget
    // carries no field /T), or the field dict itself when field and widget merge.
    private List<DictionaryObject> WidgetsOf(DictionaryObject dict)
    {
        var widgets = new List<DictionaryObject>();
        foreach (var kid in Kids(dict))
        {
            if (reader.AsDictionary(kid) is { } kidDict && !kidDict.ContainsKey("T"))
            {
                widgets.Add(kidDict);
            }
        }

        if (widgets.Count == 0)
        {
            widgets.Add(dict);
        }

        return widgets;
    }

    private string PartialName(DictionaryObject dict)
        => reader.GetString(dict, "T") is { } text
            ? FormField.DecodeTextString(text)
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
        RequireFieldType(name, terminal.Field, "Tx", allowUntyped: true);
        terminal.Field["V"] = new StringObject(value);
        ChangedObjects.Add(terminal.Field);
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
        RequireFieldType(name, terminal.Field, "Ch", allowUntyped: false);
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
        ChangedObjects.Add(terminal.Field);
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
        RequireFieldType(name, terminal.Field, "Btn", allowUntyped: false);
        var matched = false;
        foreach (var widget in terminal.Widgets)
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
        ChangedObjects.Add(terminal.Field);
        foreach (var widget in terminal.Widgets)
        {
            widget["AS"] = new NameObject(HasAppearanceState(widget, value) ? value : "Off");
            ChangedObjects.Add(widget);
        }
    }

    private void WriteTextAppearance(Terminal terminal, string value)
    {
        if (FieldAppearances.CanEncode(value) && CanBakeAppearance(terminal.Field))
        {
            // Write the appearance onto every widget so a kid's stale /AP does not override
            // the new value in a viewer; when field and widget merge this is the field. Each
            // widget bakes its own /Rect, since they need not share a size.
            foreach (var widget in terminal.Widgets)
            {
                widget["AP"] = new DictionaryObject { ["N"] = BuildTextAppearance(terminal, widget, value) };
                ChangedObjects.Add(widget);
            }
        }
        else
        {
            // The baked appearance is a single left-aligned WinAnsi line. When that would be
            // wrong - a non-WinAnsi glyph, a multiline/comb layout, a centered/right /Q, or a
            // password whose value must render masked, not in cleartext - leave /AP to the
            // viewer via /NeedAppearances rather than emit a wrong or password-leaking stream.
            Dictionary["NeedAppearances"] = new BooleanObject(true);
            ChangedObjects.Add(Dictionary);
            foreach (var widget in terminal.Widgets)
            {
                if (widget.ContainsKey("AP"))
                {
                    widget["AP"] = new NullObject();
                    ChangedObjects.Add(widget);
                }
            }
        }
    }

    // The baked single left-aligned line is faithful only for a plain text field: not for a
    // multiline, comb or password field, nor a centered/right /Q. Those defer to the viewer.
    private bool CanBakeAppearance(DictionaryObject field)
    {
        var flags = Inherited(field, "Ff") is NumberObject ff ? ff.IntValue : 0;
        if ((flags & (MultilineFlag | PasswordFlag | CombFlag)) != 0)
        {
            return false;
        }

        return Inherited(field, "Q") is not NumberObject quad || quad.IntValue == 0;
    }

    private DocumentObject? Inherited(DictionaryObject dict, string key)
        => FormField.InheritedAttribute(reader, dict, key);

    // Throws when a mutator is applied to the wrong /FT: writing a text /V onto a button,
    // a name /V onto a text field, etc., would emit a spec-invalid field viewers mishandle.
    private void RequireFieldType(string name, DictionaryObject field, string expected, bool allowUntyped)
    {
        if (Inherited(field, "FT") is NameObject ft)
        {
            if (!string.Equals(ft.Value, expected, StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"Field '{name}' is a /{ft.Value} field; this operation requires a /{expected} field.", nameof(name));
            }
        }
        else if (!allowUntyped)
        {
            throw new ArgumentException(
                $"Field '{name}' has no /FT; this operation requires a /{expected} field.", nameof(name));
        }
    }

    // The export values of a choice field's /Opt, or null when it carries none. An
    // /Opt entry is either a text string or a [export, display] pair whose first
    // element is the export value (ISO 32000-1 12.7.4.4).
    private List<string>? OptionValues(DictionaryObject field)
    {
        if (reader.GetArray(field, "Opt") is not { } options)
        {
            return null;
        }

        var values = new List<string>(options.Count);
        foreach (var entry in options)
        {
            values.Add(reader.Resolve(entry) switch
            {
                StringObject text => FormField.DecodeTextString(text.Value),
                ArrayObject pair when pair.Count > 0 && reader.AsString(pair[0]) is { } export
                    => FormField.DecodeTextString(export),
                _ => string.Empty,
            });
        }

        return values;
    }

    private bool HasAppearanceState(DictionaryObject widget, string state)
        => AppearanceStates(widget) is { } states && states.ContainsKey(state);

    private bool HasAppearanceStates(DictionaryObject widget) => AppearanceStates(widget) is not null;

    private DictionaryObject? AppearanceStates(DictionaryObject widget)
        => reader.GetDictionary(widget, "AP") is { } ap ? reader.GetDictionary(ap, "N") : null;

    /// <summary>
    /// Turns a button (check box) field on: its value and appearance state
    /// (<c>/V</c> and <c>/AS</c>) are set to the same on-state name.
    /// </summary>
    /// <param name="name">The field name (<c>/T</c>).</param>
    public void CheckField(string name)
    {
        var terminal = Find(name);
        RequireFieldType(name, terminal.Field, "Btn", allowUntyped: false);
        var on = OnStateName(terminal.Widgets[0]);
        terminal.Field["V"] = new NameObject(on);
        ChangedObjects.Add(terminal.Field);
        foreach (var widget in terminal.Widgets)
        {
            // A widget whose own /AP states do not include the value stays Off: naming a
            // state it has no stream for would render nothing at all.
            widget["AS"] = new NameObject(HasAppearanceStates(widget) && !HasAppearanceState(widget, on) ? "Off" : on);
            ChangedObjects.Add(widget);
        }
    }

    // The on-state is the first non-/Off appearance in the widget's /AP /N; only when
    // the widget has no /AP states do we fall back to the conventional "Yes".
    private string OnStateName(DictionaryObject widget)
    {
        if (AppearanceStates(widget) is { } states)
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

    private StreamObject BuildTextAppearance(Terminal terminal, DictionaryObject widget, string value)
    {
        var (width, height) = RectSize(widget);
        var (daFont, daSize) = DefaultAppearance(terminal, widget);
        var fontSize = daSize > 0.0 ? daSize : FieldAppearances.DefaultFontSize;
        return FieldAppearances.BuildText(
            value, width, height, FieldAppearances.AppearanceFont(daFont, fontSize), owner.FontScope);
    }

    private (double Width, double Height) RectSize(DictionaryObject field)
    {
        var rect = PdfRects.Read(reader, reader.GetArray(field, "Rect"), RectPolicy.DefaultSize(200.0, 14.0));
        return (rect.Width, rect.Height);
    }

    // Resolves the /DA to draw the value with: the field's own /DA wins, else the widget's,
    // else the form default /DA. A /DA size of 0 (auto) is reported as 0 for the caller to map.
    private (string? Font, double Size) DefaultAppearance(Terminal terminal, DictionaryObject widget)
        => FieldAppearances.ParseDefaultAppearance(
            DaString(terminal.Field) ?? DaString(widget) ?? DaString(Dictionary));

    private string? DaString(DictionaryObject dict)
        => reader.GetString(dict, "DA");
}
