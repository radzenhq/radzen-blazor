#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Radzen.Documents.Pdf;
using Xunit;

namespace Radzen.Blazor.Pdf.Tests;

// The behaviour that the retired re-serialize-and-compare snapshot used to guarantee: an
// untouched loaded page keeps its original bytes, an edited one re-encodes, and the two
// savers never disagree about which of those is the case.
public class LoadedContentChangeDetectionTests
{
    // Authored then round-tripped, so the loaded page carries a real base-14 font resource
    // with widths: ReplaceText and Redact need the source font's metrics.
    private static Document Loaded()
    {
        var document = new Document();
        var page = document.Pages.Add();
        page.Content.Add(new TextContent("Alpha", 72, 700) { Font = new Font { Size = 12 } });
        page.Content.Add(new TextContent("Beta", 72, 680) { Font = new Font { Size = 12 } });
        return InterpreterTestSupport.Load(document.ToArray());
    }

    private static byte[] SavedContent(Document document)
        => InterpreterTestSupport.PageContentBytes(document.ToArray(), 0);

    private static bool Contains(byte[] haystack, string needle)
        => Encoding.ASCII.GetString(haystack).Contains(needle, StringComparison.Ordinal);

    // --- Prefix identity ---

    [Fact]
    public void RemovingAnOriginalElement_DropsItFromTheSavedContent()
    {
        var document = Loaded();
        document.Pages[0].Content.RemoveAt(0);

        var content = SavedContent(document);

        Assert.False(Contains(content, "(Alpha)"), "the removed element must not come back from the raw bytes");
        Assert.True(Contains(content, "(Beta)"));
    }

    [Fact]
    public void InsertingBeforeTheOriginalPrefix_KeepsBothAndReorders()
    {
        var document = Loaded();
        document.Pages[0].Content.Insert(0, new TextContent("Gamma", 10, 660));

        var content = SavedContent(document);

        Assert.True(Contains(content, "(Gamma)"));
        Assert.True(Contains(content, "(Alpha)"));
        Assert.True(Contains(content, "(Beta)"));
    }

    // The remove-one-add-one hazard: the element count is unchanged, so a saver that checked
    // only the count would take the raw-bytes path and resurrect the removed element.
    [Fact]
    public void RemovingOneOriginalAndAddingOne_DoesNotResurrectTheRemovedElement()
    {
        var document = Loaded();
        var page = document.Pages[0];
        page.Content.RemoveAt(0);
        page.Content.Add(new TextContent("Gamma", 10, 660));

        Assert.Equal(2, page.Content.Count);

        var content = SavedContent(document);

        Assert.False(Contains(content, "(Alpha)"), "a count-only intactness check would resurrect the removed element");
        Assert.True(Contains(content, "(Beta)"));
        Assert.True(Contains(content, "(Gamma)"));
    }

    // --- Round-trip ---

    [Fact]
    public void UntouchedLoadedPage_StaysByteIdentical()
    {
        var original = Loaded();
        var before = SavedContent(original);

        var reloaded = InterpreterTestSupport.Load(original.ToArray());
        // Materializing alone must not count as an edit.
        Assert.Equal(2, reloaded.Pages[0].Content.Count);

        Assert.Equal(before, SavedContent(reloaded));
    }

    [Fact]
    public void EditedLoadedPage_RoundTripsThroughAReload()
    {
        var document = Loaded();
        ((TextContent)document.Pages[0].Content[0]).Text = "Delta";

        var reloaded = InterpreterTestSupport.Load(document.ToArray());
        var content = SavedContent(reloaded);

        Assert.True(Contains(content, "(Delta)"));
        Assert.False(Contains(content, "(Alpha)"));
        Assert.True(Contains(content, "(Beta)"));
    }

    // --- C7: flags are sticky for the object's lifetime, never cleared by a save ---

    [Fact]
    public void SavingTwice_WritesTheEditIntoBothOutputs()
    {
        var document = Loaded();
        ((TextContent)document.Pages[0].Content[0]).Text = "Delta";

        var first = SavedContent(document);
        var second = SavedContent(document);

        Assert.True(Contains(first, "(Delta)"));
        Assert.True(Contains(second, "(Delta)"), "a save that cleared the flags would write the original bytes the second time");
        Assert.False(Contains(second, "(Alpha)"));
        Assert.Equal(first, second);
    }

    [Fact]
    public void ElementStaysModifiedAfterASave()
    {
        var document = Loaded();
        var run = (TextContent)document.Pages[0].Content[0];
        run.Text = "Delta";

        document.ToArray();

        Assert.True(run.IsModified, "save must not clear an element's modified flag");
    }

    // --- Save-path agreement ---

    public static TheoryData<string> EditPaths => new()
    {
        "SetContent",
        "ReplaceText",
        "Redact",
        "ContentAdd",
        "ContentRemove",
        "NestedFontSize",
        "QueuedAppend",
    };

    private static void Apply(Document document, string edit)
    {
        var page = document.Pages[0];
        switch (edit)
        {
            case "SetContent":
                page.SetContent(InterpreterTestSupport.Ascii("BT /F0 12 Tf 72 700 Td (Delta) Tj ET\n"));
                break;
            case "ReplaceText":
                page.ReplaceText("Alpha", "Omega");
                break;
            case "Redact":
                page.Redact([PdfRect.FromSize(0, 690, 200, 30)]);
                break;
            case "ContentAdd":
                page.Content.Add(new TextContent("Gamma", 10, 660));
                break;
            case "ContentRemove":
                page.Content.RemoveAt(0);
                break;
            case "NestedFontSize":
                ((TextContent)page.Content[0]).Font.Size = 22;
                break;
            // Queues an overlay without materializing, so the raw bytes stay intact and only
            // pendingAppends carries the edit.
            case "QueuedAppend":
                document.AddWatermark("Draft");
                break;
            default:
                throw new InvalidOperationException($"Unknown edit path '{edit}'.");
        }
    }

    // The full saver and the incremental saver must never disagree about whether a page was
    // edited: one of them silently writing the pre-edit bytes is the loss this design prevents.
    [Theory]
    [MemberData(nameof(EditPaths))]
    public void BothSaversAgreeThatAnEditExists(string edit)
    {
        var unedited = SavedContent(Loaded());

        var full = Loaded();
        Apply(full, edit);
        var fullSaverSeesEdit = !SavedContent(full).AsSpan().SequenceEqual(unedited);

        var incremental = Loaded();
        Apply(incremental, edit);
        var incrementalSaverSeesEdit = false;
        try
        {
            using var stream = new MemoryStream();
            incremental.SaveIncremental(stream);
        }
        catch (NotSupportedException)
        {
            // The incremental saver refuses a content edit rather than appending one, so its
            // refusal is exactly its answer to "was this page edited?".
            incrementalSaverSeesEdit = true;
        }

        Assert.True(fullSaverSeesEdit, $"{edit}: the full saver did not see the edit");
        Assert.Equal(fullSaverSeesEdit, incrementalSaverSeesEdit);
    }

    // SetContent replaces the bytes and resets materialization, so every element-level signal
    // reads as untouched afterwards. Nothing but the page's own replaced bit can report it.
    [Fact]
    public void SetContentOnALoadedPage_WritesTheReplacementBytes()
    {
        var document = Loaded();
        document.Pages[0].SetContent(InterpreterTestSupport.Ascii("BT /F0 12 Tf 72 700 Td (Delta) Tj ET\n"));

        var content = SavedContent(document);

        Assert.True(Contains(content, "(Delta)"));
        Assert.False(Contains(content, "(Alpha)"), "the replaced bytes must not survive as the saved content");
        Assert.False(Contains(content, "(Beta)"));
    }

    // Reading Content materializes but edits nothing, so a replacement pass that finds no match
    // must leave the page reported as unedited rather than as wholesale replaced.
    [Fact]
    public void MaterializingThenReplacingNothing_LeavesThePageUnedited()
    {
        var unedited = SavedContent(Loaded());

        var document = Loaded();
        Assert.Equal(2, document.Pages[0].Content.Count);
        Assert.Equal(0, document.Pages[0].ReplaceText("Nowhere", "Somewhere"));

        Assert.Equal(unedited, SavedContent(document));

        using var stream = new MemoryStream();
        Assert.Throws<InvalidOperationException>(() => document.SaveIncremental(stream));
    }

    [Fact]
    public void BothSaversAgreeThatNoEditExistsOnAnUntouchedPage()
    {
        var unedited = SavedContent(Loaded());

        var full = Loaded();
        Assert.Equal(2, full.Pages[0].Content.Count);
        Assert.Equal(unedited, SavedContent(full));

        var incremental = Loaded();
        Assert.Equal(2, incremental.Pages[0].Content.Count);

        using var stream = new MemoryStream();
        // Nothing changed at all, so the incremental saver reports "no change" rather than
        // refusing an unsupported content edit.
        Assert.Throws<InvalidOperationException>(() => incremental.SaveIncremental(stream));
    }
}
