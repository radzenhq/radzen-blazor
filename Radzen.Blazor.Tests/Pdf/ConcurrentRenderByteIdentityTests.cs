#nullable enable
using System.Linq;
using System.Threading.Tasks;
using Radzen.Documents.LaidOut;
using Radzen.Documents.Layout;
using Radzen.Documents.Pdf.Render;
using Radzen.Documents.Pdf;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class ConcurrentRenderByteIdentityTests
{
    private const int Threads = 8;

    private static Document Authored()
    {
        var document = new Document { Language = "en" };
        document.Info.Title = "Concurrent";
        document.Info.Author = "Author";
        BuildTestSupport.RegisterLatin(document);

        var section = document.Sections.Add();

        for (var index = 0; index < 40; index++)
        {
            BuildTestSupport.AddText(section, $"Paragraph {index} with AV Wa To and éèê", BuildTestSupport.Latin);
        }

        return document;
    }

    private static DocumentRenderer Renderer()
    {
        var renderer = new DocumentRenderer
        {
            Producer = "Concurrent Producer 1.0",
            ViewerPreferences = new ViewerPreferences { HideToolbar = true, PageMode = PdfPageMode.UseOutlines },
        };

        renderer.Outline.Add(new OutlineItem("Chapter", OutlineTarget.ToPage(0)));
        renderer.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.UppercaseRoman });

        return renderer;
    }

    private static byte[] Render(LaidOutDocument laidOut)
        => DocumentRenderEngine.Generate(RenderRequest.From(Renderer()), laidOut).ToArray();

    [Fact]
    public void RenderingOneLaidOutDocumentOnManyThreads_ProducesBytesIdenticalToASingleThreadedRender()
    {
        var laidOut = DocumentLayouter.Layout(Authored());
        var expected = Render(laidOut);
        var observed = new byte[Threads][];

        var work = Enumerable.Range(0, Threads)
            .Select(index => Task.Run(() => observed[index] = Render(laidOut)))
            .ToArray();

        Task.WaitAll(work);

        Assert.NotEmpty(expected);

        foreach (var bytes in observed)
        {
            Assert.Equal(expected, bytes);
        }
    }
}
