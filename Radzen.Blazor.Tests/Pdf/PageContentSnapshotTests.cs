#nullable enable

using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class PageContentSnapshotTests
{
    [Fact]
    public void GetContent_MutatingReturnedArray_DoesNotChangeStoredBytes()
    {
        var raw = Encoding.ASCII.GetBytes("1 0 0 1 5 5 cm");
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.SetContent(raw);

        var first = page.GetContent()!;
        for (var i = 0; i < first.Length; i++)
        {
            first[i] = (byte)'X';
        }

        Assert.Equal(raw, page.GetContent());
    }

    [Fact]
    public void SetContent_MutatingSourceArray_DoesNotChangeStoredBytes()
    {
        var raw = Encoding.ASCII.GetBytes("1 0 0 1 5 5 cm");
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.SetContent(raw);

        for (var i = 0; i < raw.Length; i++)
        {
            raw[i] = (byte)'X';
        }

        Assert.Equal(Encoding.ASCII.GetBytes("1 0 0 1 5 5 cm"), page.GetContent());
    }

    [Fact]
    public void GetContent_MutatingReturnedArray_DoesNotLeakIntoSavedDocument()
    {
        var document = new PortableDocument();
        var page = document.Pages.Add();
        page.SetContent(Encoding.ASCII.GetBytes("1 0 0 1 5 5 cm"));

        var bytes = page.GetContent()!;
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)'X';
        }

        using var buffer = new MemoryStream(document.ToArray());
        var reloaded = PortableDocument.LoadFromStream(buffer);
        var content = reloaded.Pages[0].GetContent()!;

        var operators = ContentOperationTestHelpers.Operators(content);
        Assert.Contains("cm", operators);
        Assert.DoesNotContain("XXXX", operators);
    }
}
