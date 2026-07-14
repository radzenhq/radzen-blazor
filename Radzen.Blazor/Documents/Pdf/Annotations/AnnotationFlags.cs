using System;

namespace Radzen.Documents.Pdf;

/// <summary>Controls annotation visibility, printing and interaction.</summary>
[Flags]
public enum AnnotationFlags
{
    /// <summary>No annotation flags are set.</summary>
    None = 0,

    /// <summary>The annotation is invisible.</summary>
    Invisible = 1,

    /// <summary>The annotation is hidden.</summary>
    Hidden = 2,

    /// <summary>The annotation is printed with the page.</summary>
    Print = 4,

    /// <summary>The annotation is not scaled when the page is magnified.</summary>
    NoZoom = 8,

    /// <summary>The annotation is not rotated with the page.</summary>
    NoRotate = 16,

    /// <summary>The annotation is not displayed on screen.</summary>
    NoView = 32,

    /// <summary>The annotation cannot be moved or resized.</summary>
    ReadOnly = 64,

    /// <summary>The annotation cannot receive input focus.</summary>
    Locked = 128,

    /// <summary>The annotation appearance changes when the page is rotated.</summary>
    ToggleNoView = 256,

    /// <summary>The annotation contents cannot be changed.</summary>
    LockedContents = 512,
}
