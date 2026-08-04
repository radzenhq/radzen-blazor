#nullable enable
using System;
using System.Text.RegularExpressions;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class FieldAppearanceEmbeddingTests
{
    private static Document Conformant(out Paragraph paragraph)
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Form";
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Normal.Font.Family = BuildTestSupport.Latin;

        paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Font.Family = BuildTestSupport.Latin;
        return document;
    }

    private static DocumentRenderer PdfA() => new() { Conformance = PdfAConformance.PdfA2B };

    private static string Emission(Document document, DocumentRenderer? renderer = null)
        => Emit((renderer ?? new DocumentRenderer()).Render(document));

    private static string Appearance(string emission)
    {
        var widget = Line(emission, "/T (name)");
        var reference = Shaped("widget", @"/AP << /N (\d+) 0 R >>", widget);
        return IndirectObject(emission, reference.Groups[1].Value);
    }

    private static Match EmbeddedFont(string appearance)
        => Shaped("appearance /Resources", @"/Resources << /Font << /(\w+) (\d+) 0 R >> >>", appearance);

    private static void CarriesAnEmbeddedProgram(string emission, string fontNumber)
    {
        var font = IndirectObject(emission, fontNumber);
        if (font.Contains("/Subtype /Type0", StringComparison.Ordinal))
        {
            font = IndirectObject(emission, References("Type0 font", "DescendantFonts", 1, font)[0]);
        }

        var descriptor = IndirectObject(
            emission,
            Shaped("font", @"/FontDescriptor (\d+) 0 R", font).Groups[1].Value);

        Shaped("font descriptor", @"/FontFile[23]? \d+ 0 R", descriptor);
    }

    [Fact]
    public void PdfARender_GivenAnAuthoredTextInput_EmbedsTheAppearanceFont()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add("Name ");
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada", Label = "Name" });

        var emission = Emission(document, PdfA());

        CarriesAnEmbeddedProgram(emission, EmbeddedFont(Appearance(emission)).Groups[2].Value);
    }

    [Fact]
    public void PdfARender_GivenAnAuthoredTextInput_NamesTheEmbeddedFontInTheDefaultAppearance()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada", Label = "Name" });

        var emission = Emission(document, PdfA());
        var key = EmbeddedFont(Appearance(emission)).Groups[1].Value;

        var defaultAppearance = Shaped("widget", @"/DA \(([^)]*)\)", Line(emission, "/T (name)")).Groups[1].Value;
        Carries("widget /DA", "/" + key + " ", defaultAppearance);

        var resources = Shaped(
            "AcroForm /DR",
            @"/DR << /Font << (.*?) >> >>",
            Line(emission, "/Fields [")).Groups[1].Value;

        Carries("AcroForm /DR /Font", "/" + key + " ", resources);
        Lacks("AcroForm /DR /Font", "/Helv ", resources);
    }

    [Fact]
    public void PdfARender_GivenAFieldValueOutsideThePageText_SubsetsTheAppearanceGlyphs()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add("aaa ");
        paragraph.Inlines.Add(new TextInput("name") { Value = "Zq", Label = "Name" });

        var emission = Emission(document, PdfA());
        var appearance = Appearance(emission);

        CarriesAnEmbeddedProgram(emission, EmbeddedFont(appearance).Groups[2].Value);
        Carries("field appearance stream", " Tj", appearance);
        Lacks("field appearance stream", "(Zq)", appearance);
    }

    [Fact]
    public void PdfARender_GivenAFieldValueOutsideThePageText_CoversTheValueGlyphsInTheSubset()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add("aaa ");
        paragraph.Inlines.Add(new TextInput("name") { Value = "Zq", Label = "Name" });

        var rendered = PdfA().Render(document);
        rendered.Flatten();

        using var buffer = new System.IO.MemoryStream(rendered.ToArray());
        var reloaded = PortableDocument.LoadFromStream(buffer);

        Assert.Contains("Zq", reloaded.ExtractText(), StringComparison.Ordinal);
    }

    [Fact]
    public void PdfUaRender_GivenAnAuthoredTextInput_EmbedsTheAppearanceFont()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add("Name ");
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada", Label = "Name" });

        var emission = Emission(document, new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 });

        CarriesAnEmbeddedProgram(emission, EmbeddedFont(Appearance(emission)).Groups[2].Value);
    }

    [Fact]
    public void PdfARender_GivenAnEmptyFieldValue_WritesAnAppearanceWithoutAFontResource()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add("Name ");
        paragraph.Inlines.Add(new TextInput("name") { Label = "Name" });

        var emission = Emission(document, PdfA());

        Lacks("field appearance stream", "/Resources", Appearance(emission));
    }

    [Fact]
    public void PdfUaDocument_GivenAnOutputLevelChoiceFieldDefinition_FailsAtSave()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add("Name");

        var rendered = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 }.Render(document);
        rendered.FormFields.Add(new ChoiceFieldDefinition("stamped") { Width = 60, Height = 12, Value = "Ada" });

        var error = Assert.Throws<InvalidOperationException>(rendered.ToArray);

        Assert.Contains("PDF/UA", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PdfADocument_GivenAnOutputLevelTextFieldDefinition_FailsAtSave()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add("Name");

        var rendered = PdfA().Render(document);
        rendered.FormFields.Add(new TextFieldDefinition("stamped") { Width = 60, Height = 12, Value = "Ada" });

        var error = Assert.Throws<InvalidOperationException>(rendered.ToArray);

        Assert.Contains("appearance stream", error.Message, StringComparison.Ordinal);
        Assert.Contains("PortableDocument.FormFields", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ConformanceClaimedAfterAPlainRender_FailsAtSaveRatherThanShipAStandard14Appearance()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add("Name ");
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada", Label = "Name" });

        var rendered = new DocumentRenderer().Render(document);
        rendered.Conformance = PdfAConformance.PdfA2B;

        var error = Assert.Throws<InvalidOperationException>(rendered.ToArray);

        Assert.Contains("appearance stream of the form field 'name'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void PlainDocument_GivenAnAuthoredTextInput_KeepsTheStandard14AppearanceAndRendersDeterministically()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada" });

        var emission = Emission(document);
        var appearance = Appearance(emission);

        Carries("field appearance stream", "(Ada) Tj", appearance);

        var font = Shaped(
            "appearance /Resources",
            @"/Resources << /Font << /\w+ << ([^>]*) >> >> >>",
            appearance).Groups[1].Value;

        Carries("standard-14 appearance font", "/Subtype /Type1", font);
        Carries("standard-14 appearance font", "/BaseFont /Helvetica", font);
        Lacks("standard-14 appearance font", "/FontDescriptor", font);

        Assert.Equal(new DocumentRenderer().ToArray(document), new DocumentRenderer().ToArray(document));
    }

    [Fact]
    public void Flatten_UnderPdfA_BakesTheFieldValueWithTheEmbeddedFont()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add("Name ");
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada", Label = "Name" });

        var rendered = PdfA().Render(document);
        rendered.Flatten();

        var emission = Emit(rendered);
        var fonts = Shaped("page /Resources", @"/Font << (.*?) >>", Line(emission, "/Type /Page ")).Groups[1].Value;

        Lacks("page /Resources /Font", "<<", fonts);

        foreach (Match font in Regex.Matches(fonts, @"/\w+ (\d+) 0 R"))
        {
            CarriesAnEmbeddedProgram(emission, font.Groups[1].Value);
        }
    }
}
