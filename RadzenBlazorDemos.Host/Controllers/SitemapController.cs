using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Components;
using BlazorRouteAttribute = Microsoft.AspNetCore.Components.RouteAttribute;
using System.Text.RegularExpressions;

namespace RadzenBlazorDemos;

public class SitemapController : Controller
{
    string BaseUrl => $"{Request.Scheme}://{Request.Host}";

    [HttpGet("robots.txt")]
    public IActionResult Robots()
    {
        var sb = new StringBuilder();

        sb.AppendLine("User-agent: *");
        sb.AppendLine($"Sitemap: {BaseUrl}/sitemap.xml");

        return Content(sb.ToString(), "text/plain");
    }

    [HttpGet("sitemap.xml")]
    public IActionResult Sitemap()
    {
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        sb.AppendLine("<urlset xmlns=\"http://www.sitemaps.org/schemas/sitemap/0.9\"");
        sb.AppendLine("        xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
        sb.AppendLine("        xsi:schemaLocation=\"http://www.sitemaps.org/schemas/sitemap/0.9 http://www.sitemaps.org/schemas/sitemap/0.9/sitemap.xsd\">");

        var pages = GetBlazorPages();
        foreach (var page in pages)
        {
            sb.AppendLine("  <url>");
            sb.AppendLine($"    <loc>{BaseUrl}{page}</loc>");
            sb.AppendLine("    <changefreq>weekly</changefreq>");
            sb.AppendLine("    <priority>0.8</priority>");
            sb.AppendLine("  </url>");
        }

        sb.AppendLine("</urlset>");

        return Content(sb.ToString(), "application/xml");
    }


    private IEnumerable<string> GetBlazorPages(string namespaceFilter = null)
    {
        var assembly = typeof(RadzenBlazorDemos.Pages.Index).Assembly;

        var pageTypes = assembly
            .ExportedTypes
            .Where(t => typeof(ComponentBase).IsAssignableFrom(t))
            .Select(t => new { Type = t, Route = t.GetCustomAttributes<BlazorRouteAttribute>().FirstOrDefault()?.Template })
            .Where(x => x.Route is not null)
            .Select(x => Regex.Replace(x.Route, "{[^}]+}", "").TrimEnd('/'));

        return pageTypes.Distinct().OrderBy(x => x);
    }
}