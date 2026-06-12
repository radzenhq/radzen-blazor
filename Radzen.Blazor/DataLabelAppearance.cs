namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies the visual treatment of data labels. Used by <see cref="RadzenSeriesDataLabels" />.
    /// </summary>
    public enum DataLabelAppearance
    {
        /// <summary>
        /// The label renders on a rounded background chip - readable over series fills and gradients.
        /// </summary>
        Chip,
        /// <summary>
        /// The label text renders with an outline halo in the chart background color - lighter than a chip
        /// while remaining readable over series fills.
        /// </summary>
        Outline,
        /// <summary>
        /// The label renders as plain text.
        /// </summary>
        Plain
    }
}
