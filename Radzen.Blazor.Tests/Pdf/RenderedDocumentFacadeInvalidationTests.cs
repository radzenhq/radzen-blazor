#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Text;
using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf.Write;
using Xunit;

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

    private static string Save(PortableDocument document)
        => Encoding.Latin1.GetString(document.ToArray());

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

        loaded.MaterializedGraph = new DocumentGraphBuilder(loaded).Build();
        return loaded;
    }

    private static void AssertMarkInfoPrecedesStructTreeRoot(byte[] bytes)
    {
        var reader = DocumentReader.Parse(bytes);
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(reader.Trailer["Root"]));
        var keys = catalog.Keys.ToList();
        var markInfo = keys.IndexOf("MarkInfo");
        var structTreeRoot = keys.IndexOf("StructTreeRoot");

        Assert.True(markInfo >= 0, "preserved catalog has /MarkInfo");
        Assert.True(structTreeRoot >= 0, "preserved catalog has /StructTreeRoot");
        Assert.True(
            markInfo < structTreeRoot,
            $"/MarkInfo must precede /StructTreeRoot in the preserved catalog; actual keys: /{string.Join(" /", catalog.Keys)}");
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

        Assert.Contains("/Encrypt", Save(rendered), StringComparison.Ordinal);
    }

    [Fact]
    public void EncryptionMutatedAfterRender_ReachesTheSavedBytes()
    {
        var renderer = new DocumentRenderer
        {
            Encryption = new EncryptionOptions
            {
                UserPassword = "user",
                Algorithm = EncryptionAlgorithm.Aes128,
                Material = new SeededEncryptionMaterial([1, 2, 3, 4]),
            },
        };

        var rendered = renderer.Render(Authored());
        var immediate = rendered.ToArray();

        rendered.Encryption!.Algorithm = EncryptionAlgorithm.Aes256;

        Assert.NotEqual(immediate, rendered.ToArray());
        Assert.Contains("/AESV3", Save(rendered), StringComparison.Ordinal);
    }

    [Fact]
    public void CompressOutputSetAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        var immediate = rendered.ToArray();

        rendered.CompressOutput = true;

        var compressed = Save(rendered);
        Assert.NotEqual(immediate, rendered.ToArray());
        Assert.Contains("/ObjStm", compressed, StringComparison.Ordinal);
    }

    [Fact]
    public void IncludeDocumentIdSetAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        var immediate = rendered.ToArray();
        Assert.DoesNotContain("/ID", Encoding.Latin1.GetString(immediate), StringComparison.Ordinal);

        rendered.IncludeDocumentId = true;

        Assert.Contains("/ID", Save(rendered), StringComparison.Ordinal);
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

        var saved = Save(rendered);
        Assert.Contains("/AcroForm", saved, StringComparison.Ordinal);
        Assert.Contains("Ada", saved, StringComparison.Ordinal);
    }

    [Fact]
    public void FormFieldDefinitions_AreConsumedByTheRenderAndEditedThroughTheLiveForm()
    {
        var renderer = new DocumentRenderer();
        renderer.FormFields.Add(new TextFieldDefinition("given")
        {
            Value = "Ada",
            X = 40,
            Y = 40,
            Width = 120,
            Height = 20,
        });

        var rendered = renderer.Render(Authored());
        Assert.Contains("Ada", Save(rendered), StringComparison.Ordinal);
        Assert.Empty(rendered.FormFields);

        rendered.AcroForm!.FillField("given", "Grace");

        Assert.Contains("Grace", Save(rendered), StringComparison.Ordinal);
    }

    [Fact]
    public void ViewerPreferencesMutatedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer { ViewerPreferences = new ViewerPreferences { HideToolbar = true } }
            .Render(Authored());
        _ = rendered.ToArray();

        rendered.ViewerPreferences!.HideMenubar = true;

        Assert.Contains("/HideMenubar true", Save(rendered), StringComparison.Ordinal);
    }

    [Fact]
    public void InfoMutatedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        rendered.Info.Title = "Late title";

        Assert.Contains("Late title", Save(rendered), StringComparison.Ordinal);
    }

    [Fact]
    public void OutlineMutatedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        rendered.Outline.Add(new OutlineItem("Late chapter", OutlineTarget.ToPage(0)));

        Assert.Contains("Late chapter", Save(rendered), StringComparison.Ordinal);
    }

    [Fact]
    public void PageLabelsMutatedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        rendered.PageLabels.Add(new PageLabel(0) { Prefix = "Late" });

        Assert.Contains("Late", Save(rendered), StringComparison.Ordinal);
    }

    [Fact]
    public void AttachmentsMutatedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        rendered.Attachments.Add("late.txt", Encoding.ASCII.GetBytes("late"), AttachmentRelationship.Data, "text/plain");

        Assert.Contains("late.txt", Save(rendered), StringComparison.Ordinal);
    }

    [Fact]
    public void XmpMutatedAfterRender_ReachesTheSavedBytes()
    {
        var rendered = new DocumentRenderer().Render(Authored());
        _ = rendered.ToArray();

        rendered.Xmp.SetProperty("http://purl.org/dc/elements/1.1/", "source", "Late source");

        Assert.Contains("Late source", Save(rendered), StringComparison.Ordinal);
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

        Assert.Contains("https://radzen.com/late", Save(rendered), StringComparison.Ordinal);
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
        renderer.Outline.Add(new OutlineItem("Chapter", OutlineTarget.ToPage(0)));

        var first = renderer.Render(Authored());
        var second = renderer.Render(Authored());

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
        Assert.Contains("Facade mutation marker", Encoding.Latin1.GetString(mutated), StringComparison.Ordinal);
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
        AssertMarkInfoPrecedesStructTreeRoot(rebuilt);

        loaded.Info.Title = "Tagged facade mutation marker";
        var mutated = loaded.ToArray();
        Assert.NotEqual(first, mutated);
        Assert.Contains("Tagged facade mutation marker", Encoding.Latin1.GetString(mutated), StringComparison.Ordinal);
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
        var rendered = new DocumentRenderer
        {
            ViewerPreferences = new ViewerPreferences { HideToolbar = true },
        }.Render(Authored());

        var info = rendered.Info;
        var outline = rendered.Outline;
        var labels = rendered.PageLabels;
        var attachments = rendered.Attachments;
        var preferences = rendered.ViewerPreferences!;
        var xmp = rendered.Xmp;
        var content = rendered.Pages[0].Content;
        var first = rendered.ToArray();

        info.Author = "Nested author";
        Assert.Contains("Nested author", Save(rendered), StringComparison.Ordinal);

        preferences.HideMenubar = true;
        Assert.Contains("/HideMenubar true", Save(rendered), StringComparison.Ordinal);

        outline.Add(new OutlineItem("Nested chapter", OutlineTarget.ToPage(0)));
        Assert.Contains("Nested chapter", Save(rendered), StringComparison.Ordinal);

        outline[0].Title = "Renamed chapter";
        Assert.Contains("Renamed chapter", Save(rendered), StringComparison.Ordinal);

        labels.Add(new PageLabel(0) { Prefix = "Nested" });
        Assert.Contains("Nested", Save(rendered), StringComparison.Ordinal);

        labels[0].Prefix = "Relabelled";
        Assert.Contains("Relabelled", Save(rendered), StringComparison.Ordinal);

        attachments.Add("nested.txt", Encoding.ASCII.GetBytes("nested"), AttachmentRelationship.Data, "text/plain");
        Assert.Contains("nested.txt", Save(rendered), StringComparison.Ordinal);

        attachments[0].Description = "Nested description";
        Assert.Contains("Nested description", Save(rendered), StringComparison.Ordinal);

        xmp.SetProperty("http://purl.org/dc/elements/1.1/", "source", "Nested source");
        Assert.Contains("Nested source", Save(rendered), StringComparison.Ordinal);

        content.Add(new TextContent("Nested content", 72, 600) { Font = new Radzen.Documents.Fonts.Font { Size = 12 } });
        Assert.Contains("Nested content", Save(rendered), StringComparison.Ordinal);

        Assert.NotEqual(first, rendered.ToArray());
    }
}
