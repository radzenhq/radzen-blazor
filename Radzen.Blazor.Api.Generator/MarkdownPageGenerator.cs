using System.Globalization;
using System.Text;

namespace Radzen.Blazor.Api.Generator;

sealed class MarkdownPageGenerator
{
    const string BaseUrl = "https://blazor.radzen.com";
    const string TypeRefStart = "\x01TYPEREF:";
    const string TypeRefEnd = "\x01/TYPEREF\x02";

    readonly List<ApiTypeInfo> _types;
    readonly Func<string, string?> _resolveTypeUrl;
    readonly string _outputDir;
    int _pageCount;

    public int PageCount => _pageCount;

    public MarkdownPageGenerator(List<ApiTypeInfo> types, Func<string, string?> resolveTypeUrl, string outputDir)
    {
        _types = types;
        _resolveTypeUrl = resolveTypeUrl;
        _outputDir = outputDir;
    }

    public void Generate()
    {
        var docsDir = Path.Combine(_outputDir, "docs");
        var apiDir = Path.Combine(docsDir, "api");
        Directory.CreateDirectory(apiDir);

        var namespaces = _types.GroupBy(t => t.Namespace).Where(g => !string.IsNullOrEmpty(g.Key)).OrderBy(g => g.Key, StringComparer.Ordinal).ToList();

        WriteIndexPage(Path.Combine(docsDir, "api.md"), namespaces);

        foreach (var nsGroup in namespaces)
        {
            WriteNamespacePage(Path.Combine(apiDir, $"{nsGroup.Key}.md"), nsGroup.Key, nsGroup.ToList());
        }

        foreach (var type in _types)
        {
            WriteTypePage(Path.Combine(apiDir, $"{RazorPageGenerator.GetRouteTypeName(type)}.md"), type);
        }
    }

    void WriteIndexPage(string path, List<IGrouping<string, ApiTypeInfo>> namespaces)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Radzen Blazor API Reference");
        sb.AppendLine();
        sb.AppendLine("Reference documentation for every public type in the Radzen.Blazor assembly, grouped by namespace. Each namespace page lists its classes, structs, interfaces, enums and delegates and links to the full page of every type.");
        sb.AppendLine();
        sb.AppendLine("## Namespaces");
        sb.AppendLine();
        foreach (var ns in namespaces)
        {
            var count = ns.Count();
            sb.AppendLine(CultureInfo.InvariantCulture, $"- [{ns.Key}]({NamespaceUrl(ns.Key)}): {count} {(count == 1 ? "type" : "types")}");
        }

        WritePage(path, sb.ToString());
    }

    void WriteNamespacePage(string path, string ns, List<ApiTypeInfo> types)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# {ns} Namespace");
        sb.AppendLine();

        WriteTypeList(sb, "Classes", types.Where(t => t.Kind == TypeKind.Class));
        WriteTypeList(sb, "Structs", types.Where(t => t.Kind == TypeKind.Struct));
        WriteTypeList(sb, "Interfaces", types.Where(t => t.Kind == TypeKind.Interface));
        WriteTypeList(sb, "Enums", types.Where(t => t.Kind == TypeKind.Enum));
        WriteTypeList(sb, "Delegates", types.Where(t => t.Kind == TypeKind.Delegate));

        WritePage(path, sb.ToString());
    }

    void WriteTypeList(StringBuilder sb, string heading, IEnumerable<ApiTypeInfo> types)
    {
        var ordered = types.OrderBy(t => t.Name, StringComparer.Ordinal).ToList();
        if (ordered.Count == 0) return;

        sb.AppendLine(CultureInfo.InvariantCulture, $"## {heading}");
        sb.AppendLine();
        foreach (var type in ordered)
        {
            var summary = RenderInline(type.Summary);
            sb.AppendLine(CultureInfo.InvariantCulture, $"- [{type.Name}]({TypeUrl(type)}){(summary.Length > 0 ? ": " + summary : "")}");
        }
        sb.AppendLine();
    }

    void WriteTypePage(string path, ApiTypeInfo type)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"# {type.Name} {type.Kind}");
        sb.AppendLine();

        if (!string.IsNullOrEmpty(type.Summary))
        {
            sb.AppendLine(RenderInline(type.Summary));
            sb.AppendLine();
        }

        sb.AppendLine(CultureInfo.InvariantCulture, $"Namespace: [{type.Namespace}]({NamespaceUrl(type.Namespace)})");
        sb.AppendLine();
        sb.AppendLine("Assembly: Radzen.Blazor.dll");
        sb.AppendLine();

        switch (type.Kind)
        {
            case TypeKind.Enum:
                WriteEnumContent(sb, type);
                break;
            case TypeKind.Delegate:
                WriteSyntax(sb, type);
                break;
            default:
                WriteClassContent(sb, type);
                break;
        }

        WritePage(path, sb.ToString());
    }

    void WriteClassContent(StringBuilder sb, ApiTypeInfo type)
    {
        if (type.Kind == TypeKind.Class && type.Inheritance.Count > 0)
        {
            sb.AppendLine("## Inheritance");
            sb.AppendLine();
            foreach (var inherited in type.Inheritance)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"- {RenderTypeLink(inherited)}");
            }
            sb.AppendLine(CultureInfo.InvariantCulture, $"- {RazorPageGenerator.SimplifyTypeName(type.FullName)}");
            sb.AppendLine();

            if (type.DerivedTypes.Count > 0)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"Derived types: {string.Join(", ", type.DerivedTypes.Select(RenderTypeLink))}");
                sb.AppendLine();
            }
        }

        if (type.Interfaces.Count > 0)
        {
            sb.AppendLine("## Implements");
            sb.AppendLine();
            foreach (var iface in type.Interfaces)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"- {RenderTypeLink(iface)}");
            }
            sb.AppendLine();
        }

        WriteSyntax(sb, type);

        if (type.TypeParameters.Count > 0)
        {
            sb.AppendLine("## Type Parameters");
            sb.AppendLine();
            sb.AppendLine("| Name | Description |");
            sb.AppendLine("|------|-------------|");
            foreach (var tp in type.TypeParameters)
            {
                var description = type.TypeParameterDescriptions.TryGetValue(tp, out var desc) ? desc : "";
                sb.AppendLine(CultureInfo.InvariantCulture, $"| {Cell(tp)} | {Cell(description)} |");
            }
            sb.AppendLine();
        }

        if (type.Examples.Count > 0)
        {
            sb.AppendLine("## Examples");
            sb.AppendLine();
            WriteExampleSegments(sb, type.Examples);
        }

        if (!string.IsNullOrEmpty(type.Remarks))
        {
            sb.AppendLine("## Remarks");
            sb.AppendLine();
            sb.AppendLine(RenderInline(type.Remarks));
            sb.AppendLine();
        }

        var members = type.Members;
        WriteConstructors(sb, members.Where(m => m.Kind == MemberKind.Constructor));
        WriteValueMembers(sb, "Fields", "Field", members.Where(m => m.Kind == MemberKind.Field));
        WriteValueMembers(sb, "Properties", "Property", members.Where(m => m.Kind == MemberKind.Property));
        WriteMethods(sb, "Methods", "Method", members.Where(m => m.Kind == MemberKind.Method));
        WriteValueMembers(sb, "Events", "Event", members.Where(m => m.Kind == MemberKind.Event));
        WriteMethods(sb, "Operators", "Operator", members.Where(m => m.Kind == MemberKind.Operator));

        if (type.InheritedMembers.Count > 0)
        {
            sb.AppendLine("## Inherited Members");
            sb.AppendLine();
            foreach (var group in type.InheritedMembers.GroupBy(m => m.DeclaringTypeName))
            {
                var names = string.Join(", ", group.Select(m => m.Name).Distinct(StringComparer.Ordinal).OrderBy(n => n, StringComparer.Ordinal));
                sb.AppendLine(CultureInfo.InvariantCulture, $"- From {RenderTypeLink(group.Key)}: {names}");
            }
            sb.AppendLine();
        }
    }

    void WriteEnumContent(StringBuilder sb, ApiTypeInfo type)
    {
        WriteSyntax(sb, type);

        if (type.EnumFields.Count == 0) return;

        sb.AppendLine("## Fields");
        sb.AppendLine();
        sb.AppendLine("| Field | Description |");
        sb.AppendLine("|-------|-------------|");
        foreach (var field in type.EnumFields)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {Cell(field.Name)} | {Cell(RenderInline(field.Summary))} |");
        }
        sb.AppendLine();
    }

    static void WriteSyntax(StringBuilder sb, ApiTypeInfo type)
    {
        sb.AppendLine("## Syntax");
        sb.AppendLine();
        WriteCodeBlock(sb, RazorPageGenerator.SimplifySignature(type.Syntax), "csharp");
    }

    void WriteConstructors(StringBuilder sb, IEnumerable<TypeMemberInfo> members)
    {
        var ordered = members.OrderBy(m => m.Signature, StringComparer.Ordinal).ToList();
        if (ordered.Count == 0) return;

        sb.AppendLine("## Constructors");
        sb.AppendLine();
        sb.AppendLine("| Constructor | Description |");
        sb.AppendLine("|-------------|-------------|");
        foreach (var member in ordered)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| `{Cell(RazorPageGenerator.SimplifySignature(member.Signature))}` | {Cell(RenderMemberDescription(member))} |");
        }
        sb.AppendLine();
        WriteMemberExamples(sb, ordered);
    }

    void WriteValueMembers(StringBuilder sb, string heading, string column, IEnumerable<TypeMemberInfo> members)
    {
        var ordered = members.OrderBy(m => m.Name, StringComparer.Ordinal).ToList();
        if (ordered.Count == 0) return;

        sb.AppendLine(CultureInfo.InvariantCulture, $"## {heading}");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"| {column} | Type | Description |");
        sb.AppendLine("|------|------|-------------|");
        foreach (var member in ordered)
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"| {Cell(member.Name)} | {TypeCell(member.TypeName)} | {Cell(RenderInline(member.Summary))} |");
        }
        sb.AppendLine();
        WriteMemberExamples(sb, ordered);
    }

    void WriteMethods(StringBuilder sb, string heading, string column, IEnumerable<TypeMemberInfo> members)
    {
        var ordered = members.OrderBy(m => m.Name, StringComparer.Ordinal).ThenBy(m => m.Parameters.Count).ToList();
        if (ordered.Count == 0) return;

        sb.AppendLine(CultureInfo.InvariantCulture, $"## {heading}");
        sb.AppendLine();
        sb.AppendLine(CultureInfo.InvariantCulture, $"| {column} | Returns | Description |");
        sb.AppendLine("|------|---------|-------------|");
        foreach (var member in ordered)
        {
            var parameters = string.Join(", ", member.Parameters.Select(p => $"{RazorPageGenerator.SimplifyTypeName(p.TypeName)} {p.Name}"));
            var returns = member.ReturnType == null || member.ReturnType == "System.Void" || member.ReturnType == "Void" ? "`void`" : TypeCell(member.ReturnType);
            sb.AppendLine(CultureInfo.InvariantCulture, $"| `{Cell($"{member.Name}({parameters})")}` | {returns} | {Cell(RenderMemberDescription(member))} |");
        }
        sb.AppendLine();
        WriteMemberExamples(sb, ordered);
    }

    string RenderMemberDescription(TypeMemberInfo member)
    {
        var sb = new StringBuilder(RenderInline(member.Summary));

        var documented = member.Parameters.Where(p => !string.IsNullOrEmpty(p.Summary)).ToList();
        if (documented.Count > 0)
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append("Parameters: ");
            sb.Append(string.Join(" ", documented.Select(p => $"`{p.Name}`: {RenderInline(p.Summary)}")));
        }

        if (!string.IsNullOrEmpty(member.ReturnSummary))
        {
            if (sb.Length > 0) sb.Append(' ');
            sb.Append("Returns: ");
            sb.Append(RenderInline(member.ReturnSummary));
        }

        return sb.ToString();
    }

    static void WriteMemberExamples(StringBuilder sb, List<TypeMemberInfo> members)
    {
        foreach (var member in members.Where(m => m.Examples.Count > 0))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"### {member.Name} example");
            sb.AppendLine();
            WriteExampleSegments(sb, member.Examples);
        }
    }

    static void WriteExampleSegments(StringBuilder sb, List<ExampleSegment> segments)
    {
        foreach (var segment in segments)
        {
            if (segment.IsCode)
            {
                WriteCodeBlock(sb, segment.Content, "razor");
            }
            else
            {
                sb.AppendLine(segment.Content.Trim());
                sb.AppendLine();
            }
        }
    }

    static void WriteCodeBlock(StringBuilder sb, string code, string language)
    {
        sb.AppendLine(CultureInfo.InvariantCulture, $"```{language}");
        sb.AppendLine(code.Trim());
        sb.AppendLine("```");
        sb.AppendLine();
    }

    string RenderTypeLink(string typeName)
    {
        var display = RazorPageGenerator.SimplifyTypeName(typeName);
        var url = _resolveTypeUrl(typeName);
        return url != null ? $"[{display}]({BaseUrl}{url}.md)" : display;
    }

    string TypeCell(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return "";

        var display = Cell(RazorPageGenerator.SimplifyTypeName(typeName));
        var url = _resolveTypeUrl(typeName);
        return url != null ? $"[`{display}`]({BaseUrl}{url}.md)" : $"`{display}`";
    }

    string RenderInline(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        var sb = new StringBuilder();
        int i = 0;
        while (i < text.Length)
        {
            var markerStart = text.IndexOf(TypeRefStart, i, StringComparison.Ordinal);
            if (markerStart < 0)
            {
                sb.Append(text[i..]);
                break;
            }

            sb.Append(text[i..markerStart]);

            var crefEnd = text.IndexOf('\x02', markerStart);
            if (crefEnd < 0) { sb.Append(text[markerStart..]); break; }

            var cref = text[(markerStart + TypeRefStart.Length)..crefEnd];
            var displayStart = crefEnd + 1;
            var displayEnd = text.IndexOf(TypeRefEnd, displayStart, StringComparison.Ordinal);
            if (displayEnd < 0) { sb.Append(text[markerStart..]); break; }

            var display = text[displayStart..displayEnd];
            var url = _resolveTypeUrl(cref);
            sb.Append(url != null ? $"[{display}]({BaseUrl}{url}.md)" : display);

            i = displayEnd + TypeRefEnd.Length;
        }

        return CollapseWhitespace(sb.ToString());
    }

    static string CollapseWhitespace(string text)
    {
        var sb = new StringBuilder(text.Length);
        var pendingSpace = false;
        foreach (var c in text)
        {
            if (char.IsWhiteSpace(c))
            {
                pendingSpace = sb.Length > 0;
                continue;
            }
            if (pendingSpace)
            {
                sb.Append(' ');
                pendingSpace = false;
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    static string Cell(string text) => CollapseWhitespace(text ?? "").Replace("|", "\\|", StringComparison.Ordinal);

    static string NamespaceUrl(string ns) => $"{BaseUrl}/docs/api/{ns}.md";

    static string TypeUrl(ApiTypeInfo type) => $"{BaseUrl}/docs/api/{RazorPageGenerator.GetRouteTypeName(type)}.md";

    void WritePage(string path, string content)
    {
        File.WriteAllText(path, content, new UTF8Encoding(false));
        _pageCount++;
    }
}
