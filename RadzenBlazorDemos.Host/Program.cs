using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Routing;
using System.IO;
using Radzen;
using RadzenBlazorDemos;
using RadzenBlazorDemos.Data;
using RadzenBlazorDemos.Host;
using RadzenBlazorDemos.Services;
using RadzenBlazorDemos.Host.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);
// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();
builder.Services.AddSingleton(sp =>
{
    // Get the address that the app is currently running at
    var server = sp.GetRequiredService<IServer>();
    var addressFeature = server.Features.Get<IServerAddressesFeature>();
    var baseAddress = new Uri(addressFeature.Addresses.First());
    // Wildcard binds (0.0.0.0, ::, +) are valid listen addresses but cannot be used as
    // connection targets, so substitute a loopback host for the HttpClient base address.
    if (baseAddress.Host is "0.0.0.0" or "::" or "[::]" or "+" or "*")
    {
        baseAddress = new UriBuilder(baseAddress) { Host = "localhost" }.Uri;
    }
    return new HttpClient { BaseAddress = baseAddress };
});
builder.Services.AddDistributedMemoryCache();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = long.MaxValue;
});

// Add Radzen.Blazor services
builder.Services.AddRadzenComponents();
builder.Services.AddRadzenQueryStringThemeService();

// Demo services
builder.Services.AddScoped<CompilerService>();
builder.Services.AddScoped<ExampleService>();

builder.Services.AddDbContextFactory<NorthwindContext>();

builder.Services.AddScoped<NorthwindService>();
builder.Services.AddScoped<NorthwindODataService>();
builder.Services.AddSingleton<GitHubService>();

builder.Services.AddAIChatService(options =>
    builder.Configuration.GetSection("AIChatService").Bind(options));

builder.Services.Configure<PlaygroundOptions>(builder.Configuration.GetSection("Playground"));
builder.Services.AddSingleton<PlaygroundService>();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
});

builder.Services.AddLocalization();

/* --> Uncomment to enable localization
var supportedCultures = new[]
{
    new System.Globalization.CultureInfo("de-DE"),
};

builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("de-DE");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});
*/

var app = builder.Build();
var forwardingOptions = new ForwardedHeadersOptions()
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};
forwardingOptions.KnownIPNetworks.Clear();
forwardingOptions.KnownProxies.Clear();

app.UseForwardedHeaders(forwardingOptions);

// Content Security Policy
var relaxedCspPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    "/datagrid-rowreorder",
    "/datagrid-rowdragbetween",
    "/datagrid-rowdrag-scheduler",
    "/tree-dragdrop",
};

var baseCsp = string.Join("; ",
    "base-uri 'self'",
    "default-src 'self' http://localhost:* ws://localhost:*",
    "connect-src 'self' https: wss: http://localhost:* ws://localhost:*",
    "img-src data: https:",
    "object-src 'none'",
    "script-src 'self' 'unsafe-eval' 'wasm-unsafe-eval' http://localhost:* cdnjs.cloudflare.com cdn.syndication.twimg.com platform.linkedin.com www.linkedin.com analytics.radzen.com maps.googleapis.com unpkg.com",
    "style-src 'self' 'unsafe-inline' cdnjs.cloudflare.com maps.googleapis.com fonts.googleapis.com fonts.gstatic.com",
    "font-src 'self' data: cdnjs.cloudflare.com maps.googleapis.com fonts.googleapis.com fonts.gstatic.com",
    "frame-src www.youtube.com platform.twitter.com platform.linkedin.com www.linkedin.com",
    "worker-src 'self' blob:",
    "upgrade-insecure-requests");

var relaxedCsp = string.Join("; ",
    "base-uri 'self'",
    "default-src 'self' http://localhost:* ws://localhost:*",
    "connect-src 'self' https: wss: http://localhost:* ws://localhost:*",
    "img-src data: https:",
    "object-src 'none'",
    "script-src 'self' 'unsafe-inline' 'unsafe-eval' 'wasm-unsafe-eval' http://localhost:* cdnjs.cloudflare.com cdn.syndication.twimg.com platform.linkedin.com www.linkedin.com analytics.radzen.com maps.googleapis.com unpkg.com",
    "style-src 'self' 'unsafe-inline' cdnjs.cloudflare.com maps.googleapis.com fonts.googleapis.com fonts.gstatic.com",
    "font-src 'self' data: cdnjs.cloudflare.com maps.googleapis.com fonts.googleapis.com fonts.gstatic.com",
    "frame-src www.youtube.com platform.twitter.com platform.linkedin.com www.linkedin.com",
    "worker-src 'self' blob:",
    "upgrade-insecure-requests");

app.Use(async (context, next) =>
{
    var csp = relaxedCspPaths.Contains(context.Request.Path.Value) ? relaxedCsp : baseCsp;
    context.Response.Headers["Content-Security-Policy"] = csp;
    await next();
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

/* --> Uncomment to enable localization
var supportedCultures = new[]
{
    new System.Globalization.CultureInfo("de-DE"),
};

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("de-DE"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});
*/
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    if (context.Request.Path.Equals("/.well-known/api-catalog", StringComparison.OrdinalIgnoreCase))
    {
        var filePath = Path.Combine(app.Environment.WebRootPath, ".well-known", "api-catalog");
        if (File.Exists(filePath))
        {
            context.Response.ContentType = "application/linkset+json";
            context.Response.Headers.AccessControlAllowOrigin = "*";
            await context.Response.SendFileAsync(filePath);
            return;
        }
    }
    await next();
});

app.MapStaticAssets();
if (!app.Environment.IsDevelopment())
{
    app.UseStaticFiles(new StaticFileOptions {
        FileProvider = new PhysicalFileProvider(
            Path.Combine(app.Environment.WebRootPath, "demos")),
        RequestPath = "/demos"
    });

    app.UseStaticFiles(new StaticFileOptions {
        FileProvider = new PhysicalFileProvider(
            Path.Combine(app.Environment.WebRootPath)),
        RequestPath = "/images"
    });
}

var contentTypeProvider = new FileExtensionContentTypeProvider(new Dictionary<string, string>
{
    [".txt"] = "text/plain; charset=utf-8",
    [".md"] = "text/markdown; charset=utf-8"
});

app.UseLinkHeaders(app.Environment);
app.UseMarkdownNegotiation(app.Environment);

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    OnPrepareResponse = ctx =>
    {
        if (ctx.File.Name == "llms.txt")
        {
            ctx.Context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
        }
    }
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(app.Environment.WebRootPath, "md")),
    ContentTypeProvider = contentTypeProvider,
    OnPrepareResponse = ctx =>
    {
        ctx.Context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow";
    }
});

app.UseCanonicalRedirects();
app.UseTrailingSlashRedirect();
app.UseAntiforgery();
app.MapRazorPages();
app.MapRazorComponents<RadzenBlazorDemos.Host.App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(RadzenBlazorDemos.Routes).Assembly, typeof(Radzen.Blazor.Api.ApiLayout).Assembly)
    .Add(e =>
   {
       if (e.Metadata.Any(m => m is HttpMethodMetadata http && http.HttpMethods.Contains(HttpMethods.Get)))
       {
           e.Metadata.Add(new HttpMethodMetadata([HttpMethods.Get, HttpMethods.Head]));
       }
   });
app.MapControllers();
app.MapGet("/api/config/googlemaps", (IConfiguration config) =>
    Results.Ok(new { ApiKey = config["GoogleMaps:ApiKey"] ?? "" }));
app.Run();