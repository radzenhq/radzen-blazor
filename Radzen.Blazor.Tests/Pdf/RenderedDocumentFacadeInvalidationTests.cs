#nullable enable
using System;
using System.Text;
using Radzen.Documents;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects.Encryption;
using Radzen.Documents.Pdf.Render;
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

        var first = DocumentGenerator.Generate(request, laidOut);
        var second = DocumentGenerator.Generate(request, laidOut);

        Assert.NotSame(first, second);
        Assert.Equal(laidOut.Pages.Length, first.Pages.Count);
        Assert.Equal(laidOut.Pages.Length, second.Pages.Count);
        Assert.Equal(first.ToArray(), second.ToArray());
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
}
