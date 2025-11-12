using System.Text;
using System.Text.RegularExpressions;

namespace RadzenBlazorDemos.Tools;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("Usage: GenerateLlmsTxt <outputPath> <pagesPath> [servicesPath] [modelsPath]");
            return 1;
        }

        var outputPath = args[0];
        var pagesPath = args[1];
        var servicesPath = args.Length > 2 && !string.IsNullOrWhiteSpace(args[2]) ? args[2] : null;
        var modelsPath = args.Length > 3 && !string.IsNullOrWhiteSpace(args[3]) ? args[3] : null;

        if (!Directory.Exists(pagesPath))
        {
            Console.Error.WriteLine($"Pages path does not exist: {pagesPath}");
            return 1;
        }

        try
        {
            Generate(outputPath, pagesPath, servicesPath, modelsPath);
            Console.WriteLine($"Successfully generated llms.txt at: {outputPath}");
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Error generating llms.txt: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);
            return 1;
        }
    }

    static void Generate(string outputPath, string pagesPath, string servicesPath, string modelsPath)
    {
        var sb = new StringBuilder();
        
        sb.AppendLine("# Radzen Blazor Components - Demo Application");
        sb.AppendLine();
        sb.AppendLine("This file contains all demo pages and examples from the Radzen Blazor Components demo application.");
        sb.AppendLine($"Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss UTC}");
        sb.AppendLine();
        sb.AppendLine("## Table of Contents");
        sb.AppendLine();
        
        // Collect all demo files
        var allPages = Directory.GetFiles(pagesPath, "*.razor", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).StartsWith("_"))
            .ToList();
        
        // Separate main pages (with @page directive) from example components
        var mainPages = new List<string>();
        var examplePages = new List<string>();
        
        foreach (var page in allPages)
        {
            var content = File.ReadAllText(page, Encoding.UTF8);
            // Check if it has @page directive - main pages have routes
            if (Regex.IsMatch(content, @"^\s*@page\s+", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                mainPages.Add(page);
            }
            else
            {
                examplePages.Add(page);
            }
        }
        
        // Sort main pages ascending by filename
        mainPages = mainPages.OrderBy(p => Path.GetFileName(p)).ToList();
        
        // Keep all pages for content generation (sorted ascending)
        var pages = allPages.OrderBy(p => Path.GetFileName(p)).ToList();
        
        var pageCs = Directory.GetFiles(pagesPath, "*.cs", SearchOption.AllDirectories)
            .OrderBy(f => Path.GetFileName(f))
            .ToList();
        
        var services = !string.IsNullOrEmpty(servicesPath) && Directory.Exists(servicesPath)
            ? Directory.GetFiles(servicesPath, "*.cs", SearchOption.AllDirectories).OrderBy(f => Path.GetFileName(f)).ToList()
            : Enumerable.Empty<string>();
        
        var models = !string.IsNullOrEmpty(modelsPath) && Directory.Exists(modelsPath)
            ? Directory.GetFiles(modelsPath, "*.cs", SearchOption.AllDirectories).OrderBy(f => Path.GetFileName(f)).ToList()
            : Enumerable.Empty<string>();
        
        // Generate table of contents - only include main pages
        sb.AppendLine("### Demo Pages");
        foreach (var page in mainPages)
        {
            var relativePath = Path.GetRelativePath(pagesPath, page).Replace('\\', '/');
            var fileName = Path.GetFileNameWithoutExtension(page);
            sb.AppendLine($"- [{fileName}](#{SanitizeAnchor(fileName)}) - `{relativePath}`");
        }
        
        if (pageCs.Any())
        {
            sb.AppendLine();
            sb.AppendLine("### Code-Behind Files");
            foreach (var csFile in pageCs)
            {
                var fileName = Path.GetFileNameWithoutExtension(csFile);
                sb.AppendLine($"- [{fileName}](#{SanitizeAnchor(fileName)}-code-behind)");
            }
        }
        
        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine();
        
        // Add demo pages
        sb.AppendLine("## Demo Pages");
        sb.AppendLine();
        
        foreach (var page in pages)
        {
            var relativePath = Path.GetRelativePath(pagesPath, page).Replace('\\', '/');
            var fileName = Path.GetFileNameWithoutExtension(page);
            var fileContent = File.ReadAllText(page, Encoding.UTF8);
            var extractedContent = ExtractDescriptionsAndExamples(fileContent, page);
            
            if (string.IsNullOrWhiteSpace(extractedContent))
                continue;
            
            // Add explicit HTML anchor to ensure TOC links work across all markdown parsers
            var anchor = SanitizeAnchor(fileName);
            sb.AppendLine($"<a id=\"{anchor}\"></a>");
            sb.AppendLine($"### {fileName}");
            sb.AppendLine();
            sb.AppendLine($"**Path:** `{relativePath}`");
            sb.AppendLine();
            sb.AppendLine(extractedContent);
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }
        
        // Add C# code-behind files
        if (pageCs.Any())
        {
            sb.AppendLine("## Code-Behind Files");
            sb.AppendLine();
            
            foreach (var csFile in pageCs)
            {
                var relativePath = Path.GetRelativePath(pagesPath, csFile).Replace('\\', '/');
                var fileName = Path.GetFileNameWithoutExtension(csFile);
                var fileContent = File.ReadAllText(csFile, Encoding.UTF8);
                
                var codeBehindAnchor = SanitizeAnchor($"{fileName}-code-behind");
                sb.AppendLine($"<a id=\"{codeBehindAnchor}\"></a>");
                sb.AppendLine($"### {fileName} (Code-Behind)");
                sb.AppendLine();
                sb.AppendLine($"**Path:** `{relativePath}`");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine(fileContent);
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }
        }
        
        // Add services
        if (services.Any())
        {
            sb.AppendLine("## Services");
            sb.AppendLine();
            
            foreach (var service in services)
            {
                var relativePath = Path.GetRelativePath(servicesPath, service).Replace('\\', '/');
                var fileName = Path.GetFileNameWithoutExtension(service);
                var fileContent = File.ReadAllText(service, Encoding.UTF8);
                
                sb.AppendLine($"### {fileName}");
                sb.AppendLine();
                sb.AppendLine($"**Path:** `{relativePath}`");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine(fileContent);
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }
        }
        
        // Add models
        if (models.Any())
        {
            sb.AppendLine("## Data Models");
            sb.AppendLine();
            
            foreach (var model in models)
            {
                var relativePath = Path.GetRelativePath(modelsPath, model).Replace('\\', '/');
                var fileName = Path.GetFileNameWithoutExtension(model);
                var fileContent = File.ReadAllText(model, Encoding.UTF8);
                
                sb.AppendLine($"### {fileName}");
                sb.AppendLine();
                sb.AppendLine($"**Path:** `{relativePath}`");
                sb.AppendLine();
                sb.AppendLine("```csharp");
                sb.AppendLine(fileContent);
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("---");
                sb.AppendLine();
            }
        }
        
        // Write to file
        var outputDir = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDir))
        {
            Directory.CreateDirectory(outputDir);
        }
        File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
    }

    static string SanitizeAnchor(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return "";
        
        // Markdown heading anchors are generated by:
        // 1. Convert to lowercase
        // 2. Replace spaces and special chars with hyphens
        // 3. Remove multiple consecutive hyphens
        // 4. Trim hyphens from start/end
        
        var anchor = text.ToLower()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace(".", "-")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("[", "")
            .Replace("]", "")
            .Replace("{", "")
            .Replace("}", "")
            .Replace("&", "")
            .Replace(":", "")
            .Replace(";", "")
            .Replace("!", "")
            .Replace("?", "")
            .Replace("*", "")
            .Replace("/", "-")
            .Replace("\\", "-")
            .Replace("+", "-")
            .Replace("=", "-")
            .Replace("@", "")
            .Replace("#", "")
            .Replace("%", "")
            .Replace("$", "")
            .Replace("^", "")
            .Replace("~", "")
            .Replace("`", "")
            .Replace("'", "")
            .Replace("\"", "");
        
        // Remove multiple consecutive hyphens
        anchor = Regex.Replace(anchor, @"-+", "-");
        
        // Trim hyphens from start and end
        anchor = anchor.Trim('-');
        
        return anchor;
    }

    static string ExtractDescriptionsAndExamples(string razorContent, string pagePath)
    {
        var result = new StringBuilder();
        var pagesDirectory = Path.GetDirectoryName(pagePath) ?? "";
        
        // Remove @code blocks entirely
        razorContent = Regex.Replace(razorContent, 
            @"@code\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}", 
            "", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        // Remove @page, @inject, @layout directives
        razorContent = Regex.Replace(razorContent, 
            @"@(page|inject|layout|using|namespace|implements)[^\r\n]*", 
            "", 
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
        // Split content into lines to process sequentially
        var lines = razorContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Check for RadzenText
            if (line.Contains("<RadzenText"))
            {
                var textContent = ExtractRadzenTextContent(lines, ref i);
                if (!string.IsNullOrWhiteSpace(textContent.Content))
                {
                    // Format as heading if it's a heading style
                    if (textContent.IsHeading)
                    {
                        result.AppendLine();
                        result.AppendLine(textContent.Content);
                        result.AppendLine();
                    }
                    else
                    {
                        result.AppendLine(textContent.Content);
                    }
                }
            }
            // Check for RadzenExample
            else if (line.Contains("<RadzenExample"))
            {
                var exampleContent = ExtractRadzenExampleContent(lines, ref i, pagesDirectory);
                if (!string.IsNullOrWhiteSpace(exampleContent))
                {
                    result.AppendLine();
                    result.AppendLine("Example:");
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
        
        // Collect the complete RadzenText tag (may span multiple lines)
        for (int i = index; i < lines.Length; i++)
        {
            var line = lines[i];
            fullTag.AppendLine(line);
            
            // Count opening and closing tags
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
        
        // Extract attributes - check both TextStyle and TagName for heading detection
        var textStyleMatch = Regex.Match(tagContent, @"TextStyle=""TextStyle\.(H[2-6])""", RegexOptions.IgnoreCase);
        var tagNameMatch = Regex.Match(tagContent, @"TagName=""TagName\.(H[1-6])""", RegexOptions.IgnoreCase);
        
        bool isHeading = false;
        int headingLevel = 0;
        
        // Prefer TextStyle for heading level, but also check TagName
        if (textStyleMatch.Success)
        {
            var hMatch = Regex.Match(textStyleMatch.Groups[1].Value, @"H(\d)");
            if (hMatch.Success)
            {
                headingLevel = int.Parse(hMatch.Groups[1].Value);
                isHeading = headingLevel >= 2 && headingLevel <= 6;
            }
        }
        
        // If no TextStyle heading but TagName suggests heading, use that
        if (!isHeading && tagNameMatch.Success)
        {
            var hMatch = Regex.Match(tagNameMatch.Groups[1].Value, @"H(\d)");
            if (hMatch.Success)
            {
                var tagLevel = int.Parse(hMatch.Groups[1].Value);
                if (tagLevel >= 2 && tagLevel <= 6)
                {
                    headingLevel = tagLevel;
                    isHeading = true;
                }
            }
        }
        
        // Extract inner content
        var contentMatch = Regex.Match(tagContent, @"<RadzenText[^>]*>([\s\S]*?)</RadzenText>", RegexOptions.IgnoreCase);
        if (!contentMatch.Success)
            return (string.Empty, false);
        
        var content = contentMatch.Groups[1].Value.Trim();
        
        // Convert <code> tags to markdown inline code BEFORE converting links
        content = ConvertCodeTagsToMarkdown(content);
        
        // Convert RadzenLink to markdown links BEFORE removing HTML tags
        content = ConvertRadzenLinksToMarkdown(content);
        
        // Remove all HTML tags (except markdown links which are already converted)
        content = Regex.Replace(content, @"<[^>]+>", "");
        
        // Remove @ expressions
        content = Regex.Replace(content, @"@[A-Za-z0-9_.()]+", "");
        
        // Clean up whitespace
        content = Regex.Replace(content, @"\s+", " ").Trim();
        
        // Format as heading if needed
        if (isHeading && !string.IsNullOrWhiteSpace(content))
        {
            // Map heading levels: H2 -> ####, H4 -> ####, H5 -> #####, H6 -> ######
            int markdownLevel = 4; // Default to ####
            if (headingLevel == 4) markdownLevel = 4; // H4 -> ####
            else if (headingLevel == 5) markdownLevel = 5; // H5 -> #####
            else if (headingLevel == 6) markdownLevel = 6; // H6 -> ######
            else if (headingLevel == 2) markdownLevel = 4; // H2 -> ####
            
            content = new string('#', markdownLevel) + " " + content;
        }
        
        return (content, isHeading);
    }

    static string ConvertCodeTagsToMarkdown(string content)
    {
        // Match <code>...</code> tags
        var codeMatches = Regex.Matches(content, 
            @"<code>([\s\S]*?)</code>", 
            RegexOptions.IgnoreCase);
        
        // Process in reverse to maintain indices
        var matchesArray = new Match[codeMatches.Count];
        for (int i = 0; i < codeMatches.Count; i++)
        {
            matchesArray[i] = codeMatches[i];
        }
        for (int i = matchesArray.Length - 1; i >= 0; i--)
        {
            var match = matchesArray[i];
            var codeContent = match.Groups[1].Value.Trim();
            
            // Remove nested HTML tags from code content
            codeContent = Regex.Replace(codeContent, @"<[^>]+>", "");
            
            // Clean up @("@bind-Selected") to @bind-Selected (remove extra quotes and parentheses)
            // Match: @("...") pattern and extract the content inside quotes
            // Pattern: @("@bind-Selected") -> @bind-Selected
            // Use non-verbatim string to avoid quote escaping issues
            codeContent = Regex.Replace(codeContent, "@\\(\"([^\"]+)\"\\)", "$1");
            codeContent = Regex.Replace(codeContent, "@\\('([^']+)'\\)", "$1");
            
            if (!string.IsNullOrWhiteSpace(codeContent))
            {
                var markdownCode = $"`{codeContent}`";
                content = content.Substring(0, match.Index) + markdownCode + 
                         content.Substring(match.Index + match.Length);
            }
        }
        
        return content;
    }

    static string ConvertRadzenLinksToMarkdown(string content)
    {
        // Handle both self-closing and opening/closing RadzenLink tags
        // Match the entire tag first, then extract attributes
        
        // Pattern for self-closing: <RadzenLink ... />
        var selfClosingPattern = @"<RadzenLink([^>]*?)\s*/>";
        // Pattern for opening/closing: <RadzenLink ...>...</RadzenLink>
        var openClosePattern = @"<RadzenLink([^>]*?)>([\s\S]*?)</RadzenLink>";
        
        // Process self-closing tags
        var selfClosingMatches = Regex.Matches(content, selfClosingPattern, RegexOptions.IgnoreCase);
        var selfClosingArray = new Match[selfClosingMatches.Count];
        for (int i = 0; i < selfClosingMatches.Count; i++)
        {
            selfClosingArray[i] = selfClosingMatches[i];
        }
        for (int i = selfClosingArray.Length - 1; i >= 0; i--)
        {
            var match = selfClosingArray[i];
            var attributes = match.Groups[1].Value;
            var (path, text) = ExtractLinkAttributes(attributes, "");
            
            if (!string.IsNullOrWhiteSpace(path))
            {
                string linkText = !string.IsNullOrWhiteSpace(text) ? text : path;
                var markdownLink = $"[{linkText}]({path})";
                content = content.Substring(0, match.Index) + markdownLink + 
                         content.Substring(match.Index + match.Length);
            }
        }
        
        // Process opening/closing tags
        var linkMatches = Regex.Matches(content, openClosePattern, RegexOptions.IgnoreCase);
        var matchesArray = new Match[linkMatches.Count];
        for (int i = 0; i < linkMatches.Count; i++)
        {
            matchesArray[i] = linkMatches[i];
        }
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
                content = content.Substring(0, match.Index) + markdownLink + 
                         content.Substring(match.Index + match.Length);
            }
        }
        
        return content;
    }

    static (string Path, string Text) ExtractLinkAttributes(string attributes, string innerContent)
    {
        string path = "";
        string text = "";
        
        // Extract Path attribute (can be in any order)
        var pathMatch = Regex.Match(attributes, @"Path=[""]?([^""\s>]+)[""]?", RegexOptions.IgnoreCase);
        if (pathMatch.Success)
        {
            path = pathMatch.Groups[1].Value;
        }
        
        // Extract Text attribute
        var textMatch = Regex.Match(attributes, @"Text=[""]?([^""]+)[""]?", RegexOptions.IgnoreCase);
        if (textMatch.Success)
        {
            text = textMatch.Groups[1].Value.Trim();
        }
        
        // If no Text attribute and there's inner content, use that
        if (string.IsNullOrWhiteSpace(text) && !string.IsNullOrWhiteSpace(innerContent))
        {
            text = Regex.Replace(innerContent, @"<[^>]+>", "").Trim();
        }
        
        return (path, text);
    }

    static string ExtractRadzenExampleContent(string[] lines, ref int index, string pagesDirectory)
    {
        var fullTag = new StringBuilder();
        var depth = 0;
        
        // Collect the complete RadzenExample tag
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
        
        // Extract Example attribute
        var exampleMatch = Regex.Match(tagContent, @"Example=[""]?([^""\s>]+)[""]?", RegexOptions.IgnoreCase);
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
        
        // Fallback: extract inline content
        var inlineMatch = Regex.Match(tagContent, @"<RadzenExample[^>]*>([\s\S]*?)</RadzenExample>", RegexOptions.IgnoreCase);
        if (inlineMatch.Success)
        {
            return CleanExampleContent(inlineMatch.Groups[1].Value);
        }
        
        return string.Empty;
    }

    static string CleanTextContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;
        
        var result = content;
        
        // Remove all HTML tags (including <strong>, <code>, <Radzen*>, etc.)
        result = Regex.Replace(result, @"<[^>]+>", "");
        
        // Remove @ expressions (like @variable, @ExampleService)
        result = Regex.Replace(result, @"@[A-Za-z0-9_.()]+", "");
        
        // Clean up multiple spaces but preserve single newlines for readability
        result = Regex.Replace(result, @"[ \t]+", " ");
        
        // Clean up multiple newlines (max 2 consecutive)
        result = Regex.Replace(result, @"(\r?\n){3,}", Environment.NewLine + Environment.NewLine);
        
        return result.Trim();
    }

    static string CleanExampleContent(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;
        
        var result = content;
        
        // Remove nested RadzenExample tags if any
        result = Regex.Replace(result, 
            @"<RadzenExample[^>]*>[\s\S]*?</RadzenExample>", 
            "", 
            RegexOptions.IgnoreCase);
        
        // Trim each line and remove empty lines
        var lines = result.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None)
            .Select(l => l.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToList();
        
        return string.Join(Environment.NewLine, lines);
    }

    static string CleanExampleFile(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;
        
        var result = content;
        
        // Remove @code blocks
        result = Regex.Replace(result, 
            @"@code\s*\{[^{}]*(?:\{[^{}]*\}[^{}]*)*\}", 
            "", 
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        
        // Remove @using, @inject, @page directives
        result = Regex.Replace(result, 
            @"@(using|inject|page|layout|namespace|implements)[^\r\n]*", 
            "", 
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
        // Clean up multiple blank lines
        result = Regex.Replace(result, @"(\r?\n\s*){3,}", Environment.NewLine + Environment.NewLine);
        
        return result.Trim();
    }
}

