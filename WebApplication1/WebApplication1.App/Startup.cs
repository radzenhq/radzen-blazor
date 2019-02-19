using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using NorthwindBlazor.Data;
using Radzen;

namespace WebApplication1.App
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Add NorthwindContext
            services.AddSingleton<NorthwindContext>();

            services.AddSingleton<DialogService>();
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
