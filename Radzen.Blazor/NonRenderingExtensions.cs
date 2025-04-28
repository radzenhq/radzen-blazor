using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Radzen.Blazor;

static class NonRenderingExtensions
{
    public static Action AsNonRenderingEventHandler(this ComponentBase _, Action callback)
        => new SyncReceiver(callback).Invoke;
    public static Action<TValue> AsNonRenderingEventHandler<TValue>(this Action<TValue> callback) => new SyncReceiver<TValue>(callback).Invoke;
    public static Func<Task> AsNonRenderingEventHandler(this ComponentBase _, Func<Task> callback) => new AsyncReceiver(callback).Invoke;
    public static Func<TValue, Task> AsNonRenderingEventHandler<TValue>(this ComponentBase _, Func<TValue, Task> callback) => new AsyncReceiver<TValue>(callback).Invoke;

    private record SyncReceiver(Action callback) : ReceiverBase 
    { 
        public void Invoke() => callback(); 
    }

    private record SyncReceiver<T>(Action<T> callback) : ReceiverBase 
    { 
        public void Invoke(T arg) => callback(arg); 
    }

    private record AsyncReceiver(Func<Task> callback) : ReceiverBase 
    { 
        public Task Invoke() => callback(); 
    }

    private record AsyncReceiver<T>(Func<T, Task> callback) : ReceiverBase 
    { 
        public Task Invoke(T arg) => callback(arg); 
    }

    private record ReceiverBase : IHandleEvent
    {
        public Task HandleEventAsync(EventCallbackWorkItem item, object arg) => item.InvokeAsync(arg);
    }
}