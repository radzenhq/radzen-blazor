#nullable enable
using Radzen.Documents.Pdf;
using Radzen.Documents.Pdf.Objects;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// A declared RoleMap lets a paragraph carry a non-standard structure role that maps to a
// standard ISO 32000-1 type, so tagged output (PDF/UA, PDF/A Level A) stays conformant.
// With no declared roles the /StructTreeRoot carries no /RoleMap and output is unchanged.
public class StructureRoleMapTests
{
    private static DocumentBuilder AuthorTagged(bool declareRole)
    {
        var builder = new DocumentBuilder { PdfUA = true, Language = "en" };
        builder.Info.Title = "Doc";
        BuildTestSupport.RegisterLatin(builder);

        if (declareRole)
        {
            builder.RoleMap.Add("Callout", "P");
        }

        var section = builder.Sections.Add();
        var note = BuildTestSupport.AddText(section, "See the note", BuildTestSupport.Latin);
        note.StyleName = "Callout";

        return builder;
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
        var reader = BuildTestSupport.Read(AuthorTagged(declareRole: true));
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
        var reader = BuildTestSupport.Read(AuthorTagged(declareRole: false));
        var structRoot = StructTreeRoot(reader);

        Assert.False(structRoot.ContainsKey("RoleMap"), "StructTreeRoot has no /RoleMap when no roles declared");
        Assert.Equal("P", FirstStructureRole(reader, structRoot));
    }

    [Fact]
    public void UndeclaredStyleName_StaysStandardParagraph()
    {
        var builder = new DocumentBuilder { PdfUA = true, Language = "en" };
        builder.Info.Title = "Doc";
        BuildTestSupport.RegisterLatin(builder);
        var section = builder.Sections.Add();
        BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin).StyleName = "Unknown";

        var reader = BuildTestSupport.Read(builder);
        var structRoot = StructTreeRoot(reader);

        Assert.False(structRoot.ContainsKey("RoleMap"), "no /RoleMap for an undeclared style");
        Assert.Equal("P", FirstStructureRole(reader, structRoot));
    }

    [Fact]
    public void DeclaredRole_ProducesByteIdenticalOutputAcrossBuilds()
    {
        var first = AuthorTagged(declareRole: true).ToArray();
        var second = AuthorTagged(declareRole: true).ToArray();
        Assert.Equal(first, second);
    }
}
