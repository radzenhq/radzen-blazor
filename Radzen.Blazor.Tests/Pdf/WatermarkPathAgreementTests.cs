#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class WatermarkPathAgreementTests
{
    private static Watermark Mark() => new()
    {
        Text = "DRAFT",
        Opacity = 0.5,
        Rotation = 30,
        Font =
        {
            Size = 42,
            Color = Color.FromArgb(128, 20, 40, 60),
        },
    };

    private static DocumentReader Generated()
    {
        var builder = new DocumentBuilder();
        var section = builder.Sections.Add();
        section.Watermark = Mark();
        section.Blocks.Add(new Paragraph());
        return BuildTestSupport.Read(builder);
    }

    private static DocumentReader Appended()
    {
        var builder = new DocumentBuilder();
        builder.Sections.Add().Blocks.Add(new Paragraph());
        var document = builder.Build();
        document.AddWatermark(Mark());
        return DocumentReader.Parse(document.ToArray());
    }

    [Fact]
    public void GeneratedAndAppendedWatermarks_AgreeOnTextPlacementAndAlpha()
    {
        var generated = Snapshot(Generated());
        var appended = Snapshot(Appended());

        Assert.Equal(generated.Transform, appended.Transform);
        Assert.Equal(generated.Position, appended.Position);
        Assert.Equal(generated.Color, appended.Color);
        Assert.Equal(generated.Size, appended.Size);
        Assert.Equal(generated.Text, appended.Text);
        Assert.Equal(generated.Alphas, appended.Alphas);
    }

    private static WatermarkSnapshot Snapshot(DocumentReader reader)
    {
        var (page, resources) = BuildTestSupport.PageLeaves(reader)[0];
        var operations = ContentStreamTokenizer.Parse(BuildTestSupport.Content(reader, page));

        var transform = LastNumbers(operations, "cm", 6);
        var position = LastNumbers(operations, "Td", 2);
        var color = LastNumbers(operations, "rg", 3);
        var size = Last(operations, "Tf").Num(1);
        var text = Last(operations, "Tj").Operands[0].Bytes;
        var alphas = ExtGStateAlphas(reader, resources);

        return new WatermarkSnapshot(transform, position, color, size, text, alphas);
    }

    private static ContentOperation Last(List<ContentOperation> operations, string name)
        => operations.Last(operation => operation.Operator == name);

    private static double[] LastNumbers(List<ContentOperation> operations, string name, int count)
    {
        var operation = Last(operations, name);
        return Enumerable.Range(0, count).Select(operation.Num).ToArray();
    }

    private static double[] ExtGStateAlphas(DocumentReader reader, DictionaryObject? resources)
    {
        Assert.NotNull(resources);
        var states = Assert.IsType<DictionaryObject>(reader.Resolve(resources!["ExtGState"]));
        return states.Keys
            .Select(key => Assert.IsType<NumberObject>(
                Assert.IsType<DictionaryObject>(reader.Resolve(states[key]))["ca"]).DoubleValue)
            .OrderBy(value => value)
            .ToArray();
    }

    private sealed record WatermarkSnapshot(
        double[] Transform,
        double[] Position,
        double[] Color,
        double Size,
        byte[] Text,
        double[] Alphas);
}
