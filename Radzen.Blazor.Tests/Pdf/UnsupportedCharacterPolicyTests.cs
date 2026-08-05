#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Radzen.Documents;
using Radzen.Documents.Fonts;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

public class UnsupportedCharacterPolicyTests
{
    private static Document LatinDocument(string text)
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, text, BuildTestSupport.Latin);
        return document;
    }

    [Fact]
    public void Throw_ListsEveryUncoveredCharacterAndItsFont()
    {
        var document = LatinDocument("A中B\U0001F389");

        var error = Assert.Throws<InvalidOperationException>(() => new DocumentRenderer().ToArray(document));

        Assert.Contains("'中' (U+4E2D)", error.Message, StringComparison.Ordinal);
        Assert.Contains("(U+1F389)", error.Message, StringComparison.Ordinal);
        Assert.Contains("Liberation Sans", error.Message, StringComparison.Ordinal);
        Assert.Contains("UnsupportedCharacterPolicy.Substitute", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Substitute_RendersNotdefAndReportsEachDistinctCharacterOnce()
    {
        var document = LatinDocument("中 and 中 again");

        var reported = new List<UnsupportedCharacter>();
        var renderer = new DocumentRenderer
        {
            UnsupportedCharacters = UnsupportedCharacterPolicy.Substitute,
            UnsupportedCharacterFound = reported.Add,
        };
        var pdf = renderer.ToArray(document);

        var entry = Assert.Single(reported);
        Assert.Equal(0x4E2D, entry.Codepoint);
        Assert.Equal("中", entry.Character);
        Assert.Equal(BuildTestSupport.Latin, entry.FontFamily);

        using var buffer = new MemoryStream(pdf);
        var reloaded = PortableDocument.LoadFromStream(buffer);
        Assert.Equal("� and � again", reloaded.Pages[0].ExtractText());
    }

    [Fact]
    public void IgnorableCharacterMissingFromTheFont_IsSkippedWithoutWidthOrError()
    {
        var document = new Document();
        BuildTestSupport.RegisterCjk(document);

        var font = new Font { Family = BuildTestSupport.Cjk, Size = 12 };
        Assert.Equal(
            document.Fonts.MeasureText("AB", font),
            document.Fonts.MeasureText("A\u2060B", font));

        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "A\u2060B\uFE0F", BuildTestSupport.Cjk);
        var pdf = new DocumentRenderer().ToArray(document);

        using var buffer = new MemoryStream(pdf);
        var reloaded = PortableDocument.LoadFromStream(buffer);
        Assert.Equal("AB", reloaded.Pages[0].ExtractText());
    }

    [Fact]
    public void SpaceVariantMissingFromTheFont_TakesTheSpaceAdvance()
    {
        var document = new Document();
        BuildTestSupport.RegisterCjk(document);

        var font = new Font { Family = BuildTestSupport.Cjk, Size = 12 };
        Assert.Equal(
            document.Fonts.MeasureText("A B", font),
            document.Fonts.MeasureText("A\u202FB", font));
    }

    [Fact]
    public void BuiltInFont_SkipsMissingIgnorablesAndSubstitutesThroughTheCallback()
    {
        var document = new Document();
        document.Sections.Add().Blocks.Add(new Paragraph("A\uFE0FB"));
        var pdf = new DocumentRenderer().ToArray(document);

        using var buffer = new MemoryStream(pdf);
        var reloaded = PortableDocument.LoadFromStream(buffer);
        Assert.Equal("AB", reloaded.Pages[0].ExtractText());

        var ligature = new Document();
        ligature.Sections.Add().Blocks.Add(new Paragraph("AﬁB"));
        var reported = new List<UnsupportedCharacter>();
        var renderer = new DocumentRenderer
        {
            UnsupportedCharacters = UnsupportedCharacterPolicy.Substitute,
            UnsupportedCharacterFound = reported.Add,
        };
        renderer.ToArray(ligature);

        var entry = Assert.Single(reported);
        Assert.Equal(0xFB01, entry.Codepoint);
        Assert.Equal("Helvetica", entry.FontFamily);
    }
}
