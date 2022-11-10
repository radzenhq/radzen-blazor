using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using RadzenBlazorDemos.Services;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Radzen;
using RadzenBlazorDemos.Data;

namespace RadzenBlazorDemos
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");
            builder.RootComponents.Add<HeadOutlet>("head::after");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddDbContextFactory<NorthwindContext>();

            builder.Services.AddScoped<ThemeService>();
            builder.Services.AddScoped<ExampleService>();
            builder.Services.AddScoped<DialogService>();
            builder.Services.AddScoped<NotificationService>();
            builder.Services.AddScoped<TooltipService>();
            builder.Services.AddScoped<ContextMenuService>();

            builder.Services.AddScoped<NorthwindService>();
            builder.Services.AddScoped<NorthwindODataService>();
            builder.Services.AddScoped<GitHubService>();


            await builder.Build().RunAsync();
        }
    }
}
