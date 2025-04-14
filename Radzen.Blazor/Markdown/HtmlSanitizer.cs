
using System.Collections.Generic;
using System.Text.RegularExpressions;

#nullable enable
class HtmlSanitizer
{
    private readonly ISet<string> allowedTags;
    private readonly ISet<string> allowedAttributes;

    public HtmlSanitizer(IEnumerable<string>? allowedHtmlTags, IEnumerable<string>? allowedHtmlAttributes)
    {
        allowedTags = allowedHtmlTags != null ? new HashSet<string>(allowedHtmlTags) : AllowedTags;
        allowedAttributes = allowedHtmlAttributes != null ? new HashSet<string>(allowedHtmlAttributes) : AllowedAttributes;
    }

    private static ISet<string> AllowedTags { get; } = new HashSet<string>()
    {
        // https://developer.mozilla.org/en/docs/Web/Guide/HTML/HTML5/HTML5_element_list
        "a", "abbr", "acronym", "address", "area", "b",
        "big", "blockquote", "br", "button", "caption", "center", "cite",
        "code", "col", "colgroup", "dd", "del", "dfn", "dir", "div", "dl", "dt",
        "em", "fieldset", "font", "form", "h1", "h2", "h3", "h4", "h5", "h6",
        "hr", "i", "img", "input", "ins", "kbd", "label", "legend", "li", "map",
        "menu", "ol", "optgroup", "option", "p", "pre", "q", "s", "samp",
        "select", "small", "span", "strike", "strong", "sub", "sup", "table",
        "tbody", "td", "textarea", "tfoot", "th", "thead", "tr", "tt", "u",
        "ul", "var", "section", "nav", "article", "aside", "header", "footer", "main",
        "figure", "figcaption", "data", "time", "mark", "ruby", "rt", "rp", "bdi", "wbr",
        "datalist", "keygen", "output", "progress", "meter", "details", "summary", "menuitem",
        "html", "head", "body"
    };

    public static ISet<string> AllowedAttributes { get; } = new HashSet<string>()
    {
        // https://developer.mozilla.org/en-US/docs/Web/HTML/Attributes
        "abbr", "accept", "accept-charset", "accesskey",
        "action", "align", "alt", "axis", "bgcolor", "border", "cellpadding",
        "cellspacing", "char", "charoff", "charset", "checked", "cite", "class",
        "clear", "cols", "colspan", "color", "compact", "coords", "datetime",
        "dir", "disabled", "enctype", "for", "frame", "headers", "height",
        "href", "hreflang", "hspace", "id", "ismap", "label", "lang",
        "longdesc", "maxlength", "media", "method", "multiple", "name",
        "nohref", "noshade", "nowrap", "prompt", "readonly", "rel", "rev",
        "rows", "rowspan", "rules", "scope", "selected", "shape", "size",
        "span", "src", "start", "style", "summary", "tabindex", "target", "title",
        "type", "usemap", "valign", "value", "vspace", "width",
        "high", "keytype", "list", "low", "max", "min", "novalidate", "open", "optimum",
        "pattern", "placeholder", "pubdate", "radiogroup", "required", "reversed", "spellcheck", "step",
        "wrap", "challenge", "contenteditable", "draggable", "dropzone", "autocomplete", "autosave",
    };

    public static ISet<string> UriAttributes { get; } = new HashSet<string>()
    {
        "action", "background", "dynsrc", "href", "lowsrc", "src"
    };

    public string Sanitize(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        return Regex.Replace(input, @"</?([a-zA-Z0-9]+)(\s[^>]*)?>", SanitizeTag);
    }

    private string SanitizeTag(Match match)
    {
        var tag = match.Groups[1].Value.ToLowerInvariant();

        if (!allowedTags.Contains(tag))
        {
            return string.Empty;
        }

        var attributes = match.Groups[2].Value;

        var safeAttributes = Regex.Replace(attributes, @"(\w+)\s*=\s*(""[^""]*""|'[^']*'|[^\s>]+)", SanitizeAttribute);

        return $"<{(match.Value.StartsWith("</") ? "/" : "")}{tag}{safeAttributes}>";
    }

    private string SanitizeAttribute(Match match)
    {
        var name = match.Groups[1].Value.ToLowerInvariant();
        var value = match.Groups[2].Value;

        if (!allowedAttributes.Contains(name))
        {
            return string.Empty;
        }

        if (name == "style")
        {
            var decoded = HtmlDecode(value).ToLowerInvariant();

            if (Regex.IsMatch(decoded, @"expression|javascript:|vbscript:|url\s*\(\s*(['""])?\s*javascript", RegexOptions.IgnoreCase))
            {
                return string.Empty;
            }

            return $" style=\"{value}\"";
        }

        if (UriAttributes.Contains(name) && IsDangerousUrl(value))
        {
            return string.Empty;
        }

        return $" {name}=\"{value}\"";
    }

    private static string HtmlDecode(string input)
    {
        return System.Web.HttpUtility.HtmlDecode(input);
    }

    public static bool IsDangerousUrl(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var decoded = HtmlDecode(value).Trim().ToLowerInvariant();

        return decoded.StartsWith("javascript:") ||
               decoded.StartsWith("vbscript:") ||
               decoded.StartsWith("data:text/html") ||
               decoded.Contains("expression(");
    }
}