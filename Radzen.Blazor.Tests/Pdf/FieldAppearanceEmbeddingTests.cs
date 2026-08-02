#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

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

    private static DictionaryObject Widget(DocumentReader reader)
    {
        var page = BuildTestSupport.PageLeaves(reader)[0].Page;
        var annots = Assert.IsType<ArrayObject>(reader.Resolve(page["Annots"]));
        foreach (var item in annots)
        {
            var annotation = Assert.IsType<DictionaryObject>(reader.Resolve(item));
            if (BuildTestSupport.Name(reader, annotation, "Subtype") == "Widget"
                && annotation.ContainsKey("AP"))
            {
                return annotation;
            }
        }

        throw new InvalidOperationException("no widget with an appearance stream");
    }

    private static StreamObject NormalAppearance(DocumentReader reader, DictionaryObject widget)
    {
        var appearance = Assert.IsType<DictionaryObject>(reader.Resolve(widget["AP"]));
        return Assert.IsType<StreamObject>(reader.Resolve(appearance["N"]));
    }

    private static Dictionary<string, DictionaryObject> AppearanceFonts(DocumentReader reader, StreamObject stream)
    {
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(stream.Dictionary["Resources"]));
        var fonts = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Font"]));
        return fonts.Keys.ToDictionary(
            key => key,
            key => Assert.IsType<DictionaryObject>(reader.Resolve(fonts[key])));
    }

    private static DictionaryObject FontDescriptor(DocumentReader reader, DictionaryObject font)
    {
        if (BuildTestSupport.Name(reader, font, "Subtype") == "Type0")
        {
            var descendants = Assert.IsType<ArrayObject>(reader.Resolve(font["DescendantFonts"]));
            font = Assert.IsType<DictionaryObject>(reader.Resolve(descendants[0]));
        }

        return Assert.IsType<DictionaryObject>(reader.Resolve(font["FontDescriptor"]));
    }

    private static void AssertEmbedded(DocumentReader reader, DictionaryObject font)
    {
        var descriptor = FontDescriptor(reader, font);
        Assert.True(
            descriptor.ContainsKey("FontFile") || descriptor.ContainsKey("FontFile2") || descriptor.ContainsKey("FontFile3"),
            "the appearance font carries an embedded font program");
    }

    [Fact]
    public void PdfARender_GivenAnAuthoredTextInput_EmbedsTheAppearanceFont()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add("Name ");
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada", Label = "Name" });

        var reader = BuildTestSupport.Read(document, PdfA());
        var stream = NormalAppearance(reader, Widget(reader));
        var fonts = AppearanceFonts(reader, stream);

        var font = Assert.Single(fonts).Value;
        AssertEmbedded(reader, font);
    }

    [Fact]
    public void PdfARender_GivenAnAuthoredTextInput_NamesTheEmbeddedFontInTheDefaultAppearance()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada", Label = "Name" });

        var reader = BuildTestSupport.Read(document, PdfA());
        var widget = Widget(reader);
        var fonts = AppearanceFonts(reader, NormalAppearance(reader, widget));
        var key = Assert.Single(fonts).Key;

        var da = Assert.IsType<StringObject>(reader.Resolve(widget["DA"])).Value;
        Assert.Contains("/" + key + " ", da, StringComparison.Ordinal);

        var form = FormTestSupport.AcroForm(reader);
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(form["DR"]));
        var formFonts = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Font"]));
        Assert.True(formFonts.ContainsKey(key), "the default resources name the embedded appearance font");
        Assert.False(formFonts.ContainsKey("Helv"), "no standard-14 face is offered as a default");
    }

    [Fact]
    public void PdfARender_GivenAFieldValueOutsideThePageText_SubsetsTheAppearanceGlyphs()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add("aaa ");
        paragraph.Inlines.Add(new TextInput("name") { Value = "Zq", Label = "Name" });

        var reader = BuildTestSupport.Read(document, PdfA());
        var stream = NormalAppearance(reader, Widget(reader));
        var font = Assert.Single(AppearanceFonts(reader, stream)).Value;

        AssertEmbedded(reader, font);

        var text = FormTestSupport.Decode(stream);
        Assert.Contains(" Tj", text, StringComparison.Ordinal);
        Assert.DoesNotContain("(Zq)", text, StringComparison.Ordinal);
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

        var reader = BuildTestSupport.Read(document, new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 });
        var font = Assert.Single(AppearanceFonts(reader, NormalAppearance(reader, Widget(reader)))).Value;

        AssertEmbedded(reader, font);
    }

    [Fact]
    public void PdfARender_GivenAnEmptyFieldValue_WritesAnAppearanceWithoutAFontResource()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add("Name ");
        paragraph.Inlines.Add(new TextInput("name") { Label = "Name" });

        var reader = BuildTestSupport.Read(document, PdfA());
        var stream = NormalAppearance(reader, Widget(reader));

        Assert.False(stream.Dictionary.ContainsKey("Resources"), "an empty appearance renders no text");
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
    public void PlainDocument_GivenAnAuthoredTextInput_KeepsTheStandard14Appearance()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada" });

        var reader = BuildTestSupport.Read(document);
        var stream = NormalAppearance(reader, Widget(reader));

        Assert.Contains("(Ada) Tj", FormTestSupport.Decode(stream), StringComparison.Ordinal);

        var font = Assert.Single(AppearanceFonts(reader, stream)).Value;
        Assert.Equal("Type1", BuildTestSupport.Name(reader, font, "Subtype"));
        Assert.Equal("Helvetica", BuildTestSupport.Name(reader, font, "BaseFont"));
        Assert.False(font.ContainsKey("FontDescriptor"), "a standard-14 appearance embeds nothing");
    }

    [Fact]
    public void PlainDocument_GivenAnAuthoredTextInput_ProducesTheSameBytesAsBeforeEmbedding()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada" });

        var first = new DocumentRenderer().ToArray(document);
        var second = new DocumentRenderer().ToArray(document);

        Assert.Equal(first, second);
    }

    [Fact]
    public void Flatten_UnderPdfA_BakesTheFieldValueWithTheEmbeddedFont()
    {
        var document = Conformant(out var paragraph);
        paragraph.Inlines.Add("Name ");
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada", Label = "Name" });

        var rendered = PdfA().Render(document);
        rendered.Flatten();

        var reader = DocumentReader.Parse(rendered.ToArray());

        foreach (var font in BuildTestSupport.Fonts(reader))
        {
            AssertEmbedded(reader, font);
        }
    }
}
