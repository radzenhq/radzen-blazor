#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using Microsoft.JSInterop;
using Xunit;

namespace Radzen.Blazor.Tests
{
    public class LinkerConfigTests
    {
        private const string ResourceName = "Radzen.Blazor.xml";

        [Fact]
        public void LinkerConfig_Preserves_Every_Type_With_JSInvokable_Methods()
        {
            var assembly = typeof(Radzen.Blazor.RadzenCard).Assembly;

            var jsInvokableTypes = assembly.GetTypes()
                .Where(type => type
                    .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                    .Any(method => method.GetCustomAttribute<JSInvokableAttribute>() != null))
                .Select(type => type.FullName!)
                .ToHashSet();

            var preservedTypes = GetPreservedTypeNames(assembly);

            var missing = jsInvokableTypes
                .Where(name => !preservedTypes.Contains(name))
                .OrderBy(name => name)
                .ToList();

            Assert.True(missing.Count == 0,
                "Types with [JSInvokable] methods are missing from LinkerConfig.xml (their JS callbacks would be trimmed away). Add a <type fullname=\"...\" preserve=\"methods\" /> entry for each:" +
                Environment.NewLine + string.Join(Environment.NewLine, missing));
        }

        private static HashSet<string> GetPreservedTypeNames(Assembly assembly)
        {
            using var stream = assembly.GetManifestResourceStream(ResourceName);
            Assert.NotNull(stream);

            using var reader = new StreamReader(stream!);
            var document = XDocument.Parse(reader.ReadToEnd());

            return document.Descendants("type")
                .Select(element => (string?)element.Attribute("fullname"))
                .Where(name => !string.IsNullOrEmpty(name))
                .Select(name => name!)
                .ToHashSet();
        }
    }
}
