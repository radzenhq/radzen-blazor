using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Radzen;
using RadzenBlazorDemos.Data;
using RadzenBlazorDemos.Services;
using System;
using System.Linq;
using System.Net.Http;

namespace RadzenBlazorDemos
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            services.AddRazorPages();
            services.AddServerSideBlazor().AddHubOptions(o =>
            {
                o.MaximumReceiveMessageSize = 10 * 1024 * 1024;
            });

            services.AddSingleton<HttpClient>(sp =>
            {
                // Get the address that the app is currently running at
                var server = sp.GetRequiredService<IServer>();
                var addressFeature = server.Features.Get<IServerAddressesFeature>();
                string baseAddress = addressFeature.Addresses.First();
                return new HttpClient { BaseAddress = new Uri(baseAddress) };
            });

            services.AddScoped<ThemeService>();
            services.AddScoped<ExampleService>();

            services.AddDbContextFactory<NorthwindContext>();

            services.AddScoped<DialogService>();
            services.AddScoped<NotificationService>();
            services.AddScoped<TooltipService>();
            services.AddScoped<ContextMenuService>();

            services.AddScoped<NorthwindService>();
            services.AddScoped<NorthwindODataService>();
            services.AddScoped<GitHubService>();

            services.AddDistributedMemoryCache();

            services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = long.MaxValue;
            });

            services.AddLocalization();

            /* --> Uncomment to enable localization
            var supportedCultures = new[]
            {
                new System.Globalization.CultureInfo("de-DE"),
            };

            services.Configure<RequestLocalizationOptions>(options =>
            {
                options.DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("de-DE");
                options.SupportedCultures = supportedCultures;
                options.SupportedUICultures = supportedCultures;
            });
            */
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseWebAssemblyDebugging();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.Use((ctx, next) =>
                {
                    ctx.Request.Scheme = "https";
                    return next();
                });
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

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToPage("/_Host");
            });
        }
    }
}
