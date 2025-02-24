using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Radzen.Blazor;

/// <summary>
/// Specifies the white space text used when rendering the text.
/// </summary>
public enum WhiteSpace
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
    /// The text will not wrap, with addition of ellipsis and hidden overflow.
    /// </summary>
    Truncate
}
