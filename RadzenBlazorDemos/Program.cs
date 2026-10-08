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

builder.Services.AddScoped<ILocalizer, DemoLocalizer>();
builder.Services.AddRadzenComponents();
builder.Services.AddRadzenQueryStringThemeService();

builder.Services.AddScoped<ExampleService>();
builder.Services.AddScoped<NorthwindODataService>();
builder.Services.AddSingleton<GitHubService>();

builder.Services.AddAIChatService(options =>
{
    options.Proxy = "api/chat/completions";
    options.EmbeddingsProxy = "api/chat/embeddings";
    options.EmbeddingsModel = "@cf/baai/bge-base-en-v1.5";
    options.Model = "@cf/openai/gpt-oss-120b";
    options.SystemPrompt = "You are a helpful AI code assistant.";
    options.Temperature = 0.7;
    options.MaxTokens = 2048;
});

await builder.Build().RunAsync();