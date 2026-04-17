using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Radzen;

static class JSRuntimeExtensions
{
    public static void InvokeVoid(this IJSRuntime jsRuntime, string identifier, params object?[]? args)
    {
        _ = jsRuntime.InvokeVoidAsync(identifier, args).FireAndForget();
    }

    public static void InvokeVoid(this IJSObjectReference jsRef, string identifier, params object?[]? args)
    {
        _ = jsRef.InvokeVoidAsync(identifier, args).FireAndForget();
    }

    public static void DisposeFireAndForget(this IJSObjectReference jsRef)
    {
        _ = jsRef.DisposeAsync().FireAndForget();
    }

    private static async ValueTask FireAndForget(this ValueTask task)
    {
        try
        {
            await task;
        }
        catch (Exception)
        {
        }
    }
}
