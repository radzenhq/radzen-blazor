using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RadzenBlazorDemos.Tools;

class Program
{
    const string BaseUrl = "https://blazor.radzen.com";

    static readonly HashSet<string> OptionalCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "UI Fundamentals",
        "Images",
    };

    static readonly HashSet<string> OrganizationalCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Data", "Layout", "Navigation", "Forms", "Data Visualization", "Feedback", "Validators",
    };

    static readonly HashSet<string> ExcludedTopLevel = new(StringComparer.OrdinalIgnoreCase)
    {
        "Overview",
        "Get Started",
        "AI",
        "Support",
        "Accessibility",
        "UI Blocks",
        "App Templates",
        "Changelog",
    };

    static readonly HashSet<string> ExcludedPages = new(StringComparer.OrdinalIgnoreCase)
    {
        "AccessibilityPage",
        "AI",
        "Changelog",
        "Dashboard",
        "DashboardPage",
        "GetStarted",
        "Index",
        "NotFound",
        "Playground",
        "SupportPage",
        "ThemeServicePage",
        "ThemesPage",
    };

    static readonly string[] ExcludedPrefixes = ["Templates", "UIBlocks"];

    static readonly HashSet<string> LifecycleMethodNames = new(StringComparer.Ordinal)
    {
        "OnInitialized", "OnInitializedAsync",
        "OnParametersSet", "OnParametersSetAsync",
        "OnAfterRender", "OnAfterRenderAsync",
        "SetParametersAsync", "BuildRenderTree",
        "ShouldRender", "Dispose", "DisposeAsync",
    };

    static bool IsExcluded(string filePath)
    {
        var name = Path.GetFileNameWithoutExtension(filePath);

        if (ExcludedPages.Contains(name))
            return true;

        foreach (var prefix in ExcludedPrefixes)
        {
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: RadzenBlazorDemos.Tools <outputDir> <pagesPath> <exampleServicePath> [xmlDocPath] [sourceDir]");
            return 1;
        }

        var outputDir = args[0];
        var pagesPath = args[1];
        var exampleServicePath = args[2];
        var xmlDocPath = args.Length > 3 && !string.IsNullOrWhiteSpace(args[3]) ? args[3] : null;
        var inferredSourceDir = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(exampleServicePath) ?? "", "..", "..", "Radzen.Blazor"));
        var sourceDir = args.Length > 4 && !string.IsNullOrWhiteSpace(args[4]) ? args[4] : inferredSourceDir;

        if (!Directory.Exists(pagesPath))
        {
            Console.Error.WriteLine($"Pages path does not exist: {pagesPath}");
            return 1;
        }

        if (!File.Exists(exampleServicePath))
        {
            Console.Error.WriteLine($"ExampleService.cs not found: {exampleServicePath}");
            return 1;
        }

        try
        {
            var categories = ParseExampleService(exampleServicePath);
            var xmlDocs = xmlDocPath != null && File.Exists(xmlDocPath) ? ParseXmlDocs(xmlDocPath) : new Dictionary<string, XmlMemberDoc>();
            var typeMap = Directory.Exists(sourceDir) ? BuildComponentTypeMap(sourceDir) : new Dictionary<string, ComponentTypeInfo>(StringComparer.Ordinal);

            Directory.CreateDirectory(outputDir);
            var mdDir = Path.Combine(outputDir, "md");
            Directory.CreateDirectory(mdDir);

            GenerateComponentPages(categories, pagesPath, xmlDocs, typeMap, mdDir);
            GenerateIndex(categories, Path.Combine(outputDir, "llms.txt"), xmlDocs);

            var apiCount = Directory.Exists(Path.Combine(mdDir, "api")) ? Directory.GetFiles(Path.Combine(mdDir, "api"), "*.md").Length : 0;
            var pageCount = Directory.GetFiles(mdDir, "*.md").Length;
            Console.WriteLine($"Generated llms.txt, {pageCount} component pages, and {apiCount} API reference files in: {outputDir}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    // ── Data model ──────────────────────────────────────────────────────

    record ExampleNode(string Name, string Path, string Description, List<ExampleNode> Children, List<string> Tags);

    record XmlMemberDoc(string Summary, string TypeName, string Value, string Example, string Remarks);

    record ParameterDoc(string Name, string Type, string Summary, string ValueDescription, bool IsEvent);

    record PropertyTypeInfo(string Name, string Type, string XmlPrefix);
    record MethodTypeInfo(string Name, string ReturnType, List<(string Type, string Name)> Parameters, string XmlPrefix);
    record ComponentTypeInfo(string ClassName, string BaseClassName, string XmlPrefix, List<PropertyTypeInfo> Properties, List<MethodTypeInfo> Methods);

    // ── ExampleService.cs parsing via Roslyn ────────────────────────────

    static List<ExampleNode> ParseExampleService(string filePath)
    {
        var source = File.ReadAllText(filePath, Encoding.UTF8);
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();

        var fieldDecl = root.DescendantNodes()
            .OfType<FieldDeclarationSyntax>()
            .FirstOrDefault(f => f.Declaration.Variables.Any(v => v.Identifier.Text == "allExamples"));

        if (fieldDecl == null)
            throw new InvalidOperationException("Could not find 'allExamples' field in ExampleService.cs");

        var initializer = fieldDecl.Declaration.Variables.First().Initializer?.Value;
        if (initializer is not ImplicitArrayCreationExpressionSyntax arrayExpr)
            throw new InvalidOperationException("allExamples is not an implicit array creation expression");

        return ParseExampleArray(arrayExpr.Initializer);
    }

    static List<ExampleNode> ParseExampleArray(InitializerExpressionSyntax initializer)
    {
        var results = new List<ExampleNode>();

        foreach (var expr in initializer.Expressions)
        {
            if (expr is ObjectCreationExpressionSyntax objCreate && objCreate.Initializer != null)
            {
                results.Add(ParseSingleExample(objCreate.Initializer));
            }
            else if (expr is ImplicitObjectCreationExpressionSyntax implicitCreate && implicitCreate.Initializer != null)
            {
                results.Add(ParseSingleExample(implicitCreate.Initializer));
            }
        }

        return results;
    }

    static ExampleNode ParseSingleExample(InitializerExpressionSyntax init)
    {
        string name = "";
        string path = "";
        string description = "";
        List<ExampleNode> children = null;
        List<string> tags = null;

        foreach (var assignment in init.Expressions.OfType<AssignmentExpressionSyntax>())
        {
            var propName = assignment.Left.ToString();
            var value = assignment.Right;

            switch (propName)
            {
                case "Name":
                    name = ExtractStringLiteral(value);
                    break;
                case "Path":
                    path = ExtractStringLiteral(value);
                    break;
                case "Description":
                    description = ExtractStringLiteral(value);
                    break;
                case "Children":
                    children = ParseChildrenExpression(value);
                    break;
                case "Tags":
                    tags = ParseTagsExpression(value);
                    break;
            }
        }

        return new ExampleNode(name, path, description, children, tags);
    }

    static string ExtractStringLiteral(ExpressionSyntax expr)
    {
        if (expr is LiteralExpressionSyntax literal && literal.Token.IsKind(SyntaxKind.StringLiteralToken))
            return literal.Token.ValueText;
        return expr.ToString().Trim('"');
    }

    static List<ExampleNode> ParseChildrenExpression(ExpressionSyntax expr)
    {
        if (expr is ImplicitArrayCreationExpressionSyntax implicitArray)
            return ParseExampleArray(implicitArray.Initializer);
        if (expr is ArrayCreationExpressionSyntax arrayCreate && arrayCreate.Initializer != null)
            return ParseExampleArray(arrayCreate.Initializer);

        return null;
    }

    static List<string> ParseTagsExpression(ExpressionSyntax expr)
    {
        IEnumerable<ExpressionSyntax> elements = expr switch
        {
            ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer.Expressions,
            ArrayCreationExpressionSyntax arrayCreate when arrayCreate.Initializer != null => arrayCreate.Initializer.Expressions,
            CollectionExpressionSyntax collection => collection.Elements
                .OfType<ExpressionElementSyntax>()
                .Select(e => e.Expression),
            _ => null
        };

        if (elements == null)
            return null;

        return elements.Select(ExtractStringLiteral).Where(s => !string.IsNullOrEmpty(s)).ToList();
    }

    // ── XML documentation parsing ───────────────────────────────────────

    static Dictionary<string, XmlMemberDoc> ParseXmlDocs(string xmlPath)
    {
        var docs = new Dictionary<string, XmlMemberDoc>(StringComparer.Ordinal);
        var xdoc = XDocument.Load(xmlPath);
        var members = xdoc.Root?.Element("members");

        if (members == null)
            return docs;

        foreach (var member in members.Elements("member"))
        {
            var memberName = member.Attribute("name")?.Value;
            if (memberName == null) continue;

            var summary = CleanXmlDocText(member.Element("summary")?.Value);
            var value = CleanXmlDocText(member.Element("value")?.Value);
            var example = DecodeXmlCodeBlocks(member.Element("example"));
            var remarks = CleanXmlDocText(member.Element("remarks")?.Value);

            docs[memberName] = new XmlMemberDoc(summary, memberName, value, example, remarks);
        }

        return docs;
    }

    static string CleanXmlDocText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        var result = Regex.Replace(text, @"\s+", " ").Trim();
        return result;
    }

    static string DecodeXmlCodeBlocks(XElement exampleElement)
    {
        if (exampleElement == null)
            return "";

        var sb = new StringBuilder();
        foreach (var node in exampleElement.Nodes())
        {
            if (node is XElement element && element.Name.LocalName.Equals("code", StringComparison.OrdinalIgnoreCase))
            {
                var code = WebUtility.HtmlDecode(element.Value).Trim();
                if (string.IsNullOrWhiteSpace(code))
                    continue;

                if (sb.Length > 0)
                    sb.AppendLine();

                sb.AppendLine("```razor");
                sb.AppendLine(code);
                sb.AppendLine("```");
                continue;
            }

            var text = node is XText textNode
                ? textNode.Value
                : Regex.Replace(node.ToString(SaveOptions.DisableFormatting), "<[^>]+>", " ");
            var prose = CleanXmlDocText(WebUtility.HtmlDecode(text));
            if (!string.IsNullOrWhiteSpace(prose))
            {
                if (sb.Length > 0)
                    sb.AppendLine();
                sb.AppendLine(prose);
            }
        }

        return sb.ToString().Trim();
    }

    // ── Component source parsing via Roslyn ─────────────────────────────

    static Dictionary<string, ComponentTypeInfo> BuildComponentTypeMap(string radzenBlazorDir)
    {
        var map = new Dictionary<string, ComponentTypeInfo>(StringComparer.Ordinal);
        var files = Directory.GetFiles(radzenBlazorDir, "*.razor.cs", SearchOption.AllDirectories)
            .Concat(Directory.GetFiles(radzenBlazorDir, "*.cs", SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var file in files)
        {
            var source = File.ReadAllText(file, Encoding.UTF8);
            var tree = CSharpSyntaxTree.ParseText(source);
            var root = tree.GetCompilationUnitRoot();

            foreach (var classDecl in root.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                var className = ToMetadataClassName(classDecl);
                if (string.IsNullOrEmpty(className))
                    continue;

                var ns = GetNamespace(classDecl);
                var xmlPrefix = string.IsNullOrEmpty(ns) ? className : $"{ns}.{className}";

                var baseClassName = ResolveBaseClassName(classDecl);
                var properties = classDecl.Members
                    .OfType<PropertyDeclarationSyntax>()
                    .Where(HasParameterAttribute)
                    .Select(p => new PropertyTypeInfo(
                        p.Identifier.Text,
                        NormalizeTypeText(p.Type.ToString()),
                        xmlPrefix))
                    .ToList();

                var methods = classDecl.Members
                    .OfType<MethodDeclarationSyntax>()
                    .Where(IsPublicApiMethod)
                    .Select(m => new MethodTypeInfo(
                        m.Identifier.Text,
                        NormalizeTypeText(m.ReturnType.ToString()),
                        m.ParameterList.Parameters
                            .Select(p => (
                                NormalizeTypeText(p.Type?.ToString() ?? "object"),
                                p.Identifier.Text))
                            .ToList(),
                        xmlPrefix))
                    .ToList();

                if (map.TryGetValue(className, out var existing))
                {
                    var mergedProps = existing.Properties.Concat(properties).ToList();
                    var mergedMethods = existing.Methods.Concat(methods).ToList();
                    map[className] = new ComponentTypeInfo(className, existing.BaseClassName ?? baseClassName, existing.XmlPrefix ?? xmlPrefix, mergedProps, mergedMethods);
                }
                else
                {
                    map[className] = new ComponentTypeInfo(className, baseClassName, xmlPrefix, properties, methods);
                }
            }
        }

        return map;
    }

    static bool HasParameterAttribute(PropertyDeclarationSyntax property)
        => property.AttributeLists
            .SelectMany(a => a.Attributes)
            .Any(a =>
            {
                var name = a.Name.ToString();
                return name == "Parameter" || name.EndsWith(".Parameter", StringComparison.Ordinal);
            });

    static bool IsPublicApiMethod(MethodDeclarationSyntax method)
    {
        if (!method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            return false;
        if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.OverrideKeyword)))
            return false;
        if (LifecycleMethodNames.Contains(method.Identifier.Text))
            return false;
        return true;
    }

    static string ResolveBaseClassName(ClassDeclarationSyntax classDecl)
    {
        if (classDecl.BaseList == null)
            return null;

        foreach (var baseType in classDecl.BaseList.Types)
        {
            var name = ToMetadataTypeName(baseType.Type);
            if (string.IsNullOrWhiteSpace(name))
                continue;
            if (name.StartsWith("I", StringComparison.Ordinal) && name.Length > 1 && char.IsUpper(name[1]) && !name.Contains('`'))
                continue;
            return name;
        }

        return null;
    }

    static string ToMetadataClassName(ClassDeclarationSyntax classDecl)
    {
        var arity = classDecl.TypeParameterList?.Parameters.Count ?? 0;
        return arity > 0 ? $"{classDecl.Identifier.Text}`{arity}" : classDecl.Identifier.Text;
    }

    static string ToMetadataTypeName(TypeSyntax typeSyntax)
    {
        return typeSyntax switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            GenericNameSyntax generic => $"{generic.Identifier.Text}`{generic.TypeArgumentList.Arguments.Count}",
            QualifiedNameSyntax qualified => ToMetadataTypeName(qualified.Right),
            AliasQualifiedNameSyntax aliasQualified => ToMetadataTypeName(aliasQualified.Name),
            NullableTypeSyntax nullable => ToMetadataTypeName(nullable.ElementType),
            _ => NormalizeTypeText(typeSyntax.ToString())
        };
    }

    static string NormalizeTypeText(string type)
        => Regex.Replace(type ?? "", @"\s+", " ").Trim();

    static string GetNamespace(SyntaxNode node)
    {
        var parent = node.Parent;
        while (parent != null)
        {
            if (parent is FileScopedNamespaceDeclarationSyntax fileNs)
                return fileNs.Name.ToString();
            if (parent is NamespaceDeclarationSyntax ns)
                return ns.Name.ToString();
            parent = parent.Parent;
        }
        return "";
    }

    // ── Inheritance resolution ───────────────────────────────────────────

    static List<PropertyTypeInfo> ResolveFullPropertyList(string componentClassName, Dictionary<string, ComponentTypeInfo> typeMap)
    {
        var result = new List<PropertyTypeInfo>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var current = componentClassName;
        var visited = new HashSet<string>(StringComparer.Ordinal);

        while (!string.IsNullOrWhiteSpace(current) && visited.Add(current))
        {
            if (!typeMap.TryGetValue(current, out var info))
                break;

            foreach (var prop in info.Properties)
            {
                if (seen.Add(prop.Name))
                    result.Add(prop);
            }

            current = info.BaseClassName;
        }

        return result;
    }

    static List<MethodTypeInfo> ResolveComponentMethods(string componentClassName, Dictionary<string, ComponentTypeInfo> typeMap)
    {
        if (!typeMap.TryGetValue(componentClassName, out var info))
            return [];

        return info.Methods;
    }

    // ── Parameter/event extraction with types ───────────────────────────

    static List<ParameterDoc> GetComponentParameters(
        string componentClassName,
        Dictionary<string, XmlMemberDoc> xmlDocs,
        Dictionary<string, ComponentTypeInfo> typeMap)
    {
        var fullProps = ResolveFullPropertyList(componentClassName, typeMap);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var parameters = new List<ParameterDoc>();

        foreach (var prop in fullProps)
        {
            if (!seen.Add(prop.Name))
                continue;

            var xmlKey = $"P:{prop.XmlPrefix}.{prop.Name}";
            xmlDocs.TryGetValue(xmlKey, out var xmlDoc);

            var summary = xmlDoc?.Summary ?? "";
            var valueDesc = xmlDoc?.Value ?? "";
            var isEvent = prop.Type.StartsWith("EventCallback", StringComparison.Ordinal);

            parameters.Add(new ParameterDoc(prop.Name, prop.Type, summary, valueDesc, isEvent));
        }

        return parameters.OrderBy(p => p.IsEvent).ThenBy(p => p.Name, StringComparer.Ordinal).ToList();
    }

    // ── Component name mapping ──────────────────────────────────────────

    static string MapToComponentClass(string exampleName)
    {
        var cleaned = exampleName.Replace(" ", "").Replace("-", "");
        return $"Radzen{cleaned}";
    }

    static string ResolveComponentClass(string exampleName, Dictionary<string, XmlMemberDoc> xmlDocs)
    {
        var className = MapToComponentClass(exampleName);
        if (HasTypeOrProperties(className, xmlDocs))
            return className;
        for (int arity = 1; arity <= 3; arity++)
        {
            var generic = $"{className}`{arity}";
            if (HasTypeOrProperties(generic, xmlDocs))
                return generic;
        }
        return null;
    }

    static bool HasTypeOrProperties(string className, Dictionary<string, XmlMemberDoc> xmlDocs)
    {
        if (xmlDocs.ContainsKey($"T:Radzen.Blazor.{className}"))
            return true;
        var propPrefix = $"P:Radzen.Blazor.{className}.";
        return xmlDocs.Keys.Any(k => k.StartsWith(propPrefix));
    }

    static (string ResolvedClass, string ParentDisplayName) ResolveComponentForNode(
        ExampleNode node, List<string> ancestors, Dictionary<string, XmlMemberDoc> xmlDocs)
    {
        var resolved = ResolveComponentClass(node.Name, xmlDocs);
        if (resolved != null)
            return (resolved, null);
        for (int i = ancestors.Count - 1; i >= 0; i--)
        {
            if (i == 0 && OrganizationalCategories.Contains(ancestors[i]))
                continue;
            resolved = ResolveComponentClass(ancestors[i], xmlDocs);
            if (resolved != null)
                return (resolved, ancestors[i]);
        }
        return (null, null);
    }

    // ── API reference slug helpers ──────────────────────────────────────

    static string ComponentSlug(string metadataClassName)
    {
        var name = metadataClassName;
        var tick = name.IndexOf('`');
        if (tick >= 0)
            name = name[..tick];

        if (name.StartsWith("Radzen", StringComparison.Ordinal))
            name = name["Radzen".Length..];

        return name.ToLowerInvariant();
    }

    static string ComponentDisplayName(string metadataClassName)
    {
        var name = metadataClassName;
        var tick = name.IndexOf('`');
        if (tick >= 0)
            name = name[..tick];

        return name;
    }

    // ── API reference generation ────────────────────────────────────────

    static string GenerateApiReferencePage(
        string componentClassName,
        Dictionary<string, XmlMemberDoc> xmlDocs,
        Dictionary<string, ComponentTypeInfo> typeMap)
    {
        var sb = new StringBuilder();
        var displayName = ComponentDisplayName(componentClassName);

        sb.AppendLine($"# {displayName} API Reference");
        sb.AppendLine();

        var parameters = GetComponentParameters(componentClassName, xmlDocs, typeMap);
        var regularParams = parameters.Where(p => !p.IsEvent).ToList();
        var eventParams = parameters.Where(p => p.IsEvent).ToList();

        if (regularParams.Count > 0)
        {
            sb.AppendLine("## Parameters");
            sb.AppendLine();
            sb.AppendLine("| Parameter | Type | Description |");
            sb.AppendLine("|-----------|------|-------------|");
            foreach (var p in regularParams)
            {
                var type = EscapePipe(p.Type);
                var desc = EscapePipe(p.Summary);
                sb.AppendLine($"| {p.Name} | `{type}` | {desc} |");
            }
            sb.AppendLine();
        }

        if (eventParams.Count > 0)
        {
            sb.AppendLine("## Events");
            sb.AppendLine();
            sb.AppendLine("| Event | Type | Description |");
            sb.AppendLine("|-------|------|-------------|");
            foreach (var p in eventParams)
            {
                var type = EscapePipe(p.Type);
                var desc = EscapePipe(p.Summary);
                sb.AppendLine($"| {p.Name} | `{type}` | {desc} |");
            }
            sb.AppendLine();
        }

        var methods = ResolveComponentMethods(componentClassName, typeMap);
        if (methods.Count > 0)
        {
            var methodDocs = new List<(string Signature, string Returns, string Description)>();
            foreach (var m in methods.OrderBy(m => m.Name, StringComparer.Ordinal))
            {
                var paramList = string.Join(", ", m.Parameters.Select(p => $"{p.Type} {p.Name}"));
                var signature = $"{m.Name}({paramList})";

                var xmlMethodSummary = FindMethodXmlSummary(componentClassName, m, xmlDocs);

                methodDocs.Add((signature, m.ReturnType, xmlMethodSummary));
            }

            if (methodDocs.Count > 0)
            {
                sb.AppendLine("## Methods");
                sb.AppendLine();
                sb.AppendLine("| Method | Returns | Description |");
                sb.AppendLine("|--------|---------|-------------|");
                foreach (var (sig, ret, desc) in methodDocs)
                {
                    sb.AppendLine($"| {EscapePipe(sig)} | `{EscapePipe(ret)}` | {EscapePipe(desc)} |");
                }
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    static string FindMethodXmlSummary(string componentClassName, MethodTypeInfo method, Dictionary<string, XmlMemberDoc> xmlDocs)
    {
        var prefix = $"M:{method.XmlPrefix}.{method.Name}";
        foreach (var (key, doc) in xmlDocs)
        {
            if (key.StartsWith(prefix, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(doc.Summary))
                return doc.Summary;
        }
        return "";
    }

    static string EscapePipe(string text)
        => (text ?? "").Replace("|", "\\|").Replace("\n", " ").Replace("\r", "");

    record LinkInfo(string Name, string Url, string Description, string ParentComponentName);

    static string FormatLink(LinkInfo link)
    {
        var displayName = !string.IsNullOrEmpty(link.ParentComponentName)
            ? $"{link.ParentComponentName}: {link.Name}"
            : link.Name;
        var desc = !string.IsNullOrWhiteSpace(link.Description) ? $": {TrimDescription(link.Description)}" : "";
        return $"- [{displayName}]({link.Url}){desc}";
    }

    // ── Index generation (llms.txt) ─────────────────────────────────────

    static void GenerateIndex(List<ExampleNode> categories, string outputPath, Dictionary<string, XmlMemberDoc> xmlDocs)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Radzen Blazor Components");
        sb.AppendLine();
        sb.AppendLine("> The most sophisticated free UI component library for Blazor, featuring 100+ native components. MIT licensed, used by thousands of developers at companies like Microsoft, NASA, Porsche, Dell, Siemens, and DHL.");
        sb.AppendLine();
        sb.AppendLine("Radzen Blazor Components are written entirely in C# with no JavaScript framework dependencies. Supports Blazor Server, Blazor WebAssembly, .NET MAUI Blazor Hybrid, and the Blazor Web App model in .NET 10. Built with accessibility in mind (WCAG 2.2, keyboard navigation).");
        sb.AppendLine();
        sb.AppendLine("## Quick start");
        sb.AppendLine();
        sb.AppendLine("```bash");
        sb.AppendLine("dotnet add package Radzen.Blazor");
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("Add `<RadzenTheme Theme=\"material\" />` to `App.razor` `<head>`, `<script src=\"_content/Radzen.Blazor/Radzen.Blazor.js\"></script>` after the last `<script>`, and `builder.Services.AddRadzenComponents();` to `Program.cs`. Full setup: https://blazor.radzen.com/get-started");
        sb.AppendLine();
        sb.AppendLine("For premium themes, a WYSIWYG design canvas, database scaffolding, app templates, and dedicated support, see the Radzen Blazor Pro subscription: https://www.radzen.com/pricing");
        sb.AppendLine();

        var optionalLinks = new List<LinkInfo>();

        foreach (var category in categories)
        {
            if (ExcludedTopLevel.Contains(category.Name))
                continue;

            if (category.Children == null || category.Children.Count == 0)
                continue;

            var links = CollectLinkInfos(category, new List<string>(), xmlDocs);
            if (links.Count == 0)
                continue;

            if (OptionalCategories.Contains(category.Name))
            {
                optionalLinks.AddRange(links);
                continue;
            }

            sb.AppendLine($"## {category.Name}");
            sb.AppendLine();
            foreach (var link in links)
                sb.AppendLine(FormatLink(link));
            sb.AppendLine();
        }

        if (optionalLinks.Count > 0)
        {
            sb.AppendLine("## Optional");
            sb.AppendLine();
            foreach (var link in optionalLinks)
                sb.AppendLine(FormatLink(link));
            sb.AppendLine();
        }

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    static List<LinkInfo> CollectLinkInfos(ExampleNode node, List<string> ancestors, Dictionary<string, XmlMemberDoc> xmlDocs)
    {
        var links = new List<LinkInfo>();
        CollectLinkInfosRecursive(node, ancestors, xmlDocs, links);
        return links;
    }

    static void CollectLinkInfosRecursive(ExampleNode node, List<string> ancestors, Dictionary<string, XmlMemberDoc> xmlDocs, List<LinkInfo> links)
    {
        if (node.Children != null)
        {
            var newAncestors = new List<string>(ancestors) { node.Name };
            foreach (var child in node.Children)
                CollectLinkInfosRecursive(child, newAncestors, xmlDocs, links);
        }
        else if (!string.IsNullOrEmpty(node.Path))
        {
            var path = node.Path.TrimStart('/');
            var (_, parentDisplayName) = ResolveComponentForNode(node, ancestors, xmlDocs);
            links.Add(new LinkInfo(node.Name, $"{BaseUrl}/{path}.md", node.Description, parentDisplayName));
        }
    }

    static string TrimDescription(string description)
    {
        var d = description;
        d = Regex.Replace(d, @"^Demonstration and configuration of the (Radzen Blazor |Blazor Radzen|Blazor |Radzen )?", "", RegexOptions.IgnoreCase);
        d = Regex.Replace(d, @"^(Use the |Use )?(Radzen Blazor |Blazor |Radzen )?", "", RegexOptions.IgnoreCase);

        if (d.Length > 0)
            d = char.ToUpper(d[0]) + d[1..];

        d = d.TrimEnd('.');

        return d;
    }

    // ── Per-component .md generation ────────────────────────────────────

    static void GenerateComponentPages(
        List<ExampleNode> categories, string pagesPath,
        Dictionary<string, XmlMemberDoc> xmlDocs,
        Dictionary<string, ComponentTypeInfo> typeMap,
        string mdDir)
    {
        var allLeaves = new List<(ExampleNode Node, string CategoryName, List<string> Ancestors)>();
        foreach (var category in categories)
        {
            if (ExcludedTopLevel.Contains(category.Name))
                continue;
            CollectLeaves(category, category.Name, new List<string>(), allLeaves);
        }

        // Build a map of resolvedClass -> API file URL for linking
        var apiPages = new Dictionary<string, string>(StringComparer.Ordinal);
        var seenClasses = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (node, _, ancestors) in allLeaves)
        {
            if (string.IsNullOrEmpty(node.Path)) continue;
            var (resolvedClass, _) = ResolveComponentForNode(node, ancestors, xmlDocs);
            if (resolvedClass != null)
                seenClasses.Add(resolvedClass);
        }

        // Generate API reference files for all resolved component classes
        var apiDir = Path.Combine(mdDir, "api");
        Directory.CreateDirectory(apiDir);

        foreach (var resolvedClass in seenClasses)
        {
            var slug = ComponentSlug(resolvedClass);
            var apiUrl = $"{BaseUrl}/api/{slug}.md";
            apiPages[resolvedClass] = apiUrl;

            var apiContent = GenerateApiReferencePage(resolvedClass, xmlDocs, typeMap);
            if (!string.IsNullOrWhiteSpace(apiContent))
            {
                var apiPath = Path.Combine(apiDir, $"{slug}.md");
                File.WriteAllText(apiPath, apiContent, Encoding.UTF8);
            }
        }

        // Generate component pages
        foreach (var (node, categoryName, ancestors) in allLeaves)
        {
            if (string.IsNullOrEmpty(node.Path))
                continue;

            var path = node.Path.TrimStart('/');
            var mdPath = Path.Combine(mdDir, $"{path}.md");

            var mdContent = GenerateSingleComponentPage(node, categoryName, ancestors, pagesPath, xmlDocs, apiPages);
            if (!string.IsNullOrWhiteSpace(mdContent))
            {
                var dir = Path.GetDirectoryName(mdPath);
                if (!string.IsNullOrEmpty(dir))
                    Directory.CreateDirectory(dir);
                File.WriteAllText(mdPath, mdContent, Encoding.UTF8);
            }
        }
    }

    static void CollectLeaves(ExampleNode node, string categoryName, List<string> ancestors, List<(ExampleNode, string, List<string>)> leaves)
    {
        if (node.Children != null)
        {
            var newAncestors = new List<string>(ancestors) { node.Name };
            foreach (var child in node.Children)
                CollectLeaves(child, categoryName, newAncestors, leaves);
        }
        else
        {
            leaves.Add((node, categoryName, ancestors));
        }
    }

    static string GenerateSingleComponentPage(
        ExampleNode node, string categoryName, List<string> ancestors,
        string pagesPath, Dictionary<string, XmlMemberDoc> xmlDocs,
        Dictionary<string, string> apiPages)
    {
        var sb = new StringBuilder();

        var (resolvedClass, parentDisplayName) = ResolveComponentForNode(node, ancestors, xmlDocs);

        var title = !string.IsNullOrEmpty(parentDisplayName)
            ? $"{parentDisplayName}: {node.Name}"
            : node.Name;

        sb.AppendLine($"# {title}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(node.Description))
        {
            sb.AppendLine(node.Description);
            sb.AppendLine();
        }

        if (node.Tags is { Count: > 0 })
        {
            sb.AppendLine($"Keywords: {string.Join(", ", node.Tags)}");
            sb.AppendLine();
        }

        if (resolvedClass != null && apiPages.TryGetValue(resolvedClass, out var apiUrl))
        {
            var displayName = ComponentDisplayName(resolvedClass);
            sb.AppendLine($"> API reference: [{displayName} API]({apiUrl})");
            sb.AppendLine();
        }

        var examples = ExtractExamplesForPage(node, pagesPath);
        if (!string.IsNullOrWhiteSpace(examples))
        {
            examples = RemoveDuplicateIntro(examples, node.Name, parentDisplayName, node.Description);
            sb.AppendLine("## Examples");
            sb.AppendLine();
            sb.AppendLine(examples);
        }

        return sb.ToString();
    }

    static string RemoveDuplicateIntro(string examples, string componentName, string parentDisplayName, string nodeDescription)
    {
        var lines = examples.Split(["\r\n", "\r", "\n"], StringSplitOptions.None).ToList();
        int idx = 0;

        while (idx < lines.Count && string.IsNullOrWhiteSpace(lines[idx])) idx++;
        if (idx >= lines.Count) return examples;

        var heading = lines[idx].Trim();
        if (heading.StartsWith("###") && !heading.StartsWith("####"))
        {
            var headingText = heading.TrimStart('#').Trim();
            bool headingMatches = headingText.Equals(componentName, StringComparison.OrdinalIgnoreCase);

            if (!headingMatches && !string.IsNullOrEmpty(parentDisplayName))
                headingMatches = headingText.Equals($"{parentDisplayName} {componentName}", StringComparison.OrdinalIgnoreCase);

            if (headingMatches)
            {
                lines.RemoveAt(idx);
                while (idx < lines.Count && string.IsNullOrWhiteSpace(lines[idx]))
                    lines.RemoveAt(idx);

                if (idx < lines.Count)
                {
                    var para = lines[idx].Trim();
                    if (IsDuplicateDescription(para, nodeDescription))
                    {
                        lines.RemoveAt(idx);
                        while (idx < lines.Count && string.IsNullOrWhiteSpace(lines[idx]))
                            lines.RemoveAt(idx);
                    }
                }
            }
        }

        return string.Join(Environment.NewLine, lines).Trim();
    }

    static bool IsDuplicateDescription(string extracted, string original)
    {
        static string Normalize(string s) => Regex.Replace(s.ToLowerInvariant(), @"[^a-z0-9\s]", "").Trim();

        if (!string.IsNullOrWhiteSpace(original))
        {
            var normExtracted = Normalize(extracted);
            var normOriginal = Normalize(original);
            if (normExtracted.Contains(normOriginal) || normOriginal.Contains(normExtracted))
                return true;
        }

        if (Regex.IsMatch(extracted, @"^Demonstration and configuration of", RegexOptions.IgnoreCase))
            return true;
        if (Regex.IsMatch(extracted, @"^This example demonstrates\b", RegexOptions.IgnoreCase))
            return true;

        return false;
    }

    // ── Demo page example extraction ────────────────────────────────────

    static string ExtractExamplesForPage(ExampleNode node, string pagesPath)
    {
        var pagePath = FindPageFile(node, pagesPath);
        if (pagePath == null || !File.Exists(pagePath))
            return "";

        var content = File.ReadAllText(pagePath, Encoding.UTF8);
        return ExtractDescriptionsAndExamples(content, pagePath);
    }

    static string FindPageFile(ExampleNode node, string pagesPath)
    {
        var candidates = new List<string>();

        var path = node.Path?.TrimStart('/') ?? "";
        var name = node.Name.Replace(" ", "");

        candidates.Add(Path.Combine(pagesPath, $"{name}Page.razor"));

        if (!string.IsNullOrEmpty(path))
        {
            var allRazorFiles = Directory.GetFiles(pagesPath, "*.razor", SearchOption.AllDirectories);
            foreach (var file in allRazorFiles)
            {
                if (IsExcluded(file)) continue;

                var fileContent = File.ReadAllText(file, Encoding.UTF8);
                var pageDirective = Regex.Match(fileContent, @"@page\s+""(/[^""]*)""\s*$", RegexOptions.Multiline);
                if (pageDirective.Success)
                {
                    var route = pageDirective.Groups[1].Value.TrimStart('/');
                    if (route.Equals(path, StringComparison.OrdinalIgnoreCase))
                        return file;
                }
            }
        }

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    static string ExtractDescriptionsAndExamples(string razorContent, string pagePath)
    {
        var result = new StringBuilder();
        var pagesDirectory = Path.GetDirectoryName(pagePath) ?? "";
        var seenText = new HashSet<string>(StringComparer.Ordinal);

        razorContent = Regex.Replace(razorContent,
            @"@code\s*\{[^{}]*(?:\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}[^{}]*)*\}",
            "",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);

        razorContent = Regex.Replace(razorContent,
            @"@(page|inject|layout|using|namespace|implements)\b[^\r\n]*",
            "",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        var lines = razorContent.Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
        var skipUntilNextHeading = false;

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            if (line.Contains("<RadzenText"))
            {
                var textContent = ExtractRadzenTextContent(lines, ref i);
                if (!string.IsNullOrWhiteSpace(textContent.Content) && seenText.Add(textContent.Content))
                {
                    if (textContent.IsHeading && (
                        textContent.Content.Contains("Keyboard Navigation") ||
                        textContent.Content.Contains("Radzen Blazor Studio")))
                    {
                        skipUntilNextHeading = true;
                        continue;
                    }

                    if (textContent.IsHeading)
                    {
                        skipUntilNextHeading = false;
                        result.AppendLine();
                        result.AppendLine(textContent.Content);
                        result.AppendLine();
                    }
                    else if (!skipUntilNextHeading)
                    {
                        result.AppendLine(textContent.Content);
                    }
                }
            }
            else if (line.Contains("<RadzenExample"))
            {
                if (skipUntilNextHeading)
                    continue;

                var exampleContent = ExtractRadzenExampleContent(lines, ref i, pagesDirectory);
                if (!string.IsNullOrWhiteSpace(exampleContent))
                {
                    result.AppendLine();
                    result.AppendLine("```razor");
                    result.AppendLine(exampleContent);
                    result.AppendLine("```");
                    result.AppendLine();
                }
            }
        }

        return result.ToString().Trim();
    }

    static (string Content, bool IsHeading) ExtractRadzenTextContent(string[] lines, ref int index)
    {
        var fullTag = new StringBuilder();
        var depth = 0;

        for (int i = index; i < lines.Length; i++)
        {
            var line = lines[i];
            fullTag.AppendLine(line);

            var openMatches = Regex.Matches(line, @"<RadzenText", RegexOptions.IgnoreCase);
            var closeMatches = Regex.Matches(line, @"</RadzenText>", RegexOptions.IgnoreCase);

            depth += openMatches.Count - closeMatches.Count;

            if (closeMatches.Count > 0 && depth == 0)
            {
                index = i;
                break;
            }
        }

        var tagContent = fullTag.ToString();

        var textStyleMatch = Regex.Match(tagContent, @"TextStyle=""TextStyle\.(H[2-6])""", RegexOptions.IgnoreCase);

        bool isHeading = false;
        int headingLevel = 0;

        if (textStyleMatch.Success)
        {
            var hMatch = Regex.Match(textStyleMatch.Groups[1].Value, @"H(\d)");
            if (hMatch.Success)
            {
                headingLevel = int.Parse(hMatch.Groups[1].Value);
                isHeading = headingLevel >= 2 && headingLevel <= 6;
            }
        }

        var contentMatch = Regex.Match(tagContent, @"<RadzenText[^>]*>([\s\S]*?)</RadzenText>", RegexOptions.IgnoreCase);
        if (!contentMatch.Success)
            return (string.Empty, false);

        var content = contentMatch.Groups[1].Value.Trim();

        content = ConvertCodeTagsToMarkdown(content);

        if (isHeading)
            content = Regex.Replace(content, @"<RadzenLink[^>]*/\s*>|<RadzenLink[^>]*>[\s\S]*?</RadzenLink>", "", RegexOptions.IgnoreCase);
        else
            content = ConvertRadzenLinksToMarkdown(content);

        content = Regex.Replace(content, @"<[^>]+>", "");

        content = Regex.Replace(content, @"@\([^)]*\)", "");
        content = Regex.Replace(content, @"@[A-Za-z0-9_.()]+", "");
        content = Regex.Replace(content, @"\$""[^""]*""", "");
        content = Regex.Replace(content, @"\?\.\w+", "");

        content = Regex.Replace(content, @"\s+", " ").Trim();

        if (content.Contains("=>") || content.Contains("FilterOperator") || content.Contains("FilterValue"))
            return (string.Empty, false);

        if (isHeading && !string.IsNullOrWhiteSpace(content))
        {
            int markdownLevel = headingLevel switch
            {
                2 => 3,
                3 => 4,
                4 => 4,
                5 => 5,
                6 => 5,
                _ => 3
            };

            content = new string('#', markdownLevel) + " " + content;
        }

        return (content, isHeading);
    }

    static string ConvertCodeTagsToMarkdown(string content)
    {
        var codeMatches = Regex.Matches(content, @"<code>([\s\S]*?)</code>", RegexOptions.IgnoreCase);

        var matchesArray = new Match[codeMatches.Count];
        for (int i = 0; i < codeMatches.Count; i++)
            matchesArray[i] = codeMatches[i];

        for (int i = matchesArray.Length - 1; i >= 0; i--)
        {
            var match = matchesArray[i];
            var codeContent = match.Groups[1].Value.Trim();

            codeContent = Regex.Replace(codeContent, @"<[^>]+>", "");
            codeContent = Regex.Replace(codeContent, "@\\(\"([^\"]+)\"\\)", "$1");
            codeContent = Regex.Replace(codeContent, "@\\('([^']+)'\\)", "$1");

            if (!string.IsNullOrWhiteSpace(codeContent))
            {
                var markdownCode = $"`{codeContent}`";
                content = content[..match.Index] + markdownCode + content[(match.Index + match.Length)..];
            }
        }

        return content;
    }

    static string ConvertRadzenLinksToMarkdown(string content)
    {
        var selfClosingPattern = @"<RadzenLink([^>]*?)\s*/>";
        var openClosePattern = @"<RadzenLink([^>]*?)>([\s\S]*?)</RadzenLink>";

        var selfClosingMatches = Regex.Matches(content, selfClosingPattern, RegexOptions.IgnoreCase);
        var selfClosingArray = selfClosingMatches.Cast<Match>().ToArray();

        for (int i = selfClosingArray.Length - 1; i >= 0; i--)
        {
            var match = selfClosingArray[i];
            var attributes = match.Groups[1].Value;
            var (path, text) = ExtractLinkAttributes(attributes, "");

            if (!string.IsNullOrWhiteSpace(path))
            {
                string linkText = !string.IsNullOrWhiteSpace(text) ? text : path;
                var markdownLink = $"[{linkText}]({path})";
                content = content[..match.Index] + markdownLink + content[(match.Index + match.Length)..];
            }
        }

        var linkMatches = Regex.Matches(content, openClosePattern, RegexOptions.IgnoreCase);
        var matchesArray = linkMatches.Cast<Match>().ToArray();

        for (int i = matchesArray.Length - 1; i >= 0; i--)
        {
            var match = matchesArray[i];
            var attributes = match.Groups[1].Value;
            var innerContent = match.Groups[2].Value.Trim();
            var (path, text) = ExtractLinkAttributes(attributes, innerContent);

            if (!string.IsNullOrWhiteSpace(path))
            {
                string linkText = !string.IsNullOrWhiteSpace(text) ? text :
                                 (!string.IsNullOrWhiteSpace(innerContent) ? Regex.Replace(innerContent, @"<[^>]+>", "").Trim() : path);

                if (string.IsNullOrWhiteSpace(linkText))
                    linkText = path;

                var markdownLink = $"[{linkText}]({path})";
                content = content[..match.Index] + markdownLink + content[(match.Index + match.Length)..];
            }
        }

        return content;
    }

    static (string Path, string Text) ExtractLinkAttributes(string attributes, string innerContent)
    {
        string path = "";
        string text = "";

        var pathMatch = Regex.Match(attributes, @"Path=""([^""]+)""", RegexOptions.IgnoreCase);
        if (pathMatch.Success)
            path = pathMatch.Groups[1].Value;

        var textMatch = Regex.Match(attributes, @"Text=""([^""]+)""", RegexOptions.IgnoreCase);
        if (textMatch.Success)
            text = textMatch.Groups[1].Value.Trim();

        if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(innerContent))
            text = Regex.Replace(innerContent, @"<[^>]+>", "").Trim();

        return (path, text);
    }

    static string ExtractRadzenExampleContent(string[] lines, ref int index, string pagesDirectory)
    {
        var fullTag = new StringBuilder();
        var depth = 0;

        for (int i = index; i < lines.Length; i++)
        {
            var line = lines[i];
            fullTag.AppendLine(line);

            var openMatches = Regex.Matches(line, @"<RadzenExample", RegexOptions.IgnoreCase);
            var closeMatches = Regex.Matches(line, @"</RadzenExample>", RegexOptions.IgnoreCase);

            depth += openMatches.Count - closeMatches.Count;

            if (closeMatches.Count > 0 && depth == 0)
            {
                index = i;
                break;
            }
        }

        var tagContent = fullTag.ToString();

        var exampleMatch = Regex.Match(tagContent, @"Example=""([^""\s>]+)""", RegexOptions.IgnoreCase);
        if (exampleMatch.Success)
        {
            var exampleName = exampleMatch.Groups[1].Value.Trim();
            var exampleFilePath = Path.Combine(pagesDirectory, $"{exampleName}.razor");

            if (File.Exists(exampleFilePath))
            {
                var exampleContent = File.ReadAllText(exampleFilePath, Encoding.UTF8);
                return CleanExampleFile(exampleContent);
            }
        }

        var inlineMatch = Regex.Match(tagContent, @"<RadzenExample[^>]*>([\s\S]*?)</RadzenExample>", RegexOptions.IgnoreCase);
        if (inlineMatch.Success)
            return CleanExampleContent(inlineMatch.Groups[1].Value);

        return string.Empty;
    }

    static string CleanExampleContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var result = Regex.Replace(content,
            @"<RadzenExample[^>]*>[\s\S]*?</RadzenExample>",
            "",
            RegexOptions.IgnoreCase);

        var lines = result.Split(["\r\n", "\r", "\n"], StringSplitOptions.None)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();

        return string.Join(Environment.NewLine, lines);
    }

    static string CleanExampleFile(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var result = Regex.Replace(content,
            @"@(using|inject|page|layout|namespace|implements)\b[^\r\n]*",
            "",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        result = Regex.Replace(result, @"(\r?\n\s*){3,}", Environment.NewLine + Environment.NewLine);

        return result.Trim();
    }
}
