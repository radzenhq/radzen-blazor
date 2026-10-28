namespace Radzen.Blazor
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenTabs.Close" /> event that is raised when the user closes a tab.
    /// </summary>
    public class TabsCloseEventArgs
    {
        /// <summary>
        /// The index of the tab being closed.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// The tab being closed.
        /// </summary>
        public RadzenTabsItem Tab { get; set; } = default!;
    }
}
