namespace Radzen.Blazor
{
    /// <summary>
    /// A service to control a RadzenBusyLoading component
    /// </summary>
    public class BusyLoadingService
    {
        internal delegate void StateChanged(bool val);
        internal event StateChanged BusyChanged;
        internal event StateChanged LoadedChanged;

        public void Busy(bool val)
        {
            BusyChanged.Invoke(val);
        }

        public void Loaded(bool val)
        {
            BusyChanged.Invoke(val);
        }
    }
}