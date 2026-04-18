using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace RadzenBlazorDemos.Host;

/// <summary>
/// Serves the markdown version of a page when the request includes Accept: text/markdown.
/// See https://llmstxt.org/ and Cloudflare's Markdown for Agents specification.
/// </summary>
public static class MarkdownNegotiationMiddleware
{
    public static IApplicationBuilder UseMarkdownNegotiation(this IApplicationBuilder app, IWebHostEnvironment env)
    {
        var mdRoot = Path.GetFullPath(Path.Combine(env.WebRootPath, "md"));

        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value ?? "/";

            if (!WantsMarkdown(context.Request.Headers.Accept) ||
                path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            {
                AddVaryAccept(context.Response.Headers);
                await next();
                return;
            }

            var relative = path.Trim('/');
            if (string.IsNullOrEmpty(relative))
            {
                relative = "index";
            }

            var fullPath = Path.GetFullPath(Path.Combine(mdRoot, relative + ".md"));
            if (!fullPath.StartsWith(mdRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal) ||
                !File.Exists(fullPath))
            {
                AddVaryAccept(context.Response.Headers);
                await next();
                return;
            }

            var content = await File.ReadAllBytesAsync(fullPath, context.RequestAborted);

            var response = context.Response;
            response.ContentType = "text/markdown; charset=utf-8";
            response.ContentLength = content.Length;
            AddVaryAccept(response.Headers);
            response.Headers["X-Robots-Tag"] = "noindex, nofollow";
            response.Headers["x-markdown-tokens"] = EstimateTokens(content.Length).ToString();

            await response.Body.WriteAsync(content, context.RequestAborted);
        });

        return app;
    }

    private static bool WantsMarkdown(StringValues accept)
    {
        foreach (var value in accept)
        {
            if (value is null)
            {
                continue;
            }

            foreach (var part in value.Split(','))
            {
                var mime = part.Split(';')[0].Trim();
                if (mime.Equals("text/markdown", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    // Rough heuristic: ~4 UTF-8 bytes per token for English prose.
    private static int EstimateTokens(int byteLength) => Math.Max(1, byteLength / 4);

    private static void AddVaryAccept(IHeaderDictionary headers)
    {
        var current = headers.Vary;
        foreach (var value in current)
        {
            if (value is null)
            {
                continue;
            }

            foreach (var part in value.Split(','))
            {
                if (part.Trim().Equals("Accept", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }
        }

        headers.Append("Vary", "Accept");
    }
}
