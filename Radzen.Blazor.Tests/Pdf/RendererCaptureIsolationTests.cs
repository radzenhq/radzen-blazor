#nullable enable
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;
using Radzen.Documents;

namespace Radzen.Blazor.Pdf.Tests;

public class RendererCaptureIsolationTests
{
    private sealed class MutableEncryptionMaterial(byte seed) : IEncryptionMaterial
    {
        public byte Seed { get; set; } = seed;

        public byte[] GetBytes(int index, int count)
            => new SeededEncryptionMaterial([Seed]).GetBytes(index, count);
    }

    private static Document Authored()
    {
        var document = new Document();
        BuildTestSupport.RegisterLatin(document);
        BuildTestSupport.AddText(document.Sections.Add(), "Body", BuildTestSupport.Latin);
        return document;
    }

    private static string Save(PortableDocument document)
        => Encoding.Latin1.GetString(document.ToArray());

    [Fact]
    public void ViewerPreferencesMutatedAfterRender_DoesNotReachTheRenderedDocument()
    {
        var renderer = new DocumentRenderer { ViewerPreferences = new ViewerPreferences { HideToolbar = true } };

        var document = renderer.Render(Authored());
        renderer.ViewerPreferences!.HideMenubar = true;
        renderer.ViewerPreferences.PageMode = PdfPageMode.UseThumbs;

        var saved = Save(document);
        Assert.Contains("/HideToolbar true", saved, System.StringComparison.Ordinal);
        Assert.DoesNotContain("/HideMenubar", saved, System.StringComparison.Ordinal);
        Assert.DoesNotContain("/UseThumbs", saved, System.StringComparison.Ordinal);
    }

    [Fact]
    public void RoleMapMutatedAfterRender_DoesNotReachTheRenderedDocument()
    {
        var document = new Document { Language = "en" };
        document.Info.Title = "Doc";
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Add("Lead");
        BuildTestSupport.AddText(document.Sections.Add(), "See the note", BuildTestSupport.Latin).StyleName = "Lead";

        var renderer = new DocumentRenderer { Accessibility = PdfUaConformance.PdfUa1 };
        renderer.RoleMap.Add("Lead", "P");

        var pdf = renderer.Render(document);
        renderer.RoleMap.Add("Caption", "P");

        var saved = Save(pdf);
        Assert.Contains("/Lead", saved, System.StringComparison.Ordinal);
        Assert.DoesNotContain("/Caption", saved, System.StringComparison.Ordinal);
    }

    [Fact]
    public void OutlineItemMutatedAfterRender_DoesNotReachTheRenderedDocument()
    {
        var renderer = new DocumentRenderer();
        var item = new OutlineItem("Chapter", OutlineTarget.ToPage(0));
        renderer.Outline.Add(item);

        var document = renderer.Render(Authored());
        item.Title = "Renamed";
        item.Children.Add(new OutlineItem("Added", OutlineTarget.ToPage(0)));

        var saved = Save(document);
        Assert.DoesNotContain("Renamed", saved, System.StringComparison.Ordinal);
        Assert.DoesNotContain("Added", saved, System.StringComparison.Ordinal);
    }

    [Fact]
    public void TwoRenders_DoNotShareViewerPreferencesOrOutlineInstances()
    {
        var renderer = new DocumentRenderer { ViewerPreferences = new ViewerPreferences { HideToolbar = true } };
        renderer.Outline.Add(new OutlineItem("Chapter", OutlineTarget.ToPage(0)));

        var first = renderer.Render(Authored());
        var second = renderer.Render(Authored());

        Assert.NotSame(first.ViewerPreferences, second.ViewerPreferences);
        Assert.NotSame(first.Outline[0], second.Outline[0]);
    }

    [Fact]
    public void ProducerOnRenderer_IsWrittenToTheInfoDictionary()
    {
        var renderer = new DocumentRenderer { Producer = "Acme Publisher 3.0" };

        Assert.Contains("Acme Publisher 3.0", Encoding.Latin1.GetString(renderer.ToArray(Authored())), System.StringComparison.Ordinal);
    }

    public static TheoryData<bool> Configurations => new(true, false);

    private static Document Authored(string style)
    {
        var document = new Document { Language = "en" };
        document.Info.Title = "Enforcement";
        document.Info.Author = "Author";
        BuildTestSupport.RegisterLatin(document);
        document.Styles.Add(style);
        var section = document.Sections.Add();
        BuildTestSupport.AddText(section, "Body", BuildTestSupport.Latin).StyleName = style;
        BuildTestSupport.AddText(section, "More", BuildTestSupport.Latin);
        return document;
    }

    private static DocumentRenderer Everything(bool tagged)
    {
        var renderer = new DocumentRenderer
        {
            Producer = "Enforcement Producer 1.0",
            Accessibility = tagged ? PdfUaConformance.PdfUa1 : PdfUaConformance.None,
            CompressOutput = true,
            IncludeDocumentId = true,
            ViewerPreferences = new ViewerPreferences
            {
                HideToolbar = true,
                PageLayout = PdfPageLayout.TwoColumnLeft,
                PageMode = PdfPageMode.UseOutlines,
            },
        };

        renderer.RoleMap.Add("Lead", "P");

        var chapter = new OutlineItem("Chapter", OutlineTarget.ToPage(0)) { Bold = true };
        chapter.Children.Add(new OutlineItem("Nested", OutlineTarget.ToPage(0)));
        renderer.Outline.Add(chapter);

        renderer.Attachments.Add("data.xml", Encoding.ASCII.GetBytes("<invoice/>"), AttachmentRelationship.Data, "text/xml")
            .Description = "Machine readable";

        renderer.PageLabels.Add(new PageLabel(0) { Style = PageLabelStyle.UppercaseRoman, Prefix = "Front", Start = 3 });

        if (tagged)
        {
            return renderer;
        }

        renderer.Encryption = new EncryptionOptions
        {
            UserPassword = "initial-user",
            OwnerPassword = "initial-owner",
            Algorithm = EncryptionAlgorithm.Aes128,
            Material = new MutableEncryptionMaterial(3),
            AllowContentCopy = false,
        };

        renderer.FormFields.Add(new TextFieldDefinition("given")
        {
            Value = "Ada",
            X = 40,
            Y = 40,
            Width = 120,
            Height = 20,
            MaxLength = 32,
        });

        renderer.FormFields.Add(new CheckBoxFieldDefinition("agree") { Checked = true, X = 40, Y = 70, Width = 12, Height = 12 });

        var radio = new RadioGroupFieldDefinition("choice") { SelectedValue = "one" };
        radio.Options.Add(new RadioOptionDefinition("one") { X = 40, Y = 100, Width = 12, Height = 12 });
        radio.Options.Add(new RadioOptionDefinition("two") { X = 60, Y = 100, Width = 12, Height = 12 });
        renderer.FormFields.Add(radio);

        var choice = new ChoiceFieldDefinition("city") { Value = "Sofia", ComboBox = true, X = 40, Y = 130, Width = 120, Height = 20 };
        choice.Options.Add("Sofia");
        choice.Options.Add("Varna");
        renderer.FormFields.Add(choice);

        return renderer;
    }

    private static void MutateEverything(DocumentRenderer renderer, bool tagged)
    {
        renderer.Producer = "Mutated Producer 9.9";
        renderer.CompressOutput = false;
        renderer.IncludeDocumentId = false;

        if (tagged)
        {
            renderer.Accessibility = PdfUaConformance.None;
            renderer.Conformance = PdfAConformance.PdfA3B;
        }
        else
        {
            renderer.Encryption!.UserPassword = "mutated-user";
            renderer.Encryption.OwnerPassword = "mutated-owner";
            renderer.Encryption.Algorithm = EncryptionAlgorithm.Aes256;
            renderer.Encryption.EncryptMetadata = false;
            renderer.Encryption.AllowPrinting = false;
            renderer.Encryption.AllowHighResPrinting = false;
            renderer.Encryption.AllowModification = false;
            renderer.Encryption.AllowContentCopy = true;
            renderer.Encryption.AllowAnnotation = false;
            renderer.Encryption.AllowFormFill = false;
            renderer.Encryption.AllowAssembly = false;
            ((MutableEncryptionMaterial)renderer.Encryption.Material!).Seed = 9;
        }

        renderer.ViewerPreferences!.HideToolbar = false;
        renderer.ViewerPreferences.HideMenubar = true;
        renderer.ViewerPreferences.PageLayout = PdfPageLayout.SinglePage;
        renderer.ViewerPreferences.PageMode = PdfPageMode.UseThumbs;
        renderer.ViewerPreferences.FitWindow = true;
        renderer.ViewerPreferences.CenterWindow = true;
        renderer.ViewerPreferences.DisplayDocTitle = true;
        renderer.ViewerPreferences.Direction = PdfReadingDirection.RightToLeft;

        renderer.RoleMap.Add("Caption", "Span");

        renderer.Outline[0].Title = "MutatedChapter";
        renderer.Outline[0].Bold = false;
        renderer.Outline[0].Italic = true;
        renderer.Outline[0].Collapsed = true;
        renderer.Outline[0].Children[0].Title = "MutatedNested";
        renderer.Outline[0].Children.Add(new OutlineItem("MutatedExtra", OutlineTarget.ToPage(0)));
        renderer.Outline.Add(new OutlineItem("MutatedRoot", OutlineTarget.ToPage(0)));

        renderer.Attachments[0].Description = "MutatedDescription";
        renderer.Attachments[0].ModificationDate = new System.DateTimeOffset(2020, 5, 6, 7, 8, 9, System.TimeSpan.Zero);
        renderer.Attachments.Add("extra.txt", Encoding.ASCII.GetBytes("mutated"), AttachmentRelationship.Supplement, "text/plain");

        renderer.PageLabels[0].Style = PageLabelStyle.LowercaseLetters;
        renderer.PageLabels[0].Prefix = "MutatedPrefix";
        renderer.PageLabels[0].Start = 77;

        if (tagged)
        {
            return;
        }

        var text = (TextFieldDefinition)renderer.FormFields[0];
        text.Value = "MutatedValue";
        text.X = 200;
        text.MaxLength = 4;
        text.Font.Size = 30;
        text.Multiline = true;

        ((CheckBoxFieldDefinition)renderer.FormFields[1]).Checked = false;

        var radio = (RadioGroupFieldDefinition)renderer.FormFields[2];
        radio.SelectedValue = "two";
        radio.Options[0].X = 300;
        radio.Options.Add(new RadioOptionDefinition("three") { X = 80, Y = 100, Width = 12, Height = 12 });

        var choice = (ChoiceFieldDefinition)renderer.FormFields[3];
        choice.Value = "Varna";
        choice.ComboBox = false;
        choice.Options.Add("MutatedOption");
        choice.Font.Size = 30;

        renderer.FormFields.Add(new CheckBoxFieldDefinition("mutated") { X = 40, Y = 200, Width = 12, Height = 12 });
    }

    private static void MutateModel(Document model)
    {
        model.Info.Title = "Mutated model title";
        model.Info.Author = "Mutated model author";
        model.Language = "fr";
        model.Fonts.EnableKerning = true;
        model.Fonts.AllowRestrictedEmbedding = true;
        model.Fonts.AllowDegradedFonts = true;
        model.Fonts.AllowUnsupportedCharacters = true;
        model.Fonts.SetFallback(BuildTestSupport.Latin);
        model.Fonts.Register(
            BuildTestSupport.Latin,
            new System.IO.MemoryStream(PdfTestResources.ReadAllBytes("Fonts/LiberationSerif-Regular.ttf")));

        var paragraph = (Paragraph)model.Sections[0].Blocks[0];
        var first = (Run)paragraph.Inlines[0];
        first.Text = "Mutated model body";
        first.Font.Family = "Helvetica";
        model.Sections[0].Blocks.Add(new Paragraph { Text = "Added after render" });
        model.Sections.Add().Blocks.Add(new Paragraph { Text = "Added section after render" });
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void MutatingTheRendererAndRenderingAgain_LeavesTheFirstDocumentByteIdentical(bool tagged)
    {
        var renderer = Everything(tagged);
        var model = Authored("Lead");

        var first = renderer.Render(model);
        var immediate = first.ToArray();
        Assert.Null(first.Fonts);
        Assert.NotSame(renderer.RoleMap, first.RoleMap);
        if (!tagged)
        {
            Assert.NotSame(renderer.Encryption, first.Encryption);
        }

        MutateEverything(renderer, tagged);
        MutateModel(model);
        var second = renderer.Render(Authored("Lead")).ToArray();

        Assert.Equal(immediate, first.ToArray());
        Assert.NotEqual(immediate, second);
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void MutatingTheRenderedDocument_LeavesTheRendererAndItsNextRenderUnaffected(bool tagged)
    {
        var renderer = Everything(tagged);

        var first = renderer.Render(Authored("Lead"));
        var baseline = renderer.Render(Authored("Lead")).ToArray();

        first.Info.Producer = "Hijacked";
        first.ViewerPreferences!.HideToolbar = false;
        first.Outline[0].Title = "Hijacked";
        first.Outline.Add(new OutlineItem("HijackedRoot", OutlineTarget.ToPage(0)));
        first.PageLabels[0].Prefix = "Hijacked";
        first.PageLabels.Add(new PageLabel(1));
        first.Attachments[0].Description = "Hijacked";
        if (!tagged)
        {
            first.AcroForm!.FillField("given", "Hijacked");
            first.FormFields.Add(new CheckBoxFieldDefinition("hijacked") { X = 10, Y = 10, Width = 5, Height = 5 });
        }

        Assert.Equal("Enforcement Producer 1.0", renderer.Producer);
        Assert.True(renderer.ViewerPreferences!.HideToolbar);
        Assert.Equal(1, renderer.Outline.Count);
        Assert.Equal("Chapter", renderer.Outline[0].Title);
        Assert.Equal("Front", renderer.PageLabels[0].Prefix);
        Assert.Equal(1, renderer.PageLabels.Count);
        Assert.Equal("Machine readable", renderer.Attachments[0].Description);
        Assert.Equal(baseline, renderer.Render(Authored("Lead")).ToArray());
    }
}
