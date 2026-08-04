#nullable enable
using System;
using System.IO;
using System.Text;
using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf.Write;
using Xunit;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class RenderedDocumentFacadeInvalidationTests
{
    private static Document Authored()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        BuildTestSupport.AddText(document.Sections.Add(), "Body", BuildTestSupport.Latin);
        return document;
    }

    private static PortableDocument LoadedTaggedWithPreservationGraphCached()
    {
        var authored = Authored();
        authored.Info.Title = "Tagged facade invalidation";
        authored.Language = "en";

        var rendered = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 }.Render(authored);
        using var stream = new MemoryStream(rendered.ToArray());
        var loaded = PortableDocument.LoadFromStream(stream);

        Assert.Null(loaded.Output);
        Assert.True(loaded.HasPreservableStructureGraph);
        Assert.True(loaded.Loaded!.SourceCatalog!.ContainsKey("MarkInfo"));

        loaded.MaterializedGraph = new DocumentGraphBuilder(loaded, renderTime: false).Build();
        return loaded;
    }

    private static void AssertMarkInfoPrecedesStructTreeRoot(string emission)
    {
        var catalog = Line(emission, "/Type /Catalog");
        Carries("preserved catalog", "/MarkInfo ", catalog);
        Carries("preserved catalog", "/StructTreeRoot ", catalog);

        var markInfo = catalog.IndexOf("/MarkInfo ", StringComparison.Ordinal);
        var structTreeRoot = catalog.IndexOf("/StructTreeRoot ", StringComparison.Ordinal);

        Assert.True(
            markInfo < structTreeRoot,
            $"/MarkInfo must precede /StructTreeRoot in the preserved catalog.\npreserved catalog:\n{Excerpt(catalog)}");
    }

    [Fact]
    public void EncryptionSetAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        rendered.Encryption = new EncryptionOptions
        {
            UserPassword = "user",
            Material = new SeededEncryptionMaterial([1, 2, 3, 4]),
        };

        Carries("emission", "/Encrypt", Emit(rendered));
    }

    [Fact]
    public void EncryptionMutatedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        rendered.Encryption = new EncryptionOptions
        {
            UserPassword = "user",
            Algorithm = EncryptionAlgorithm.Aes128,
            Material = new SeededEncryptionMaterial([1, 2, 3, 4]),
        };

        var immediate = rendered.ToArray();

        rendered.Encryption!.Algorithm = EncryptionAlgorithm.Aes256;

        Assert.NotEqual(immediate, rendered.ToArray());
        Carries("emission", "/AESV3", Emit(rendered));
    }

    [Fact]
    public void CompressOutputSetAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        var immediate = rendered.ToArray();

        rendered.CompressOutput = true;

        var compressed = Emit(rendered);
        Assert.NotEqual(immediate, rendered.ToArray());
        Carries("emission", "/ObjStm", compressed);
    }

    [Fact]
    public void IncludeDocumentIdSetAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        Lacks("trailer", "/ID ", Line(Emit(rendered), "/Root "));

        rendered.IncludeDocumentId = true;

        Carries("trailer", "/ID ", Line(Emit(rendered), "/Root "));
    }

    [Fact]
    public void FormFieldAddedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        rendered.FormFields.Add(new TextFieldDefinition("given")
        {
            Value = "Ada",
            X = 40,
            Y = 40,
            Width = 120,
            Height = 20,
        });

        var saved = Emit(rendered);
        Carries("emission", "/AcroForm", saved);
        Carries("emission", "Ada", saved);
    }

    [Fact]
    public void ViewerPreferencesMutatedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        rendered.ViewerPreferences = new ViewerPreferences { HideToolbar = true };
        _ = rendered.ToArray();

        rendered.ViewerPreferences!.HideMenubar = true;

        Carries("emission", "/HideMenubar true", Emit(rendered));
    }

    [Fact]
    public void InfoMutatedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        rendered.Info.Title = "Late title";

        Carries("emission", "Late title", Emit(rendered));
    }

    [Fact]
    public void OutlineMutatedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        rendered.Outline.Add(new OutlineItem("Late chapter", OutlineTarget.ToPage(0)));

        Carries("emission", "Late chapter", Emit(rendered));
    }

    [Fact]
    public void PageLabelsMutatedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        rendered.PageLabels.Add(new PageLabel(0) { Prefix = "Late" });

        Carries("emission", "Late", Emit(rendered));
    }

    [Fact]
    public void AttachmentsMutatedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        rendered.Attachments.Add("late.txt", Encoding.ASCII.GetBytes("late"), AttachmentRelationship.Data, "text/plain");

        Carries("emission", "late.txt", Emit(rendered));
    }

    [Fact]
    public void XmpMutatedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        rendered.Xmp.SetProperty("http://purl.org/dc/elements/1.1/", "source", "Late source");

        Carries("emission", "Late source", Emit(rendered));
    }

    [Fact]
    public void PagesMutatedAfterRender_ReachTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        var immediate = rendered.ToArray();

        rendered.Pages.Add();

        Assert.NotEqual(immediate, rendered.ToArray());
        Assert.Equal(2, rendered.Pages.Count);
    }

    [Fact]
    public void PageAnnotationAddedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        rendered.Pages[0].Annotations.Add(new LinkAnnotation(PdfRect.FromSize(10, 10, 20, 20))
        {
            Uri = new Uri("https://radzen.com/late"),
        });

        Carries("emission", "https://radzen.com/late", Emit(rendered));
    }

    [Fact]
    public void GenerateTwiceFromOneRequest_ProducesTwoIndependentDocuments()
    {
        var model = Authored();
        var laidOut = DocumentLayouter.Layout(model);
        var request = RenderRequest.From(new DocumentRenderer());

        var first = DocumentRenderEngine.Generate(request, laidOut);
        var second = DocumentRenderEngine.Generate(request, laidOut);

        Assert.NotSame(first, second);
        Assert.Equal(laidOut.Pages.Length, first.Pages.Count);
        Assert.Equal(laidOut.Pages.Length, second.Pages.Count);
        Assert.Equal(first.ToArray(), second.ToArray());
    }

    [Fact]
    public void UnadoptedOutput_WithStructuredLink_MaterializesRepeatably()
    {
        var model = Authored();
        var paragraph = new Paragraph();
        paragraph.Inlines.Add("Structured link").Link = "https://radzen.com/emission-plan";
        model.Sections[0].Blocks.Add(paragraph);
        var laidOut = DocumentLayouter.Layout(model);
        var request = RenderRequest.From(new DocumentRenderer());
        var generated = DocumentRenderEngine.Generate(request, laidOut);

        Assert.Equal(generated.ToArray(), generated.ToArray());
    }

    [Fact]
    public void RenderTwice_ProducesTwoIndependentDocuments()
    {
        var renderer = new DocumentRenderer();

        var first = renderer.Render(Authored());
        var second = renderer.Render(Authored());
        first.Outline.Add(new OutlineItem("Chapter", OutlineTarget.ToPage(0)));
        second.Outline.Add(new OutlineItem("Chapter", OutlineTarget.ToPage(0)));

        Assert.NotSame(first, second);
        Assert.Equal(1, first.Pages.Count);
        Assert.Equal(1, second.Pages.Count);
        Assert.Equal(first.ToArray(), second.ToArray());
    }

    [Fact]
    public void ReadingEveryDocumentFacade_RebuildsByteIdentically_AndLaterMutationStillSaves()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        var first = rendered.ToArray();

        _ = rendered.Info;
        _ = rendered.Pages;
        _ = rendered.AcroForm;
        _ = rendered.FormFields;
        _ = rendered.Encryption;
        _ = rendered.CompressOutput;
        _ = rendered.IncludeDocumentId;
        _ = rendered.ViewerPreferences;
        _ = rendered.PageLabels;
        _ = rendered.Outline;
        _ = rendered.Attachments;
        _ = rendered.Xmp;

        Assert.Equal(first, rendered.ToArray());

        rendered.Info.Title = "Facade mutation marker";
        var mutated = rendered.ToArray();
        Assert.NotEqual(first, mutated);
        Carries("emission", "Facade mutation marker", Emit(rendered));
    }

    [Fact]
    public void ReadingEveryDocumentFacade_OnLoadedTaggedDocument_RebuildsByteIdentically_AndLaterMutationStillSaves()
    {
        var loaded = LoadedTaggedWithPreservationGraphCached();
        var first = loaded.ToArray();

        _ = loaded.Info;
        _ = loaded.Pages;
        _ = loaded.AcroForm;
        _ = loaded.FormFields;
        _ = loaded.Encryption;
        _ = loaded.CompressOutput;
        _ = loaded.IncludeDocumentId;
        _ = loaded.ViewerPreferences;
        _ = loaded.PageLabels;
        _ = loaded.Outline;
        _ = loaded.Attachments;
        _ = loaded.Xmp;

        var rebuilt = loaded.ToArray();
        Assert.Equal(first, rebuilt);
        AssertMarkInfoPrecedesStructTreeRoot(Encoding.Latin1.GetString(rebuilt));

        loaded.Info.Title = "Tagged facade mutation marker";
        var mutated = loaded.ToArray();
        Assert.NotEqual(first, mutated);
        Carries("emission", "Tagged facade mutation marker", Emit(loaded));
    }

    [Fact]
    public void ReadingEveryDocumentFacade_KeepsTheMaterializedGraph()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        _ = rendered.Info;
        _ = rendered.Pages;
        _ = rendered.AcroForm;
        _ = rendered.FormFields;
        _ = rendered.ViewerPreferences;
        _ = rendered.PageLabels;
        _ = rendered.Outline;
        _ = rendered.Attachments;
        _ = rendered.Xmp;
        _ = rendered.Pages[0].Content;

        Assert.NotNull(rendered.MaterializedGraph);
    }

    [Fact]
    public void MutatingANestedMemberThroughAPreviouslyReadFacade_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        rendered.ViewerPreferences = new ViewerPreferences { HideToolbar = true };

        var info = rendered.Info;
        var outline = rendered.Outline;
        var labels = rendered.PageLabels;
        var attachments = rendered.Attachments;
        var preferences = rendered.ViewerPreferences!;
        var xmp = rendered.Xmp;
        var content = rendered.Pages[0].Content;
        var first = rendered.ToArray();

        info.Author = "Nested author";
        Carries("emission", "Nested author", Emit(rendered));

        preferences.HideMenubar = true;
        Carries("emission", "/HideMenubar true", Emit(rendered));

        outline.Add(new OutlineItem("Nested chapter", OutlineTarget.ToPage(0)));
        Carries("emission", "Nested chapter", Emit(rendered));

        outline[0].Title = "Renamed chapter";
        Carries("emission", "Renamed chapter", Emit(rendered));

        labels.Add(new PageLabel(0) { Prefix = "Nested" });
        Carries("emission", "Nested", Emit(rendered));

        labels[0].Prefix = "Relabelled";
        Carries("emission", "Relabelled", Emit(rendered));

        attachments.Add("nested.txt", Encoding.ASCII.GetBytes("nested"), AttachmentRelationship.Data, "text/plain");
        Carries("emission", "nested.txt", Emit(rendered));

        attachments[0].Description = "Nested description";
        Carries("emission", "Nested description", Emit(rendered));

        xmp.SetProperty("http://purl.org/dc/elements/1.1/", "source", "Nested source");
        Carries("emission", "Nested source", Emit(rendered));

        content.Add(new TextContent("Nested content", 72, 600) { Font = new Radzen.Documents.Fonts.Font { Size = 12 } });
        Carries("emission", "Nested content", Emit(rendered));

        Assert.NotEqual(first, rendered.ToArray());
    }
}
