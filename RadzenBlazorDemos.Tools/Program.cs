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
        var pages = Directory.GetFiles(pagesPath, "*.razor", SearchOption.AllDirectories)
            .Where(f => !Path.GetFileName(f).StartsWith("_"))
            .OrderBy(f => f)
            .ToList();
        
        var pageCs = Directory.GetFiles(pagesPath, "*.cs", SearchOption.AllDirectories)
            .OrderBy(f => f)
            .ToList();
        
        var services = !string.IsNullOrEmpty(servicesPath) && Directory.Exists(servicesPath)
            ? Directory.GetFiles(servicesPath, "*.cs", SearchOption.AllDirectories).OrderBy(f => f).ToList()
            : Enumerable.Empty<string>();
        
        var models = !string.IsNullOrEmpty(modelsPath) && Directory.Exists(modelsPath)
            ? Directory.GetFiles(modelsPath, "*.cs", SearchOption.AllDirectories).OrderBy(f => f).ToList()
            : Enumerable.Empty<string>();
        
        // Generate table of contents
        sb.AppendLine("### Demo Pages");
        foreach (var page in pages)
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
        return text.ToLower()
            .Replace(" ", "-")
            .Replace("_", "-")
            .Replace(".", "-")
            .Replace("(", "")
            .Replace(")", "");
    }

    static string ExtractDescriptionsAndExamples(string razorContent, string pagePath)
    {
        var result = new StringBuilder();
        var pagesDirectory = Path.GetDirectoryName(pagePath) ?? "";
        
        // Extract descriptions from RadzenText components (preserve HTML formatting)
        var textMatches = Regex.Matches(razorContent, 
            @"<RadzenText[^>]*>([\s\S]*?)</RadzenText>", 
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
        foreach (Match match in textMatches)
        {
            var textContent = match.Groups[1].Value.Trim();
            if (!string.IsNullOrWhiteSpace(textContent))
            {
                // Keep HTML tags like <strong>, <code> but remove RadzenText wrapper
                textContent = CleanTextContent(textContent);
                if (!string.IsNullOrWhiteSpace(textContent))
                {
                    result.AppendLine(textContent);
                    result.AppendLine();
                }
            }
        }
        
        // Extract component example references from RadzenExample components
        var exampleMatches = Regex.Matches(razorContent, 
            @"<RadzenExample[^>]*Example=[""]?([^""\s>]+)[""]?[^>]*>([\s\S]*?)</RadzenExample>", 
            RegexOptions.IgnoreCase | RegexOptions.Multiline);
        
        foreach (Match match in exampleMatches)
        {
            var exampleName = match.Groups[1].Value.Trim();
            var inlineContent = match.Groups[2].Value.Trim();
            
            // Try to load the example component file
            var exampleFilePath = Path.Combine(pagesDirectory, $"{exampleName}.razor");
            if (File.Exists(exampleFilePath))
            {
                var exampleContent = File.ReadAllText(exampleFilePath, Encoding.UTF8);
                // Remove @code blocks and directives, keep just the component markup
                exampleContent = CleanExampleFile(exampleContent);
                
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
            else if (!string.IsNullOrWhiteSpace(inlineContent))
            {
                // Fallback: use inline content if example file not found
                inlineContent = CleanExampleContent(inlineContent);
                if (!string.IsNullOrWhiteSpace(inlineContent))
                {
                    result.AppendLine();
                    result.AppendLine("Example:");
                    result.AppendLine("```razor");
                    result.AppendLine(inlineContent);
                    result.AppendLine("```");
                    result.AppendLine();
                }
            }
        }
        
        return result.ToString().Trim();
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

