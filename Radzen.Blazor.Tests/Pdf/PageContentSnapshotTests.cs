#nullable enable

using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class PageContentSnapshotTests
{
    [Fact]
    public void GetContent_MutatingReturnedArray_DoesNotChangeStoredBytes()
    {
        var raw = Encoding.ASCII.GetBytes("1 0 0 1 5 5 cm");
        var document = new Document();
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
    public void GetContent_MutatingReturnedArray_DoesNotLeakIntoSavedDocument()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.SetContent(Encoding.ASCII.GetBytes("1 0 0 1 5 5 cm"));

        var bytes = page.GetContent()!;
        for (var i = 0; i < bytes.Length; i++)
        {
            bytes[i] = (byte)'X';
        }

        using var buffer = new MemoryStream(document.ToArray());
        var reloaded = Document.LoadFromStream(buffer);
        var content = reloaded.Pages[0].GetContent()!;

        Assert.Contains("cm", Encoding.ASCII.GetString(content));
        Assert.DoesNotContain("XXXX", Encoding.ASCII.GetString(content));
    }
}
