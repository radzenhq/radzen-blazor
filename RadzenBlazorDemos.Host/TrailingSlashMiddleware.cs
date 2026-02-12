using Microsoft.AspNetCore.Builder;

namespace RadzenBlazorDemos.Host;

/// <summary>
/// Middleware that removes trailing slashes from URLs with a 301 permanent redirect.
/// This prevents duplicate content issues where both /pricing and /pricing/ serve the same page.
/// </summary>
public static class TrailingSlashMiddleware
{
    public static IApplicationBuilder UseTrailingSlashRedirect(this IApplicationBuilder app)
    {
        app.Use(async (context, next) =>
        {
            var path = context.Request.Path.Value;

            if (path != "/" && path?.EndsWith('/') == true)
            {
                var newPath = path.TrimEnd('/') + context.Request.QueryString;

                context.Response.StatusCode = 301;
                context.Response.Headers.Location = newPath;
                context.Response.Headers.CacheControl = "public, max-age=86400";
                return;
            }

            await next();
        });

        return app;
    }
}
