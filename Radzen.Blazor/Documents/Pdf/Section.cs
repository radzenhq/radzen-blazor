namespace Radzen.Documents.Pdf;


/// <summary>
/// A page section with its own page setup, header, footer and body content.
/// </summary>
public class Section
{
    /// <summary>Gets or sets the page size. Defaults to <see cref="PageSizes.A4"/>.</summary>
    public PageSize PageSize { get; set; } = PageSizes.A4;

    /// <summary>Gets or sets the page orientation. Defaults to <see cref="PageOrientation.Portrait"/>.</summary>
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;

    /// <summary>Gets or sets the base flow direction. Defaults to <see cref="FlowDirection.LeftToRight"/>.</summary>
    public FlowDirection Direction { get; set; } = FlowDirection.LeftToRight;

    /// <summary>Gets or sets the writing mode. Defaults to <see cref="WritingMode.HorizontalTopToBottom"/>.</summary>
    public WritingMode WritingMode { get; set; } = WritingMode.HorizontalTopToBottom;

    /// <summary>Gets the page margins. Defaults to 2.5 cm on every edge.</summary>
    public Margins Margins { get; } = new()
    {
        Top = Unit.FromCentimeter(2.5),
        Right = Unit.FromCentimeter(2.5),
        Bottom = Unit.FromCentimeter(2.5),
        Left = Unit.FromCentimeter(2.5),
    };

    /// <summary>
    /// Gets or sets a single margin value applied to all four edges. Reading returns the top margin.
    /// </summary>
    public Unit Margin
    {
        get => Margins.Top;
        set
        {
            Margins.Top = value;
            Margins.Right = value;
            Margins.Bottom = value;
            Margins.Left = value;
        }
    }

    /// <summary>
    /// Gets or sets the distance between the top page edge and the header band.
    /// Sections created through <see cref="DocumentBuilder"/> default to 1.25 cm.
    /// </summary>
    public Unit HeaderDistance { get; set; }

    /// <summary>
    /// Gets or sets the distance between the bottom page edge and the footer band.
    /// Sections created through <see cref="DocumentBuilder"/> default to 1.25 cm.
    /// </summary>
    public Unit FooterDistance { get; set; }

    /// <summary>Gets the page header.</summary>
    public HeaderFooter Header { get; } = new();

    /// <summary>Gets the page footer.</summary>
    public HeaderFooter Footer { get; } = new();

    /// <summary>Gets the body content blocks.</summary>
    public BlockCollection Blocks { get; } = [];
}
