using System;
using System.Threading.Tasks;

namespace Radzen;

/// <summary>
/// Utility class for debouncing and throttling function calls.
/// </summary>
internal class Debouncer : IDisposable
{
    private System.Timers.Timer? timer;
    private System.Timers.ElapsedEventHandler? timerElapsedHandler;
    private DateTime timerStarted { get; set; } = DateTime.UtcNow.AddYears(-1);

    /// <summary>
    /// Debounces the specified action.
    /// </summary>
    /// <param name="interval">The debounce interval in milliseconds.</param>
    /// <param name="action">The action to debounce.</param>
    public void Debounce(int interval, Func<Task> action)
    {
        ClearTimer();
        timer = new System.Timers.Timer() { Interval = interval, Enabled = false, AutoReset = false };
        timerElapsedHandler = (s, e) =>
        {
            if (timer == null)
            {
                return;
            }

            timer?.Stop();
            timer = null;

            try
            {
                Task.Run(action);
            }
            catch (TaskCanceledException)
            {
                //
            }
        };
        timer.Elapsed += timerElapsedHandler;

        timer.Start();
    }

    /// <summary>
    /// Throttles the specified action.
    /// </summary>
    /// <param name="interval">The throttle interval in milliseconds.</param>
    /// <param name="action">The action to throttle.</param>
    public void Throttle(int interval, Func<Task> action)
    {
        ClearTimer();
        var curTime = DateTime.UtcNow;

        if (curTime.Subtract(timerStarted).TotalMilliseconds < interval)
        {
            interval -= (int)curTime.Subtract(timerStarted).TotalMilliseconds;
        }

        timer = new System.Timers.Timer() { Interval = interval, Enabled = false, AutoReset = false };
        timerElapsedHandler = (s, e) =>
        {
            if (timer == null)
            {
                return;
            }

            timer?.Stop();
            timer = null;

            try
            {
                Task.Run(action);
            }
            catch (TaskCanceledException)
            {
                //
            }
        };
        timer.Elapsed += timerElapsedHandler;

        timer.Start();
        timerStarted = curTime;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        ClearTimer();
    }

    private void ClearTimer()
    {
        if (timer == null)
        {
            return;
        }

        if (timerElapsedHandler != null)
        {
            timer.Elapsed -= timerElapsedHandler;
            timerElapsedHandler = null;
        }

        timer.Stop();
        timer.Dispose();
        timer = null;
    }
}
