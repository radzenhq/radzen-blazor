using System.Diagnostics;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
            Console.Error.WriteLine("Usage: RadzenBlazorDemos.Tools <outputDir> <pagesPath> <exampleServicePath>");
            return 1;
        }

        var outputDir = args[0];
        var pagesPath = args[1];
        var exampleServicePath = args[2];

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
            Directory.CreateDirectory(outputDir);
            var mdDir = Path.Combine(outputDir, "md");
            Directory.CreateDirectory(mdDir);

            var apiRoutes = LoadApiRoutes(Path.Combine(mdDir, "docs", "api"));
            var apiIndexLinks = ReadApiIndexLinks(Path.Combine(mdDir, "docs", "api.md"));

            GenerateComponentPages(categories, pagesPath, apiRoutes, mdDir);
            GenerateIndex(categories, Path.Combine(outputDir, "llms.txt"), apiRoutes, apiIndexLinks);
            GenerateHomePage(pagesPath, Path.Combine(mdDir, "index.md"));
            GenerateSitemap(categories, pagesPath, outputDir);

            var pageCount = Directory.GetFiles(mdDir, "*.md").Length;
            Console.WriteLine($"Generated llms.txt and {pageCount} component pages linking {apiRoutes.Count} API reference pages in: {outputDir}");
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

    // ── Component name mapping ──────────────────────────────────────────

    static string MapToComponentClass(string exampleName)
    {
        var cleaned = exampleName.Replace(" ", "").Replace("-", "");
        return $"Radzen{cleaned}";
    }

    const string ChartClass = "RadzenChart";

    static string ResolveComponentClass(string exampleName, Dictionary<string, string> apiRoutes)
    {
        var className = MapToComponentClass(exampleName);
        if (apiRoutes.ContainsKey(className))
            return className;
        if (className.Contains("Chart", StringComparison.Ordinal) && apiRoutes.ContainsKey(ChartClass))
            return ChartClass;
        return null;
    }

    static (string ResolvedClass, string ParentDisplayName) ResolveComponentForNode(
        ExampleNode node, List<string> ancestors, Dictionary<string, string> apiRoutes)
    {
        var resolved = ResolveComponentClass(node.Name, apiRoutes);
        if (resolved != null)
            return (resolved, null);
        for (int i = ancestors.Count - 1; i >= 0; i--)
        {
            if (i == 0 && OrganizationalCategories.Contains(ancestors[i]))
                continue;
            resolved = ResolveComponentClass(ancestors[i], apiRoutes);
            if (resolved != null)
                return (resolved, ancestors[i]);
        }
        if (node.Path != null && node.Path.Contains("chart", StringComparison.OrdinalIgnoreCase) && apiRoutes.ContainsKey(ChartClass))
            return (ChartClass, "Chart");
        return (null, null);
    }

    const string ApiRoutePrefix = "Radzen.Blazor.";

    static Dictionary<string, string> LoadApiRoutes(string apiDir)
    {
        var routes = new Dictionary<string, string>(StringComparer.Ordinal);
        if (!Directory.Exists(apiDir))
            return routes;

        var candidates = Directory.GetFiles(apiDir, ApiRoutePrefix + "*.md")
            .Select(Path.GetFileNameWithoutExtension)
            .Select(route => (Route: route, Class: route[ApiRoutePrefix.Length..]))
            .Where(c => c.Class.Length > 0 && !c.Class.Contains('.'))
            .Select(c =>
            {
                var dash = c.Class.IndexOf('-');
                var arity = dash >= 0 && int.TryParse(c.Class[(dash + 1)..], out var n) ? n : 0;
                return (c.Route, Class: dash >= 0 ? c.Class[..dash] : c.Class, Arity: arity);
            })
            .OrderBy(c => c.Arity);

        foreach (var candidate in candidates)
        {
            routes.TryAdd(candidate.Class, candidate.Route);
        }

        return routes;
    }

    static List<string> ReadApiIndexLinks(string apiIndexPath)
    {
        if (!File.Exists(apiIndexPath))
            return new List<string>();

        return File.ReadLines(apiIndexPath, Encoding.UTF8)
            .Where(line => line.StartsWith("- [", StringComparison.Ordinal))
            .ToList();
    }

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

    static void GenerateIndex(List<ExampleNode> categories, string outputPath, Dictionary<string, string> apiRoutes, List<string> apiIndexLinks)
    {
        var sb = new StringBuilder();

        sb.AppendLine("# Radzen Blazor Components");
        sb.AppendLine();
        sb.AppendLine("> The most comprehensive free UI component library for Blazor — 110+ native components with Material 3, Material 2, Fluent, and Bootstrap design systems. MIT licensed. The only Blazor component library with a companion visual IDE (Radzen Blazor Studio) and an MCP server for AI-assisted development.");
        sb.AppendLine();
        sb.AppendLine("Written entirely in C# with no JavaScript framework dependencies. Supports Blazor Server, Blazor WebAssembly, .NET MAUI Blazor Hybrid, and the Blazor Web App model in .NET 10. Built with accessibility in mind (WCAG 2.2, keyboard navigation). Used at Microsoft, NASA, Porsche, Dell, Siemens, and DHL.");
        sb.AppendLine();
        sb.AppendLine("**Companion tools:** Radzen Blazor Studio — visual IDE with WYSIWYG designer and database scaffolding (https://www.radzen.com/blazor-studio). Radzen Blazor MCP Server — Model Context Protocol server for AI-assisted Blazor development, works with VS Code, Visual Studio, Cursor, and other MCP-capable IDEs (https://blazor.radzen.com/ai). Free Community edition available. Blazor Pro: $799/year (https://www.radzen.com/pricing).");
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

            var links = CollectLinkInfos(category, new List<string>(), apiRoutes);
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

        if (apiIndexLinks.Count > 0)
        {
            sb.AppendLine("## API Reference");
            sb.AppendLine();
            sb.AppendLine($"- [API Reference]({BaseUrl}/docs/api.md): Every public type in Radzen.Blazor grouped by namespace, with parameters, methods, events and enum values");
            foreach (var link in apiIndexLinks)
                sb.AppendLine(link);
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

    static List<LinkInfo> CollectLinkInfos(ExampleNode node, List<string> ancestors, Dictionary<string, string> apiRoutes)
    {
        var links = new List<LinkInfo>();
        CollectLinkInfosRecursive(node, ancestors, apiRoutes, links);
        return links;
    }

    static void CollectLinkInfosRecursive(ExampleNode node, List<string> ancestors, Dictionary<string, string> apiRoutes, List<LinkInfo> links)
    {
        if (node.Children != null)
        {
            var newAncestors = new List<string>(ancestors) { node.Name };
            foreach (var child in node.Children)
                CollectLinkInfosRecursive(child, newAncestors, apiRoutes, links);
        }
        else if (!string.IsNullOrEmpty(node.Path))
        {
            var path = node.Path.TrimStart('/');
            var (_, parentDisplayName) = ResolveComponentForNode(node, ancestors, apiRoutes);
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

    static void GenerateHomePage(string pagesPath, string outputPath)
    {
        var indexPath = Path.Combine(pagesPath, "Index.razor");
        if (!File.Exists(indexPath))
            return;

        var razorContent = File.ReadAllText(indexPath, Encoding.UTF8);
        var body = ExtractDescriptionsAndExamples(razorContent, indexPath);

        var sb = new StringBuilder();
        sb.AppendLine("# Radzen Blazor Components");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(body))
            sb.AppendLine(body);

        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    // ── Per-component .md generation ────────────────────────────────────

    static void GenerateComponentPages(
        List<ExampleNode> categories, string pagesPath,
        Dictionary<string, string> apiRoutes,
        string mdDir)
    {
        var allLeaves = new List<(ExampleNode Node, string CategoryName, List<string> Ancestors)>();
        foreach (var category in categories)
        {
            if (ExcludedTopLevel.Contains(category.Name))
                continue;
            CollectLeaves(category, category.Name, new List<string>(), allLeaves);
        }

        var apiPages = apiRoutes.ToDictionary(r => r.Key, r => $"{BaseUrl}/docs/api/{r.Value}.md", StringComparer.Ordinal);

        foreach (var (node, categoryName, ancestors) in allLeaves)
        {
            if (string.IsNullOrEmpty(node.Path))
                continue;

            var path = node.Path.TrimStart('/');
            var mdPath = Path.Combine(mdDir, $"{path}.md");

            var mdContent = GenerateSingleComponentPage(node, categoryName, ancestors, pagesPath, apiRoutes, apiPages);
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
        string pagesPath, Dictionary<string, string> apiRoutes,
        Dictionary<string, string> apiPages)
    {
        var sb = new StringBuilder();

        var (resolvedClass, parentDisplayName) = ResolveComponentForNode(node, ancestors, apiRoutes);

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
            sb.AppendLine($"> API reference: [{resolvedClass} API]({apiUrl})");
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

            var selfClosingCount = Regex.Matches(line, @"<RadzenText\b[^>]*/>", RegexOptions.IgnoreCase).Count;
            var openCount = Regex.Matches(line, @"<RadzenText\b", RegexOptions.IgnoreCase).Count - selfClosingCount;
            var closeCount = Regex.Matches(line, @"</RadzenText>", RegexOptions.IgnoreCase).Count;

            depth += openCount - closeCount;

            if (depth == 0 && (closeCount > 0 || selfClosingCount > 0))
            {
                index = i;
                break;
            }
        }

        var tagContent = fullTag.ToString();

        bool isHeading = false;
        int headingLevel = 0;

        var tagNameMatch = Regex.Match(tagContent, @"TagName=""(?:Radzen\.Blazor\.)?TagName\.H([1-6])""", RegexOptions.IgnoreCase);
        if (tagNameMatch.Success)
        {
            headingLevel = int.Parse(tagNameMatch.Groups[1].Value);
            isHeading = true;
        }
        else
        {
            var textStyleMatch = Regex.Match(tagContent, @"TextStyle=""(?:Radzen\.Blazor\.)?TextStyle\.(?:Display)?H([1-6])""", RegexOptions.IgnoreCase);
            if (textStyleMatch.Success)
            {
                headingLevel = int.Parse(textStyleMatch.Groups[1].Value);
                isHeading = headingLevel >= 2;
            }
        }

        string content;
        var contentMatch = Regex.Match(tagContent, @"<RadzenText[^>]*>([\s\S]*?)</RadzenText>", RegexOptions.IgnoreCase);
        if (contentMatch.Success && !string.IsNullOrWhiteSpace(contentMatch.Groups[1].Value))
        {
            content = contentMatch.Groups[1].Value.Trim();
        }
        else
        {
            var textAttrMatch = Regex.Match(tagContent, @"\bText=""([^""]*)""", RegexOptions.IgnoreCase);
            if (!textAttrMatch.Success || string.IsNullOrWhiteSpace(textAttrMatch.Groups[1].Value))
                return (string.Empty, false);
            content = WebUtility.HtmlDecode(textAttrMatch.Groups[1].Value).Trim();
        }

        content = ConvertCodeTagsToMarkdown(content);

        if (isHeading)
            content = Regex.Replace(content, @"<RadzenLink[^>]*/\s*>|<RadzenLink[^>]*>[\s\S]*?</RadzenLink>", "", RegexOptions.IgnoreCase);
        else
            content = ConvertRadzenLinksToMarkdown(content);

        content = Regex.Replace(content, @"<[^>]+>", "");

        content = Regex.Replace(content, @"@\((?:[^()]|\([^()]*\))*\)", "");
        content = Regex.Replace(content, @"@[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*(?:\([^()]*(?:\([^()]*\)[^()]*)*\))?", "");
        content = Regex.Replace(content, @"\$""[^""]*""", "");
        content = Regex.Replace(content, @"\?\.\w+", "");

        content = Regex.Replace(content, @"\s+", " ").Trim();

        if (content.Contains("=>") || content.Contains("FilterOperator") || content.Contains("FilterValue"))
            return (string.Empty, false);

        if (isHeading && !string.IsNullOrWhiteSpace(content))
        {
            int markdownLevel = headingLevel switch
            {
                1 => 2,
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

    // ── Sitemap generation ──────────────────────────────────────────────

    static void GenerateSitemap(List<ExampleNode> categories, string pagesPath, string outputDir)
    {
        var routeFileMap = BuildRouteFileMap(pagesPath);
        var gitRoot = FindGitRoot(pagesPath);

        var urls = new List<(string Url, string Lastmod)>();

        foreach (var category in categories)
        {
            CollectSitemapUrls(category, routeFileMap, gitRoot, urls);
        }

        var sitemapPath = Path.Combine(outputDir, "sitemap.xml");
        WriteSitemap(sitemapPath, urls);

        var robotsPath = Path.Combine(outputDir, "robots.txt");
        WriteRobotsTxt(robotsPath);

        Console.WriteLine($"Generated sitemap.xml ({urls.Count} URLs) and robots.txt in: {outputDir}");
    }

    static void CollectSitemapUrls(ExampleNode node, Dictionary<string, string> routeFileMap, string gitRoot, List<(string Url, string Lastmod)> urls)
    {
        if (!string.IsNullOrEmpty(node.Path))
        {
            var route = node.Path.TrimStart('/');
            var url = string.IsNullOrEmpty(route) ? BaseUrl + "/" : $"{BaseUrl}/{route}";

            string lastmod = null;
            if (gitRoot != null && routeFileMap.TryGetValue(route, out var filePath))
            {
                lastmod = GetGitLastModified(gitRoot, filePath);
            }

            urls.Add((url, lastmod));
        }

        if (node.Children != null)
        {
            foreach (var child in node.Children)
            {
                CollectSitemapUrls(child, routeFileMap, gitRoot, urls);
            }
        }
    }

    static Dictionary<string, string> BuildRouteFileMap(string pagesPath)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.GetFiles(pagesPath, "*.razor", SearchOption.AllDirectories))
        {
            var content = File.ReadAllText(file);
            var match = Regex.Match(content, @"@page\s+""(/[^""]*)""\s*$", RegexOptions.Multiline);
            if (match.Success)
                map[match.Groups[1].Value.TrimStart('/')] = file;
        }
        return map;
    }

    static string FindGitRoot(string startDir)
    {
        var dir = Path.GetFullPath(startDir);
        while (dir != null)
        {
            if (Directory.Exists(Path.Combine(dir, ".git")))
                return dir;
            dir = Path.GetDirectoryName(dir);
        }
        return null;
    }

    static string GetGitLastModified(string repoRoot, string filePath)
    {
        var relativePath = Path.GetRelativePath(repoRoot, filePath);
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = $"log --format=%aI -1 -- \"{relativePath}\"",
                WorkingDirectory = repoRoot,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            var output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
            {
                if (DateTimeOffset.TryParse(output, out var dto))
                    return dto.ToString("yyyy-MM-dd");
            }
        }
        catch
        {
            // git not available — skip lastmod
        }

        return null;
    }

    static void WriteSitemap(string filePath, List<(string Url, string Lastmod)> urls)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\">");

        foreach (var (url, lastmod) in urls)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{url}</loc>");
            if (!string.IsNullOrEmpty(lastmod))
                sb.AppendLine($"    <lastmod>{lastmod}</lastmod>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");
        File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(false));
    }

    static void WriteRobotsTxt(string filePath)
    {
        var sb = new StringBuilder();
        sb.AppendLine("User-agent: *");
        sb.AppendLine("Content-Signal: ai-train=yes, search=yes, ai-input=yes");
        sb.AppendLine($"Sitemap: {BaseUrl}/sitemap.xml");
        File.WriteAllText(filePath, sb.ToString(), new UTF8Encoding(false));
    }
}
