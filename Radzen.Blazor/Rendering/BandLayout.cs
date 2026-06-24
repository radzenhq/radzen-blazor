namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Helpers for laying out grouped category bands (columns and bars) within a single category.
    /// </summary>
    internal static class BandLayout
    {
        /// <summary>
        /// Computes the per-series item size (optionally capped to <paramref name="max" />) and the total
        /// group size used to center the group on its category. When the size is not capped the group
        /// equals the band, so the result is identical to dividing the band evenly; when capped the group
        /// is narrower and stays centered.
        /// </summary>
        /// <param name="band">The total space available for the category.</param>
        /// <param name="seriesCount">The number of series sharing the band.</param>
        /// <param name="margin">The margin between items within the band.</param>
        /// <param name="max">The optional maximum item size in pixels.</param>
        public static (double Size, double Group) Resolve(double band, int seriesCount, double margin, double? max)
        {
            if (seriesCount < 1)
            {
                seriesCount = 1;
            }

            var size = band / seriesCount - margin + margin / seriesCount;

            if (max is double cap && size > cap)
            {
                size = cap;
            }

            var group = seriesCount * size + (seriesCount - 1) * margin;

            return (size, group);
        }
    }
}
