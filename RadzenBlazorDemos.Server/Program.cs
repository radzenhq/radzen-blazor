using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Radzen;
using RadzenBlazorDemos;
using RadzenBlazorDemos.Data;
using RadzenBlazorDemos.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor().AddHubOptions(o =>
{
    o.MaximumReceiveMessageSize = 10 * 1024 * 1024;
});
builder.Services.AddSingleton(sp =>
{
    // Get the address that the app is currently running at
    var server = sp.GetRequiredService<IServer>();
    var addressFeature = server.Features.Get<IServerAddressesFeature>();
    string baseAddress = addressFeature.Addresses.First();
    return new HttpClient { BaseAddress = new Uri(baseAddress) };
});

// Add Radzen.Blazor services
builder.Services.AddRadzenComponents();

// Demo services
builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<CompilerService>();
builder.Services.AddScoped<ExampleService>();

builder.Services.AddDbContextFactory<NorthwindContext>();

builder.Services.AddScoped<NorthwindService>();
builder.Services.AddScoped<NorthwindODataService>();
builder.Services.AddSingleton<GitHubService>();

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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapBlazorHub();
app.MapControllers();
app.MapFallbackToPage("/_Host");
app.Run();