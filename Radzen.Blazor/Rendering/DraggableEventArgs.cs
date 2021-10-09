namespace Radzen.Blazor.Rendering
{
    /// <summary>
    /// Class DraggableEventArgs.
    /// </summary>
    public class DraggableEventArgs
    {
        /// <summary>
        /// Gets or sets the client x.
        /// </summary>
        /// <value>The client x.</value>
        public double ClientX { get; set; }
        /// <summary>
        /// Gets or sets the client y.
        /// </summary>
        /// <value>The client y.</value>
        public double ClientY { get; set; }
        /// <summary>
        /// Gets or sets the rect.
        /// </summary>
        /// <value>The rect.</value>
        public Rect Rect { get; set; }
    }
}