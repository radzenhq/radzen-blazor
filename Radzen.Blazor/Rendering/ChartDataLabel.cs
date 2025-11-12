namespace  Radzen.Blazor.Rendering
{
    /// <summary>
    /// Represents a data label in RadzenChart.
    /// </summary>
   public class ChartDataLabel 
   {
        /// <summary>
        /// The position of the label.
        /// </summary>
        public Point Position { get; set; } = new Point();
        /// <summary>
        /// The text of the label.
        /// </summary>
        public string Text { get; set; } = string.Empty;
        /// <summary>
        /// The text anchor of the label.
        /// </summary>
        public string TextAnchor { get; set; } = string.Empty;
        /// <summary>
        /// Defines the fill color of the component.
        /// </summary>
        public string Fill { get; set; } = string.Empty;
    }
}