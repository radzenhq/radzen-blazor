#nullable enable

using System;
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Content;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ContentWriterDisposalTests
{
    private static byte[] BuildInvoiceLike()
    {
        var builder = new DocumentBuilder();
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Invoice 12345", BuildTestSupport.Latin, 18);
        BuildTestSupport.AddText(section, "Amount due: 4200.00", BuildTestSupport.Latin);
        return builder.ToArray();
    }

    [Fact]
    public void RepeatedGeneratedBuilds_ProduceByteIdenticalOutput()
    {
        var baseline = BuildInvoiceLike();
        for (var i = 0; i < 40; i++)
        {
            Assert.Equal(baseline, BuildInvoiceLike());
        }
    }

    [Fact]
    public void WriteAfterDispose_ThrowsObjectDisposedException()
    {
        var writer = new ContentWriter();
        writer.WriteRaw("q\n");
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => writer.WriteRaw("Q\n"));
        Assert.Throws<ObjectDisposedException>(() => writer.WriteNumber(1));
        Assert.Throws<ObjectDisposedException>(() => writer.WriteString(new byte[] { 65 }));
    }

    [Fact]
    public void RepeatedDispose_DoesNotDoubleReturnBuffer()
    {
        var writer = new ContentWriter();
        writer.WriteRaw("q\n");
        writer.Dispose();
        writer.Dispose();
    }

    [Fact]
    public void RepeatedLoadMaterializeReencode_ProduceByteIdenticalContent()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(InterpreterTestSupport.Ascii("10 10 100 50 re f\nBT (hello) Tj ET"));
        var original = document.ToArray();

        byte[]? baseline = null;
        for (var i = 0; i < 40; i++)
        {
            var loaded = InterpreterTestSupport.Load(original);
            loaded.Pages[0].Content[0].Transform = Matrix.Translate(3, 3);
            var content = InterpreterTestSupport.PageContentBytes(loaded.ToArray(), 0);
            baseline ??= content;
            Assert.Equal(baseline, content);
        }
    }
}
