#nullable enable

using System;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

using Radzen.Documents.Pdf.Emit;
using Radzen.Documents;
namespace Radzen.Blazor.Pdf.Tests;

public class SoftMaskShadowTests
{
    private static string Content(Document document)
        => Encoding.Latin1.GetString(ContentTestHelpers.PageContent(BuildTestSupport.Read(document), 0));

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


    private static StreamObject ShadowGroupForm(DocumentReader reader)
    {
        var resources = BuildTestSupport.PageLeaves(reader)[0].Resources!;
        var states = Assert.IsType<DictionaryObject>(reader.Resolve(resources["ExtGState"]!));
        foreach (var key in states.Keys)
        {
            if (reader.Resolve(states[key]) is DictionaryObject state
                && state.TryGetValue("SMask", out var sm)
                && reader.Resolve(sm!) is DictionaryObject mask
                && mask.TryGetValue("G", out var g)
                && reader.Resolve(g!) is StreamObject form)
            {
                return form;
            }
        }

        throw new Xunit.Sdk.XunitException("no ExtGState carried an /SMask /G group form");
    }

    [Fact]
    public void Shadow_EmitsFormTransparencyGroup_WithDeviceGrayImage()
    {
        var reader = BuildTestSupport.Read(ShadowDocument());
        var form = ShadowGroupForm(reader);

        Assert.Equal("Form", Assert.IsType<NameObject>(reader.Resolve(form.Dictionary["Subtype"]!)).Value);
        var groupDict = Assert.IsType<DictionaryObject>(reader.Resolve(form.Dictionary["Group"]!));
        Assert.Equal("Transparency", Assert.IsType<NameObject>(reader.Resolve(groupDict["S"]!)).Value);
        Assert.Equal("DeviceGray", Assert.IsType<NameObject>(reader.Resolve(groupDict["CS"]!)).Value);

        var formResources = Assert.IsType<DictionaryObject>(reader.Resolve(form.Dictionary["Resources"]!));
        var formXobjects = Assert.IsType<DictionaryObject>(reader.Resolve(formResources["XObject"]!));
        var image = Assert.IsType<StreamObject>(reader.Resolve(formXobjects[Assert.Single(formXobjects.Keys)]));
        Assert.Equal("Image", Assert.IsType<NameObject>(reader.Resolve(image.Dictionary["Subtype"]!)).Value);
        Assert.Equal("DeviceGray", Assert.IsType<NameObject>(reader.Resolve(image.Dictionary["ColorSpace"]!)).Value);
    }

    [Fact]
    public void Shadow_InstallsLuminositySoftMask_ThroughExtGState()
    {
        var reader = BuildTestSupport.Read(ShadowDocument());
        var resources = BuildTestSupport.PageLeaves(reader)[0].Resources!;
        var states = Assert.IsType<DictionaryObject>(reader.Resolve(resources["ExtGState"]!));

        DictionaryObject? smask = null;
        foreach (var key in states.Keys)
        {
            if (reader.Resolve(states[key]) is DictionaryObject state
                && state.TryGetValue("SMask", out var sm)
                && reader.Resolve(sm!) is DictionaryObject dict)
            {
                smask = dict;
            }
        }

        Assert.NotNull(smask);
        Assert.Equal("Luminosity", Assert.IsType<NameObject>(reader.Resolve(smask!["S"]!)).Value);
        var g = reader.Resolve(smask!["G"]!);
        var group = Assert.IsType<StreamObject>(g);
        Assert.Equal("Form", Assert.IsType<NameObject>(reader.Resolve(group.Dictionary["Subtype"]!)).Value);
    }

    [Fact]
    public void Shadow_PaintsMaskedFill_UnderBoxBackground()
    {
        var content = Content(ShadowDocument());
        Assert.Contains(" gs", content, StringComparison.Ordinal);
        Assert.Contains(" re f", content, StringComparison.Ordinal);

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
