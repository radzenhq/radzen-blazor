#nullable enable
using System.IO;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class LoadedPatternOverlayCollisionTests
{
    private static PortableDocument LoadWithPatternP0()
    {
        const string streamData = "/Pattern cs /P0 scn 0 0 100 100 re f";
        var contentObject = $"4 0 obj\n<< /Length {streamData.Length} >>\nstream\n{streamData}\nendstream\nendobj\n";
        var pdf = new FixturePdf()
            .Append("%PDF-1.7\n")
            .Object(1, "1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n")
            .Object(2, "2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n")
            .Object(3, "3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] "
                + "/Resources << /Pattern << /P0 5 0 R >> >> /Contents 4 0 R >>\nendobj\n")
            .Object(4, contentObject)
            .Object(5, "5 0 obj\n<< /Type /Pattern /PatternType 2 /Shading << /ShadingType 3 "
                + "/ColorSpace /DeviceGray /Coords [0 0 0 0 0 1] "
                + "/Function << /FunctionType 2 /Domain [0 1] /C0 [0] /C1 [1] /N 1 >> >> >>\nendobj\n");
        var xref = pdf.Position;
        pdf.Append("xref\n0 6\n").Append(FixturePdf.Entry20(0, 65535, 'f'));
        for (var number = 1; number <= 5; number++)
        {
            pdf.Append(FixturePdf.Entry20(pdf.OffsetOf(number)));
        }

        pdf.Append("trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n" + xref + "\n%%EOF\n");
        using var input = new MemoryStream(pdf.ToArray());
        return PortableDocument.LoadFromStream(input);
    }

    private static DictionaryObject Patterns(DocumentReader reader)
    {
        var resources = BuildTestSupport.PageLeaves(reader)[0].Resources;
        Assert.NotNull(resources);
        Assert.True(resources!.TryGetValue("Pattern", out var patternObject));
        return (DictionaryObject)reader.Resolve(patternObject!);
    }

    private static int ShadingType(DocumentReader reader, DictionaryObject pattern)
    {
        var shading = (DictionaryObject)reader.Resolve(pattern["Shading"]);
        return ((NumberObject)reader.Resolve(shading["ShadingType"])).IntValue;
    }

    [Fact]
    public void AddingGradientOverlay_PreservesTheLoadedPatternResource()
    {
        var document = LoadWithPatternP0();
        var path = new PathContent
        {
            Fill = true,
            FillGradient = new LinearGradient(0, 0, 100, 100, new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue)),
        };
        path.MoveTo(200, 200);
        path.LineTo(300, 200);
        path.LineTo(300, 300);
        path.Close();
        document.Pages[0].Content.Add(path);

        var reader = DocumentReader.Parse(document.ToArray());
        var patterns = Patterns(reader);

        Assert.True(patterns.TryGetValue("P0", out var p0));
        Assert.Equal(3, ShadingType(reader, (DictionaryObject)reader.Resolve(p0!)));
        Assert.True(patterns.Keys.Count >= 2, "the overlay gradient is registered under a non-colliding name");
    }

    [Fact]
    public void ReemittingModifiedContentWithInsertedGradient_PreservesTheLoadedPatternResource()
    {
        var document = LoadWithPatternP0();
        var page = document.Pages[0];
        var existing = (PathContent)page.Content[0];
        existing.EvenOdd = true;

        var path = new PathContent
        {
            Fill = true,
            FillGradient = new LinearGradient(0, 0, 100, 100, new GradientStop(0, Color.Red), new GradientStop(1, Color.Blue)),
        };
        path.MoveTo(200, 200);
        path.LineTo(300, 200);
        path.LineTo(300, 300);
        path.Close();
        page.Content.Add(path);

        var reader = DocumentReader.Parse(document.ToArray());
        var patterns = Patterns(reader);

        Assert.True(patterns.TryGetValue("P0", out var p0));
        Assert.Equal(3, ShadingType(reader, (DictionaryObject)reader.Resolve(p0!)));
        Assert.True(patterns.Keys.Count >= 2, "the reemitted gradient is registered under a non-colliding name");
    }
}
