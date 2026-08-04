#nullable enable

using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf.Write;
using Radzen.Documents;
using Radzen.Documents.Core;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;
namespace Radzen.Blazor.Pdf.Tests;

public class SoftMaskShadowTests
{
    private static byte[] ContentBytes(Document document)
        => ContentTestHelpers.PageContent(BuildTestSupport.Read(document), 0);

    private static string Content(Document document) => Encoding.Latin1.GetString(ContentBytes(document));

    private static Paragraph Text(string text)
    {
        var paragraph = new Paragraph();
        paragraph.Inlines.Add(text);
        return paragraph;
    }

    private static Document ShadowDocument()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var container = section.Blocks.Add(new Container
        {
            Padding = Unit.FromPoint(10),
            Background = Color.FromRgb(255, 255, 255),
            CornerRadius = Unit.FromPoint(6),
            Shadow = new BoxShadow
            {
                Color = Color.FromArgb(160, 0, 0, 0),
                BlurRadius = Unit.FromPoint(8),
                OffsetX = Unit.FromPoint(2),
                OffsetY = Unit.FromPoint(3),
                Spread = Unit.FromPoint(1),
            },
        });
        container.Blocks.Add(Text("Shadowed panel"));
        return document;
    }


    [Fact]
    public void ExtGStateDictionary_Default_HasNoSMask()
    {
        var dict = PageResourceBuilder.ExtGStateDictionary(0.5, 0.5);
        Assert.False(dict.ContainsKey("SMask"));
        Assert.Equal(new[] { "Type", "ca", "CA" }, dict.Keys);
    }

    [Fact]
    public void ExtGStateDictionary_SMaskNone_EmitsNameNone()
    {
        var dict = PageResourceBuilder.ExtGStateDictionary(1, 1, softMask: new NameObject("None"));
        Assert.Equal("None", Assert.IsType<NameObject>(dict["SMask"]!).Value);
        Assert.Equal(new[] { "Type", "ca", "CA", "SMask" }, dict.Keys);
    }


    private static string ShadowGroupForm(string emission)
        => IndirectObject(
            emission,
            Shaped(
                "page /ExtGState",
                @"/ExtGState <<(?: /\w+ << [^>]*>>)* /\w+ << /Type /ExtGState "
                    + @"[^>]*/SMask << /Type /Mask /S /Luminosity /G (\d+) 0 R >>",
                emission).Groups[1].Value);

    [Fact]
    public void Shadow_EmitsFormTransparencyGroup_WithDeviceGrayImage()
    {
        var emission = Emit(new DocumentRenderer().Render(ShadowDocument()));
        var form = ShadowGroupForm(emission);

        Carries("soft mask group form", "/Subtype /Form", form);
        Carries("soft mask group form", "/Group << /S /Transparency /CS /DeviceGray >>", form);

        var xobject = Shaped(
            "soft mask group form",
            @"/Resources << /XObject << /\w+ (\d+) 0 R >> >>",
            form);
        var image = IndirectObject(emission, xobject.Groups[1].Value);

        Carries("soft mask image", "/Subtype /Image", image);
        Carries("soft mask image", "/ColorSpace /DeviceGray", image);
    }

    [Fact]
    public void Shadow_InstallsLuminositySoftMask_ThroughExtGState()
    {
        var emission = Emit(new DocumentRenderer().Render(ShadowDocument()));

        Carries("soft mask group form", "/Subtype /Form", ShadowGroupForm(emission));
    }

    [Fact]
    public void Shadow_PaintsMaskedFill_UnderBoxBackground()
    {
        var bytes = ContentBytes(ShadowDocument());
        var content = Content(ShadowDocument());
        Assert.Contains("gs", ContentOperationTestHelpers.Operators(bytes));
        Assert.True(ContentOperationTestHelpers.HasSequence(bytes, "re", "f"));

        var firstFill = content.IndexOf(" re f", StringComparison.Ordinal);
        var background = content.IndexOf("1 1 1 rg", StringComparison.Ordinal);
        Assert.True(firstFill >= 0 && background >= 0, "expected both a shadow fill and the box background");
        Assert.True(firstFill < background, "the shadow must paint before the box background");
    }


    [Fact]
    public void Shadow_IsDeterministic_AcrossBuilds()
        => Assert.Equal(new DocumentRenderer().ToArray(ShadowDocument()), new DocumentRenderer().ToArray(ShadowDocument()));

    [Fact]
    public void NoShadow_IsByteIdenticalAcrossBuilds()
    {
        static byte[] Build()
        {
            var document = new Document();
            var section = document.Sections.Add();
            var container = section.Blocks.Add(new Container
            {
                Padding = Unit.FromPoint(10),
                Background = Color.FromRgb(255, 255, 255),
                CornerRadius = Unit.FromPoint(6),
            });
            container.Blocks.Add(Text("Plain panel"));
            return new DocumentRenderer().ToArray(document);
        }

        Assert.Equal(Build(), Build());
    }
}
