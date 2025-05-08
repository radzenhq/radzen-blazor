using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Radzen.Blazor.Rendering;
using System;

namespace Radzen.Blazor
{
    /// <summary>
    /// RadzenLayout component.
    /// </summary>
    public partial class RadzenLayout : RadzenComponentWithChildren
    {
        [Inject]
        private IServiceProvider ServiceProvider { get; set; }

        private ThemeService themeService;

        /// <inheritdoc />
        protected override void OnInitialized()
        {
            themeService = ServiceProvider.GetService<ThemeService>();

            if (themeService != null)
            {
                themeService.ThemeChanged += OnThemeChanged;
            }

            base.OnInitialized();
        }

        private void OnThemeChanged()
        {
            StateHasChanged();
        }

        /// <inheritdoc />
        protected override string GetComponentCssClass() => ClassList.Create("rz-layout")
            .Add($"rz-{themeService?.Theme}", themeService != null)
            .ToString();
    }
}