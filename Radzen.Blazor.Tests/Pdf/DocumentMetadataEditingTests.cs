#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class DocumentMetadataEditingTests
{
    private const string CustomNamespace = "urn:radzen:test:metadata";

    private static PortableDocument SourceDocument()
    {
        var document = new PortableDocument();
        document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (one) Tj ET"));
        document.Pages.Add(PageSizes.A4).SetContent(Encoding.ASCII.GetBytes("BT (two) Tj ET"));

        var chapter = new OutlineItem("Chapter", OutlineTarget.ToPageFit(0));
        chapter.Children.Add(new OutlineItem("Section", OutlineTarget.ToPageFitHorizontal(1, 700)));
        document.Outline.Add(chapter);

        document.Attachments.Add("old.txt", Encoding.UTF8.GetBytes("old payload"), AttachmentRelationship.Supplement, "text/plain");

        document.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.LowercaseRoman });
        document.PageLabels.Add(new PageLabel(1) { Style = PageLabelStyle.Decimal, Prefix = "P-", Start = 4 });
        return document;
    }

    private static PortableDocument Load(byte[] bytes)
        => PortableDocument.LoadFromStream(new MemoryStream(bytes));

    [Fact]
    public void LoadedOutlineAttachmentsAndPageLabels_CanBeReadEditedAndReloaded()
    {
        var document = Load(SourceDocument().ToArray());

        var chapter = Assert.Single(document.Outline);
        Assert.Equal("Chapter", chapter.Title);
        Assert.Equal(0, chapter.Target.PageIndex);
        Assert.Equal("Section", Assert.Single(chapter.Children).Title);

        var oldAttachment = Assert.Single(document.Attachments);
        Assert.Equal("old.txt", oldAttachment.Name);
        Assert.Equal("old payload", Encoding.UTF8.GetString(oldAttachment.GetBytes()));

        Assert.Equal(2, document.PageLabels.Count);
        Assert.Equal("P-", document.PageLabels[1].Prefix);
        Assert.Equal(4, document.PageLabels[1].Start);

        chapter.Title = "Retargeted chapter";
        chapter.Target = OutlineTarget.ToPageXYZ(1, 10, 650, 1.25);
        chapter.Children.Clear();
        document.Outline.Insert(0, new OutlineItem("Cover", OutlineTarget.ToPageFit(0)));

        Assert.True(document.Attachments.Remove(oldAttachment));
        document.Attachments.Add("new.bin", [1, 3, 5, 7], AttachmentRelationship.Data, "application/octet-stream");

        document.PageLabels.Clear();
        document.PageLabels.Add(new PageLabel(0) { Prefix = "Sheet-", Style = PageLabelStyle.Decimal, Start = 10 });

        var reloaded = Load(document.ToArray());
        Assert.Equal(new[] { "Cover", "Retargeted chapter" }, new[] { reloaded.Outline[0].Title, reloaded.Outline[1].Title });
        Assert.Equal(1, reloaded.Outline[1].Target.PageIndex);
        Assert.Empty(reloaded.Outline[1].Children);

        var attachment = Assert.Single(reloaded.Attachments);
        Assert.Equal("new.bin", attachment.Name);
        Assert.Equal(new byte[] { 1, 3, 5, 7 }, attachment.GetBytes());

        var label = Assert.Single(reloaded.PageLabels);
        Assert.Equal("Sheet-", label.Prefix);
        Assert.Equal(PageLabelStyle.Decimal, label.Style);
        Assert.Equal(10, label.Start);
    }

    [Fact]
    public void LoadedNamedOutline_RemainsNavigableWhenUntouched()
    {
        var document = new Document();
        var section = document.Sections.Add();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Named destination").Anchor = "destination";
        section.Blocks.Add(paragraph);
        var renderer = new DocumentRenderer();
        renderer.Outline.Add(new OutlineItem("Named", OutlineTarget.ToAnchor("destination")));

        var loaded = Load(renderer.ToArray(document));
        Assert.Equal("Named", Assert.Single(loaded.Outline).Title);

        var reloaded = Load(loaded.ToArray());
        Assert.Equal("Named", Assert.Single(reloaded.Outline).Title);
        Assert.Equal(0, reloaded.Outline[0].Target.PageIndex);
    }

    [Fact]
    public void XmpPacket_CanBeReadAndPropertiesCanBeSet()
    {
        var document = SourceDocument();
        document.Xmp.SetPacket(Encoding.UTF8.GetBytes(
            "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\"><rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\"><rdf:Description rdf:about=\"\" xmlns:t=\"urn:radzen:test:metadata\"><t:status>draft</t:status></rdf:Description></rdf:RDF></x:xmpmeta>"));

        var loaded = Load(document.ToArray());
        Assert.True(loaded.Xmp.Exists);
        Assert.Equal("draft", loaded.Xmp.GetProperty(CustomNamespace, "status"));
        Assert.NotEmpty(Assert.IsType<byte[]>(loaded.Xmp.GetPacket()));

        loaded.Xmp.SetProperty(CustomNamespace, "status", "approved");
        loaded.Xmp.SetProperty(CustomNamespace, "owner", "Radzen");

        var reloaded = Load(loaded.ToArray());
        Assert.Equal("approved", reloaded.Xmp.GetProperty(CustomNamespace, "status"));
        Assert.Equal("Radzen", reloaded.Xmp.GetProperty(CustomNamespace, "owner"));
    }

    [Fact]
    public void XmpPacket_InvalidXmlThrows()
    {
        var document = new PortableDocument();
        Assert.Throws<InvalidDataException>(() => document.Xmp.SetPacket(Encoding.UTF8.GetBytes("<not-closed>")));
        Assert.Throws<InvalidDataException>(() => document.Xmp.SetPacket(Encoding.UTF8.GetBytes("<xml />")));
    }

    [Fact]
    public void LoadedPdfA_XmpPacketIsUnchangedWhenNewApiIsUnused()
    {
        var document = new Document();
        var renderer = new DocumentRenderer { Conformance = PdfAConformance.PdfA3B };
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Conforming metadata").Font.Family = BuildTestSupport.Latin;
        section.Blocks.Add(paragraph);

        var loaded = Load(renderer.ToArray(document));
        var before = Assert.IsType<byte[]>(loaded.Xmp.GetPacket());

        var reloaded = Load(loaded.ToArray());
        Assert.Equal(before, reloaded.Xmp.GetPacket());
    }

    [Fact]
    public void CallerEditedXmp_WithConformanceThrows()
    {
        var document = new Document();
        var renderer = new DocumentRenderer { Conformance = PdfAConformance.PdfA3B };
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Conforming metadata").Font.Family = BuildTestSupport.Latin;
        section.Blocks.Add(paragraph);

        var pdf = renderer.Render(document);
        pdf.Xmp.SetProperty(CustomNamespace, "status", "custom");

        Assert.Throws<InvalidOperationException>(() => pdf.ToArray());
    }
}
