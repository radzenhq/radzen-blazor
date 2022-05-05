using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.AspNetCore.Razor.Language.CodeGeneration;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Razor;
using Radzen.Blazor;
using RadzenBlazorDemos.Shared;

namespace RadzenBlazorDemos
{
    public class Compiler
    {
        private static CSharpCompilation BaseCompilation { get; }

        private static CSharpParseOptions CSharpParseOptions { get; }

        static Compiler()
        {
            var referenceAssemblyRoots = new[]
            {
                typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly, // System.Runtime
                typeof(ComponentBase).Assembly,
                typeof(RadzenButton).Assembly,
                typeof(ExpandoObject).Assembly,
                typeof(Compiler).Assembly, // Reference this assembly, so that we can refer to test component types
            };

            var referenceAssemblies = referenceAssemblyRoots
              .SelectMany(assembly => assembly.GetReferencedAssemblies().Concat(new[] { assembly.GetName() }))
              .Distinct()
              .Select(Assembly.Load)
              .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
              .ToList();
            BaseCompilation = CSharpCompilation.Create(
                "TestAssembly",
                Array.Empty<SyntaxTree>(),
                referenceAssemblies,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            CSharpParseOptions = new CSharpParseOptions(LanguageVersion.Default);
        }

        public Compiler()
        {
            Configuration = RazorConfiguration.Create(RazorLanguageVersion.Latest, "MVC-3.0", Array.Empty<RazorExtension>());
            FileSystem = new EmptyRazorProjectFileSystem();
            DefaultRootNamespace = "Radzen";
            ProjectEngine = CreateProjectEngine(BaseCompilation.References.ToArray());
        }

        private RazorConfiguration Configuration { get; }
        private RazorProjectEngine ProjectEngine { get; }

        private string DefaultRootNamespace { get; }

        private EmptyRazorProjectFileSystem FileSystem { get; }

        private RazorProjectEngine CreateProjectEngine(MetadataReference[] references)
        {
            return RazorProjectEngine.Create(Configuration, FileSystem, b =>
            {
                b.SetRootNamespace(DefaultRootNamespace);

                // Turn off checksums, we're testing code generation.
                b.Features.Add(new SuppressChecksum());

                // Including MVC here so that we can find any issues that arise from mixed MVC + Components.
                // Microsoft.AspNetCore.Mvc.Razor.Extensions.RazorExtensions.Register(b);

                // Features that use Roslyn are mandatory for components
                Microsoft.CodeAnalysis.Razor.CompilerFeatures.Register(b);

                b.Features.Add(new CompilationTagHelperFeature());
                b.Features.Add(new DefaultMetadataReferenceFeature()
                {
                    References = references,
                });
            });
        }
        public const string Imports = @"
@using System
@using System.Linq
@using System.Net
@using System.Net.Http
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.JSInterop
@using Microsoft.EntityFrameworkCore
@using RadzenBlazorDemos
@using RadzenBlazorDemos.Shared
@using RadzenBlazorDemos.Data
@using RadzenBlazorDemos.Pages
@using RadzenBlazorDemos.Models.Northwind
@using Radzen
@using Radzen.Blazor
";
        public Type Compile(string cshtml)
        {
            cshtml = $"{Imports}{cshtml}";
            var projectItem = new VirtualProjectItem("/", "/Page.cshtml", "/Page.cshtml", "Page.cshtml", FileKinds.Component, Encoding.UTF8.GetBytes(cshtml));

            var codeDocument = ProjectEngine.Process(projectItem);

            var csharp = codeDocument.GetCSharpDocument().GeneratedCode;

            var syntaxTree = CSharpSyntaxTree.ParseText(csharp, CSharpParseOptions);

            var compilation = BaseCompilation.AddSyntaxTrees(syntaxTree);

            var errors = compilation.GetDiagnostics().Any(d => d.Severity == DiagnosticSeverity.Error);

            if (errors)
            {
                throw new Exception(String.Join(Environment.NewLine, compilation.GetDiagnostics()
                                                 .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                                                 .Select(diagnostic => diagnostic.GetMessage())
                                                 .Distinct()));
            }

            using (var stream = new MemoryStream())
            {
                compilation.Emit(stream);

                var assembly = Assembly.Load(stream.ToArray());

                return assembly.GetType($"{DefaultRootNamespace}.Page");
            }
        }

        private class SuppressChecksum : IConfigureRazorCodeGenerationOptionsFeature
        {
            public int Order => 0;

            public RazorEngine Engine { get; set; }

            public void Configure(RazorCodeGenerationOptionsBuilder options)
            {
                options.SuppressChecksum = true;
                options.SuppressMetadataAttributes = true;
                options.SuppressNullabilityEnforcement = true;
            }
        }
    }
}
