using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using RadzenBlazorDemos.Services;
using Radzen;
using RadzenBlazorDemos.Data;
using RadzenBlazorDemos;
using Microsoft.Extensions.Configuration;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddDbContextFactory<NorthwindContext>();

builder.Services.AddRadzenComponents();
builder.Services.AddRadzenQueryStringThemeService();

builder.Services.AddScoped<CompilerService>();
builder.Services.AddScoped<ExampleService>();
builder.Services.AddScoped<NorthwindService>();
builder.Services.AddScoped<NorthwindODataService>();
builder.Services.AddSingleton<GitHubService>();

builder.Services.AddAIChatService(options =>
{
    options.Proxy = "api/chat/completions";
    options.Model = "@cf/meta/llama-3.1-8b-instruct";
    options.SystemPrompt = "You are a helpful AI code assistant.";
    options.Temperature = 0.7;
});

await builder.Build().RunAsync();