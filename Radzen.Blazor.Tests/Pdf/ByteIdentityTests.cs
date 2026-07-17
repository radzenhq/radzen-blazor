#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ByteIdentityTests
{
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static Document Load(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return Document.LoadFromStream(stream);
    }

    private static Document BuildThreePages()
    {
        var document = new Document();
        document.Info.Title = "Identity";
        document.Pages.Add(PageSizes.A4).SetContent(Ascii("BT (page zero) Tj ET"));
        document.Pages.Add(PageSizes.Letter).SetContent(Ascii("BT (page one) Tj ET"));
        document.Pages.Add(PageSizes.A4).SetContent(Ascii("BT (page two) Tj ET"));
        return document;
    }

    [Fact]
    public void LoadThenSave_UntouchedPage_ContentStreamBytesIdentical()
    {
        var save1 = BuildThreePages().ToArray();
        var save2 = Load(save1).ToArray();

        var r1 = DocumentReader.Parse(save1);
        var r2 = DocumentReader.Parse(save2);

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(
                DocumentLoadTests.KidContent(r1, i),
                DocumentLoadTests.KidContent(r2, i));
        }
    }

    [Fact]
    public void LoadThenSave_UntouchedPage_PageDictionaryBytesIdentical()
    {
        var save1 = BuildThreePages().ToArray();
        var save2 = Load(save1).ToArray();

        var r1 = DocumentReader.Parse(save1);
        var r2 = DocumentReader.Parse(save2);

        for (var i = 0; i < 3; i++)
        {
            var number1 = KidNumber(r1, i);
            var number2 = KidNumber(r2, i);
            Assert.Equal(ObjectBytes(save1, number1), ObjectBytes(save2, number2));
        }
    }

    [Fact]
    public void AddPageAfterLoad_UntouchedPages_ContentStreamsRemainIdentical()
    {
        var save1 = BuildThreePages().ToArray();

        var loaded = Load(save1);
        loaded.Pages.Add(PageSizes.A4).SetContent(Ascii("BT (added page) Tj ET"));
        var save2 = loaded.ToArray();

        var r1 = DocumentReader.Parse(save1);
        var r2 = DocumentReader.Parse(save2);

        Assert.Equal(3, DocumentLoadTests.PageCount(r1));
        Assert.Equal(4, DocumentLoadTests.PageCount(r2));

        for (var i = 0; i < 3; i++)
        {
            Assert.Equal(
                DocumentLoadTests.KidContent(r1, i),
                DocumentLoadTests.KidContent(r2, i));
        }
    }

    private static int KidNumber(DocumentReader reader, int index)
        => Assert.IsType<ReferenceObject>(DocumentLoadTests.Kids(reader)[index]).ObjectNumber;

    private static byte[] ObjectBytes(byte[] file, int number)
    {
        var header = Encoding.ASCII.GetBytes($"{number} 0 obj");
        var start = IndexOf(file, header, 0);
        Assert.True(start >= 0, $"object {number} header not found");

        var end = IndexOf(file, Encoding.ASCII.GetBytes("endobj"), start);
        Assert.True(end >= 0, $"object {number} endobj not found");
        end += "endobj".Length;

        return file[start..end];
    }

    private static int IndexOf(byte[] haystack, byte[] needle, int from)
    {
        for (var i = from; i <= haystack.Length - needle.Length; i++)
        {
            var match = true;
            for (var j = 0; j < needle.Length; j++)
            {
                if (haystack[i + j] != needle[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }
}
