namespace Radzen.Blazor.Rendering
{
    public class DraggableEventArgs
    {
        public double ClientX { get; set; }
        public double ClientY { get; set; }
        public Rect Rect { get; set; }
    }
}