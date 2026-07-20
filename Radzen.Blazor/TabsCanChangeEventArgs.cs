namespace Radzen.Blazor
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenTabs.CanChange" /> event that is being raised.
    /// </summary>
    public class TabsCanChangeEventArgs
    {
        /// <summary>
        /// Index of the currently selected tab.
        /// </summary>
        public int SelectedIndex { get; set; }
        /// <summary>
        /// Index of the tab the user is switching to.
        /// </summary>
        public int NewIndex { get; set; }
        /// <summary>
        /// Has the tab change been prevented from occuring.
        /// </summary>
        public bool IsDefaultPrevented { get; private set; }
        /// <summary>
        /// Prevent the change of the tab.
        /// </summary>
        public void PreventDefault()
        {
            IsDefaultPrevented = true;
        }
    }
}
