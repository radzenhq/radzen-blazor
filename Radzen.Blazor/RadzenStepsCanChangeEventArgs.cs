namespace Radzen.Blazor
{
    /// <summary>
    /// Supplies information about a <see cref="RadzenSteps.CanChange" /> event that is being raised.
    /// </summary>
    public class RadzenStepsCanChangeEventArgs
    {
        /// <summary>
        /// Constructor to create args with selected index and new index.
        /// </summary>
        /// <param name="selectedIndex"></param>
        /// <param name="newIndex"></param>
        public RadzenStepsCanChangeEventArgs(int selectedIndex, int newIndex)
        {
            SelectedIndex = selectedIndex;
            NewIndex = newIndex;
        }

        /// <summary>
        /// Index of clicked step.
        /// </summary>
        public int SelectedIndex { get; private set; }
        /// <summary>
        /// Index of clicked step.
        /// </summary>
        public int NewIndex { get; private set; }
        /// <summary>
        /// Has step change action been prevented from occuring.
        /// </summary>
        public bool IsDefaultPrevented { get; private set; }
        /// <summary>
        /// Prevent the change of the step.
        /// </summary>
        public void PreventDefault()
        {
            IsDefaultPrevented = true;
        }
    }
}