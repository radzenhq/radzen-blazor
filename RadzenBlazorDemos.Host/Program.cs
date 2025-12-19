using System;
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
using System.IO;
using Radzen;
using RadzenBlazorDemos;
using RadzenBlazorDemos.Data;
using RadzenBlazorDemos.Services;
using RadzenBlazorDemos.Host.Services;
using Microsoft.AspNetCore.Http;

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
    string baseAddress = addressFeature.Addresses.First();
    return new HttpClient { BaseAddress = new Uri(baseAddress) };
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
app.UseDefaultFiles();
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

app.UseRouting();
app.UseAntiforgery();
app.MapGet("/llms.txt", () =>
{
    var path = Path.Combine(app.Environment.WebRootPath, "llms.txt");

    return File.Exists(path)
        ? Results.File(path, "text/plain")
        : Results.NotFound();
});
app.MapRazorPages();
app.MapRazorComponents<RadzenBlazorDemos.Host.App>()
    .AddInteractiveWebAssemblyRenderMode().AddAdditionalAssemblies(typeof(RadzenBlazorDemos.Routes).Assembly);
app.MapControllers();
app.Run();