#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents;
using Radzen.Documents.Core;
using Radzen.Documents.Fonts;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Signing;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ColdReviewRegressionTests
{
    [Fact]
    public void Unit_ConversionAndArithmetic_RejectOverflowFromFiniteOperands()
    {
        Assert.Throws<ArgumentException>(() => Unit.FromInch(double.MaxValue));
        Assert.Throws<ArgumentException>(() => Unit.FromCentimeter(double.MaxValue));
        Assert.Throws<ArgumentException>(() => Unit.FromPoint(double.MaxValue) + Unit.FromPoint(double.MaxValue));
        Assert.Throws<ArgumentException>(() => Unit.FromPoint(double.MinValue) - Unit.FromPoint(double.MaxValue));
        Assert.False(Unit.TryParse("1e308cm", out _));
        Assert.Throws<FormatException>(() => Unit.Parse("1e308in"));
    }

    [Fact]
    public void TextField_CombWithoutMaxLength_FailsAtSave()
    {
        var document = new PortableDocument();
        document.Pages.Add();
        document.FormFields.Add(new TextFieldDefinition("f")
        {
            Comb = true,
            X = Unit.FromPoint(50),
            Y = Unit.FromPoint(700),
            Width = Unit.FromPoint(120),
            Height = Unit.FromPoint(16),
        });

        var exception = Assert.Throws<InvalidOperationException>(document.ToArray);
        Assert.Contains("Comb", exception.Message);
    }

    [Fact]
    public void TextField_NonPositiveMaxLength_FailsAtSave()
    {
        var document = new PortableDocument();
        document.Pages.Add();
        document.FormFields.Add(new TextFieldDefinition("f")
        {
            MaxLength = 0,
            X = Unit.FromPoint(50),
            Y = Unit.FromPoint(700),
            Width = Unit.FromPoint(120),
            Height = Unit.FromPoint(16),
        });

        var exception = Assert.Throws<InvalidOperationException>(document.ToArray);
        Assert.Contains("MaxLength", exception.Message);
    }

    [Fact]
    public void ListItem_WithEmptyParagraph_StillRendersItsMarker()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(0));
        var list = section.Blocks.AddList(ListStyle.Number);
        var paragraph = new Paragraph();
        paragraph.Font.Family = BuildTestSupport.Latin;
        list.AddItem().Blocks.Add(paragraph);

        var lines = DocumentLayouter.Layout(document).Pages[0].Body.Lines;

        Assert.Contains(
            lines.SelectMany(line => line.Line.Fragments),
            fragment => fragment.IsMarker && fragment.Text == "1.");
    }

    private static PortableDocument LoadedText(string text)
    {
        var document = new PortableDocument();
        document.Pages.Add().Content.Add(new TextContent(text, 72, 700) { Font = new Font { Size = 12 } });
        return PortableDocument.LoadFromStream(new MemoryStream(document.ToArray(), writable: false));
    }

    [Fact]
    public void Redact_RegionCoveringOnlyDescenders_StillRemovesTheGlyphs()
    {
        var document = LoadedText("gap");

        document.Pages[0].Redact([new PdfRect(70, 696.5, 110, 699.5)]);

        Assert.DoesNotContain("gap", PortableDocument.LoadFromStream(
            new MemoryStream(document.ToArray(), writable: false)).ExtractText());
    }

    [Fact]
    public void PdfUa_FormInputWithoutLabel_FailsAtRender()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Form";
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Normal.Font.Family = BuildTestSupport.Latin;
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada" });

        var renderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        var exception = Assert.Throws<InvalidOperationException>(() => renderer.Render(document).ToArray());
        Assert.Contains("accessible name", exception.Message);
    }

    private static bool HasLinkWrappingForm(DocumentReader reader, DictionaryObject element)
    {
        if (reader.GetName(element, "S") == "Link"
            && reader.Resolve(element.TryGetValue("K", out var kids) ? kids! : new NullObject()) is { } resolved)
        {
            foreach (var kid in resolved is ArrayObject array ? array.ToArray() : [resolved])
            {
                if (reader.Resolve(kid) is DictionaryObject child && reader.GetName(child, "S") == "Form")
                {
                    return true;
                }
            }
        }

        if (element.TryGetValue("K", out var walk) && reader.Resolve(walk!) is { } value)
        {
            foreach (var kid in value is ArrayObject array ? array.ToArray() : [value])
            {
                if (reader.Resolve(kid) is DictionaryObject child && HasLinkWrappingForm(reader, child))
                {
                    return true;
                }
            }
        }

        return false;
    }

    [Fact]
    public void LinkedFormField_IsWrappedInALinkStructureElement()
    {
        var document = new Document { Language = "en-US" };
        document.Info.Title = "Form";
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Normal.Font.Family = BuildTestSupport.Latin;
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.Add(new TextInput("name") { Value = "Ada", Label = "Name", Link = "https://radzen.com" });

        var rendered = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 }.Render(document);
        var reader = DocumentReader.Parse(rendered.ToArray());
        var catalog = ContentTestHelpers.Catalog(reader);
        var root = Assert.IsType<DictionaryObject>(reader.Resolve(catalog["StructTreeRoot"]));

        Assert.True(HasLinkWrappingForm(reader, root), "the widget's Link annotation must be owned by a Link element wrapping the Form");
    }

    [Fact]
    public void NestedOverflowingBox_IsClippedByTheAncestorInThePaintedContent()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        section.PageSize = new PageSize(Unit.FromPoint(400), Unit.FromPoint(400));
        section.Margins.SetAll(Unit.FromPoint(0));
        var outer = section.Blocks.Add(new Container { Width = Unit.FromPoint(6), Padding = Unit.FromPoint(0) });
        var wide = outer.Blocks.AddParagraph().Inlines.Add("Wide");
        wide.Font.Family = BuildTestSupport.Latin;
        wide.Font.Size = 24;
        var inner = new Container { Width = Unit.FromPoint(200) };
        var run = inner.Blocks.AddParagraph().Inlines.Add("inner");
        run.Font.Family = BuildTestSupport.Latin;
        outer.Blocks.Add(inner);

        var reader = BuildTestSupport.Read(document);
        var content = Encoding.Latin1.GetString(ContentTestHelpers.PageContent(reader, 0));

        Assert.True(content.Split(" W n").Length - 1 >= 2,
            "both the clipping box's own text and the nested box's text must be clipped");
    }

    private sealed class NullSigner : ISigner
    {
        public byte[] Sign(SignedContent content) => new byte[16];
    }

    [Fact]
    public void Sign_VisibleAppearanceOnAConformantDocument_IsRejected()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var paragraph = document.Sections.Add().Blocks.AddParagraph();
        paragraph.Font.Family = BuildTestSupport.Latin;
        paragraph.Inlines.Add("Body");
        var bytes = new DocumentRenderer { Conformance = PdfAConformance.PdfA2B }.Render(document).ToArray();

        var options = new SignatureOptions
        {
            SignerName = "Test",
            Appearance = new SignatureAppearance
            {
                X = 50,
                Y = 50,
                Width = 180,
                Height = 40,
            },
        };

        var exception = Assert.Throws<InvalidOperationException>(() => PdfSigner.Sign(bytes, options, new NullSigner()));
        Assert.Contains("conformance claim", exception.Message);

        Assert.NotEmpty(PdfSigner.Sign(bytes, new SignatureOptions(), new NullSigner()));
    }
}
