using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Options;
using Radzen;
using RadzenBlazorDemos;
using RadzenBlazorDemos.Data;
using RadzenBlazorDemos.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents().AddHubOptions(o =>
{
    o.MaximumReceiveMessageSize = 10 * 1024 * 1024;
});
builder.Services.AddSingleton(sp =>
{
    // Get the address that the app is currently running at
    var server = sp.GetRequiredService<IServer>();
    var addressFeature = server.Features.Get<IServerAddressesFeature>();
    string baseAddress = addressFeature != null ? addressFeature.Addresses.First() : string.Empty;
    return new HttpClient { BaseAddress = new Uri(baseAddress) };
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
    "script-src 'self' http://localhost:* cdnjs.cloudflare.com cdn.syndication.twimg.com platform.linkedin.com www.linkedin.com analytics.radzen.com maps.googleapis.com unpkg.com",
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
    "script-src 'self' 'unsafe-inline' 'unsafe-eval' http://localhost:* cdnjs.cloudflare.com cdn.syndication.twimg.com platform.linkedin.com www.linkedin.com analytics.radzen.com maps.googleapis.com unpkg.com",
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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

/* --> Uncomment to enable localization
app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("de-DE"),
    SupportedCultures = supportedCultures,
    SupportedUICultures = supportedCultures
});
*/
app.UseStatusCodePagesWithReExecute("/not-found");
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();
app.MapRazorPages();
app.MapRazorComponents<RadzenBlazorDemos.Server.App>()
    .AddInteractiveServerRenderMode().AddAdditionalAssemblies(typeof(RadzenBlazorDemos.Routes).Assembly);
app.MapControllers();

app.Run();