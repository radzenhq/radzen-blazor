using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.WebAssembly.Services;

namespace RadzenBlazorDemos
{
    public class CompilerServiceFactory
    {
        private static readonly string[] LazyAssemblies = new[]
        {
            "Microsoft.CodeAnalysis.dll",
            "Microsoft.CodeAnalysis.CSharp.dll",
            "Microsoft.CodeAnalysis.Razor.dll",
            "Microsoft.AspNetCore.Razor.Language.dll",
            "Microsoft.AspNetCore.Mvc.Razor.Extensions.dll",
            "MetadataReferenceService.BlazorWasm.dll",
            "MetadataReferenceService.Abstractions.dll"
        };

        private readonly IServiceProvider serviceProvider;
        private readonly NavigationManager navigationManager;
        private ICompilerService compiler;

        public CompilerServiceFactory(IServiceProvider serviceProvider, NavigationManager navigationManager)
        {
            this.serviceProvider = serviceProvider;
            this.navigationManager = navigationManager;
        }

        public async Task<ICompilerService> GetCompilerAsync()
        {
            if (compiler != null)
            {
                return compiler;
            }

            var assemblyLoader = serviceProvider.GetService(typeof(LazyAssemblyLoader)) as LazyAssemblyLoader;

            if (assemblyLoader != null)
            {
                await assemblyLoader.LoadAssembliesAsync(LazyAssemblies);
            }

            var compilerType = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType("RadzenBlazorDemos.CompilerService", false))
                .FirstOrDefault(t => t != null);

            compiler = (ICompilerService)Activator.CreateInstance(compilerType, navigationManager);

            return compiler;
        }
    }
}

