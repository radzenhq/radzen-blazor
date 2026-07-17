#nullable enable
using System.Collections.Generic;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class AppendMergeTests
{
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static Document BuildA()
    {
        var a = new Document();
        a.Pages.Add().SetContent(Ascii("A0-content"));
        a.Pages.Add().SetContent(Ascii("shared-bytes"));
        return a;
    }

    private static Document BuildB()
    {
        var b = new Document();
        b.Pages.Add().SetContent(Ascii("B0-content"));
        b.Pages.Add().SetContent(Ascii("shared-bytes"));
        b.Pages.Add().SetContent(Ascii("B2-content"));
        return b;
    }

    [Fact]
    public void Append_MergesPageCountsAndOrder()
    {
        var a = BuildA();
        a.Append(BuildB());

        Assert.Equal(5, a.Pages.Count);

        var reader = DocumentReader.Parse(a.ToArray());
        Assert.Equal(5, DocumentLoadTests.PageCount(reader));
        Assert.Equal(5, DocumentLoadTests.Kids(reader).Count);

        Assert.Equal(Ascii("A0-content"), DocumentLoadTests.KidContent(reader, 0));
        Assert.Equal(Ascii("shared-bytes"), DocumentLoadTests.KidContent(reader, 1));
        Assert.Equal(Ascii("B0-content"), DocumentLoadTests.KidContent(reader, 2));
        Assert.Equal(Ascii("shared-bytes"), DocumentLoadTests.KidContent(reader, 3));
        Assert.Equal(Ascii("B2-content"), DocumentLoadTests.KidContent(reader, 4));
    }

    [Fact]
    public void Append_EveryReferenceResolves()
    {
        var a = BuildA();
        a.Append(BuildB());

        var reader = DocumentReader.Parse(a.ToArray());
        AssertResolves(reader, reader.Trailer, []);
    }

    [Fact]
    public void Append_AllKidsParentPointsToSinglePagesNode()
    {
        var a = BuildA();
        a.Append(BuildB());

        var reader = DocumentReader.Parse(a.ToArray());
        var pagesRef = Assert.IsType<ReferenceObject>(DocumentLoadTests.Catalog(reader)["Pages"]).ObjectNumber;

        var kids = DocumentLoadTests.Kids(reader);
        for (var i = 0; i < kids.Count; i++)
        {
            var parent = Assert.IsType<ReferenceObject>(DocumentLoadTests.Kid(reader, i)["Parent"]);
            Assert.Equal(pagesRef, parent.ObjectNumber);
        }
    }

    [Fact]
    public void Append_NoResourceDedup_EveryPageHasDistinctContentObject()
    {
        var a = BuildA();
        a.Append(BuildB());

        var reader = DocumentReader.Parse(a.ToArray());
        var kids = DocumentLoadTests.Kids(reader);

        var contentNumbers = new HashSet<int>();
        for (var i = 0; i < kids.Count; i++)
        {
            var contents = Assert.IsType<ReferenceObject>(DocumentLoadTests.Kid(reader, i)["Contents"]);
            Assert.True(contentNumbers.Add(contents.ObjectNumber));
        }

        Assert.Equal(5, contentNumbers.Count);
    }

    [Fact]
    public void Append_LeavesSourceDocumentUntouched()
    {
        var a = BuildA();
        var b = BuildB();
        a.Append(b);

        Assert.Equal(3, b.Pages.Count);

        var reader = DocumentReader.Parse(b.ToArray());
        Assert.Equal(3, DocumentLoadTests.PageCount(reader));
        Assert.Equal(Ascii("B0-content"), DocumentLoadTests.KidContent(reader, 0));
        Assert.Equal(Ascii("B2-content"), DocumentLoadTests.KidContent(reader, 2));
    }

    [Fact]
    public void Append_DeepCopies_MutatingSourceAfterwardDoesNotAffectTarget()
    {
        var a = BuildA();
        var b = BuildB();
        a.Append(b);

        b.Pages[0].SetContent(Ascii("mutated-after-append"));

        var reader = DocumentReader.Parse(a.ToArray());
        Assert.Equal(Ascii("B0-content"), DocumentLoadTests.KidContent(reader, 2));
    }

    private static void AssertResolves(DocumentReader reader, DocumentObject value, HashSet<int> visited)
    {
        switch (value)
        {
            case ReferenceObject reference:
                if (!visited.Add(reference.ObjectNumber))
                {
                    return;
                }

                var resolved = reader.Resolve(reference);
                Assert.NotNull(resolved);
                AssertResolves(reader, resolved, visited);
                break;
            case DictionaryObject dictionary:
                foreach (var key in dictionary.Keys)
                {
                    AssertResolves(reader, dictionary[key], visited);
                }

                break;
            case StreamObject stream:
                foreach (var key in stream.Dictionary.Keys)
                {
                    AssertResolves(reader, stream.Dictionary[key], visited);
                }

                break;
            case ArrayObject array:
                for (var i = 0; i < array.Count; i++)
                {
                    AssertResolves(reader, array[i], visited);
                }

                break;
        }
    }
}
