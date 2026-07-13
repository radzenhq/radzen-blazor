#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Model-level incremental save (ISO 32000-1 7.5.6): a loaded, edited document is
// re-saved by appending only the changed objects over the verbatim original bytes.
// Each test asserts the incremental contract - the original is an exact prefix, the
// appended xref chains via /Prev, the edit re-parses, untouched objects still
// resolve from the original revision - plus determinism and the loaded-only guard.
public class IncrementalSaveTests
{
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    // A minimal one-page loadable document carrying an /Info dictionary.
    private static byte[] BaseDocument()
    {
        var document = new Document();
        document.Info.Title = "Original title";
        document.Info.Author = "Original author";
        document.Pages.Add(PageSizes.A4).SetContent(Ascii("BT /F1 12 Tf 72 720 Td (page zero) Tj ET"));
        return document.ToArray();
    }

    private static byte[] FormFixture() => PdfTestResources.ReadAllBytes(FormTestSupport.Fixture);

    private static Document Load(byte[] bytes) => Document.LoadFromStream(new MemoryStream(bytes));

    private static byte[] SaveIncremental(Document document)
    {
        using var stream = new MemoryStream();
        document.SaveIncremental(stream);
        return stream.ToArray();
    }

    private static void AssertVerbatimPrefix(byte[] original, byte[] updated)
    {
        Assert.True(updated.Length > original.Length, "incremental output must be longer than the original");
        Assert.True(updated.AsSpan(0, original.Length).SequenceEqual(original),
            "the original bytes must be an exact prefix of the incremental output");
    }

    private static long OriginalStartXref(byte[] data)
    {
        var text = Encoding.Latin1.GetString(data);
        var index = text.LastIndexOf("startxref", StringComparison.Ordinal);
        Assert.True(index >= 0);
        var rest = text[(index + "startxref".Length)..].TrimStart('\r', '\n', ' ');
        var end = 0;
        while (end < rest.Length && char.IsAsciiDigit(rest[end]))
        {
            end++;
        }

        return long.Parse(rest[..end]);
    }

    // --- Guards ---

    [Fact]
    public void FreshlyBuiltDocumentThrows()
    {
        var document = new Document();
        document.Pages.Add(PageSizes.A4);

        Assert.Throws<InvalidOperationException>(() => SaveIncremental(document));
    }

    [Fact]
    public void LoadedButUnchangedDocumentThrows()
    {
        var document = Load(BaseDocument());

        Assert.Throws<InvalidOperationException>(() => SaveIncremental(document));
    }

    // --- Metadata edit ---

    [Fact]
    public void MetadataEditIsAppendedAndReParses()
    {
        var original = BaseDocument();
        var document = Load(original);
        document.Info.Title = "Updated title";

        var updated = SaveIncremental(document);
        AssertVerbatimPrefix(original, updated);

        var reader = DocumentReader.Parse(updated);
        var info = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Info"]));
        Assert.Equal("Updated title", ((StringObject)reader.Resolve(info["Title"])).Value);
        // An untouched modeled field is preserved from the original /Info.
        Assert.Equal("Original author", ((StringObject)reader.Resolve(info["Author"])).Value);
        Assert.Equal(OriginalStartXref(original), ((NumberObject)reader.Trailer["Prev"]).IntValue);
    }

    [Fact]
    public void MetadataEditPreservesTheLoadedForm()
    {
        var original = FormFixture();
        var document = Load(original);
        document.Info.Title = "Signed Agreement";

        var updated = SaveIncremental(document);
        AssertVerbatimPrefix(original, updated);

        var reader = DocumentReader.Parse(updated);
        var catalog = FormTestSupport.Catalog(reader);
        Assert.True(catalog.TryGetValue("AcroForm", out _));
        Assert.Equal("Signed Agreement", ((StringObject)reader.Resolve(
            ((DictionaryObject)reader.Resolve(reader.Trailer["Info"]))["Title"])).Value);
    }

    // --- Form fill ---

    [Fact]
    public void FilledFieldIsAppendedAndReParses()
    {
        var original = FormFixture();
        var document = Load(original);
        document.AcroForm!.FillField("Name", "Radzen Ltd");

        var updated = SaveIncremental(document);
        AssertVerbatimPrefix(original, updated);

        var reader = DocumentReader.Parse(updated);
        var field = FormTestSupport.Field(reader, "Name");
        Assert.Equal("Radzen Ltd", ((StringObject)reader.Resolve(field["V"])).Value);
        Assert.Contains("Radzen Ltd", FormTestSupport.NormalAppearanceText(reader, field));
    }

    [Fact]
    public void FillingOneFieldLeavesOtherFieldsResolvableFromOriginalRevision()
    {
        var original = FormFixture();
        var document = Load(original);
        document.AcroForm!.FillField("Name", "Radzen Ltd");

        var reader = DocumentReader.Parse(SaveIncremental(document));

        // The untouched checkbox field was never re-emitted; it must still resolve
        // (from the original revision) with its original /Off value and its /Rect.
        var agree = FormTestSupport.Field(reader, "Agree");
        Assert.Equal("Off", ((NameObject)reader.Resolve(agree["V"])).Value);
        Assert.IsType<ArrayObject>(reader.Resolve(agree["Rect"]));
        Assert.Equal("Widget", ((NameObject)reader.Resolve(agree["Subtype"])).Value);
    }

    [Fact]
    public void CheckedBoxIsAppendedAndReParses()
    {
        var original = FormFixture();
        var document = Load(original);
        document.AcroForm!.CheckField("Agree");

        var updated = SaveIncremental(document);
        AssertVerbatimPrefix(original, updated);

        var reader = DocumentReader.Parse(updated);
        var agree = FormTestSupport.Field(reader, "Agree");
        Assert.Equal("Yes", ((NameObject)reader.Resolve(agree["V"])).Value);
        Assert.Equal("Yes", ((NameObject)reader.Resolve(agree["AS"])).Value);
    }

    // --- Append page ---

    [Fact]
    public void AppendedPageIsAppendedAndReParses()
    {
        var original = BaseDocument();
        var document = Load(original);
        Assert.Single(document.Pages);

        var page = document.Pages.Add(PageSizes.Letter);
        page.SetContent(Ascii("BT /F1 12 Tf 72 720 Td (appended page) Tj ET"));

        var updated = SaveIncremental(document);
        AssertVerbatimPrefix(original, updated);

        var reader = DocumentReader.Parse(updated);
        var pages = (DictionaryObject)reader.Resolve(FormTestSupport.Catalog(reader)["Pages"]);
        Assert.Equal(2, ((NumberObject)reader.Resolve(pages["Count"])).IntValue);
        var kids = (ArrayObject)reader.Resolve(pages["Kids"]);
        Assert.Equal(2, kids.Count);

        var appended = (DictionaryObject)reader.Resolve(kids[1]);
        var content = (StreamObject)reader.Resolve(appended["Contents"]);
        Assert.Contains("appended page", Encoding.Latin1.GetString(reader.DecodeStream(content)));

        // The original first page still resolves and keeps its content.
        var firstContent = (StreamObject)reader.Resolve(((DictionaryObject)reader.Resolve(kids[0]))["Contents"]);
        Assert.Contains("page zero", Encoding.Latin1.GetString(reader.DecodeStream(firstContent)));
    }

    [Fact]
    public void AppendedPageWithAuthoredElementsThrows()
    {
        var document = Load(BaseDocument());
        var page = document.Pages.Add(PageSizes.A4);
        page.Content.Add(new TextContent("authored", Unit.FromPoint(72), Unit.FromPoint(720)));

        Assert.Throws<NotSupportedException>(() => SaveIncremental(document));
    }

    // --- Determinism ---

    [Fact]
    public void SameEditsProduceIdenticalBytes()
    {
        var original = FormFixture();

        byte[] Build()
        {
            var document = Load(original);
            document.Info.Title = "Deterministic";
            document.AcroForm!.FillField("Name", "Radzen Ltd");
            var page = document.Pages.Add(PageSizes.Letter);
            page.SetContent(Ascii("BT (extra) Tj ET"));
            return SaveIncremental(document);
        }

        Assert.Equal(Build(), Build());
    }

    // --- Combined edit re-parses as a whole ---

    [Fact]
    public void CombinedEditsAllReParse()
    {
        var original = FormFixture();
        var document = Load(original);
        document.Info.Title = "Combined";
        document.AcroForm!.FillField("Name", "Radzen Ltd");
        document.Pages.Add(PageSizes.Letter).SetContent(Ascii("BT (combined) Tj ET"));

        var updated = SaveIncremental(document);
        AssertVerbatimPrefix(original, updated);

        var reader = DocumentReader.Parse(updated);
        Assert.Equal("Combined", ((StringObject)reader.Resolve(
            ((DictionaryObject)reader.Resolve(reader.Trailer["Info"]))["Title"])).Value);
        Assert.Equal("Radzen Ltd", ((StringObject)reader.Resolve(FormTestSupport.Field(reader, "Name")["V"])).Value);
        var pages = (DictionaryObject)reader.Resolve(FormTestSupport.Catalog(reader)["Pages"]);
        Assert.Equal(2, ((NumberObject)reader.Resolve(pages["Count"])).IntValue);
    }
}
