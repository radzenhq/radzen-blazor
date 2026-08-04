#nullable enable
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;
using static Radzen.Blazor.Pdf.Tests.RawPdfAssertions;

namespace Radzen.Blazor.Pdf.Tests;

public class StructureRoleMapTests
{
    private static string EmitAuthored((Document Document, DocumentRenderer Renderer) authored)
        => Emit(authored.Renderer.Render(authored.Document));

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

    private static string StructTreeRoot(string emission)
        => Line(emission, "/Type /StructTreeRoot");

    private static string FirstKid(string subject, string body)
        => Shaped(subject, @"/K \[?(\d+) 0 R", body).Groups[1].Value;

    private static string FirstStructureElement(string emission)
    {
        var document = IndirectObject(emission, FirstKid("StructTreeRoot", StructTreeRoot(emission)));
        var sect = IndirectObject(emission, FirstKid("Document element", document));
        return IndirectObject(emission, FirstKid("Sect element", sect));
    }

    private static void FirstStructureRoleIs(string emission, string type)
        => Carries(
            "first structure element",
            $"/Type /StructElem /S /{type} /P ",
            FirstStructureElement(emission));

    private static void RoleMaps(string root, string role, string type)
        => Shaped("StructTreeRoot /RoleMap", $@"/RoleMap <<[^>]* /{role} /{type}\b", root);

    [Fact]
    public void DeclaredRole_EmitsRoleMapAndTagsElementWithTheRole()
    {
        var emission = EmitAuthored(AuthorTagged(declareRole: true));

        RoleMaps(StructTreeRoot(emission), "Callout", "P");
        FirstStructureRoleIs(emission, "Callout");
    }

    [Fact]
    public void NoDeclaredRoles_OmitsRoleMapAndKeepsStandardTag()
    {
        var emission = EmitAuthored(AuthorTagged(declareRole: false));

        Lacks("StructTreeRoot", "/RoleMap", StructTreeRoot(emission));
        FirstStructureRoleIs(emission, "P");
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

        var emission = Emit(builderRenderer.Render(document));

        Lacks("StructTreeRoot", "/RoleMap", StructTreeRoot(emission));
        FirstStructureRoleIs(emission, "P");
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
        FirstStructureRoleIs(EmitAuthored(AuthorWithStyleRole("Body", "Callout", "P")), "Callout");
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

        FirstStructureRoleIs(Emit(renderer.Render(document)), "Callout");
    }

    [Fact]
    public void HeadingLevel_WinsOverADeclaredRole()
    {
        var emission = EmitAuthored(AuthorWithStyleRole("Heading2", "Callout", "P"));

        FirstStructureRoleIs(emission, "H2");
        Carries("StructTreeRoot", "/RoleMap <<", StructTreeRoot(emission));
    }

    [Fact]
    public void HeadingLevelWithAnUnmappedRole_RendersUnderPdfUa()
    {
        var authored = AuthorWithStyleRole("Subtitle", "Sub", mapsTo: null);
        authored.Document.Styles["Subtitle"].HeadingLevel = 2;

        var emission = EmitAuthored(authored);

        FirstStructureRoleIs(emission, "H2");
        Lacks("StructTreeRoot", "/RoleMap", StructTreeRoot(emission));
    }

    [Fact]
    public void StandardStructureTypeAsARole_NeedsNoRoleMapEntry()
    {
        var emission = EmitAuthored(AuthorWithStyleRole("Quote", "BlockQuote", mapsTo: null));

        Lacks("StructTreeRoot", "/RoleMap", StructTreeRoot(emission));
        FirstStructureRoleIs(emission, "BlockQuote");
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

        var emission = EmitAuthored(authored);

        FirstStructureRoleIs(emission, "Callout");
        Carries("StructTreeRoot", "/RoleMap <<", StructTreeRoot(emission));
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
    public void RoleAddedToTheDocumentAfterRender_ReachesTheSavedRoleMap()
    {
        var (document, renderer) = AuthorTagged(declareRole: true);
        var rendered = renderer.Render(document);

        rendered.RoleMap.Add("Aside", "Div");

        var root = StructTreeRoot(Emit(rendered));

        RoleMaps(root, "Aside", "Div");
        Shaped("StructTreeRoot /RoleMap", @"/RoleMap <<[^>]* /Callout\b", root);
    }

    [Fact]
    public void DanglingRoleAddedToTheDocumentAfterRender_FailsAtSave()
    {
        var (document, renderer) = AuthorTagged(declareRole: true);
        var rendered = renderer.Render(document);

        rendered.RoleMap.Add("Aside", "Sidebar");

        var error = Assert.Throws<System.InvalidOperationException>(rendered.ToArray);

        Assert.Contains("Sidebar", error.Message, System.StringComparison.Ordinal);
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
