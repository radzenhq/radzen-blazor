#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf.Write;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Core;

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
        var document = new PortableDocument();
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
    }

    [Fact]
    public void GradientBrush_UnknownSubclass_IsRejected()
    {
        var brush = new SweepGradient(
            new GradientStop(0, Color.Red),
            new GradientStop(1, Color.Blue));

        Assert.Throws<NotSupportedException>(() => ShadingBuilder.BuildShading(brush));
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

        public bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out DecodedImage? image)
        {
            if (!data.Span.StartsWith(Magic))
            {
                image = null;
                return false;
            }

            image = new DecodedImage(new byte[] { 0xFF }, 1, 1, 8, ImageColorSpace.DeviceGray);
            return true;
        }
    }

    [Fact]
    public void ImageDecoders_AddedCustomDecoder_IsDispatched()
    {
        var decoders = ImageDecoders.BuiltIn.Add(new RzimImageDecoder());

        var decoded = decoders.Decode(new byte[] { 0x52, 0x5A, 0x49, 0x4D, 0x00, 0x01 }, ReaderLimits.Default);
        var dictionary = ImageTestHelpers.Stream(decoded).Dictionary;

        Assert.Equal(1, ImageTestHelpers.Int(dictionary, "Width"));
        Assert.Equal(1, ImageTestHelpers.Int(dictionary, "Height"));
        Assert.Equal("DeviceGray", ImageTestHelpers.Name(dictionary, "ColorSpace"));
        Assert.Equal(new byte[] { 0xFF }, decoded.Samples.ToArray());
    }

    [Fact]
    public void ImageContent_DocumentScopedCustomFormat_RoundTripsThroughPublicPipeline()
    {
        var document = new PortableDocument { ImageDecoders = ImageDecoders.BuiltIn.Add(new RzimImageDecoder()) };
        var page = document.Pages.Add();
        page.Content.Add(new ImageContent([.. RzimImageDecoder.Magic, 0x2A]) { Bounds = PdfRect.FromSize(0, 0, 10, 10) });

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);

        Assert.Contains("Do", ContentStreamTokenizer.Operators(content));
    }
}
