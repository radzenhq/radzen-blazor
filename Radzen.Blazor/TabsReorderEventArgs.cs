namespace Radzen.Blazor
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenTabs.Reorder" /> event that is raised when tabs are reordered via drag and drop.
    /// </summary>
    public class TabsReorderEventArgs
    {
        /// <summary>
        /// The original index of the tab before it was moved.
        /// </summary>
        public int OldIndex { get; set; }

        /// <summary>
        /// The new index of the tab after it was moved.
        /// </summary>
        public int NewIndex { get; set; }
    }
}
