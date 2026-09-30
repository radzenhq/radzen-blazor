namespace Radzen.Blazor
{
    /// <summary>
    /// Specifies how <see cref="RadzenCategoryAxis" /> fits category labels that do not fit the available space. Set via <see cref="RadzenCategoryAxis.LabelFit" />.
    /// </summary>
    public enum CategoryAxisLabelFit
    {
        /// <summary>
        /// Labels that fit are displayed unchanged. On a horizontal axis, when a label does not fit, labels are first skipped at the regular interval that
        /// rotated labels need. The displayed labels stay horizontal when they fit that interval; otherwise they are rotated by -45 degrees (or by
        /// <see cref="AxisBase.LabelAutoRotation" /> when set), skipped so the rotated ones do not overlap, and labels longer than a third of the chart height
        /// or than the room between their tick and the edge of the chart are shortened after their last whole word that fits, with an ellipsis.
        /// <see cref="AxisBase.LabelRotation" /> rotates the labels by its angle even when they fit, and they are skipped and shortened the same way.
        /// On a vertical axis (horizontal bar charts) the labels take at most a third of the chart width unless the axis sets
        /// <see cref="AxisBase.Width" />, and longer labels are shortened with an ellipsis. A shortened label displays its full text on hover.
        /// This is the default.
        /// </summary>
        Auto,
        /// <summary>
        /// Labels are displayed in full and are not skipped, even when they overlap; they are rotated only as <see cref="AxisBase.LabelRotation" /> and
        /// <see cref="AxisBase.LabelAutoRotation" /> specify.
        /// </summary>
        None
    }
}
