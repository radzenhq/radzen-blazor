using System;
using System.Net.Http;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;

using RadzenBlazorDemos.Services;
using Radzen;
using RadzenBlazorDemos.Data;
using RadzenBlazorDemos;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

builder.Services.AddDbContextFactory<NorthwindContext>();

builder.Services.AddRadzenComponents();

builder.Services.AddScoped<ThemeService>();
builder.Services.AddScoped<CompilerService>();
builder.Services.AddScoped<ExampleService>();
builder.Services.AddScoped<NorthwindService>();
builder.Services.AddScoped<NorthwindODataService>();
builder.Services.AddSingleton<GitHubService>();

await builder.Build().RunAsync();