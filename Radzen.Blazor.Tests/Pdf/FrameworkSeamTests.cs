#nullable enable
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Emit;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class FrameworkSeamTests
{
    private sealed class RectangleElement : ContentElement
    {
        protected override void EmitBody(ContentWriter writer)
        {
            writer.WriteNumber(12);
            writer.WriteRaw(" ");
            writer.WriteNumber(34);
            writer.WriteRaw(" ");
            writer.WriteNumber(56);
            writer.WriteRaw(" ");
            writer.WriteNumber(78);
            writer.WriteRaw(" re\nf\n");
        }
    }

    [Fact]
    public void ContentElement_ExternalSubclass_EmitBodyIsDispatched()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new RectangleElement());

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);
        var operators = ContentStreamTokenizer.Operators(content);

        Assert.Contains("re", operators);
        ContentOperation? rectangle = null;
        foreach (var operation in ContentStreamTokenizer.Parse(content))
        {
            if (operation.Operator == "re")
            {
                rectangle = operation;
            }
        }

        Assert.NotNull(rectangle);
        Assert.Equal(12, rectangle!.Num(0), 3);
        Assert.Equal(34, rectangle.Num(1), 3);
        Assert.Equal(56, rectangle.Num(2), 3);
        Assert.Equal(78, rectangle.Num(3), 3);
    }

    private sealed class SweepGradient : GradientBrush
    {
        public SweepGradient(params GradientStop[] stops)
            : base(stops)
        {
        }

        protected internal override int ShadingType => 7;

        protected internal override ArrayObject BuildCoords() =>
        [
            new NumberObject(1),
            new NumberObject(2),
            new NumberObject(3),
        ];
    }

    [Fact]
    public void GradientBrush_ExternalSubclass_ShadingHooksAreDispatched()
    {
        var brush = new SweepGradient(
            new GradientStop(0, Color.Red),
            new GradientStop(1, Color.Blue));

        var shading = ShadingBuilder.BuildShading(brush);

        Assert.Equal(7, Assert.IsType<NumberObject>(shading["ShadingType"]!).IntValue);
        var coords = Assert.IsType<ArrayObject>(shading["Coords"]!);
        Assert.Equal(3, coords.Count);
        Assert.Equal(1, Assert.IsType<NumberObject>(coords[0]).DoubleValue, 3);
        Assert.Equal(2, Assert.IsType<NumberObject>(coords[1]).DoubleValue, 3);
        Assert.Equal(3, Assert.IsType<NumberObject>(coords[2]).DoubleValue, 3);
    }

    [Fact]
    public void FormFieldDefinition_ProtectedSurface_HasNoCosTypes()
    {
        var members = typeof(FormFieldDefinition).GetMembers(
            System.Reflection.BindingFlags.Instance
            | System.Reflection.BindingFlags.NonPublic
            | System.Reflection.BindingFlags.DeclaredOnly);

        Assert.DoesNotContain(members, member => member.Name is "EmitCreatedField" or "PopulateWidget");
    }

    private sealed class RzimImageDecoder : IImageDecoder
    {
        public static readonly byte[] Magic = [0x52, 0x5A, 0x49, 0x4D];

        public bool TryDecode(byte[] data, ReaderLimits limits, [NotNullWhen(true)] out ImageXObject? xobject)
        {
            if (data.Length < 4 || data[0] != Magic[0] || data[1] != Magic[1] || data[2] != Magic[2] || data[3] != Magic[3])
            {
                xobject = null;
                return false;
            }

            var stream = new StreamObject(new byte[] { 0xFF });
            stream.Dictionary["Type"] = new NameObject("XObject");
            stream.Dictionary["Subtype"] = new NameObject("Image");
            stream.Dictionary["Width"] = new NumberObject(1);
            stream.Dictionary["Height"] = new NumberObject(1);
            stream.Dictionary["ColorSpace"] = new NameObject("DeviceGray");
            stream.Dictionary["BitsPerComponent"] = new NumberObject(8);
            xobject = new ImageXObject(stream, null);
            return true;
        }
    }

    [Fact]
    public void ImageDecoder_RegisteredCustomDecoder_IsDispatched()
    {
        ImageDecoder.Register(new RzimImageDecoder());

        var decoded = ImageDecoder.Decode([.. RzimImageDecoder.Magic, 0x00, 0x01]);

        Assert.Equal(1, ImageTestHelpers.Int(decoded.Image.Dictionary, "Width"));
        Assert.Equal(1, ImageTestHelpers.Int(decoded.Image.Dictionary, "Height"));
        Assert.Equal("DeviceGray", ImageTestHelpers.Name(decoded.Image.Dictionary, "ColorSpace"));
        Assert.Equal(new byte[] { 0xFF }, decoded.Image.Data);
    }

    [Fact]
    public void ImageContent_RegisteredCustomFormat_RoundTripsThroughPublicPipeline()
    {
        ImageDecoder.Register(new RzimImageDecoder());

        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new ImageContent([.. RzimImageDecoder.Magic, 0x2A]) { Bounds = PdfRect.FromSize(0, 0, 10, 10) });

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);

        Assert.Contains("Do", ContentStreamTokenizer.Operators(content));
    }
}
