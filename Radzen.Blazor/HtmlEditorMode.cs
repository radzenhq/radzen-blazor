using System;

namespace Radzen;

/// <summary>
/// Html editor mode (Rendered or Raw). Also used for toolbar buttons to enable/disable according to mode.
/// </summary>
[Flags]
public enum HtmlEditorMode
{
    /// <summary>
    /// The editor is in Design mode.
    /// </summary>
    Design = 1,

    /// <summary>
    /// The editor is in Source mode.
    /// </summary>
    Source = 2,
}

