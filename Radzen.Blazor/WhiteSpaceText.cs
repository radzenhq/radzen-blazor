using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radzen.Blazor;

/// <summary>
/// Specifies the white space text used when rendering the text.
/// </summary>
public enum WhiteSpaceText
{
    /// <summary>
    /// The text will wrap when necessary.
    /// </summary>
    Wrap,
    /// <summary>
    /// The text will not wrap.
    /// </summary>
    Nowrap,
    /// <summary>
    /// The text will wrap when necessary, and any overflowed text will be clipped.
    /// </summary>
    Truncate
}
