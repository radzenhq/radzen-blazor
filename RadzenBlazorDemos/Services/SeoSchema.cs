using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RadzenBlazorDemos
{
    // Builds schema.org JSON-LD for the chart pages. Mirrors the approved pattern in
    // www.radzen.com/Client/Shared/BreadcrumbSchema.razor: plain Dictionary<string,object>
    // graphs serialized with default System.Text.Json options (escapes < > & to \uXXXX, which
    // also prevents </script> breakout). Production URLs are hardcoded, like that component.
    public static class SeoSchema
    {
        const string BaseUrl = "https://blazor.radzen.com";

        // Returns the JSON-LD document for the page, or null when the page is not a chart page.
        public static string BuildFor(Example example, ExampleService exampleService)
        {
            if (example == null)
                return null;

            var path = example.Path?.TrimStart('/');
            var url = AbsoluteUrl(path);

            if (path == "charts")
            {
                return Serialize(new List<object>
                {
                    CollectionPage(example, url),
                    ItemList(exampleService.GetChartPages()),
                    Breadcrumb(new[] { ("Home", BaseUrl + "/"), ("Charts", url) })
                });
            }

            var chartPaths = new HashSet<string>(
                exampleService.GetChartPages().Select(p => p.Path?.TrimStart('/')),
                StringComparer.OrdinalIgnoreCase);

            if (!chartPaths.Contains(path))
                return null;

            var graph = new List<object>
            {
                TechArticle(example, exampleService, url),
                Breadcrumb(new[] { ("Home", BaseUrl + "/"), ("Charts", BaseUrl + "/charts"), (example.Name, url) })
            };

            if (example.Faq != null && example.Faq.Any())
                graph.Add(FaqPage(example));

            return Serialize(graph);
        }

        static string Serialize(List<object> graph) => JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["@context"] = "https://schema.org",
            ["@graph"] = graph
        });

        static string AbsoluteUrl(string path) => $"{BaseUrl}/{path}";

        static Dictionary<string, object> CollectionPage(Example e, string url) => new()
        {
            ["@type"] = "CollectionPage",
            ["name"] = e.Title ?? e.Name,
            ["description"] = e.Description,
            ["url"] = url
        };

        static Dictionary<string, object> ItemList(IEnumerable<Example> pages) => new()
        {
            ["@type"] = "ItemList",
            ["itemListElement"] = pages.Select((p, i) => (object)new Dictionary<string, object>
            {
                ["@type"] = "ListItem",
                ["position"] = i + 1,
                ["name"] = p.Name,
                ["item"] = AbsoluteUrl(p.Path?.TrimStart('/'))
            }).ToList()
        };

        static Dictionary<string, object> Breadcrumb(IEnumerable<(string Name, string Url)> crumbs) => new()
        {
            ["@type"] = "BreadcrumbList",
            ["itemListElement"] = crumbs.Select((c, i) => (object)new Dictionary<string, object>
            {
                ["@type"] = "ListItem",
                ["position"] = i + 1,
                ["name"] = c.Name,
                ["item"] = c.Url
            }).ToList()
        };

        static Dictionary<string, object> TechArticle(Example e, ExampleService svc, string url) => new()
        {
            ["@type"] = "TechArticle",
            ["headline"] = e.Title ?? svc.TitleFor(e),
            ["description"] = e.Description ?? svc.DescriptionFor(e),
            ["url"] = url,
            ["author"] = Organization(),
            ["publisher"] = Organization()
        };

        static Dictionary<string, object> Organization() => new()
        {
            ["@type"] = "Organization",
            ["name"] = "Radzen"
        };

        static Dictionary<string, object> FaqPage(Example e) => new()
        {
            ["@type"] = "FAQPage",
            ["mainEntity"] = e.Faq.Select(f => (object)new Dictionary<string, object>
            {
                ["@type"] = "Question",
                ["name"] = f.Question,
                ["acceptedAnswer"] = new Dictionary<string, object>
                {
                    ["@type"] = "Answer",
                    ["text"] = f.Answer
                }
            }).ToList()
        };
    }
}
