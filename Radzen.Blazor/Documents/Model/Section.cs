using Radzen.Documents.Core;

namespace Radzen.Documents;


/// <summary>
/// A page section with its own page setup, header, footer and body content.
/// </summary>
public sealed class Section
{
    private Unit headerDistance = Unit.FromCentimeter(1.25);
    private Unit footerDistance = Unit.FromCentimeter(1.25);

    internal object? Owner { get; set; }

    /// <summary>Gets or sets the page size. Defaults to <see cref="PageSizes.A4"/>.</summary>
    public PageSize PageSize { get; set; } = PageSizes.A4;

    /// <summary>Gets or sets the page orientation. Defaults to <see cref="PageOrientation.Portrait"/>.</summary>
    public PageOrientation Orientation { get; set; } = PageOrientation.Portrait;

    /// <summary>Gets or sets the base flow direction. Defaults to <see cref="FlowDirection.LeftToRight"/>.
    /// Other values are reserved and currently throw <see cref="System.NotSupportedException"/> during layout.</summary>
    public FlowDirection Direction { get; set; } = FlowDirection.LeftToRight;

    /// <summary>Gets or sets the writing mode. Defaults to <see cref="WritingMode.HorizontalTopToBottom"/>.
    /// Other values are reserved and currently throw <see cref="System.NotSupportedException"/> during layout.</summary>
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
    /// Gets or sets the distance between the top page edge and the header band.
    /// Defaults to 1.25 cm.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit HeaderDistance
    {
        get => headerDistance;
        set => headerDistance = AuthoredNumber.Absolute(value, "Section.HeaderDistance");
    }

    /// <summary>
    /// Gets or sets the distance between the bottom page edge and the footer band.
    /// Defaults to 1.25 cm.
    /// </summary>
    /// <exception cref="System.ArgumentOutOfRangeException">The value is relative.</exception>
    public Unit FooterDistance
    {
        get => footerDistance;
        set => footerDistance = AuthoredNumber.Absolute(value, "Section.FooterDistance");
    }

    /// <summary>Gets the page header.</summary>
    public HeaderFooter Header { get; } = new();

    /// <summary>Gets the page footer.</summary>
    public HeaderFooter Footer { get; } = new();

    /// <summary>Gets the body content blocks.</summary>
    public BlockCollection Blocks { get; } = [];

    /// <summary>
    /// Gets or sets the watermark stamped over every page of the section, or
    /// <see langword="null"/> for none.
    /// </summary>
    public Watermark? Watermark { get; set; }
}
