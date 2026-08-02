#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System;
using Radzen.Documents.Fonts.Sfnt;
using Radzen.Documents.Pdf.Fonts;
using Radzen.Documents.Fonts;
using Radzen.Documents.LaidOut;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

using Radzen.Blazor.Pdf.Tests;

public class SharedSfntFontConcurrencyTests
{
    private const string Sans = "Liberation Sans";

    private const string Serif = "Liberation Serif";

    private static readonly string[] Inputs =
    [
        "Radzen",
        "AV Wa To",
        "The quick brown fox",
        "éèê",
        "0123456789",
    ];

    private static FontCollection Fonts()
    {
        var fonts = new FontCollection();
        fonts.Register(Sans, new MemoryStream(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf")));
        fonts.Register(Serif, new MemoryStream(PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf")));
        return fonts;
    }

    private static (double Advance, int Spans, int Glyphs, string Text) Measure(FontCollection fonts, string input, string family)
    {
        var font = new Font { Family = family, Size = 12 };
        var run = fonts.CaptureGlyphRun(input, font);

        return (
            fonts.MeasureText(input, font),
            run.Spans.Length,
            run.Spans.Sum(span => span.Glyphs.Length),
            string.Join(
                ",",
                run.Spans.SelectMany(span =>
                    span.Glyphs.Select(glyph =>
                        $"{PdfFontProgram.Of(span.Face).GetGlyphId(glyph.Codepoint)}:{glyph.Advance:R}"))));
    }

    [Fact]
    public void ParallelMeasurementOverSharedFaces_MatchesSingleThreadedResults()
    {
        var fonts = Fonts();
        var families = new[] { Sans, Serif };
        var expected = new Dictionary<(string Input, string Family), (double, int, int, string)>();

        foreach (var family in families)
        {
            foreach (var input in Inputs)
            {
                expected[(input, family)] = Measure(Fonts(), input, family);
            }
        }

        var work = Enumerable.Range(0, 400)
            .Select(index => (Input: Inputs[index % Inputs.Length], Family: families[index % families.Length]))
            .ToArray();

        var observed = new (double, int, int, string)[work.Length];

        Parallel.For(0, work.Length, index =>
        {
            observed[index] = Measure(fonts, work[index].Input, work[index].Family);
        });

        for (var index = 0; index < work.Length; index++)
        {
            Assert.Equal(expected[work[index]], observed[index]);
        }
    }

    [Fact]
    public void ParallelKerningLookupsOverASharedFace_AgreeWithASingleThreadedFace()
    {
        var shared = Assert.Single(Fonts().RegisteredFaces().Where(face => face.Family == Sans)).Face;
        var solo = SfntFont.Parse(PdfTestResources.ReadAllBytes("Fonts/LiberationSans-Regular.ttf"));

        var pairs = Enumerable.Range(32, 96)
            .SelectMany(left => Enumerable.Range(32, 96).Select(right => (Left: left, Right: right)))
            .Select(pair => (shared.GetGlyphId(pair.Left), shared.GetGlyphId(pair.Right)))
            .ToArray();

        var expected = pairs.Select(pair => solo.GetKerning(pair.Item1, pair.Item2)).ToArray();
        var observed = new int[pairs.Length];

        Parallel.For(0, pairs.Length, index =>
        {
            observed[index] = shared.GetKerning(pairs[index].Item1, pairs[index].Item2);
        });

        Assert.Equal(expected, observed);
    }

    private static readonly Dictionary<string, int> LazyCachesByDeclaringType = new(StringComparer.Ordinal)
    {
        ["Radzen.Documents.Fonts.Sfnt.Cmap"] = 1,
        ["Radzen.Documents.Fonts.Sfnt.SfntFont"] = 1,
    };

    [Fact]
    public void SfntFontFields_AreReadOnlyApartFromOneLazyCachePerCachingType()
        => AssertLazyCacheInvariant();

    internal static void AssertLazyCacheInvariant()
    {
        var mutable = ReachableFrom(typeof(SfntFont))
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            .Where(field => !field.IsInitOnly)
            .GroupBy(field => field.DeclaringType!.FullName!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        Assert.Equal(LazyCachesByDeclaringType, mutable);
    }

    [Fact]
    public void SfntFontProperties_AreNeverPubliclySettable()
    {
        var settable = ReachableFrom(typeof(SfntFont))
            .SelectMany(type => type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => $"{property.DeclaringType!.FullName}.{property.Name}")
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(settable);
    }

    private static IReadOnlyCollection<Type> ReachableFrom(Type root)
    {
        var seen = new HashSet<Type> { root };
        var pending = new Queue<Type>([root]);

        while (pending.Count > 0)
        {
            foreach (var field in pending.Dequeue().GetFields(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            {
                var type = field.FieldType;
                type = Nullable.GetUnderlyingType(type) ?? type;
                type = type.IsArray ? type.GetElementType()! : type;

                if (type.Assembly == root.Assembly && !type.IsEnum && seen.Add(type))
                {
                    pending.Enqueue(type);
                }
            }
        }

        return seen;
    }
}
