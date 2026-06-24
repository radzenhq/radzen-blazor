using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace RadzenBlazorDemos
{
    // Builds schema.org JSON-LD for the chart and DataGrid pages. Mirrors the approved pattern in
    // www.radzen.com/Client/Shared/BreadcrumbSchema.razor: plain Dictionary<string,object> graphs
    // serialized with default System.Text.Json options (escapes < > & to \uXXXX, which also prevents
    // </script> breakout). Production URLs are hardcoded, like that component.
    public static class SeoSchema
    {
        const string BaseUrl = "https://blazor.radzen.com";

        // Standalone pages (not part of a cluster) that have been individually SEO-optimized.
        // Each gets TechArticle + BreadcrumbList (Home > Page) keyed on the example's own name.
        static readonly HashSet<string> StandalonePages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "scheduler",
            "icon",
            "dashboard",
            "themes",
            "menu",
            "tabs",
            "accordion",
            "contextmenu",
            "breadcrumb",
            "panelmenu",
            "login",
            "profile-menu",
            "carousel",
            "steps",
            "link",
            "toc",
            "dialog",
            "layout",
            "card",
            "dropzone",
            "popup",
            "splitter",
            "card-group",
            "panel",
            "row",
            "stack",
            "column",
            "tile-layout",
            "markdown",
        };

        // Returns the JSON-LD document for the page, or null when the page is not in a covered cluster.
        public static string BuildFor(Example example, ExampleService exampleService)
        {
            if (example == null)
            {
                return null;
            }

            var path = example.Path?.TrimStart('/');
            var url = AbsoluteUrl(path);

            // Charts gallery hub: CollectionPage + ItemList of every chart page.
            if (path == "charts")
            {
                return Serialize(new List<object>
                {
                    CollectionPage(example, url),
                    ItemList(exampleService.GetChartPages()),
                    Breadcrumb(new[] { ("Home", BaseUrl + "/"), ("Charts", url) })
                });
            }

            if (InCluster(exampleService.GetChartPages().Concat(exampleService.GetChartConfigPages()), path))
            {
                return ArticleGraph(example, url, exampleService, "Charts", "charts");
            }

            if (InCluster(exampleService.GetDataGridPages(), path))
            {
                return ArticleGraph(example, url, exampleService, "DataGrid", "datagrid");
            }

            if (InCluster(exampleService.GetPivotDataGridPages(), path))
            {
                return ArticleGraph(example, url, exampleService, "Pivot DataGrid", "pivot-data-grid");
            }

            if (InCluster(exampleService.GetSpreadsheetPages(), path))
            {
                return ArticleGraph(example, url, exampleService, "Spreadsheet", "spreadsheet");
            }

            // Forms has no single hub; each component is its own breadcrumb root.
            if (path != null && exampleService.GetFormsComponentHubs().TryGetValue(path, out var formsHub))
            {
                return ArticleGraph(example, url, exampleService, formsHub.Label, formsHub.Path);
            }

            // Standalone single-page components (scheduler, etc.): Home > Page.
            if (path != null && StandalonePages.Contains(path))
            {
                return ArticleGraph(example, url, exampleService, example.Name, path);
            }

            return null;
        }

        static bool InCluster(IEnumerable<Example> pages, string path)
        {
            var paths = new HashSet<string>(pages.Select(p => p.Path?.TrimStart('/')), StringComparer.OrdinalIgnoreCase);

            return paths.Contains(path);
        }

        // TechArticle + BreadcrumbList (+ FAQPage) under a cluster hub (Home > {hub} > Page).
        // The hub page itself gets only Home > {hub}.
        static string ArticleGraph(Example example, string url, ExampleService exampleService, string hubLabel, string hubPath)
        {
            var crumbs = new List<(string, string)> { ("Home", BaseUrl + "/"), (hubLabel, AbsoluteUrl(hubPath)) };

            if (!string.Equals(example.Path?.TrimStart('/'), hubPath, StringComparison.OrdinalIgnoreCase))
            {
                crumbs.Add((example.Name, url));
            }

            var graph = new List<object> { TechArticle(example, exampleService, url), Breadcrumb(crumbs) };

            if (example.Faq != null && example.Faq.Any())
            {
                graph.Add(FaqPage(example));
            }

            return Serialize(graph);
        }

        static string Serialize(List<object> graph)
        {
            return JsonSerializer.Serialize(new Dictionary<string, object>
            {
                ["@context"] = "https://schema.org",
                ["@graph"] = graph
            });
        }

        static string AbsoluteUrl(string path)
        {
            return $"{BaseUrl}/{path}";
        }

        static Dictionary<string, object> CollectionPage(Example example, string url)
        {
            return new Dictionary<string, object>
            {
                ["@type"] = "CollectionPage",
                ["name"] = example.Title ?? example.Name,
                ["description"] = example.Description,
                ["url"] = url
            };
        }

        static Dictionary<string, object> ItemList(IEnumerable<Example> pages)
        {
            return new Dictionary<string, object>
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
        }

        static Dictionary<string, object> Breadcrumb(IEnumerable<(string Name, string Url)> crumbs)
        {
            return new Dictionary<string, object>
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
        }

        static Dictionary<string, object> TechArticle(Example example, ExampleService exampleService, string url)
        {
            return new Dictionary<string, object>
            {
                ["@type"] = "TechArticle",
                ["headline"] = example.Title ?? exampleService.TitleFor(example),
                ["description"] = example.Description ?? exampleService.DescriptionFor(example),
                ["url"] = url,
                ["author"] = Organization(),
                ["publisher"] = Organization()
            };
        }

        static Dictionary<string, object> Organization()
        {
            return new Dictionary<string, object>
            {
                ["@type"] = "Organization",
                ["name"] = "Radzen"
            };
        }

        static Dictionary<string, object> FaqPage(Example example)
        {
            return new Dictionary<string, object>
            {
                ["@type"] = "FAQPage",
                ["mainEntity"] = example.Faq.Select(f => (object)new Dictionary<string, object>
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
}
