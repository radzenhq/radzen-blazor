using Radzen.Documents.Core;

namespace Radzen.Documents.Pdf;


/// <summary>
/// The page layout a viewer uses when the document is first opened
/// (ISO 32000-1 Table 28, catalog <c>/PageLayout</c>).
/// </summary>
public enum PdfPageLayout
{
    /// <summary>Display one page at a time.</summary>
    SinglePage,
    /// <summary>Display the pages in one continuous column.</summary>
    OneColumn,
    /// <summary>Display the pages in two continuous columns, odd-numbered pages on the left.</summary>
    TwoColumnLeft,
    /// <summary>Display the pages in two continuous columns, odd-numbered pages on the right.</summary>
    TwoColumnRight,
    /// <summary>Display the pages two at a time, odd-numbered pages on the left.</summary>
    TwoPageLeft,
    /// <summary>Display the pages two at a time, odd-numbered pages on the right.</summary>
    TwoPageRight,
}

/// <summary>
/// How a viewer displays the document when it is first opened
/// (ISO 32000-1 Table 28, catalog <c>/PageMode</c>).
/// </summary>
public enum PdfPageMode
{
    /// <summary>Neither the outline nor thumbnails are visible.</summary>
    UseNone,
    /// <summary>The outline (bookmark) panel is visible.</summary>
    UseOutlines,
    /// <summary>The thumbnail panel is visible.</summary>
    UseThumbs,
    /// <summary>Open in full-screen mode.</summary>
    FullScreen,
    /// <summary>The optional-content group panel is visible.</summary>
    UseOC,
    /// <summary>The attachments panel is visible.</summary>
    UseAttachments,
}

/// <summary>
/// The predominant reading order for text, used to position the first page when
/// displaying two pages side by side (<c>/ViewerPreferences /Direction</c>).
/// </summary>
public enum PdfReadingDirection
{
    /// <summary>Left to right.</summary>
    LeftToRight,
    /// <summary>Right to left.</summary>
    RightToLeft,
}

/// <summary>
/// The viewer preferences written to the document catalog: the initial
/// <see cref="PageLayout"/> and <see cref="PageMode"/>, and the
/// <c>/ViewerPreferences</c> dictionary flags (ISO 32000-1 sections 7.7.2 and
/// 12.2). Every option is opt-in; an unset option emits nothing.
/// </summary>
public sealed class ViewerPreferences
{
    private ChangeTracker tracker;
    private PdfPageLayout? pageLayout;
    private PdfPageMode? pageMode;
    private bool hideToolbar;
    private bool hideMenubar;
    private bool fitWindow;
    private bool centerWindow;
    private bool displayDocTitle;
    private PdfReadingDirection? direction;

    /// <summary>Gets or sets the initial page layout, written as catalog <c>/PageLayout</c>.</summary>
    public PdfPageLayout? PageLayout
    {
        get => pageLayout;
        set => tracker.Set(ref pageLayout, value);
    }

    /// <summary>Gets or sets the initial page mode, written as catalog <c>/PageMode</c>.</summary>
    public PdfPageMode? PageMode
    {
        get => pageMode;
        set => tracker.Set(ref pageMode, value);
    }

    /// <summary>Gets or sets whether to hide the viewer's tool bars (<c>/HideToolbar</c>). Defaults to <see langword="false"/>.</summary>
    public bool HideToolbar
    {
        get => hideToolbar;
        set => tracker.Set(ref hideToolbar, value);
    }

    /// <summary>Gets or sets whether to hide the viewer's menu bar (<c>/HideMenubar</c>). Defaults to <see langword="false"/>.</summary>
    public bool HideMenubar
    {
        get => hideMenubar;
        set => tracker.Set(ref hideMenubar, value);
    }

    /// <summary>Gets or sets whether to resize the document window to fit the first displayed page (<c>/FitWindow</c>). Defaults to <see langword="false"/>.</summary>
    public bool FitWindow
    {
        get => fitWindow;
        set => tracker.Set(ref fitWindow, value);
    }

    /// <summary>Gets or sets whether to center the document window on the screen (<c>/CenterWindow</c>). Defaults to <see langword="false"/>.</summary>
    public bool CenterWindow
    {
        get => centerWindow;
        set => tracker.Set(ref centerWindow, value);
    }

    /// <summary>Gets or sets whether the window title bar shows the document title rather than the file name (<c>/DisplayDocTitle</c>). Defaults to <see langword="false"/>.</summary>
    public bool DisplayDocTitle
    {
        get => displayDocTitle;
        set => tracker.Set(ref displayDocTitle, value);
    }

    /// <summary>Gets or sets the predominant reading direction (<c>/Direction</c>). When <see langword="null"/> no direction is written.</summary>
    public PdfReadingDirection? Direction
    {
        get => direction;
        set => tracker.Set(ref direction, value);
    }

    internal void OwnedBy(System.Action? changed) => tracker.OwnedBy(changed);
}
