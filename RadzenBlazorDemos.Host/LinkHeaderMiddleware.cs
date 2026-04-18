using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace RadzenBlazorDemos.Host;

/// <summary>
/// Emits Link response headers per RFC 8288 pointing agents to machine-readable
/// resources: the page's markdown twin (describedby), the site-wide llms.txt index
/// (describedby), the Radzen Blazor MCP manifest (service-desc), the MCP Server Card,
/// the /.well-known/api-catalog (api-catalog), and optional service-doc.
/// </summary>
public static class LinkHeaderMiddleware
{
    public static IApplicationBuilder UseLinkHeaders(this IApplicationBuilder app, IWebHostEnvironment env, string serviceDocUrl = null)
    {
        var mdRoot = Path.GetFullPath(Path.Combine(env.WebRootPath, "md"));

        app.Use(async (context, next) =>
        {
            context.Response.OnStarting(() =>
            {
                var contentType = context.Response.ContentType ?? string.Empty;
                if (!contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) &&
                    !contentType.StartsWith("text/markdown", StringComparison.OrdinalIgnoreCase))
                {
                    return Task.CompletedTask;
                }

                var path = context.Request.Path.Value ?? "/";
                var headers = context.Response.Headers;
                var links = headers.Link;

                if (!path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                {
                    var relative = path.Trim('/');
                    if (string.IsNullOrEmpty(relative))
                    {
                        relative = "index";
                    }

                    var mdPath = Path.GetFullPath(Path.Combine(mdRoot, relative + ".md"));
                    if (mdPath.StartsWith(mdRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal) &&
                        File.Exists(mdPath))
                    {
                        links = StringValues.Concat(links,
                            $"</{relative}.md>; rel=\"describedby\"; type=\"text/markdown\"");
                    }
                }

                links = StringValues.Concat(links,
                    "</llms.txt>; rel=\"describedby\"; type=\"text/plain\"; title=\"Radzen Blazor demos index\"");
                links = StringValues.Concat(links,
                    "</server.json>; rel=\"service-desc\"; type=\"application/json\"; title=\"Radzen Blazor MCP\"");
                links = StringValues.Concat(links,
                    "</.well-known/mcp/server-card.json>; rel=\"service-desc\"; type=\"application/json\"; title=\"Radzen Blazor MCP Server Card\"");
                links = StringValues.Concat(links,
                    "</.well-known/api-catalog>; rel=\"api-catalog\"; type=\"application/linkset+json\"");

                if (!string.IsNullOrEmpty(serviceDocUrl))
                {
                    links = StringValues.Concat(links,
                        $"<{serviceDocUrl}>; rel=\"service-doc\"; type=\"text/html\"");
                }

                headers.Link = links;
                return Task.CompletedTask;
            });

            await next();
        });

        return app;
    }
}
