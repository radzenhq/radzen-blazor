using System;
using System.Diagnostics;
using System.Threading;
using Xunit;

namespace Radzen.Blazor.Tests
{
    static class GarbageCollection
    {
        public static void AssertCollected(WeakReference reference, string message)
        {
            var stopwatch = Stopwatch.StartNew();

            while (true)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();

                if (!reference.IsAlive || stopwatch.Elapsed > TimeSpan.FromSeconds(10))
                {
                    break;
                }

                Thread.Sleep(20);
            }

            Assert.False(reference.IsAlive, message);
        }
    }
}
