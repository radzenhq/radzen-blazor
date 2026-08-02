#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class FormAppearanceConformanceTests
{
    private const string Cyrillic = "Атанас";

    private static Document WithTextInput(out Paragraph paragraph)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Form";
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Normal.Font.Family = BuildTestSupport.Latin;

        paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada" });
        return document;
    }

    private static PortableDocument Render(DocumentRenderer renderer)
        => renderer.Render(WithTextInput(out _));

    private static byte[] Save(PortableDocument document)
    {
        using var stream = new MemoryStream();
        document.SaveToStream(stream);
        return stream.ToArray();
    }

    private static byte[] MultilineFieldDocument()
    {
        var model = new Document();
        BuildTestSupport.AddText(model.Sections.Add(), "Form", "Helvetica");
        var document = new DocumentRenderer().Render(model);
        document.FormFields.Add(new TextFieldDefinition("Notes")
        {
            X = 100,
            Y = 600,
            Width = 250,
            Height = 80,
            Value = "line",
            Multiline = true,
        });

        return document.ToArray();
    }

    private static byte[] DonorFormWithNeedAppearances()
    {
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << >> /Annots [5 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] /NeedAppearances true /DA (/Helv 0 Tf 0 g) >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (Name) /V () /P 3 0 R /Rect [100 700 350 720] /DA (/Helv 8 Tf 0 g) /AP << /N 6 0 R >> >>\nendobj\n")
            .Object(6, "6 0 obj\n<< /Type /XObject /Subtype /Form /BBox [0 0 250 20] /Length 0 >>\nstream\n\nendstream\nendobj\n");
        return FixturePdf.Wrap(pdf, 7);
    }

    [Fact]
    public void FillFieldWithUnbakeableValueUnderPdfUaThrows()
    {
        var document = Render(new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 });

        var error = Assert.Throws<InvalidOperationException>(() => document.AcroForm!.FillField("name", Cyrillic));

        Assert.Contains("PDF/UA requires every widget annotation to carry an appearance stream", error.Message);
        Assert.Contains("'name'", error.Message);
    }

    [Fact]
    public void FillFieldWithUnbakeableValueUnderPdfAThrows()
    {
        var document = Render(new DocumentRenderer { Conformance = PdfAConformance.PdfA2B });

        var error = Assert.Throws<InvalidOperationException>(() => document.AcroForm!.FillField("name", Cyrillic));

        Assert.Contains("PDF/A requires every widget annotation to carry an appearance stream", error.Message);
        Assert.Contains("PdfAConformance.None", error.Message);
    }

    [Fact]
    public void FillFieldWithUnbakeableValueWithoutClaimKeepsNeedAppearancesBehaviour()
    {
        var document = Render(new DocumentRenderer());
        document.AcroForm!.FillField("name", Cyrillic);

        var reader = DocumentReader.Parse(Save(document));
        var form = FormTestSupport.AcroForm(reader);

        Assert.True(form.TryGetValue("NeedAppearances", out var need)
            && reader.Resolve(need!) is BooleanObject { Value: true });

        var field = FormTestSupport.Field(reader, "name");
        Assert.True(field.TryGetValue("AP", out var appearance));
        Assert.IsType<NullObject>(reader.Resolve(appearance!));
    }

    [Fact]
    public void ClaimAddedAfterLoadingAMultilineFieldFailsAtSaveToStream()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(MultilineFieldDocument()));
        document.Conformance = PdfAConformance.PdfA2B;

        var error = Assert.Throws<InvalidOperationException>(() => Save(document));

        Assert.Contains("PDF/A forbids the interactive form flag", error.Message);
        Assert.Contains("/AcroForm /NeedAppearances", error.Message);
    }

    [Fact]
    public void ClaimAddedAfterLoadingAMultilineFieldFailsAtSaveIncremental()
    {
        var document = PortableDocument.LoadFromStream(new MemoryStream(MultilineFieldDocument()));
        document.Conformance = PdfAConformance.PdfA2B;

        using var stream = new MemoryStream();
        var error = Assert.Throws<InvalidOperationException>(() => document.SaveIncremental(stream));

        Assert.Contains("PDF/A forbids the interactive form flag", error.Message);
    }

    [Fact]
    public void WidgetWithoutAppearanceUnderAClaimFailsAtSave()
    {
        var source = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R /AcroForm 4 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [5 0 R] >>\nendobj\n")
            .Object(4, "4 0 obj\n<< /Fields [5 0 R] /DA (/Helv 0 Tf 0 g) >>\nendobj\n")
            .Object(5, "5 0 obj\n<< /Type /Annot /Subtype /Widget /FT /Tx /T (Name) /V (x) /P 3 0 R /Rect [100 700 350 720] /DA (/Helv 8 Tf 0 g) >>\nendobj\n");

        var document = PortableDocument.LoadFromStream(new MemoryStream(FixturePdf.Wrap(source, 6)));
        document.Conformance = PdfAConformance.PdfA2B;

        var error = Assert.Throws<InvalidOperationException>(() => Save(document));

        Assert.Contains("PDF/A requires every widget annotation to carry an appearance stream", error.Message);
        Assert.Contains("'Name' has no /AP /N appearance", error.Message);
    }

    [Fact]
    public void GraphMutatedAfterRenderingIsCaughtByTheExistenceRuleOnceClaimed()
    {
        var rendered = Render(new DocumentRenderer());
        rendered.AcroForm!.FillField("name", Cyrillic);

        var document = PortableDocument.LoadFromStream(new MemoryStream(rendered.ToArray()));
        document.Conformance = PdfAConformance.PdfA2B;

        var error = Assert.Throws<InvalidOperationException>(() => Save(document));

        Assert.Contains("PDF/A forbids the interactive form flag", error.Message);
    }

    [Fact]
    public void AppendingADonorFormWithNeedAppearancesUnderAClaimIsRejected()
    {
        var model = new Document();
        BuildTestSupport.RegisterLatin(model);
        var paragraph = model.Sections.Add().Blocks.AddParagraph();
        paragraph.Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.Add("base");

        var document = new DocumentRenderer { Conformance = PdfAConformance.PdfA2B }.Render(model);
        document.Append(PortableDocument.LoadFromStream(new MemoryStream(DonorFormWithNeedAppearances())));

        var error = Assert.Throws<InvalidOperationException>(() => Save(document));

        Assert.Contains("PDF/A forbids the interactive form flag", error.Message);
        Assert.Contains("the appended document's form sets it", error.Message);
    }

    [Fact]
    public void AppendingADonorFormWithNeedAppearancesWithoutAClaimStillMergesTheFlag()
    {
        var document = new PortableDocument();
        document.Pages.Add().SetContent(Encoding.ASCII.GetBytes("base"));
        document.Append(PortableDocument.LoadFromStream(new MemoryStream(DonorFormWithNeedAppearances())));

        var reader = DocumentReader.Parse(document.ToArray());
        var form = FormTestSupport.AcroForm(reader);

        Assert.True(form.TryGetValue("NeedAppearances", out var need)
            && reader.Resolve(need!) is BooleanObject { Value: true });
    }

    [Fact]
    public void ClaimedFormWithBakedAppearancesSavesUnchanged()
    {
        var before = Save(Render(new DocumentRenderer { Conformance = PdfAConformance.PdfA2B }));
        var after = Save(Render(new DocumentRenderer { Conformance = PdfAConformance.PdfA2B }));

        Assert.Equal(before, after);

        var reader = DocumentReader.Parse(after);
        var form = FormTestSupport.AcroForm(reader);
        Assert.False(form.ContainsKey("NeedAppearances"));

        var field = FormTestSupport.Field(reader, "name");
        var appearance = Assert.IsType<DictionaryObject>(reader.Resolve(field["AP"]!));
        Assert.IsType<StreamObject>(reader.Resolve(appearance["N"]!));
    }
}
