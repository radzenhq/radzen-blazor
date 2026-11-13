
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Radzen.Documents.Markdown;

#nullable enable
class HtmlSanitizer
{
    private readonly ISet<string> allowedTags;
    private readonly ISet<string> allowedAttributes;

    public HtmlSanitizer(IEnumerable<string>? allowedHtmlTags, IEnumerable<string>? allowedHtmlAttributes)
    {
        allowedTags = allowedHtmlTags != null ? new HashSet<string>(allowedHtmlTags, StringComparer.OrdinalIgnoreCase) : AllowedTags;
        allowedAttributes = allowedHtmlAttributes != null ? new HashSet<string>(allowedHtmlAttributes, StringComparer.OrdinalIgnoreCase) : AllowedAttributes;
    }

    private static ISet<string> AllowedTags { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // https://developer.mozilla.org/en/docs/Web/Guide/HTML/HTML5/HTML5_element_list
        "a", "abbr", "acronym", "address", "b",
        "big", "blockquote", "br", "caption", "center", "cite",
        "code", "col", "colgroup", "dd", "del", "dfn", "dir", "div", "dl", "dt",
        "em", "font", "h1", "h2", "h3", "h4", "h5", "h6",
        "hr", "i", "img", "ins", "kbd", "li",
        "menu", "ol", "p", "pre", "q", "s", "samp",
        "small", "span", "strike", "strong", "sub", "sup", "table",
        "tbody", "td", "tfoot", "th", "thead", "tr", "tt", "u",
        "ul", "var", "section", "nav", "article", "aside", "header", "footer", "main",
        "figure", "figcaption", "data", "time", "mark", "ruby", "rt", "rp", "bdi", "wbr",
        "progress", "meter", "details", "summary", "menuitem"
    };

    private static ISet<string> AllowedAttributes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes
        "abbr", "accept", "accept-charset", "accesskey",
        "align", "alt", "axis", "bgcolor", "border", "cellpadding",
        "cellspacing", "char", "charoff", "charset", "checked", "cite", "class",
        "clear", "cols", "colspan", "color", "compact", "coords", "datetime",
        "dir", "disabled", "enctype", "for", "frame", "headers", "height",
        "href", "hreflang", "hspace", "ismap", "label", "lang",
        "longdesc", "maxlength", "media", "method", "multiple",
        "nohref", "noshade", "nowrap", "prompt", "readonly", "rel", "rev",
        "rows", "rowspan", "rules", "scope", "selected", "shape", "size",
        "span", "src", "start", "summary", "tabindex", "target", "title",
        "type", "usemap", "valign", "value", "vspace", "width",
        "high", "keytype", "list", "low", "max", "min", "novalidate", "open", "optimum",
        "pattern", "placeholder", "pubdate", "radiogroup", "required", "reversed", "spellcheck", "step",
        "wrap", "challenge", "draggable", "dropzone", "autocomplete", "autosave",
    };

    private static ISet<string> UriAttributes { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "action", "background", "cite", "data", "dynsrc", "formaction", "href", "longdesc", "lowsrc", "ping", "poster", "src", "xlink:href"
    };

    private static readonly char[] SchemeTerminators = [':', '/', '?', '#'];

    public string Sanitize(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var output = new StringBuilder();
        var openElements = new List<string>();
        var textStart = 0;
        var index = 0;

        while (index < html.Length)
        {
            var offset = html.AsSpan(index).IndexOf('<');

            if (offset < 0)
            {
                break;
            }

            index += offset;

            var match = BlockParser.HtmlRegex.Match(html, index);

            if (!match.Success)
            {
                index++;
                continue;
            }

            AppendText(output, html, textStart, index);

            index += match.Length;

            var openTag = match.Groups["openTag"];
            var closeTag = match.Groups["closeTag"];

            if (openTag.Success)
            {
                var tagName = openTag.Value.ToLowerInvariant();

                if (allowedTags.Contains(tagName))
                {
                    output.Append('<').Append(tagName);

                    foreach (var attribute in GetAllowedAttributes(tagName, match))
                    {
                        output.Append(' ').Append(attribute.Key).Append("=\"").Append(WebUtility.HtmlEncode(attribute.Value)).Append('"');
                    }

                    output.Append('>');

                    if (match.Groups["selfClosing"].Success && !IsVoidElement(tagName))
                    {
                        output.Append("</").Append(tagName).Append('>');
                    }
                    else if (!IsVoidElement(tagName))
                    {
                        openElements.Add(tagName);
                    }
                }
                else if (tagName is "script" or "style")
                {
                    index = SkipRawText(html, index, tagName);
                }
            }
            else if (closeTag.Success)
            {
                var position = openElements.LastIndexOf(closeTag.Value.ToLowerInvariant());

                if (position >= 0)
                {
                    CloseElements(output, openElements, position);
                }
            }

            textStart = index;
        }

        AppendText(output, html, textStart, html.Length);
        CloseElements(output, openElements, 0);

        return output.ToString();
    }

    private static void AppendText(StringBuilder output, string html, int start, int end)
    {
        if (end > start)
        {
            output.Append(WebUtility.HtmlEncode(WebUtility.HtmlDecode(html[start..end])));
        }
    }

    private static void CloseElements(StringBuilder output, List<string> openElements, int index)
    {
        for (var i = openElements.Count - 1; i >= index; i--)
        {
            output.Append("</").Append(openElements[i]).Append('>');
        }

        openElements.RemoveRange(index, openElements.Count - index);
    }

    private static int SkipRawText(string html, int index, string tagName)
    {
        while ((index = html.IndexOf("</" + tagName, index, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            var match = BlockParser.HtmlRegex.Match(html, index);

            if (match.Success && match.Groups["closeTag"].Value.Equals(tagName, StringComparison.OrdinalIgnoreCase))
            {
                return index + match.Length;
            }

            index++;
        }

        return html.Length;
    }

    public static bool IsVoidElement(string tagName)
    {
        return tagName switch
        {
            "area" or "base" or "br" or "col" or "embed" or "hr" or "img" or "input" or "link" or "meta" or "param" or "source" or "track" or "wbr" => true,
            _ => false
        };
    }

    public bool IsAllowedTag(string tagName)
    {
        return allowedTags.Contains(tagName);
    }

    public IEnumerable<KeyValuePair<string, string>> GetAllowedAttributes(string tagName, Match tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        var names = tag.Groups["attributeName"].Captures;
        var values = tag.Groups["attributeValue"].Captures;
        var emitted = new HashSet<string>(StringComparer.Ordinal);

        for (var index = 0; index < names.Count; index++)
        {
            var name = names[index].Value.ToLowerInvariant();

            if (!allowedAttributes.Contains(name) || !emitted.Add(name))
            {
                continue;
            }

            var value = WebUtility.HtmlDecode(values[index].Value);

            if (UriAttributes.Contains(name) && !IsSafeUrl(value, tagName == "img" && name == "src"))
            {
                continue;
            }

            yield return new KeyValuePair<string, string>(name, value);
        }
    }

    public static bool IsSafeMarkdownUrl(string? destination, bool image)
    {
        // https://spec.commonmark.org/0.31.2/#entity-and-numeric-character-references
        return IsSafeUrl(destination, image) && IsSafeUrl(WebUtility.HtmlDecode(destination), image);
    }

    public static bool IsSafeUrl(string? url, bool allowDataImage)
    {
        if (string.IsNullOrEmpty(url))
        {
            return true;
        }

        // https://url.spec.whatwg.org/#concept-basic-url-parser
        var start = 0;
        var end = url.Length;

        while (start < end && url[start] <= ' ')
        {
            start++;
        }

        while (end > start && url[end - 1] <= ' ')
        {
            end--;
        }

        var normalized = url[start..end].Replace("\t", string.Empty, StringComparison.Ordinal)
            .Replace("\n", string.Empty, StringComparison.Ordinal)
            .Replace("\r", string.Empty, StringComparison.Ordinal);

        // RFC 3986 section 4.2
        var terminator = normalized.IndexOfAny(SchemeTerminators);

        if (terminator < 0 || normalized[terminator] != ':')
        {
            return true;
        }

        var scheme = normalized[..terminator];

        if (scheme.Equals("http", StringComparison.OrdinalIgnoreCase) ||
            scheme.Equals("https", StringComparison.OrdinalIgnoreCase) ||
            scheme.Equals("mailto", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return allowDataImage &&
               scheme.Equals("data", StringComparison.OrdinalIgnoreCase) &&
               normalized.AsSpan(terminator + 1).StartsWith("image/", StringComparison.OrdinalIgnoreCase);
    }
}
