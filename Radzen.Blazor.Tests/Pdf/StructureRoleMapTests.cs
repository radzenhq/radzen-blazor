#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class StructureRoleMapTests
{
    private static DocumentReader ReadAuthored((Document Document, DocumentRenderer Renderer) authored)
        => BuildTestSupport.Read(authored.Document, authored.Renderer);

    private static byte[] RenderAuthored((Document Document, DocumentRenderer Renderer) authored)
        => authored.Renderer.ToArray(authored.Document);

    private static (Document Document, DocumentRenderer Renderer) AuthorTagged(bool declareRole)
    {
        var document = new Document { Language = "en" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Doc";
        BuildTestSupport.RegisterLatin(document);

        if (declareRole)
        {
            builderRenderer.RoleMap.Add("Callout", "P");
        }

        document.Styles.Add("Callout");
        var section = document.Sections.Add();
        var note = BuildTestSupport.AddText(section, "See the note", BuildTestSupport.Latin);
        note.StyleName = "Callout";

        return (document, builderRenderer);
    }

    private static DictionaryObject StructTreeRoot(DocumentReader reader)
    {
        Assert.True(reader.Trailer.TryGetValue("Root", out var rootObject), "trailer has /Root");
        var catalog = Assert.IsType<DictionaryObject>(reader.Resolve(rootObject!));
        Assert.True(catalog.TryGetValue("StructTreeRoot", out var structObject), "catalog has /StructTreeRoot");
        return Assert.IsType<DictionaryObject>(reader.Resolve(structObject!));
    }

    private static string? FirstStructureRole(DocumentReader reader, DictionaryObject structRoot)
    {
        var document = Assert.IsType<DictionaryObject>(reader.Resolve(FirstKid(reader, structRoot)));
        var sect = Assert.IsType<DictionaryObject>(reader.Resolve(FirstKid(reader, document)));
        var element = Assert.IsType<DictionaryObject>(reader.Resolve(FirstKid(reader, sect)));
        return element.TryGetValue("S", out var s) && reader.Resolve(s!) is NameObject role ? role.Value : null;
    }

    private static DocumentObject FirstKid(DocumentReader reader, DictionaryObject parent)
    {
        Assert.True(parent.TryGetValue("K", out var k), "structure element has /K");
        var resolved = reader.Resolve(k!);
        return resolved is ArrayObject array ? array[0] : resolved;
    }

    [Fact]
    public void DeclaredRole_EmitsRoleMapAndTagsElementWithTheRole()
    {
        var reader = ReadAuthored(AuthorTagged(declareRole: true));
        var structRoot = StructTreeRoot(reader);

        Assert.True(structRoot.TryGetValue("RoleMap", out var mapObject), "StructTreeRoot has /RoleMap");
        var roleMap = Assert.IsType<DictionaryObject>(reader.Resolve(mapObject!));
        Assert.True(roleMap.TryGetValue("Callout", out var mapped), "/RoleMap maps Callout");
        Assert.Equal("P", Assert.IsType<NameObject>(reader.Resolve(mapped!)).Value);

        Assert.Equal("Callout", FirstStructureRole(reader, structRoot));
    }

    [Fact]
    public void NoDeclaredRoles_OmitsRoleMapAndKeepsStandardTag()
    {
        var reader = ReadAuthored(AuthorTagged(declareRole: false));
        var structRoot = StructTreeRoot(reader);

        Assert.False(structRoot.ContainsKey("RoleMap"), "StructTreeRoot has no /RoleMap when no roles declared");
        Assert.Equal("P", FirstStructureRole(reader, structRoot));
    }

    [Fact]
    public void StyleNameWithoutADeclaredRole_StaysStandardParagraph()
    {
        var document = new Document { Language = "en" };
        var builderRenderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Doc";
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Add("Unknown");
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin).StyleName = "Unknown";

        var reader = BuildTestSupport.Read(document, builderRenderer);
        var structRoot = StructTreeRoot(reader);

        Assert.False(structRoot.ContainsKey("RoleMap"), "no /RoleMap for an undeclared style");
        Assert.Equal("P", FirstStructureRole(reader, structRoot));
    }

    private static (Document Document, DocumentRenderer Renderer) AuthorWithStyleRole(
        string styleName,
        string role,
        string? mapsTo)
    {
        var document = new Document { Language = "en" };
        var renderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Doc";
        BuildTestSupport.RegisterLatin(document);

        if (mapsTo is not null)
        {
            renderer.RoleMap.Add(role, mapsTo);
        }

        if (!document.Styles.Contains(styleName))
        {
            document.Styles.Add(styleName);
        }

        document.Styles[styleName].Role = role;
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin).StyleName = styleName;
        return (document, renderer);
    }

    [Fact]
    public void StyleRole_ReplacesTheStyleNameAsTheRoleMapLookup()
    {
        var reader = ReadAuthored(AuthorWithStyleRole("Body", "Callout", "P"));

        Assert.Equal("Callout", FirstStructureRole(reader, StructTreeRoot(reader)));
    }

    [Fact]
    public void StyleRole_InheritsThroughTheBaseStyleChain()
    {
        var document = new Document { Language = "en" };
        var renderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        document.Info.Title = "Doc";
        BuildTestSupport.RegisterLatin(document);
        renderer.RoleMap.Add("Callout", "P");
        document.Styles.Add("Base").Role = "Callout";
        document.Styles.Add("Derived", "Base");
        BuildTestSupport.AddText(document.Sections.Add(), "Body", BuildTestSupport.Latin).StyleName = "Derived";

        var reader = BuildTestSupport.Read(document, renderer);

        Assert.Equal("Callout", FirstStructureRole(reader, StructTreeRoot(reader)));
    }

    [Fact]
    public void HeadingLevel_WinsOverADeclaredRole()
    {
        var reader = ReadAuthored(AuthorWithStyleRole("Heading2", "Callout", "P"));
        var structRoot = StructTreeRoot(reader);

        Assert.Equal("H2", FirstStructureRole(reader, structRoot));
        Assert.True(structRoot.ContainsKey("RoleMap"), "the declared role is still role mapped");
    }

    [Fact]
    public void StandardStructureTypeAsARole_NeedsNoRoleMapEntry()
    {
        var reader = ReadAuthored(AuthorWithStyleRole("Quote", "BlockQuote", mapsTo: null));
        var structRoot = StructTreeRoot(reader);

        Assert.False(structRoot.ContainsKey("RoleMap"), "a standard type is not remapped");
        Assert.Equal("BlockQuote", FirstStructureRole(reader, structRoot));
    }

    [Fact]
    public void UnmappedDeclaredRole_FailsLoudlyUnderPdfUa()
    {
        var authored = AuthorWithStyleRole("Body", "Callout", mapsTo: null);

        var error = Assert.Throws<System.InvalidOperationException>(() => RenderAuthored(authored));

        Assert.Contains("Callout", error.Message, System.StringComparison.Ordinal);
        Assert.Contains("RoleMap", error.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public void UnmappedDeclaredRole_IsToleratedWithoutTagging()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Add("Body").Role = "Callout";
        BuildTestSupport.AddText(document.Sections.Add(), "Body", BuildTestSupport.Latin).StyleName = "Body";

        Assert.NotEmpty(new DocumentRenderer().ToArray(document));
    }

    [Fact]
    public void MultiHopRoleChainTerminatingAtAStandardType_Renders()
    {
        var authored = AuthorWithStyleRole("Body", "Callout", mapsTo: null);
        authored.Renderer.RoleMap.Add("Callout", "Aside");
        authored.Renderer.RoleMap.Add("Aside", "Sidebar");
        authored.Renderer.RoleMap.Add("Sidebar", "Div");

        var reader = ReadAuthored(authored);
        var structRoot = StructTreeRoot(reader);

        Assert.Equal("Callout", FirstStructureRole(reader, structRoot));
        Assert.True(structRoot.ContainsKey("RoleMap"), "the chain is written to /RoleMap");
    }

    [Fact]
    public void DanglingRoleChain_FailsAtSaveUnderPdfUa()
    {
        var authored = AuthorWithStyleRole("Body", "Callout", mapsTo: null);
        authored.Renderer.RoleMap.Add("Callout", "Aside");

        var error = Assert.Throws<System.InvalidOperationException>(() => RenderAuthored(authored));

        Assert.Contains("Aside", error.Message, System.StringComparison.Ordinal);
        Assert.Contains("neither a standard type nor itself role mapped", error.Message, System.StringComparison.Ordinal);
    }

    [Fact]
    public void DeclaredRole_ProducesByteIdenticalOutputAcrossBuilds()
    {
        var first = RenderAuthored(AuthorTagged(declareRole: true));
        var second = RenderAuthored(AuthorTagged(declareRole: true));
        Assert.Equal(first, second);
    }
}
