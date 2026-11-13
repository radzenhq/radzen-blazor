using System.Collections.Generic;
using System.Text.RegularExpressions;

#nullable enable
abstract class HtmlElementWriter(HtmlSanitizer sanitizer)
{
    private const int MaxOpenElements = 100;
    private readonly List<string> openElements = [];
    private int scopeStart;

    protected abstract void OpenElement(string tagName, IEnumerable<KeyValuePair<string, string>> attributes);

    protected abstract void CloseElement(string tagName, bool isVoid);

    public int EnterScope()
    {
        var parentScopeStart = scopeStart;
        scopeStart = openElements.Count;
        return parentScopeStart;
    }

    public void ExitScope(int parentScopeStart)
    {
        CloseElements(scopeStart);
        scopeStart = parentScopeStart;
    }

    public void Write(Match tag)
    {
        var openTag = tag.Groups["openTag"];

        if (openTag.Success)
        {
            var tagName = openTag.Value.ToLowerInvariant();
            var isVoid = HtmlSanitizer.IsVoidElement(tagName);
            var closesImmediately = isVoid || tag.Groups["selfClosing"].Success;

            if (!sanitizer.IsAllowedTag(tagName) || (!closesImmediately && openElements.Count >= MaxOpenElements))
            {
                return;
            }

            OpenElement(tagName, sanitizer.GetAllowedAttributes(tagName, tag));

            if (closesImmediately)
            {
                CloseElement(tagName, isVoid);
            }
            else
            {
                openElements.Add(tagName);
            }

            return;
        }

        var closeTag = tag.Groups["closeTag"];

        if (closeTag.Success && openElements.Count > scopeStart)
        {
            var index = openElements.LastIndexOf(closeTag.Value.ToLowerInvariant(), openElements.Count - 1, openElements.Count - scopeStart);

            if (index >= 0)
            {
                CloseElements(index);
            }
        }
    }

    private void CloseElements(int index)
    {
        for (var i = openElements.Count - 1; i >= index; i--)
        {
            CloseElement(openElements[i], false);
        }

        openElements.RemoveRange(index, openElements.Count - index);
    }
}
