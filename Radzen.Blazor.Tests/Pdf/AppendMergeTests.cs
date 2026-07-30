#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;
using Radzen.Documents.Fonts;

namespace Radzen.Blazor.Pdf.Tests;

public class AppendMergeTests
{
    private static byte[] Ascii(string text) => Encoding.ASCII.GetBytes(text);

    private static PortableDocument BuildA()
    {
        var a = new PortableDocument();
        a.Pages.Add().SetContent(Ascii("A0-content"));
        a.Pages.Add().SetContent(Ascii("shared-bytes"));
        return a;
    }

    private static PortableDocument BuildB()
    {
        var b = new PortableDocument();
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

    [Fact]
    public void Append_DeepCopiesModeledAnnotationScalarAndAppearanceState()
    {
        var source = new PortableDocument();
        var annotation = source.Pages.Add().Annotations.Add(
            new TextAnnotation(PdfRect.FromSize(10, 20, 30, 40)) { Contents = "original" });
        annotation.Appearance = new AnnotationAppearance();
        annotation.Appearance.Content.Add(new TextContent("appearance", Unit.FromPoint(1), Unit.FromPoint(2)));
        var target = new PortableDocument();

        target.Append(source);
        annotation.Contents = "mutated";
        Assert.IsType<TextContent>(annotation.Appearance.Content[0]).Text = "mutated appearance";
        annotation.Appearance.Content.Add(new PathContent { Stroke = true });

        var copy = Assert.IsType<TextAnnotation>(target.Pages[0].Annotations[0]);
        Assert.Equal("original", copy.Contents);
        Assert.NotSame(annotation.Appearance, copy.Appearance);
        Assert.Single(copy.Appearance!.Content);
        Assert.Equal("appearance", Assert.IsType<TextContent>(copy.Appearance.Content[0]).Text);
        var reloaded = InterpreterTestSupport.Load(target.ToArray());
        Assert.Equal("original", Assert.IsType<TextAnnotation>(reloaded.Pages[0].Annotations[0]).Contents);
    }

    [Fact]
    public void Append_RebasesModeledLinkTargetsAfterExistingPages()
    {
        var source = new PortableDocument();
        source.Pages.Add().Annotations.Add(
            new LinkAnnotation(PdfRect.FromSize(10, 20, 30, 40)) { TargetPageIndex = 1 });
        source.Pages.Add();
        var target = new PortableDocument();
        target.Pages.Add();

        target.Append(source);

        Assert.Equal(2, Assert.IsType<LinkAnnotation>(target.Pages[1].Annotations[0]).TargetPageIndex);
        var reloaded = InterpreterTestSupport.Load(target.ToArray());
        Assert.Equal(2, Assert.IsType<LinkAnnotation>(reloaded.Pages[1].Annotations[0]).TargetPageIndex);
    }

    [Fact]
    public void Append_NonIntactFallbackRebasesModeledLinkAndKeepsUriLink()
    {
        var source = new PortableDocument();
        var page = source.Pages.Add();
        page.SetContent(Ascii("edited source content"));
        page.Annotations.Add(new LinkAnnotation(PdfRect.FromSize(10, 20, 30, 40)) { TargetPageIndex = 1 });
        page.Annotations.Add(new LinkAnnotation(PdfRect.FromSize(50, 20, 30, 40)) { Uri = new System.Uri("https://example.com/") });
        source.Pages.Add();
        var target = new PortableDocument();
        target.Pages.Add();

        target.Append(source);

        Assert.Equal(2, Assert.IsType<LinkAnnotation>(target.Pages[1].Annotations[0]).TargetPageIndex);
        Assert.Equal("https://example.com/", Assert.IsType<LinkAnnotation>(target.Pages[1].Annotations[1]).Uri?.AbsoluteUri);
        var reloaded = InterpreterTestSupport.Load(target.ToArray());
        Assert.Equal(2, Assert.IsType<LinkAnnotation>(reloaded.Pages[1].Annotations[0]).TargetPageIndex);
    }

    [Fact]
    public void Append_ResolvedNamedDestinationBecomesRebasedPageLinkAndKeepsUriLink()
    {
        var source = PortableDocument.LoadFromStream(new MemoryStream(NamedDestinationPreservationTests.Source()));
        source.Pages[0].Annotations.Add(
            new LinkAnnotation(PdfRect.FromSize(120, 10, 100, 20)) { Uri = new System.Uri("https://example.com/") });
        var target = new PortableDocument();
        target.Pages.Add();

        target.Append(source);
        var bytes = target.ToArray();
        var reloaded = PortableDocument.LoadFromStream(new MemoryStream(bytes));

        var pageLink = Assert.IsType<LinkAnnotation>(reloaded.Pages[1].Annotations[0]);
        Assert.Null(pageLink.Destination);
        Assert.Equal(2, pageLink.TargetPageIndex);
        Assert.Equal("https://example.com/", Assert.IsType<LinkAnnotation>(
            reloaded.Pages[1].Annotations[1]).Uri?.AbsoluteUri);

        var reader = DocumentReader.Parse(bytes);
        var page = DocumentLoadTests.Kid(reader, 1);
        var annotation = reader.AsDictionary(Assert.IsType<ArrayObject>(reader.Resolve(page["Annots"]))[0])!;
        var destination = Assert.IsType<ArrayObject>(reader.Resolve(annotation["Dest"]));
        Assert.Equal(Assert.IsType<ReferenceObject>(DocumentLoadTests.Kids(reader)[2]).ObjectNumber,
            Assert.IsType<ReferenceObject>(destination[0]).ObjectNumber);
        Assert.Equal("XYZ", Assert.IsType<NameObject>(destination[1]).Value);
        Assert.Equal(0.0, Assert.IsType<NumberObject>(destination[2]).DoubleValue);
        Assert.Equal(500.0, Assert.IsType<NumberObject>(destination[3]).DoubleValue);
        Assert.Equal(0.0, Assert.IsType<NumberObject>(destination[4]).DoubleValue);
    }

    [Fact]
    public void Append_PreservesUnmanagedKeysOfUnmodifiedLoadedAnnotation()
    {
        var sourceDocument = new PortableDocument();
        sourceDocument.Pages.Add().Annotations.Add(
            new TextAnnotation(PdfRect.FromSize(10, 20, 30, 40)) { Contents = "note" });
        var source = InterpreterTestSupport.Load(sourceDocument.ToArray());
        var sourceReader = source.Loaded!.Source!;
        var sourcePage = source.Loaded.SourcePages[source.Pages[0]];
        var sourceAnnotation = sourceReader.AsDictionary(
            Assert.Single(sourceReader.GetArray(sourcePage, "Annots")!))!;
        sourceAnnotation["Custom"] = new StringObject("keep-me");

        var target = new PortableDocument();
        target.Pages.Add();
        target.Append(source);

        var reader = DocumentReader.Parse(target.ToArray());
        var annotation = reader.AsDictionary(
            Assert.Single(reader.GetArray(DocumentLoadTests.Kid(reader, 1), "Annots")!))!;
        Assert.Equal("keep-me", reader.GetString(annotation, "Custom"));
        Assert.Equal("note", reader.GetString(annotation, "Contents"));
    }

    private static byte[] UnrebuildableAnnotationsPdf()
    {
        var body = Ascii("BT /F1 12 Tf 72 700 Td (hi) Tj ET");
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
            + "/Resources << /Font << /F1 5 0 R >> >> /Contents 4 0 R /Annots [6 0 R 7 0 R] >>\nendobj\n");
        pdf.Mark(4);
        pdf.Append("4 0 obj\n<< /Length " + body.Length + " >>\nstream\n").Append(body).Append("\nendstream\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
        pdf.Object(6, "6 0 obj\n<< /Type /Annot /Subtype /Link /Rect [10 10 100 30] "
            + "/A << /S /Launch /F (other.pdf) >> >>\nendobj\n");
        pdf.Object(7, "7 0 obj\n<< /Type /Annot /Subtype /Text /Rect [50 50 50 50] /Contents (degenerate) >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 8\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 8; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 8 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static byte[] UriLinkWithUnmanagedKeyPdf()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 1 /Kids [3 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [4 0 R] >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Type /Annot /Subtype /Link /Rect [10 10 100 30] /H /I "
            + "/A << /S /URI /URI (https://x/) >> >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 5\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 5; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 5 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    private static byte[] UnmodeledCrossPageLinkPdf()
    {
        var pdf = new FixturePdf().Append("%PDF-1.7\n");
        pdf.Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        pdf.Object(2, "2 0 obj\n<< /Type /Pages /Count 2 /Kids [3 0 R 5 0 R] >>\nendobj\n");
        pdf.Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Annots [4 0 R] >>\nendobj\n");
        pdf.Object(4, "4 0 obj\n<< /Type /Annot /Subtype /Screen /Rect [10 10 30 30] "
            + "/A << /S /GoTo /D [5 0 R /Fit] >> >>\nendobj\n");
        pdf.Object(5, "5 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var i = 1; i < 6; i++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(i)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\n").Append("startxref\n" + xref + "\n%%EOF\n");
        return pdf.ToArray();
    }

    [Fact]
    public void ExtractPage_WithUnmodeledCrossPageLink_DoesNotLeakExcludedPage()
    {
        var source = PortableDocument.LoadFromStream(new MemoryStream(UnmodeledCrossPageLinkPdf()));

        var bytes = source.Pages.ExtractPages(0..1).ToArray();

        var text = Encoding.Latin1.GetString(bytes);
        Assert.Equal(1, System.Text.RegularExpressions.Regex.Matches(text, @"/Type\s*/Page\b").Count);
    }

    [Fact]
    public void Append_ForeignUriLinkPreservesUnmanagedKeys()
    {
        var source = PortableDocument.LoadFromStream(new MemoryStream(UriLinkWithUnmanagedKeyPdf()));
        var target = new PortableDocument();
        target.Pages.Add();

        target.Append(source);

        var reader = DocumentReader.Parse(target.ToArray());
        var annotation = reader.AsDictionary(
            Assert.Single(reader.GetArray(DocumentLoadTests.Kid(reader, 1), "Annots")!))!;
        Assert.Equal("I", reader.GetName(annotation, "H"));
    }

    [Fact]
    public void Append_OfAppendedContentPage_CarriesResources()
    {
        var a = new PortableDocument();
        a.Pages.Add().Content.Add(new TextContent("hello", 72, 700) { Font = new Font { Size = 12 } });
        var loadedA = InterpreterTestSupport.Load(a.ToArray());

        var b = new PortableDocument();
        b.Append(loadedA);

        var c = new PortableDocument();
        c.Append(b);

        var reader = DocumentReader.Parse(c.ToArray());
        var page = DocumentLoadTests.Kid(reader, 0);
        var resources = Assert.IsType<DictionaryObject>(reader.Resolve(page["Resources"]));
        var fonts = Assert.IsType<DictionaryObject>(reader.Resolve(resources["Font"]));
        Assert.NotEmpty(fonts.Keys);
    }

    [Fact]
    public void Append_ForeignUnsupportedActionAndDegenerateAnnotations_SurviveWithoutThrowing()
    {
        var source = PortableDocument.LoadFromStream(new MemoryStream(UnrebuildableAnnotationsPdf()));
        var target = new PortableDocument();
        target.Pages.Add();

        target.Append(source);

        var reader = DocumentReader.Parse(target.ToArray());
        var annots = reader.GetArray(DocumentLoadTests.Kid(reader, 1), "Annots")!;
        Assert.Equal(2, annots.Count);
        var action = reader.AsDictionary(reader.AsDictionary(annots[0])!["A"])!;
        Assert.Equal("Launch", reader.GetName(action, "S"));
    }

    [Fact]
    public void Append_OfAppendedDocument_PreservesAnnotationWithoutLeakingSourcePages()
    {
        var a = new PortableDocument();
        a.Pages.Add().Annotations.Add(
            new TextAnnotation(PdfRect.FromSize(10, 20, 30, 40)) { Contents = "note" });
        a.Pages.Add();
        var loadedA = InterpreterTestSupport.Load(a.ToArray());

        var b = new PortableDocument();
        b.Append(loadedA);

        var c = new PortableDocument();
        c.Pages.Add();
        c.Append(b);

        var bytes = c.ToArray();
        var reader = DocumentReader.Parse(bytes);
        var text = Encoding.Latin1.GetString(bytes);

        Assert.Equal(3, DocumentLoadTests.PageCount(reader));
        Assert.Equal(3, System.Text.RegularExpressions.Regex.Matches(text, @"/Type\s*/Page\b").Count);
        var annotation = reader.AsDictionary(
            Assert.Single(reader.GetArray(DocumentLoadTests.Kid(reader, 1), "Annots")!))!;
        Assert.Equal("note", reader.GetString(annotation, "Contents"));
    }

    [Fact]
    public void Append_DeepCopiesPathAppearanceContent()
    {
        var path = new PathContent { Stroke = true, Fill = true };
        path.MoveTo(Unit.FromPoint(1), Unit.FromPoint(2));
        path.LineTo(Unit.FromPoint(3), Unit.FromPoint(4));
        path.SetDash([2, 3], 1);
        path.SetFillCmyk(0.1, 0.2, 0.3, 0.4);
        var annotation = new TextAnnotation(PdfRect.FromSize(10, 20, 30, 40))
        {
            Appearance = new AnnotationAppearance(),
        };
        annotation.Appearance.Content.Add(path);
        var source = new PortableDocument();
        source.Pages.Add().Annotations.Add(annotation);
        var target = new PortableDocument();

        target.Append(source);
        path.LineTo(Unit.FromPoint(100), Unit.FromPoint(200));
        path.SetDash([9], 8);
        path.SetFillGray(0.8);

        var clone = Assert.IsType<PathContent>(target.Pages[0].Annotations[0].Appearance!.Content[0]);
        Assert.Equal(new PdfRect(1, 2, 3, 4), clone.GetBounds());
        Assert.Equal([2, 3], clone.DashArray!.Value.ToArray());
        Assert.Equal(DeviceColorKind.Cmyk, clone.FillPaint!.Value.Kind);
        Assert.Equal([0.1, 0.2, 0.3, 0.4], clone.FillPaint.Value.Operands);
    }

    [Fact]
    public void Append_DeepCopiesImageAppearanceContent()
    {
        var image = new ImageContent([1, 2, 3]) { Bounds = PdfRect.FromSize(1, 2, 3, 4) };
        var annotation = new TextAnnotation(PdfRect.FromSize(10, 20, 30, 40))
        {
            Appearance = new AnnotationAppearance(),
        };
        annotation.Appearance.Content.Add(image);
        var source = new PortableDocument();
        source.Pages.Add().Annotations.Add(annotation);
        var target = new PortableDocument();

        target.Append(source);
        image.EncodedXObject[0] = 9;
        image.Bounds = PdfRect.FromSize(10, 20, 30, 40);

        var clone = Assert.IsType<ImageContent>(target.Pages[0].Annotations[0].Appearance!.Content[0]);
        Assert.Equal([1, 2, 3], clone.EncodedXObject);
        Assert.Equal(PdfRect.FromSize(1, 2, 3, 4), clone.Bounds);
    }

    [Fact]
    public void Append_DeepCopiesXObjectAppearanceContent()
    {
        var xObject = new XObjectContent("Original") { Transform = Matrix.Translate(1, 2) };
        var annotation = new TextAnnotation(PdfRect.FromSize(10, 20, 30, 40))
        {
            Appearance = new AnnotationAppearance(),
        };
        annotation.Appearance.Content.Add(xObject);
        var source = new PortableDocument();
        source.Pages.Add().Annotations.Add(annotation);
        var target = new PortableDocument();

        target.Append(source);
        xObject.Transform = Matrix.Translate(9, 10);
        xObject.IsArtifact = true;

        var clone = Assert.IsType<XObjectContent>(target.Pages[0].Annotations[0].Appearance!.Content[0]);
        Assert.Equal("Original", clone.Name);
        Assert.Equal(Matrix.Translate(1, 2), clone.Transform);
        Assert.False(clone.IsArtifact);
    }

    [Fact]
    public void Append_DeepCopiesFreeTextAndAppearanceFonts()
    {
        var text = new TextContent("appearance", Unit.FromPoint(1), Unit.FromPoint(2));
        text.Font.Family = "Courier";
        text.Font.Size = 8;
        var annotation = new FreeTextAnnotation(PdfRect.FromSize(10, 20, 30, 40))
        {
            Appearance = new AnnotationAppearance(),
        };
        annotation.Font.Family = "Times-Roman";
        annotation.Font.Size = 12;
        annotation.Appearance.Content.Add(text);
        var source = new PortableDocument();
        source.Pages.Add().Annotations.Add(annotation);
        var target = new PortableDocument();

        target.Append(source);
        annotation.Font.Family = "Helvetica";
        annotation.Font.Size = 20;
        text.Font.Family = "Symbol";
        text.Font.Size = 30;

        var annotationClone = Assert.IsType<FreeTextAnnotation>(target.Pages[0].Annotations[0]);
        Assert.Equal("Times-Roman", annotationClone.Font.Family);
        Assert.Equal(12, annotationClone.Font.Size);
        var textClone = Assert.IsType<TextContent>(annotationClone.Appearance!.Content[0]);
        Assert.Equal("Courier", textClone.Font.Family);
        Assert.Equal(8, textClone.Font.Size);
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
