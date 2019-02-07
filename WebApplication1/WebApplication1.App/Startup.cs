using Microsoft.AspNetCore.Components.Builder;
using Microsoft.Extensions.DependencyInjection;
using NorthwindBlazor.Data;

namespace WebApplication1.App
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            // Add NorthwindContext
            services.AddSingleton<NorthwindContext>();
        }

        public void Configure(IComponentsApplicationBuilder app)
        {
            app.AddComponent<App>("app");
        }
    }
}
