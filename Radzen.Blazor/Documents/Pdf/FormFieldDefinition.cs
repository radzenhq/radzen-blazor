using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;


/// <summary>
/// The base definition of an interactive form field to create on a
/// <see cref="Document"/>. Add definitions to <see cref="Document.FormFields"/>;
/// on save each one becomes a widget annotation on its page and an entry in the
/// catalog <c>/AcroForm /Fields</c> with a generated appearance stream.
/// </summary>
public abstract class FormFieldDefinition
{
    private protected FormFieldDefinition(string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        Name = name;
    }

    /// <summary>Gets the field name (<c>/T</c>).</summary>
    public string Name { get; }

    /// <summary>Gets or sets the zero-based index of the page carrying the field.</summary>
    public int PageIndex { get; set; }

    /// <summary>Gets or sets the left edge of the field rectangle, in PDF user space (origin at the bottom-left of the page).</summary>
    public Unit X { get; set; }

    /// <summary>Gets or sets the bottom edge of the field rectangle, in PDF user space (origin at the bottom-left of the page).</summary>
    public Unit Y { get; set; }

    /// <summary>Gets or sets the width of the field rectangle.</summary>
    public Unit Width { get; set; }

    /// <summary>Gets or sets the height of the field rectangle.</summary>
    public Unit Height { get; set; }

    // Draws this field's flattened static appearance onto its page (the redraw a viewer
    // would otherwise generate from /V), replacing the per-type dispatch in FormWriter.
    internal abstract void WriteFlattenedContent(Page page);

    // The (value, font) a text-bearing field renders, used to seed the created form's
    // /DR fonts and /NeedAppearances; null for a field that carries no such text.
    internal virtual (string Value, Font Font)? TextAppearance => null;

    // Emits this definition as one or more field/widget annotations on save, adding each
    // to the AcroForm fields list and to the created page bindings. The common single-widget
    // scaffolding lives here; per-type entries go through PopulateWidget, and a field with a
    // different structure (a radio group) overrides this outright.
    internal virtual void EmitCreatedField(
        DocumentWriter writer,
        ReferenceObject pageReference,
        List<DocumentObject> fields,
        List<(int, ReferenceObject)> created)
    {
        var x = X.Point;
        var y = Y.Point;
        var width = Width.Point;
        var height = Height.Point;
        var widget = new DictionaryObject
        {
            ["Type"] = new NameObject("Annot"),
            ["Subtype"] = new NameObject("Widget"),
            ["T"] = new StringObject(Name),
            ["Rect"] = new ArrayObject
            {
                new NumberObject(x),
                new NumberObject(y),
                new NumberObject(x + width),
                new NumberObject(y + height),
            },
            ["F"] = new NumberObject(4),
            ["P"] = pageReference,
        };

        PopulateWidget(widget, writer, width, height);

        var reference = writer.Add(widget);
        fields.Add(reference);
        created.Add((PageIndex, reference));
    }

    // Stamps the type-specific /FT, /V, /DA, appearance and flag entries onto the shared
    // widget dictionary. No-op on the base; each single-widget field type overrides it.
    private protected virtual void PopulateWidget(DictionaryObject widget, DocumentWriter writer, double width, double height)
    {
    }
}


/// <summary>
/// A single-line text field (<c>/FT /Tx</c>) with an initial value and the
/// base-14 font and size its <c>/DA</c> declares.
/// </summary>
/// <param name="name">The field name (<c>/T</c>).</param>
public sealed class TextFieldDefinition(string name) : FormFieldDefinition(name)
{
    /// <summary>Gets or sets the initial field value (<c>/V</c>). Defaults to empty.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets the font the value renders with. Base-14 families only; defaults to Helvetica 10pt.</summary>
    public Font Font { get; set; } = new();

    /// <summary>
    /// Gets or sets a value indicating whether the field accepts multiple lines of
    /// text (the Multiline flag, <c>/Ff</c> bit 13). Defaults to <c>false</c>.
    /// </summary>
    public bool Multiline { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the field masks its input (the
    /// Password flag, <c>/Ff</c> bit 14). Defaults to <c>false</c>.
    /// </summary>
    public bool Password { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the value is laid out in equally
    /// spaced comb cells (the Comb flag, <c>/Ff</c> bit 25). A comb field is only
    /// meaningful together with <see cref="MaxLength"/>. Defaults to <c>false</c>.
    /// </summary>
    public bool Comb { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of characters the field accepts
    /// (<c>/MaxLen</c>), or <c>null</c> to leave it unbounded (the default). It
    /// also sets the number of comb cells when <see cref="Comb"/> is set.
    /// </summary>
    public int? MaxLength { get; set; }

    internal override (string Value, Font Font)? TextAppearance => (Value, Font);

    internal override void WriteFlattenedContent(Page page)
    {
        if (Value.Length == 0)
        {
            return;
        }

        var baseline = FieldAppearances.Baseline(Height.Point, Font.Size);
        page.Content.Add(new TextContent(
            Value,
            X + Unit.FromPoint(2.0),
            Y + Unit.FromPoint(baseline))
        {
            Font = Font,
        });
    }

    private protected override void PopulateWidget(DictionaryObject widget, DocumentWriter writer, double width, double height)
    {
        widget["FT"] = new NameObject("Tx");
        widget["V"] = new StringObject(Value);
        widget["DA"] = new StringObject(FormWriter.DefaultAppearanceOf(Font));
        var flags = FormWriter.TextFieldFlags(this);
        if (flags != 0)
        {
            widget["Ff"] = new NumberObject(flags);
        }

        if (MaxLength is { } maxLength)
        {
            widget["MaxLen"] = new NumberObject(maxLength);
        }

        if (FieldAppearances.CanEncode(Value))
        {
            widget["AP"] = new DictionaryObject
            {
                ["N"] = writer.Add(FieldAppearances.BuildText(Value, width, height, Font)),
            };
        }
    }
}


/// <summary>
/// A check box field (<c>/FT /Btn</c>) whose on-state is named <c>Yes</c>.
/// </summary>
/// <param name="name">The field name (<c>/T</c>).</param>
public sealed class CheckBoxFieldDefinition(string name) : FormFieldDefinition(name)
{
    /// <summary>Gets or sets a value indicating whether the box is initially checked.</summary>
    public bool Checked { get; set; }

    internal override void WriteFlattenedContent(Page page)
    {
        if (!Checked)
        {
            return;
        }

        page.Content.Add(FieldAppearances.CheckMark(
            X.Point, Y.Point, Width.Point, Height.Point));
    }

    private protected override void PopulateWidget(DictionaryObject widget, DocumentWriter writer, double width, double height)
    {
        var state = Checked ? "Yes" : "Off";
        widget["FT"] = new NameObject("Btn");
        widget["V"] = new NameObject(state);
        widget["AS"] = new NameObject(state);
        widget["AP"] = new DictionaryObject
        {
            ["N"] = new DictionaryObject
            {
                ["Yes"] = writer.Add(FieldAppearances.BuildCheck(width, height)),
                ["Off"] = writer.Add(FieldAppearances.BuildOff(width, height)),
            },
        };
    }
}


/// <summary>
/// One selectable option of a <see cref="RadioGroupFieldDefinition"/>. Each option
/// becomes a widget annotation (a <c>/Kids</c> entry of the group field) at its own
/// rectangle, with its <see cref="Value"/> used as the widget's on-state name.
/// </summary>
public sealed class RadioOptionDefinition
{
    /// <summary>Initializes a new instance of the <see cref="RadioOptionDefinition"/> class.</summary>
    /// <param name="value">The export value; also the widget's on-state name. Must not be <c>Off</c>.</param>
    public RadioOptionDefinition(string value)
    {
        ArgumentException.ThrowIfNullOrEmpty(value);
        if (string.Equals(value, "Off", StringComparison.Ordinal))
        {
            throw new ArgumentException("A radio option value cannot be 'Off'; that name is reserved for the unselected state.", nameof(value));
        }

        Value = value;
    }

    /// <summary>Gets the export value; also the widget's on-state name.</summary>
    public string Value { get; }

    /// <summary>Gets or sets the left edge of the option rectangle, in PDF user space (origin at the bottom-left of the page).</summary>
    public Unit X { get; set; }

    /// <summary>Gets or sets the bottom edge of the option rectangle, in PDF user space (origin at the bottom-left of the page).</summary>
    public Unit Y { get; set; }

    /// <summary>Gets or sets the width of the option rectangle.</summary>
    public Unit Width { get; set; }

    /// <summary>Gets or sets the height of the option rectangle.</summary>
    public Unit Height { get; set; }
}


/// <summary>
/// A radio button group (<c>/FT /Btn</c> with the Radio flag, <c>/Ff</c> bit 16).
/// The group saves as a parent field holding <c>/V</c> and <c>/DV</c>, with one kid
/// widget annotation per <see cref="Options"/> entry. Option geometry comes from each
/// <see cref="RadioOptionDefinition"/>; the rectangle inherited from
/// <see cref="FormFieldDefinition"/> is ignored. At least two options with distinct
/// values are required, and at most one may be selected.
/// </summary>
/// <param name="name">The field name (<c>/T</c>).</param>
public sealed class RadioGroupFieldDefinition(string name) : FormFieldDefinition(name)
{
    /// <summary>Gets the selectable options. At least two, with distinct values.</summary>
    public List<RadioOptionDefinition> Options { get; } = [];

    /// <summary>
    /// Gets or sets the value of the initially selected option, or <c>null</c> when
    /// none is selected. Must match the <see cref="RadioOptionDefinition.Value"/> of
    /// one of the <see cref="Options"/>.
    /// </summary>
    public string? SelectedValue { get; set; }

    internal override void WriteFlattenedContent(Page page)
    {
        var selected = SelectedValue is null
            ? null
            : Options.Find(option => string.Equals(option.Value, SelectedValue, StringComparison.Ordinal));
        if (selected is not null)
        {
            page.Content.Add(FieldAppearances.RadioDot(
                selected.X.Point, selected.Y.Point, selected.Width.Point, selected.Height.Point));
        }
    }

    // Emits a radio group as a parent /Btn field (Radio flag, /V and /DV holding
    // the selected on-state) with one kid widget per option, each carrying its own
    // /AP /N keyed by the option value plus /Off and an /AS matching the selection.
    internal override void EmitCreatedField(
        DocumentWriter writer,
        ReferenceObject pageReference,
        List<DocumentObject> fields,
        List<(int, ReferenceObject)> created)
    {
        if (Options.Count < 2)
        {
            throw new InvalidOperationException($"Radio group '{Name}' needs at least two options.");
        }

        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var option in Options)
        {
            if (!values.Add(option.Value))
            {
                throw new InvalidOperationException($"Radio group '{Name}' has duplicate option value '{option.Value}'.");
            }
        }

        if (SelectedValue is not null && !values.Contains(SelectedValue))
        {
            throw new InvalidOperationException($"Radio group '{Name}' selects '{SelectedValue}' which is not among its options.");
        }

        var state = SelectedValue ?? "Off";
        var parent = new DictionaryObject
        {
            ["FT"] = new NameObject("Btn"),
            ["T"] = new StringObject(Name),
            ["Ff"] = new NumberObject(FormWriter.RadioFlag),
            ["V"] = new NameObject(state),
            ["DV"] = new NameObject(state),
        };
        var parentReference = writer.Add(parent);

        var kids = new ArrayObject();
        foreach (var option in Options)
        {
            var x = option.X.Point;
            var y = option.Y.Point;
            var width = option.Width.Point;
            var height = option.Height.Point;
            var selected = string.Equals(option.Value, SelectedValue, StringComparison.Ordinal);
            var kid = new DictionaryObject
            {
                ["Type"] = new NameObject("Annot"),
                ["Subtype"] = new NameObject("Widget"),
                ["Rect"] = new ArrayObject
                {
                    new NumberObject(x),
                    new NumberObject(y),
                    new NumberObject(x + width),
                    new NumberObject(y + height),
                },
                ["F"] = new NumberObject(4),
                ["P"] = pageReference,
                ["Parent"] = parentReference,
                ["AS"] = new NameObject(selected ? option.Value : "Off"),
                ["AP"] = new DictionaryObject
                {
                    ["N"] = new DictionaryObject
                    {
                        [option.Value] = writer.Add(FieldAppearances.BuildRadio(width, height, selected: true)),
                        ["Off"] = writer.Add(FieldAppearances.BuildRadio(width, height, selected: false)),
                    },
                },
            };

            var kidReference = writer.Add(kid);
            kids.Add(kidReference);
            created.Add((PageIndex, kidReference));
        }

        parent["Kids"] = kids;
        fields.Add(parentReference);
    }
}


/// <summary>
/// A choice field (<c>/FT /Ch</c>) listing <see cref="Options"/> in <c>/Opt</c>.
/// Saves as a combo box (<c>/Ff</c> bit 18) when <see cref="ComboBox"/> is set,
/// otherwise as a list box. The generated appearance shows the selected
/// <see cref="Value"/> as plain text for both variants; the list box does not
/// paint the option list or a selection highlight.
/// </summary>
/// <param name="name">The field name (<c>/T</c>).</param>
public sealed class ChoiceFieldDefinition(string name) : FormFieldDefinition(name)
{
    /// <summary>Gets the options exported to <c>/Opt</c>.</summary>
    public List<string> Options { get; } = [];

    /// <summary>Gets or sets the selected value (<c>/V</c>). Defaults to empty.</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the field is a combo box rather than a list box.</summary>
    public bool ComboBox { get; set; }

    /// <summary>Gets or sets the font the value renders with. Base-14 families only; defaults to Helvetica 10pt.</summary>
    public Font Font { get; set; } = new();

    internal override (string Value, Font Font)? TextAppearance => (Value, Font);

    internal override void WriteFlattenedContent(Page page)
    {
        if (Value.Length == 0)
        {
            return;
        }

        var baseline = FieldAppearances.Baseline(Height.Point, Font.Size);
        page.Content.Add(new TextContent(
            Value,
            X + Unit.FromPoint(2.0),
            Y + Unit.FromPoint(baseline))
        {
            Font = Font,
        });
    }

    private protected override void PopulateWidget(DictionaryObject widget, DocumentWriter writer, double width, double height)
    {
        var options = new ArrayObject();
        foreach (var option in Options)
        {
            options.Add(new StringObject(option));
        }

        widget["FT"] = new NameObject("Ch");
        widget["Opt"] = options;
        if (ComboBox)
        {
            widget["Ff"] = new NumberObject(FormWriter.ComboFlag);
        }

        widget["V"] = new StringObject(Value);
        widget["DA"] = new StringObject(FormWriter.DefaultAppearanceOf(Font));
        if (FieldAppearances.CanEncode(Value))
        {
            widget["AP"] = new DictionaryObject
            {
                ["N"] = writer.Add(FieldAppearances.BuildText(Value, width, height, Font)),
            };
        }
    }
}
