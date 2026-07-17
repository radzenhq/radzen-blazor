#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Signing;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// ISO 32000-1 7.5.6: incremental save appends only the changed objects over the verbatim original bytes.
public class IncrementalSaveTests
{
    private sealed class FixedSigner : ISigner
    {
        public byte[] Sign(SignedContent content) => [0x30, 0x00];
    }

    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static byte[] BaseDocument()
    {
        var document = new Document();
        document.Info.Title = "Original title";
        document.Info.Author = "Original author";
        document.Pages.Add(PageSizes.A4).SetContent(Ascii("BT /F1 12 Tf 72 720 Td (page zero) Tj ET"));
        return document.ToArray();
    }

    private static byte[] FormFixture() => PdfTestResources.ReadAllBytes(FormTestSupport.Fixture);

    private static byte[] NestedPageTree()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 2 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Pages /Parent 2 0 R /Kids [4 0 R 5 0 R] /Count 2 >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Type /Page /Parent 3 0 R /MediaBox [0 0 612 792] >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Page /Parent 3 0 R /MediaBox [0 0 612 792] >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 5; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        return pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n").ToArray();
    }

    private static byte[] GenerationTwoPageTree()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [4 0 R 5 2 R] /Count 2 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Length 5 >>\nstream\nfirst\nendstream\nendobj\n")
            .Object(4, "4 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 3 0 R >>\nendobj\n")
            .Object(5, "5 2 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n")
            .Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 4; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(5), 2));
        return pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n").ToArray();
    }

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

        var firstContent = (StreamObject)reader.Resolve(((DictionaryObject)reader.Resolve(kids[0]))["Contents"]);
        Assert.Contains("page zero", Encoding.Latin1.GetString(reader.DecodeStream(firstContent)));
    }

    [Fact]
    public void AppendedLoadedPageCarriesResourcesIncrementally()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Fonted append", BuildTestSupport.Latin);
        var other = Load(builder.ToArray());

        var original = BaseDocument();
        var document = Load(original);
        document.Append(other);

        var updated = SaveIncremental(document);
        AssertVerbatimPrefix(original, updated);

        var reader = DocumentReader.Parse(updated);
        var pages = (DictionaryObject)reader.Resolve(FormTestSupport.Catalog(reader)["Pages"]);
        var kids = (ArrayObject)reader.Resolve(pages["Kids"]);
        var appended = (DictionaryObject)reader.Resolve(kids[^1]);

        var resources = reader.GetDictionary(appended, "Resources");
        Assert.NotNull(resources);
        var fonts = reader.GetDictionary(resources!, "Font");
        Assert.NotNull(fonts);
        Assert.NotEmpty(fonts!.Keys);
    }

    [Fact]
    public void AppendedPageWithAuthoredElementsThrows()
    {
        var document = Load(BaseDocument());
        var page = document.Pages.Add(PageSizes.A4);
        page.Content.Add(new TextContent("authored", Unit.FromPoint(72), Unit.FromPoint(720)));

        Assert.Throws<NotSupportedException>(() => SaveIncremental(document));
    }

    [Fact]
    public void AddedAnnotationIsAppendedAndReParses()
    {
        var original = BaseDocument();
        var document = Load(original);
        document.Pages[0].Annotations.Add(new HighlightAnnotation(PdfRect.FromSize(10, 20, 100, 20))
        {
            Contents = "incremental note",
        });

        var updated = SaveIncremental(document);
        AssertVerbatimPrefix(original, updated);

        var reloaded = Load(updated);
        var annotation = Assert.IsType<HighlightAnnotation>(Assert.Single(reloaded.Pages[0].Annotations));
        Assert.Equal("incremental note", annotation.Contents);
        var reader = DocumentReader.Parse(updated);
        var page = DocumentLoadTests.Kid(reader, 0);
        var dictionary = reader.AsDictionary(Assert.Single(reader.GetArray(page, "Annots")!))!;
        var appearance = reader.GetDictionary(dictionary, "AP")!;
        Assert.IsType<StreamObject>(reader.Resolve(appearance["N"]));
    }

    [Fact]
    public void CompressOutputDoesNotPreventIncrementalAnnotationSave()
    {
        var original = BaseDocument();
        var document = Load(original);
        document.CompressOutput = true;
        document.Pages[0].Annotations.Add(new TextAnnotation(PdfRect.FromSize(10, 20, 24, 24))
        {
            Contents = "incremental note",
        });

        var updated = SaveIncremental(document);

        AssertVerbatimPrefix(original, updated);
        var annotation = Assert.IsType<TextAnnotation>(Assert.Single(Load(updated).Pages[0].Annotations));
        Assert.Equal("incremental note", annotation.Contents);
    }

    [Fact]
    public void EditedAndRemovedAnnotationsAreAppendedAndReParse()
    {
        var source = new Document();
        var page = source.Pages.Add();
        page.Annotations.Add(new TextAnnotation(PdfRect.FromSize(10, 20, 24, 24)) { Contents = "old" });
        page.Annotations.Add(new SquareAnnotation(PdfRect.FromSize(40, 20, 24, 24)));
        var original = source.ToArray();
        var document = Load(original);
        Assert.IsType<TextAnnotation>(document.Pages[0].Annotations[0]).Contents = "edited";
        document.Pages[0].Annotations.RemoveAt(1);

        var updated = SaveIncremental(document);
        AssertVerbatimPrefix(original, updated);

        var annotation = Assert.IsType<TextAnnotation>(Assert.Single(Load(updated).Pages[0].Annotations));
        Assert.Equal("edited", annotation.Contents);
    }

    [Fact]
    public void DeletedAndReorderedPagesAreAppendedAndReParse()
    {
        var source = new Document();
        source.Pages.Add().SetContent(Ascii("first"));
        source.Pages.Add().SetContent(Ascii("remove"));
        source.Pages.Add().SetContent(Ascii("last"));
        var original = source.ToArray();
        var document = Load(original);
        document.Pages.RemoveAt(1);
        document.Pages.Move(1, 0);

        var updated = SaveIncremental(document);
        AssertVerbatimPrefix(original, updated);

        var reloaded = Load(updated);
        Assert.Equal(2, reloaded.Pages.Count);
        Assert.Equal("last", Encoding.ASCII.GetString(reloaded.Pages[0].GetContent()!));
        Assert.Equal("first", Encoding.ASCII.GetString(reloaded.Pages[1].GetContent()!));
    }

    // ISO 32000-1 7.3.10: an indirect reference matches on number AND generation.
    [Fact]
    public void ReorderedPageTreeKeepsTheNonZeroGenerationOfAReusedPageNumber()
    {
        var original = GenerationTwoPageTree();
        var document = Load(original);
        Assert.Equal(2, document.Pages.Count);
        document.Pages.Move(1, 0);

        var updated = SaveIncremental(document);
        AssertVerbatimPrefix(original, updated);

        var appended = Encoding.Latin1.GetString(updated, original.Length, updated.Length - original.Length);
        Assert.Contains("5 2 R", appended, StringComparison.Ordinal);
        Assert.DoesNotContain("5 0 R", appended, StringComparison.Ordinal);

        var reader = DocumentReader.Parse(updated);
        var pages = (DictionaryObject)reader.Resolve(FormTestSupport.Catalog(reader)["Pages"]);
        var kids = (ArrayObject)reader.Resolve(pages["Kids"]);
        Assert.Equal(2, ((ReferenceObject)kids[0]).Generation);
    }

    [Fact]
    public void InsertedPageIsAppendedAndPlacedInTheUpdatedPageTree()
    {
        var source = new Document();
        source.Pages.Add().SetContent(Ascii("first"));
        source.Pages.Add().SetContent(Ascii("last"));
        var original = source.ToArray();
        var document = Load(original);
        document.Pages.Add().SetContent(Ascii("inserted"));
        document.Pages.Move(2, 1);

        var updated = SaveIncremental(document);
        AssertVerbatimPrefix(original, updated);

        var reloaded = Load(updated);
        Assert.Equal(3, reloaded.Pages.Count);
        Assert.Equal("first", Encoding.ASCII.GetString(reloaded.Pages[0].GetContent()!));
        Assert.Equal("inserted", Encoding.ASCII.GetString(reloaded.Pages[1].GetContent()!));
        Assert.Equal("last", Encoding.ASCII.GetString(reloaded.Pages[2].GetContent()!));
    }

    [Fact]
    public void ReorderingNestedPageTreeThrowsWithFullSaveDirection()
    {
        var document = Load(NestedPageTree());
        document.Pages.Move(1, 0);

        var exception = Assert.Throws<NotSupportedException>(() => SaveIncremental(document));

        Assert.Contains("nested or shared page tree", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SaveToStream", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadedPageBoxesAreAppendedAndReParse()
    {
        var original = BaseDocument();
        var document = Load(original);
        document.Pages[0].MediaBox = PdfRect.FromSize(10, 20, 300, 400);
        document.Pages[0].CropBox = PdfRect.FromSize(20, 30, 250, 350);

        var updated = SaveIncremental(document);
        AssertVerbatimPrefix(original, updated);

        var page = Load(updated).Pages[0];
        Assert.Equal(PdfRect.FromSize(10, 20, 300, 400), page.MediaBox);
        Assert.Equal(PdfRect.FromSize(20, 30, 250, 350), page.CropBox);
    }

    [Fact]
    public void SignedRevisionRemainsAnExactPrefixAfterAnnotationEdit()
    {
        var signed = PdfSigner.Sign(BaseDocument(), new SignatureOptions { SignerName = "Signer" }, new FixedSigner());
        var document = Load(signed);
        document.Pages[0].Annotations.Add(new TextAnnotation(PdfRect.FromSize(10, 20, 24, 24)) { Contents = "after signing" });

        var updated = SaveIncremental(document);

        AssertVerbatimPrefix(signed, updated);
        var reader = DocumentReader.Parse(updated);
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var form = reader.GetDictionary(catalog, "AcroForm")!;
        var fields = reader.GetArray(form, "Fields")!;
        var signatureField = reader.AsDictionary(Assert.Single(fields))!;
        var signature = reader.GetDictionary(signatureField, "V")!;
        var byteRange = reader.GetArray(signature, "ByteRange")!;
        var signedTail = Assert.IsType<NumberObject>(byteRange[2]).IntValue
            + Assert.IsType<NumberObject>(byteRange[3]).IntValue;
        Assert.Equal(signed.Length, signedTail);
        var page = DocumentLoadTests.Kid(reader, 0);
        var annotations = reader.GetArray(page, "Annots")!;
        Assert.Contains(annotations, value => reader.GetName(reader.AsDictionary(value)!, "Subtype") == "Widget");
        Assert.Contains(annotations, value => reader.GetName(reader.AsDictionary(value)!, "Subtype") == "Text");
        Assert.Equal("after signing", Assert.IsType<TextAnnotation>(Assert.Single(Load(updated).Pages[0].Annotations)).Contents);
    }

    [Fact]
    public void LoadedContentEditThrowsWithFullSaveDirection()
    {
        var document = Load(BaseDocument());
        document.Pages[0].SetContent(Ascii("replacement"));

        var exception = Assert.Throws<NotSupportedException>(() => SaveIncremental(document));

        Assert.Contains("page content", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SaveToStream", exception.Message, StringComparison.Ordinal);
    }


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
