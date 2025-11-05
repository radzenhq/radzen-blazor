using System;
using Microsoft.AspNetCore.Components;

namespace Radzen;

#if NET7_0_OR_GREATER
#else
/// <summary>
/// Enables "onmouseenter" and "onmouseleave" event support in Blazor. Not for public use.
/// </summary>
[EventHandler("onmouseenter", typeof(EventArgs), true, true)]
[EventHandler("onmouseleave", typeof(EventArgs), true, true)]
public static class EventHandlers
{
}
#endif

