#nullable enable
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Filters;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

internal static class FormTestSupport
{
    public const string Fixture = "Documents/form-simple.pdf";

    public static Document LoadFixture()
        => Document.LoadFromStream(new MemoryStream(PdfTestResources.ReadAllBytes(Fixture)));

    public static byte[] Save(Document document)
    {
        using var stream = new MemoryStream();
        document.SaveToStream(stream);
        return stream.ToArray();
    }

    public static DocumentReader Reload(Document document)
        => DocumentReader.Parse(Save(document));

    public static DictionaryObject Catalog(DocumentReader reader)
    {
        Assert.True(reader.Trailer.TryGetValue("Root", out var root));
        return (DictionaryObject)reader.Resolve(root!);
    }

    public static DictionaryObject AcroForm(DocumentReader reader)
    {
        var catalog = Catalog(reader);
        Assert.True(catalog.TryGetValue("AcroForm", out var form));
        return (DictionaryObject)reader.Resolve(form!);
    }

    public static DictionaryObject FirstPage(DocumentReader reader)
    {
        var catalog = Catalog(reader);
        var node = (DictionaryObject)reader.Resolve(catalog["Pages"]);
        while (node.TryGetValue("Kids", out var kidsObject)
            && reader.Resolve(kidsObject!) is ArrayObject kids && kids.Count > 0)
        {
            node = (DictionaryObject)reader.Resolve(kids[0]);
        }

        return node;
    }

    public static DictionaryObject Field(DocumentReader reader, string name)
    {
        var form = AcroForm(reader);
        Assert.True(form.TryGetValue("Fields", out var fieldsObject));
        var fields = (ArrayObject)reader.Resolve(fieldsObject!);
        foreach (var entry in fields)
        {
            var dict = (DictionaryObject)reader.Resolve(entry);
            if (dict.TryGetValue("T", out var title) && reader.Resolve(title!) is StringObject text
                && text.Value == name)
            {
                return dict;
            }
        }

        Assert.Fail($"Field '{name}' not found.");
        return null!;
    }

    public static string? NameValue(DocumentReader reader, DictionaryObject dict, string key)
        => dict.TryGetValue(key, out var value) && reader.Resolve(value!) is NameObject name ? name.Value : null;

    public static string NormalAppearanceText(DocumentReader reader, DictionaryObject field)
    {
        Assert.True(field.TryGetValue("AP", out var apObject), "field has no /AP");
        var ap = (DictionaryObject)reader.Resolve(apObject!);
        Assert.True(ap.TryGetValue("N", out var nObject), "/AP has no /N");
        var normal = reader.Resolve(nObject!);
        if (normal is DictionaryObject states)
        {
            foreach (var key in states.Keys)
            {
                if (reader.Resolve(states[key]) is StreamObject stateStream)
                {
                    return Decode(stateStream);
                }
            }

            Assert.Fail("/AP /N state dictionary held no stream.");
        }

        return Decode((StreamObject)normal);
    }

    public static string Decode(StreamObject stream)
    {
        var data = stream.Data.ToArray();
        if (stream.Dictionary.TryGetValue("Filter", out var filter)
            && filter is NameObject name && name.Value is "FlateDecode" or "Fl")
        {
            data = FlateFilter.Decode(data);
        }

        return Encoding.Latin1.GetString(data);
    }

    public static string PageContentText(DocumentReader reader)
    {
        var page = FirstPage(reader);
        Assert.True(page.TryGetValue("Contents", out var contents), "page has no /Contents");
        var resolved = reader.Resolve(contents!);
        return resolved is StreamObject stream ? Decode(stream) : string.Empty;
    }
}
