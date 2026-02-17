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

    // Top-level categories whose content is secondary (CSS utilities, styling helpers).
    // These are placed in the ## Optional section of llms.txt.
    static readonly HashSet<string> OptionalCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "UI Fundamentals",
        "Images",
    };

    // Top-level categories that are purely organizational groupings (not component parents).
    // Their names should NOT be used as fallback component classes for child pages.
    static readonly HashSet<string> OrganizationalCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Data", "Layout", "Navigation", "Forms", "Data Visualization", "Feedback", "Validators",
    };

    // Top-level entries that are not component documentation.
    static readonly HashSet<string> ExcludedTopLevel = new(StringComparer.OrdinalIgnoreCase)
    {
        "Overview",
        "Get Started",
        "AI",
        "Support",
        "Accessibility",
        "Markdown",
        "UI Blocks",
        "App Templates",
        "Changelog",
    };

    // Pages that are not component documentation (marketing, meta, showcases).
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
            Console.Error.WriteLine("Usage: RadzenBlazorDemos.Tools <outputDir> <pagesPath> <exampleServicePath> [xmlDocPath]");
            return 1;
        }

        var outputDir = args[0];
        var pagesPath = args[1];
        var exampleServicePath = args[2];
        var xmlDocPath = args.Length > 3 && !string.IsNullOrWhiteSpace(args[3]) ? args[3] : null;

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

            Directory.CreateDirectory(outputDir);
            var mdDir = Path.Combine(outputDir, "md");
            Directory.CreateDirectory(mdDir);

            GenerateComponentPages(categories, pagesPath, xmlDocs, mdDir);
            GenerateIndex(categories, Path.Combine(outputDir, "llms.txt"), xmlDocs);

            Console.WriteLine($"Generated llms.txt and {Directory.GetFiles(mdDir, "*.md").Length} component pages in: {outputDir}");
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

    record XmlMemberDoc(string Summary, string TypeName);

    record ParameterDoc(string Name, string Type, string Summary, bool IsEvent);

    // ── ExampleService.cs parsing via Roslyn ────────────────────────────

    static List<ExampleNode> ParseExampleService(string filePath)
    {
        var source = File.ReadAllText(filePath, Encoding.UTF8);
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetCompilationUnitRoot();

        // Find the allExamples field initializer: Example[] allExamples = new[] { ... }
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
        // new [] { ... } or new Example[] { ... }
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
            // new [] { "a", "b" }
            ImplicitArrayCreationExpressionSyntax implicitArray => implicitArray.Initializer.Expressions,
            // new string[] { "a", "b" }
            ArrayCreationExpressionSyntax arrayCreate when arrayCreate.Initializer != null => arrayCreate.Initializer.Expressions,
            // ["a", "b"] (C# 12 collection expression)
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

            docs[memberName] = new XmlMemberDoc(summary, memberName);
        }

        return docs;
    }

    static string CleanXmlDocText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";

        // Collapse whitespace and trim
        var result = Regex.Replace(text, @"\s+", " ").Trim();
        return result;
    }

    static List<ParameterDoc> GetComponentParameters(string componentClassName, Dictionary<string, XmlMemberDoc> xmlDocs)
    {
        var parameters = new List<ParameterDoc>();
        var prefix = $"P:Radzen.Blazor.{componentClassName}.";

        foreach (var (key, doc) in xmlDocs)
        {
            if (!key.StartsWith(prefix)) continue;

            var propName = key[prefix.Length..];
            if (propName.Contains('.')) continue; // skip nested

            var summary = doc.Summary;
            if (string.IsNullOrWhiteSpace(summary)) continue;

            // Determine type from the summary or name heuristics
            var isEvent = summary.Contains("EventCallback") ||
                          summary.Contains("event callback") ||
                          summary.Contains("Fires when") ||
                          summary.Contains("Raised when") ||
                          summary.Contains("callback");

            parameters.Add(new ParameterDoc(propName, "", summary, isEvent));
        }

        return parameters.OrderBy(p => p.IsEvent).ThenBy(p => p.Name).ToList();
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
        sb.AppendLine("> A free and open-source set of 90+ native Blazor UI components including DataGrid, Scheduler, Charts, Forms, and more.");
        sb.AppendLine();
        sb.AppendLine("Radzen Blazor Components supports Blazor Server, Blazor WebAssembly, and .NET MAUI Blazor Hybrid. Built with accessibility in mind (WCAG 2.2, keyboard navigation). Available as a MIT-licensed NuGet package.");
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
        // Remove boilerplate prefixes
        var d = description;
        d = Regex.Replace(d, @"^Demonstration and configuration of the (Radzen Blazor |Blazor Radzen|Blazor |Radzen )?", "", RegexOptions.IgnoreCase);
        d = Regex.Replace(d, @"^(Use the |Use )?(Radzen Blazor |Blazor |Radzen )?", "", RegexOptions.IgnoreCase);

        // Capitalize first letter
        if (d.Length > 0)
            d = char.ToUpper(d[0]) + d[1..];

        // Ensure no trailing period for consistency
        d = d.TrimEnd('.');

        return d;
    }

    // ── Per-component .md generation ────────────────────────────────────

    static void GenerateComponentPages(List<ExampleNode> categories, string pagesPath, Dictionary<string, XmlMemberDoc> xmlDocs, string mdDir)
    {
        var allLeaves = new List<(ExampleNode Node, string CategoryName, List<string> Ancestors)>();
        foreach (var category in categories)
        {
            if (ExcludedTopLevel.Contains(category.Name))
                continue;
            CollectLeaves(category, category.Name, new List<string>(), allLeaves);
        }

        // First pass: determine the primary (API reference) page for each resolved component class.
        // The first leaf per component gets the full parameter tables; others link to it.
        var primaryPages = new Dictionary<string, (string Url, string DisplayName)>();
        foreach (var (node, categoryName, ancestors) in allLeaves)
        {
            if (string.IsNullOrEmpty(node.Path)) continue;
            var (resolvedClass, parentDisplayName) = ResolveComponentForNode(node, ancestors, xmlDocs);
            if (resolvedClass != null && !primaryPages.ContainsKey(resolvedClass))
            {
                var path = node.Path.TrimStart('/');
                primaryPages[resolvedClass] = ($"{BaseUrl}/{path}.md", parentDisplayName ?? node.Name);
            }
        }

        foreach (var (node, categoryName, ancestors) in allLeaves)
        {
            if (string.IsNullOrEmpty(node.Path))
                continue;

            var path = node.Path.TrimStart('/');
            var mdPath = Path.Combine(mdDir, $"{path}.md");

            var mdContent = GenerateSingleComponentPage(node, categoryName, ancestors, pagesPath, xmlDocs, primaryPages);
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

    static string GenerateSingleComponentPage(ExampleNode node, string categoryName, List<string> ancestors, string pagesPath, Dictionary<string, XmlMemberDoc> xmlDocs, Dictionary<string, (string Url, string DisplayName)> primaryPages)
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

        if (resolvedClass != null)
        {
            var thisUrl = $"{BaseUrl}/{node.Path.TrimStart('/')}.md";
            var isPrimary = primaryPages.TryGetValue(resolvedClass, out var primary) && primary.Url == thisUrl;

            if (isPrimary)
            {
                var typeKey = $"T:Radzen.Blazor.{resolvedClass}";
                if (xmlDocs.TryGetValue(typeKey, out var typeDoc) && !string.IsNullOrWhiteSpace(typeDoc.Summary))
                {
                    sb.AppendLine(typeDoc.Summary);
                    sb.AppendLine();
                }

                var parameters = GetComponentParameters(resolvedClass, xmlDocs);
                var regularParams = parameters.Where(p => !p.IsEvent).ToList();
                var eventParams = parameters.Where(p => p.IsEvent).ToList();

                if (regularParams.Count > 0)
                {
                    sb.AppendLine("## Parameters");
                    sb.AppendLine();
                    sb.AppendLine("| Parameter | Description |");
                    sb.AppendLine("|-----------|-------------|");
                    foreach (var p in regularParams)
                    {
                        var desc = p.Summary.Replace("|", "\\|").Replace("\n", " ");
                        sb.AppendLine($"| {p.Name} | {desc} |");
                    }
                    sb.AppendLine();
                }

                if (eventParams.Count > 0)
                {
                    sb.AppendLine("## Events");
                    sb.AppendLine();
                    sb.AppendLine("| Event | Description |");
                    sb.AppendLine("|-------|-------------|");
                    foreach (var p in eventParams)
                    {
                        var desc = p.Summary.Replace("|", "\\|").Replace("\n", " ");
                        sb.AppendLine($"| {p.Name} | {desc} |");
                    }
                    sb.AppendLine();
                }
            }
            else if (primaryPages.TryGetValue(resolvedClass, out var apiRef))
            {
                sb.AppendLine($"> API reference: [{apiRef.DisplayName}]({apiRef.Url})");
                sb.AppendLine();
            }
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
        // Try to find the matching .razor page file
        var pagePath = FindPageFile(node, pagesPath);
        if (pagePath == null || !File.Exists(pagePath))
            return "";

        var content = File.ReadAllText(pagePath, Encoding.UTF8);
        return ExtractDescriptionsAndExamples(content, pagePath);
    }

    static string FindPageFile(ExampleNode node, string pagesPath)
    {
        // Try common naming patterns
        var candidates = new List<string>();

        var path = node.Path?.TrimStart('/') ?? "";
        var name = node.Name.Replace(" ", "");

        // Pattern: {Name}Page.razor
        candidates.Add(Path.Combine(pagesPath, $"{name}Page.razor"));

        // Pattern: match by @page directive
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

        // In per-component .md files: # = component title, ## = Parameters/Events/Examples,
        // so demo headings start at ###.
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
