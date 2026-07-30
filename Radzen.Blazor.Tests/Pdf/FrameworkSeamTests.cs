#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf.Write;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

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

        public bool TryDecode(ReadOnlyMemory<byte> data, ReaderLimits limits, [NotNullWhen(true)] out ImageXObject? xobject)
        {
            if (!data.Span.StartsWith(Magic))
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

    private sealed class DecoderRegistryScope : IDisposable
    {
        private static readonly System.Reflection.FieldInfo DecoderField =
            typeof(ImageDecoder).GetField("registered", Flags)!;

        private static readonly System.Reflection.FieldInfo ProbeField =
            typeof(ImageProbe).GetField("registered", Flags)!;

        private const System.Reflection.BindingFlags Flags =
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;

        private readonly object? decoders = DecoderField.GetValue(null);
        private readonly object? probes = ProbeField.GetValue(null);

        public void Dispose()
        {
            DecoderField.SetValue(null, decoders);
            ProbeField.SetValue(null, probes);
        }
    }

    [Fact]
    public void ImageDecoder_RegisteredCustomDecoder_IsDispatched()
    {
        using var registry = new DecoderRegistryScope();
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
        using var registry = new DecoderRegistryScope();
        ImageDecoder.Register(new RzimImageDecoder());

        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.Content.Add(new ImageContent([.. RzimImageDecoder.Magic, 0x2A]) { Bounds = PdfRect.FromSize(0, 0, 10, 10) });

        var content = ContentTestHelpers.PageContent(ContentTestHelpers.Reload(document), 0);

        Assert.Contains("Do", ContentStreamTokenizer.Operators(content));
    }
}
