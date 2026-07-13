#nullable enable

using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Objects.Encryption;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// Lane cq-reader, finding #107: this writer has no encryption scope, so an incremental
// update over an encrypted original would silently corrupt the document (the appended
// objects are plaintext while the readers still treat the originals as encrypted). The
// constructor must refuse an encrypted original, matching PdfSigner/DssBuilder.
public class IncrementalUpdateEncryptedGuardTests
{
    private static byte[] WriteGraph(EncryptionOptions? options)
    {
        using var buffer = new MemoryStream();
        var writer = new DocumentWriter(buffer) { Encryption = options };

        var catalog = new DictionaryObject { ["Type"] = new NameObject("Catalog") };
        var pages = new DictionaryObject { ["Type"] = new NameObject("Pages"), ["Count"] = new NumberObject(1) };
        var page = new DictionaryObject { ["Type"] = new NameObject("Page") };
        var content = new StreamObject(Encoding.Latin1.GetBytes("BT ET"));

        var catalogRef = writer.Add(catalog);
        var pagesRef = writer.Add(pages);
        var pageRef = writer.Add(page);
        var contentRef = writer.Add(content);

        catalog["Pages"] = pagesRef;
        pages["Kids"] = new ArrayObject { pageRef };
        page["Parent"] = pagesRef;
        page["MediaBox"] = new ArrayObject
        {
            new NumberObject(0), new NumberObject(0), new NumberObject(612), new NumberObject(792),
        };
        page["Contents"] = contentRef;

        writer.Trailer["Root"] = catalogRef;
        writer.Close();
        return buffer.ToArray();
    }

    [Fact]
    public void EncryptedOriginal_IsRejected()
    {
        var encrypted = WriteGraph(new EncryptionOptions { Material = new SeededEncryptionMaterial([7]) });

        Assert.Throws<NotSupportedException>(() => new IncrementalUpdateWriter(encrypted));
    }

    [Fact]
    public void UnencryptedOriginal_IsAccepted()
    {
        var plain = WriteGraph(null);

        var writer = new IncrementalUpdateWriter(plain);
        writer.Add(new DictionaryObject { ["Type"] = new NameObject("Marker") });

        Assert.NotEmpty(writer.ToArray());
    }
}
