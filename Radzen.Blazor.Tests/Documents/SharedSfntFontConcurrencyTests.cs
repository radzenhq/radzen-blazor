#nullable enable
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System;
using Radzen.Documents.Fonts.Sfnt;
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
            run.Spans.Sum(span => span.SfntGlyphs.Length),
            string.Join(
                ",",
                run.Spans.SelectMany(span => span.SfntGlyphs).Select(glyph => $"{glyph.GlyphId}:{glyph.Advance:R}")));
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

    private static readonly string[] NamedLazyCaches =
    [
        "Radzen.Documents.Fonts.Sfnt.Cmap.memo",
        "Radzen.Documents.Fonts.Sfnt.SfntFont.kerning",
    ];

    [Fact]
    public void SfntFontFields_AreReadOnlyApartFromTheTwoNamedLazyCaches()
        => AssertNamedLazyCacheInvariant();

    internal static void AssertNamedLazyCacheInvariant()
    {
        var mutable = ReachableFrom(typeof(SfntFont))
            .SelectMany(type => type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
            .Where(field => !field.IsInitOnly)
            .Select(Describe)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(NamedLazyCaches, mutable);
    }

    private static string Describe(FieldInfo field)
        => $"{field.DeclaringType!.FullName}.{BackedProperty(field)?.Name ?? field.Name}";

    private static PropertyInfo? BackedProperty(FieldInfo field)
    {
        if (!field.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false)
            || !field.Name.StartsWith('<')
            || !field.Name.EndsWith(">k__BackingField", StringComparison.Ordinal))
        {
            return null;
        }

        return field.DeclaringType!.GetProperty(
            field.Name[1..field.Name.IndexOf('>')],
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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
