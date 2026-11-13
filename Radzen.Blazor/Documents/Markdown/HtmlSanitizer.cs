
using System;
using System.Collections.Generic;
using System.Linq;
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

    private static readonly string C0ControlOrSpace = new([.. Enumerable.Range(0, 33).Select(code => (char)code)]);

    public string Sanitize(string html)
    {
        ArgumentNullException.ThrowIfNull(html);

        var output = new StringBuilder();
        var writer = new StringHtmlElementWriter(this, output);
        var scope = writer.EnterScope();
        var textStart = 0;
        var index = 0;
        var terminators = new BlockParser.HtmlTerminators(html);

        while (index < html.Length)
        {
            var offset = html.AsSpan(index).IndexOf('<');

            if (offset < 0)
            {
                break;
            }

            index += offset;

            var match = terminators.Match(index);

            if (!match.Success)
            {
                index++;
                continue;
            }

            AppendText(output, html, textStart, index);

            index += match.Length;

            var openTag = match.Groups["openTag"].Value;

            if ((openTag.Equals("script", StringComparison.OrdinalIgnoreCase) || openTag.Equals("style", StringComparison.OrdinalIgnoreCase)) && !allowedTags.Contains(openTag))
            {
                index = SkipRawText(html, index, openTag);
            }
            else
            {
                writer.Write(match);
            }

            textStart = index;
        }

        AppendText(output, html, textStart, html.Length);
        writer.ExitScope(scope);

        return output.ToString();
    }

    private static void AppendText(StringBuilder output, string html, int start, int end)
    {
        if (end > start)
        {
            output.Append(WebUtility.HtmlEncode(WebUtility.HtmlDecode(html[start..end])));
        }
    }

    private static int SkipRawText(string html, int index, string tagName)
    {
        var closeTag = "</" + tagName;

        while ((index = html.IndexOf(closeTag, index, StringComparison.OrdinalIgnoreCase)) >= 0)
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
        var emitted = names.Count > 1 ? new HashSet<string>(StringComparer.Ordinal) : null;

        for (var index = 0; index < names.Count; index++)
        {
            var name = names[index].Value.ToLowerInvariant();

            if (!allowedAttributes.Contains(name) || emitted?.Add(name) == false)
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
        return IsSafeUrl(destination, image) &&
               (destination?.Contains('&', StringComparison.Ordinal) != true || IsSafeUrl(WebUtility.HtmlDecode(destination), image));
    }

    public static bool IsSafeUrl(string? url, bool allowDataImage)
    {
        // https://url.spec.whatwg.org/#concept-basic-url-parser
        var value = url.AsSpan().Trim(C0ControlOrSpace);
        Span<char> scheme = stackalloc char[6];
        var schemeLength = 0;

        for (var index = 0; index < value.Length; index++)
        {
            var ch = value[index];

            if (ch is '\t' or '\n' or '\r')
            {
                continue;
            }

            // RFC 3986 section 4.2
            if (ch is '/' or '?' or '#')
            {
                return true;
            }

            if (ch == ':')
            {
                if (schemeLength > scheme.Length)
                {
                    return false;
                }

                var name = scheme[..schemeLength];

                if (name.Equals("http", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("https", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("mailto", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                return allowDataImage &&
                       name.Equals("data", StringComparison.OrdinalIgnoreCase) &&
                       StartsWithIgnoringTabAndNewline(value[(index + 1)..], "image/");
            }

            if (schemeLength < scheme.Length)
            {
                scheme[schemeLength] = ch;
            }

            schemeLength++;
        }

        return true;
    }

    private static bool StartsWithIgnoringTabAndNewline(ReadOnlySpan<char> value, string prefix)
    {
        var matched = 0;

        for (var index = 0; index < value.Length && matched < prefix.Length; index++)
        {
            var ch = value[index];

            if (ch is '\t' or '\n' or '\r')
            {
                continue;
            }

            if (char.ToLowerInvariant(ch) != prefix[matched])
            {
                return false;
            }

            matched++;
        }

        return matched == prefix.Length;
    }

    private sealed class StringHtmlElementWriter(HtmlSanitizer sanitizer, StringBuilder output) : HtmlElementWriter(sanitizer)
    {
        protected override void OpenElement(string tagName, IEnumerable<KeyValuePair<string, string>> attributes)
        {
            output.Append('<').Append(tagName);

            foreach (var attribute in attributes)
            {
                output.Append(' ').Append(attribute.Key).Append("=\"").Append(WebUtility.HtmlEncode(attribute.Value)).Append('"');
            }

            output.Append('>');
        }

        protected override void CloseElement(string tagName, bool isVoid)
        {
            if (!isVoid)
            {
                output.Append("</").Append(tagName).Append('>');
            }
        }
    }
}
