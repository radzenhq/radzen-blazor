#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

internal static class RawPdfAssertions
{
    public static string Emit(PortableDocument document) => Encoding.Latin1.GetString(document.ToArray());

    public static string Emit(Document document, DocumentRenderer? renderer = null)
        => Emit((renderer ?? new DocumentRenderer()).Render(document));

    public static string Excerpt(string text)
        => text.Length <= 600 ? text : text[..600] + "...";

    public static string Line(string emission, string marker)
    {
        var index = emission.IndexOf(marker, StringComparison.Ordinal);
        Assert.True(
            index >= 0,
            $"No emitted line carries '{marker}'. Emission starts:\n{Excerpt(emission)}");

        var start = emission.LastIndexOf('\n', index) + 1;
        var end = emission.IndexOf('\n', index);
        return end < 0 ? emission[start..] : emission[start..end];
    }

    public static string IndirectObject(string emission, string number)
    {
        var header = $"\n{number} 0 obj\n";
        var start = emission.IndexOf(header, StringComparison.Ordinal);
        Assert.True(start >= 0, $"Object '{number} 0 obj' is not in the emission.");

        var body = start + header.Length;
        var end = emission.IndexOf("\nendobj", body, StringComparison.Ordinal);
        Assert.True(end >= 0, $"Object '{number} 0 obj' has no endobj. Body starts:\n{Excerpt(emission[body..])}");
        return emission[body..end];
    }

    public static void Carries(string subject, string fragment, string container)
        => Assert.True(
            container.Contains(fragment, StringComparison.Ordinal),
            $"{subject} is missing '{fragment}'.\n{subject}:\n{Excerpt(container)}");

    public static void Lacks(string subject, string fragment, string container)
        => Assert.True(
            !container.Contains(fragment, StringComparison.Ordinal),
            $"{subject} unexpectedly carries '{fragment}'.\n{subject}:\n{Excerpt(container)}");

    public static Match Shaped(string subject, string pattern, string container)
    {
        var match = Regex.Match(container, pattern);
        Assert.True(match.Success, $"{subject} does not match '{pattern}'.\n{subject}:\n{Excerpt(container)}");
        return match;
    }

    public static string[] References(string subject, string key, int count, string container)
    {
        var pattern = $"/{key} \\[{string.Join(" ", Enumerable.Repeat(@"(\d+) 0 R", count))}\\]";
        var match = Shaped($"{subject} /{key} with {count} references", pattern, container);
        return [.. match.Groups.Cast<System.Text.RegularExpressions.Group>().Skip(1).Select(group => group.Value)];
    }

    public static string[] ReferencesIn(string subject, string key, string container)
    {
        var array = Shaped($"{subject} /{key}", $@"/{Regex.Escape(key)} \[([^\]]*)\]", container);
        return [.. Regex.Matches(array.Groups[1].Value, @"(\d+) 0 R").Select(match => match.Groups[1].Value)];
    }

    public static string StructureMarker(string type) => $"/Type /StructElem /S /{type} /P ";

    public static string StructureElement(string emission, string type)
        => Line(emission, StructureMarker(type));

    public static string StructureRoot(string emission)
        => IndirectObject(
            emission,
            Shaped("catalog", @"/StructTreeRoot (\d+) 0 R", Line(emission, "/Type /Catalog")).Groups[1].Value);

    public static string StructureKids(string subject, string element)
        => Shaped(subject, @"/K \[([^\]]*)\]", element).Groups[1].Value;

    public static string[] ChildElements(string kids)
        => [.. Regex.Matches(Regex.Replace(kids, "<< [^>]*>>", " "), @"(\d+) 0 R")
            .Select(match => match.Groups[1].Value)];

    public static int[] Mcids(string kids)
        => [.. Regex.Matches(Regex.Replace(kids, @"<< [^>]*>>|\d+ 0 R", " "), @"\d+")
            .Select(match => int.Parse(match.Value, CultureInfo.InvariantCulture))];

    public static int NumberIn(string objectBody, string key)
    {
        var match = Regex.Match(objectBody, $@"/{Regex.Escape(key)} (-?\d+)");
        Assert.True(
            match.Success,
            $"No '/{key} <integer>' in the object.\nObject:\n{Excerpt(objectBody)}");
        return int.Parse(match.Groups[1].Value);
    }

    public static void CarriesFlag(string label, string objectBody, string key, int bit)
    {
        var value = FlagValue(objectBody, key);
        Assert.True(
            (value & bit) == bit,
            $"{label} is missing bit {bit} of '/{key}'; /{key} is {value}.\n{label}:\n{Excerpt(objectBody)}");
    }

    public static void LacksFlag(string label, string objectBody, string key, int bit)
    {
        var value = FlagValue(objectBody, key);
        Assert.True(
            (value & bit) == 0,
            $"{label} unexpectedly carries bit {bit} of '/{key}'; /{key} is {value}.\n{label}:\n{Excerpt(objectBody)}");
    }

    private static int FlagValue(string objectBody, string key)
        => Regex.IsMatch(objectBody, $@"/{Regex.Escape(key)} -?\d+") ? NumberIn(objectBody, key) : 0;
}
