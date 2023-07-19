using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;
using Radzen.Blazor;
using RadzenBlazorDemos.Shared;

namespace RadzenBlazorDemos
{
    public class CompilerService
    {
        class CollectibleAssemblyLoadContext : AssemblyLoadContext
        {
            public CollectibleAssemblyLoadContext() : base(isCollectible: true)
            {
            }
        }

        class InMemoryProjectItem : RazorProjectItem
        {
            private readonly string source;
            private readonly string fileName;

            public override string BasePath => fileName;

            public override string FilePath => fileName;

            public override string PhysicalPath => fileName;

            public override bool Exists => true;

            public override Stream Read()
            {
                return new MemoryStream(Encoding.UTF8.GetBytes(source));
            }

            private readonly string fileKind = FileKinds.Component;

            public override string FileKind => fileKind;

            public InMemoryProjectItem(string source)
            {
                this.source = source;
                fileName = "DynamicComponent.razor";
                fileKind = "component";
            }
        }

        private CSharpCompilation compilation;
        private RazorProjectEngine razorProjectEngine;
        private CollectibleAssemblyLoadContext loadContext;

        private AssemblyLoadContext GetLoadContext()
        {
            if (loadContext != null)
            {
                loadContext.Unload();
                GC.Collect();
            }

            loadContext = new CollectibleAssemblyLoadContext();

            return loadContext;;
        }
        private const string Imports = @"@using System.Net.Http
@using System
@using System.Collections.Generic
@using System.Linq
@using System.Threading.Tasks
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Forms
@using Microsoft.AspNetCore.Components.Routing
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.AspNetCore.Components.Web.Virtualization
@using Microsoft.JSInterop
@using Radzen
@using Radzen.Blazor
@using RadzenBlazorDemos
@using RadzenBlazorDemos.Shared
@using RadzenBlazorDemos.Pages
";
        private readonly HttpClient httpClient;

        public CompilerService(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        private async Task InitializeAsync()
        {
            var streams = await GetStreamsAsync();

            var referenceAssemblies = streams.Select(stream => MetadataReference.CreateFromStream(stream)).ToList();

            compilation = CSharpCompilation.Create(
                 "RadzenBlazorDemos.DynamicAssembly",
                 Array.Empty<SyntaxTree>(),
                 referenceAssemblies,
                 new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
            );

            razorProjectEngine = RazorProjectEngine.Create(RazorConfiguration.Default, RazorProjectFileSystem.Create("/"), builder =>
            {
                builder.SetRootNamespace("RadzenBlazorDemos");

                CompilerFeatures.Register(builder);

                var metadataReferenceFeature = new DefaultMetadataReferenceFeature { References = referenceAssemblies.ToArray() };

                builder.Features.Add(metadataReferenceFeature);

                builder.Features.Add(new CompilationTagHelperFeature());
            });
        }


        private async Task<IEnumerable<Stream>> GetStreamsAsync()
        {
             var referenceAssemblyRoots = new[]
             {
                 typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly, // System.Runtime
                 typeof(ComponentBase).Assembly,
                 typeof(RadzenButton).Assembly,
                 typeof(ExpandoObject).Assembly,
                 typeof(EventConsole).Assembly,
             };

             var referencedAssemblies = referenceAssemblyRoots
               .SelectMany(assembly => assembly.GetReferencedAssemblies().Append(assembly.GetName()))
               .Select(Assembly.Load)
               .Distinct()
               .ToList();

            if (referencedAssemblies.Any(assembly => string.IsNullOrEmpty(assembly.Location)))
            {
                var list = new List<Stream>();

                await Task.WhenAll(
                    referencedAssemblies.Select(async assembly =>
                    {
                        var result = await httpClient.GetAsync($"/_framework/{assembly.GetName().Name}.dll");

                        result.EnsureSuccessStatusCode();

                        list.Add(await result.Content.ReadAsStreamAsync());
                    })
                );

                return list;
            }
            else
            {
                return referencedAssemblies.Select(assembly => File.OpenRead(assembly.Location));
            }
        }

        public async Task<Type> CompileAsync(string source)
        {
            if (compilation == null)
            {
                await InitializeAsync();
            }

            var projectItem = new InMemoryProjectItem(source);

            var codeDocument = razorProjectEngine.Process(RazorSourceDocument.ReadFrom(projectItem), FileKinds.Component,
                new [] {RazorSourceDocument.Create(Imports, "_Imports.razor")}, null);

            var csharpDocument = codeDocument.GetCSharpDocument();

            var syntaxTree = CSharpSyntaxTree.ParseText(csharpDocument.GeneratedCode, CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp10));

            compilation = compilation.RemoveAllSyntaxTrees().AddSyntaxTrees(syntaxTree);

            var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);

            if (errors.Any())
            {
                throw new ApplicationException(string.Join(Environment.NewLine, errors.Select(e => e.GetMessage())));
            }

            using var stream = new MemoryStream();

            var emitResult = compilation.Emit(stream);

            if (!emitResult.Success)
            {
                throw new ApplicationException(string.Join(Environment.NewLine, emitResult.Diagnostics.Select(d => d.GetMessage())));
            }

            stream.Seek(0, SeekOrigin.Begin);

            var assembly = GetLoadContext().LoadFromStream(stream);

            var type = assembly.GetType("RadzenBlazorDemos.DynamicComponent");

            return type;
        }
    }
}