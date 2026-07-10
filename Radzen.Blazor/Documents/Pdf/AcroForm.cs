using Radzen.Documents.Pdf.Objects;
using System;
using System.Collections.Generic;

namespace Radzen.Documents.Pdf;

#nullable enable

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

    internal AcroForm(DocumentReader reader, DictionaryObject dictionary)
    {
        this.reader = reader;
        Dictionary = dictionary;

        if (dictionary.TryGetValue("Fields", out var fieldsObject)
            && reader.Resolve(fieldsObject!) is ArrayObject entries)
        {
            foreach (var entry in entries)
            {
                if (reader.Resolve(entry) is DictionaryObject field)
                {
                    fields.Add(new FormField(reader, field));
                }
            }
        }
    }

    internal DictionaryObject Dictionary { get; }

    /// <summary>Gets the terminal fields of the form.</summary>
    public IReadOnlyList<FormField> Fields => fields;

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

        var field = Find(name).Dictionary;
        field["V"] = new StringObject(value);
        field["AP"] = new DictionaryObject { ["N"] = BuildTextAppearance(field, value) };
    }

    /// <summary>
    /// Turns a button (check box) field on: its value and appearance state
    /// (<c>/V</c> and <c>/AS</c>) are set to the same on-state name.
    /// </summary>
    /// <param name="name">The field name (<c>/T</c>).</param>
    public void CheckField(string name)
    {
        var field = Find(name).Dictionary;
        field["V"] = new NameObject(OnState);
        field["AS"] = new NameObject(OnState);
    }

    private FormField Find(string name)
    {
        foreach (var field in fields)
        {
            if (string.Equals(field.Name, name, StringComparison.Ordinal))
            {
                return field;
            }
        }

        throw new ArgumentException($"Field '{name}' not found.", nameof(name));
    }

    private StreamObject BuildTextAppearance(DictionaryObject field, string value)
    {
        var (width, height) = RectSize(field);
        const double fontSize = 12.0;
        var baseline = height > fontSize ? (height - fontSize) / 2.0 : 2.0;

        var writer = new ContentWriter();
        writer.WriteRaw("/Tx BMC\nq\n");
        new TextContent(value, Unit.FromPoint(2.0), Unit.FromPoint(baseline))
        {
            Font = new Font { Size = fontSize },
        }.Emit(writer);
        writer.WriteRaw("Q\nEMC\n");

        ArrayObject bbox =
        [
            new NumberObject(0.0),
            new NumberObject(0.0),
            new NumberObject(width),
            new NumberObject(height),
        ];

        var appearance = new StreamObject(writer.ToArray());
        appearance.Dictionary["Type"] = new NameObject("XObject");
        appearance.Dictionary["Subtype"] = new NameObject("Form");
        appearance.Dictionary["BBox"] = bbox;

        var resources = BuildFontResources(writer);
        if (resources is not null)
        {
            appearance.Dictionary["Resources"] = resources;
        }

        return appearance;
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

    private static DictionaryObject? BuildFontResources(ContentWriter writer)
    {
        DictionaryObject? fonts = null;
        foreach (var (baseFont, key) in writer.Fonts)
        {
            fonts ??= new DictionaryObject();
            fonts[key] = new DictionaryObject
            {
                ["Type"] = new NameObject("Font"),
                ["Subtype"] = new NameObject("Type1"),
                ["BaseFont"] = new NameObject(baseFont),
                ["Encoding"] = new NameObject("WinAnsiEncoding"),
            };
        }

        return fonts is null ? null : new DictionaryObject { ["Font"] = fonts };
    }
}
