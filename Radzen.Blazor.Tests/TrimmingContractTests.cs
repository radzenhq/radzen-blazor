#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Xunit;

namespace Radzen.Blazor.Tests
{
    // Executable form of TRIMMING_AUDIT.md. Each fact below is a trimming/AOT hazard the audit found.
    //
    // Tests 1-9 are EXPECTED TO FAIL on current code: they assert the suggested fix is in place, and the
    // failure message names exactly what to add. As the audit remediations land (DAM annotations,
    // LinkerConfig entries, RUC/RDC attributes, narrowed suppressions) each test turns green - so the
    // suite doubles as the fix checklist and a permanent regression guard.
    //
    // Tests 10-11 are green-now regression guards for already-fixed hazard classes.
    public class TrimmingContractTests
    {
        private static readonly Assembly LibraryAssembly = typeof(Radzen.Blazor.RadzenCard).Assembly;
        private const string LinkerResourceName = "Radzen.Blazor.xml";

        // ----- Test 1: H6/H7/H8 - [JSInvokable] parameter DTOs are deserialized from JS and must be
        // preserved with preserve="all" (else their members are trimmed and the callback arrives empty
        // or throws). The trim analyzer is blind to this. RED now: FileInfo, PreviewFileInfo,
        // GoogleMapClickEventArgs are missing from LinkerConfig.xml.
        [Fact]
        public void JSInvokable_Parameter_DTOs_Are_Preserved_All()
        {
            var preserved = GetPreservedTypes();
            var offenders = new SortedSet<string>();

            foreach (var type in LibraryAssembly.GetTypes())
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic
                    | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
                foreach (var method in methods)
                {
                    if (method.GetCustomAttribute<JSInvokableAttribute>() == null) continue;
                    foreach (var parameter in method.GetParameters())
                        foreach (var leaf in LeafTypes(parameter.ParameterType))
                        {
                            if (!IsLibraryDataType(leaf)) continue;
                            if (!preserved.TryGetValue(leaf.FullName!, out var preserve) || preserve != "all")
                                offenders.Add($"{leaf.FullName}  (param of {type.Name}.{method.Name})");
                        }
                }
            }

            Assert.True(offenders.Count == 0,
                "These types are deserialized from JS as [JSInvokable] parameters but are not preserved "
                + "with preserve=\"all\" in LinkerConfig.xml. Add <type fullname=\"...\" preserve=\"all\" /> "
                + "for each (same as CellEventArgs):" + Environment.NewLine
                + string.Join(Environment.NewLine, offenders));
        }

        // ----- Test 2: H1-H4 / M1-M6 / M12 - data components bind by string Property/ValueProperty and
        // reflect over the consumer's model T. Their public generic parameter must carry
        // [DynamicallyAccessedMembers(PublicProperties|PublicFields)] so the trimmer keeps the consumer's
        // model members. RED now: none are annotated (they rely on suppressions instead). Annotate the
        // base classes first (PagedDataBoundComponent/DataBoundFormComponent/DropDownBase) so derived
        // generics inherit, except those that derive from FormComponent<T> directly.
        [Fact]
        public void DataComponent_Generic_Parameters_Have_DAM()
        {
            string[] genericTypeNames =
            {
                // bases (namespace Radzen)
                "Radzen.PagedDataBoundComponent`1",
                "Radzen.DataBoundFormComponent`1",
                "Radzen.DropDownBase`1",
                // grid family
                "Radzen.Blazor.RadzenDataGrid`1",
                "Radzen.Blazor.RadzenDataGridColumn`1",
                "Radzen.Blazor.RadzenDataList`1",
                "Radzen.Blazor.RadzenPivotDataGrid`1",
                // dropdown / list family (last three derive from FormComponent<T>, NOT DropDownBase<T>,
                // so they will not inherit the base annotation - they need their own)
                "Radzen.Blazor.RadzenDropDown`1",
                "Radzen.Blazor.RadzenDropDownDataGrid`1",
                "Radzen.Blazor.RadzenListBox`1",
                "Radzen.Blazor.RadzenCheckBoxList`1",
                "Radzen.Blazor.RadzenRadioButtonList`1",
                "Radzen.Blazor.RadzenSelectBar`1",
                "Radzen.Blazor.RadzenPickList`1",
                // data filter + scheduler + form
                "Radzen.Blazor.RadzenDataFilter`1",
                "Radzen.Blazor.RadzenDataFilterProperty`1",
                "Radzen.Blazor.RadzenScheduler`1",
                "Radzen.Blazor.RadzenTemplateForm`1",
            };

            var offenders = new List<string>();
            foreach (var name in genericTypeNames)
            {
                var type = LibraryAssembly.GetType(name);
                Assert.True(type != null, $"Test out of date: type '{name}' not found in Radzen.Blazor.");
                if (!GenericParameterHasDam(type!, DynamicallyAccessedMemberTypes.PublicProperties))
                    offenders.Add(name);
            }

            Assert.True(offenders.Count == 0,
                "These public generic data components reflect over the consumer model T but do not flow a "
                + "[DynamicallyAccessedMembers(PublicProperties|PublicFields)] annotation on their type "
                + "parameter, so a consumer's trimmed model loses its members (blank columns / sort+filter "
                + "throw). Annotate the type parameter (start at the base classes):" + Environment.NewLine
                + string.Join(Environment.NewLine, offenders));
        }

        // ----- Test 3: H9 / M18 / M29 / M30 - generic methods/types that reflect over T must flow DAM.
        [Fact]
        public void Reflective_Generic_Members_Have_DAM()
        {
            var offenders = new List<string>();

            // H9: ReadAsync<T> reflection-based JSON deserialize of the consumer model.
            CheckMethodGenericDam("Radzen.HttpResponseMessageExtensions", "ReadAsync",
                DynamicallyAccessedMemberTypes.PublicProperties, offenders);
            // M18: GroupByMany<T> dynamic grouping over T.
            CheckMethodGenericDam("Radzen.QueryableExtension", "GroupByMany",
                DynamicallyAccessedMemberTypes.PublicProperties, offenders);
            // M29: HtmlEditor selection attributes deserialized into T.
            CheckMethodGenericDam("Radzen.Blazor.RadzenHtmlEditor", "GetSelectionAttributes",
                DynamicallyAccessedMemberTypes.PublicProperties, offenders);
            // M30: ComplexPropertiesConverter<T> serializes T's properties.
            var converter = LibraryAssembly.GetType("Radzen.ComplexPropertiesConverter`1");
            Assert.True(converter != null, "Test out of date: Radzen.ComplexPropertiesConverter`1 not found.");
            if (!GenericParameterHasDam(converter!, DynamicallyAccessedMemberTypes.PublicProperties))
                offenders.Add("Radzen.ComplexPropertiesConverter`1 (type parameter)");

            Assert.True(offenders.Count == 0,
                "These generic members reflect over T (JSON serialize/deserialize or dynamic grouping) "
                + "without a [DynamicallyAccessedMembers] annotation on the generic parameter:"
                + Environment.NewLine + string.Join(Environment.NewLine, offenders));
        }

        // ----- Test 4: M13/M31/M32 - APIs that take a System.Type and instantiate / open it must annotate
        // that parameter with [DynamicallyAccessedMembers] (PublicConstructors / All), like the existing
        // DialogService.Open(Type). RED now: RadzenComponentActivator.Override(Type,Type) and
        // DialogService.OpenSide(Type)/OpenSideAsync(Type) do not.
        [Fact]
        public void Type_Parameter_APIs_Have_DAM()
        {
            var offenders = new List<string>();
            CheckTypeParameterDam("Radzen.RadzenComponentActivator", "Override", offenders);
            CheckTypeParameterDam("Radzen.DialogService", "OpenSide", offenders);
            CheckTypeParameterDam("Radzen.DialogService", "OpenSideAsync", offenders);

            Assert.True(offenders.Count == 0,
                "These APIs accept a Type that is constructed/opened via reflection but do not annotate the "
                + "Type parameter with [DynamicallyAccessedMembers] (so its constructor can be trimmed):"
                + Environment.NewLine + string.Join(Environment.NewLine, offenders));
        }

        // ----- Test 5: H11 - the public dynamic-LINQ API builds+compiles expression trees over arbitrary
        // T and cannot be made trim-safe; it must propagate [RequiresUnreferencedCode] so consumers are
        // warned, instead of silencing the warning with a suppression. RED now: no RUC on these methods.
        [Fact]
        public void DynamicLinq_Public_Methods_Are_RequiresUnreferencedCode()
        {
            var type = LibraryAssembly.GetType("System.Linq.Dynamic.Core.DynamicExtensions");
            Assert.True(type != null, "Test out of date: DynamicExtensions not found.");

            string[] names = { "Where", "OrderBy", "Select" }; // Select has two public overloads
            var offenders = type!.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => names.Contains(m.Name))
                .Where(m => m.GetCustomAttribute<RequiresUnreferencedCodeAttribute>() == null)
                .Select(m => $"{type.FullName}.{m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})")
                .ToList();

            Assert.True(offenders.Count == 0,
                "These public dynamic-LINQ methods are trim-unsafe (reflect+compile over T) but do not carry "
                + "[RequiresUnreferencedCode], so consumers get no warning:" + Environment.NewLine
                + string.Join(Environment.NewLine, offenders));
        }

        // ----- AOT (IL3050) is intentionally NOT gated. The audit's M22-M28 (RequiresDynamicCode on
        // Reflection.Emit / Expression.Compile / MakeGenericType / dynamic / AsQueryable) are real, but
        // Native AOT is not a supported target for a Blazor component library: Blazor Server runs on the
        // JIT, and Blazor WASM (even with RunAOTCompilation) keeps an interpreter fallback, so these
        // patterns work at runtime there. [RequiresDynamicCode] only PROPAGATES the warning (it fixes
        // nothing), and much of the surface lives in Razor-generated code that cannot be annotated, so a
        // zero-IL3050 gate could never go green. We do not keep a permanently-failing test. (Trimming is
        // different - it is a real, high-likelihood scenario and IS gated above and below.) If Radzen ever
        // commits to supporting standalone helper use under Native AOT, add focused [RequiresDynamicCode]
        // assertions on the PUBLIC helper APIs then.

        // ----- Test 7: H3/H5 - library types resolved by reflection at runtime must be preserved in
        // LinkerConfig.xml. RED now: System.Linq.Enumerable (filter In/Contains/Intersect branches use it
        // reflectively; only System.Linq.Queryable is preserved) and the Spreadsheet ChartDataPoint
        // (chart series reflect over it) are absent.
        [Fact]
        public void Reflectively_Resolved_Library_Types_Are_Preserved_All()
        {
            var preserved = GetPreservedTypes();
            string[] required =
            {
                "System.Linq.Enumerable",                          // H3/H13
                "Radzen.Documents.Spreadsheet.ChartDataPoint",     // H5
            };

            var offenders = required
                .Where(name => !preserved.TryGetValue(name, out var p) || p != "all")
                .ToList();

            Assert.True(offenders.Count == 0,
                "These types are located by reflection at runtime and must be preserved with preserve=\"all\" "
                + "in LinkerConfig.xml:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
        }

        // ----- Test 7b: the DataGrid filter engine resolves System.String filter methods by reflection
        // (typeof(string).GetMethod) for the string operators; trimming strips them unless rooted. They
        // must be kept via [DynamicDependency] on QueryableExtension.GetExpression. Found by the WASM e2e
        // gate (the metadata/console gates structurally cannot - they don't exercise the filter engine).
        [Fact]
        public void String_Filter_Methods_Are_Rooted_For_Trimming()
        {
            var type = LibraryAssembly.GetType("Radzen.QueryableExtension");
            Assert.True(type != null, "Test out of date: Radzen.QueryableExtension not found.");
            var method = type!.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(m => m.Name == "GetExpression");
            Assert.True(method != null, "Test out of date: QueryableExtension.GetExpression not found.");

            var rootedStringMembers = method!.GetCustomAttributesData()
                .Where(a => a.AttributeType.Name == "DynamicDependencyAttribute"
                    && a.ConstructorArguments.Count == 2
                    && a.ConstructorArguments[1].Value as Type == typeof(string))
                .Select(a => a.ConstructorArguments[0].Value as string)
                .ToHashSet();

            string[] required = { "Contains", "StartsWith", "EndsWith", "ToLower" };
            var missing = required.Where(m => !rootedStringMembers.Contains(m)).ToList();

            Assert.True(missing.Count == 0,
                "DataGrid string filter operators resolve these System.String methods by reflection; root "
                + "them via [DynamicDependency(\"<name>\", typeof(string))] on QueryableExtension.GetExpression "
                + "so trimming keeps them:" + Environment.NewLine + string.Join(Environment.NewLine, missing));
        }

        // ----- Test 8: M7/M8 - RadzenDataGrid/RadzenPivotDataGrid carry broad CLASS-LEVEL trim
        // suppressions that auto-silence ALL future reflection warnings on the type. Suppressions must be
        // scoped to the specific members that need them. RED now: both carry class-level IL2026/IL2070/
        // IL2087 suppressions (among others).
        [Fact]
        public void Data_Components_Have_No_Blanket_ClassLevel_Suppressions()
        {
            string[] dangerousCodes = { "2026", "2070", "2087" };
            string[] typeNames = { "Radzen.Blazor.RadzenDataGrid`1", "Radzen.Blazor.RadzenPivotDataGrid`1" };

            var offenders = new List<string>();
            foreach (var name in typeNames)
            {
                var type = LibraryAssembly.GetType(name);
                Assert.True(type != null, $"Test out of date: type '{name}' not found.");
                foreach (var data in type!.GetCustomAttributesData()
                    .Where(a => a.AttributeType.Name == "UnconditionalSuppressMessageAttribute"))
                {
                    var checkId = data.ConstructorArguments.Count > 1
                        ? data.ConstructorArguments[1].Value as string : null;
                    if (checkId != null && dangerousCodes.Any(checkId.Contains))
                        offenders.Add($"{name}: class-level suppression of {checkId}");
                }
            }

            Assert.True(offenders.Count == 0,
                "These types blanket-suppress trim warnings at the CLASS level, which silences all future "
                + "unsafe reflection on the type. Move each suppression onto the specific member that needs "
                + "it:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
        }

        // ----- Test 9: M15/L6 - ExpressionSerializer serializes an enum filter value as a cast to the
        // enum's FullName, which forces deserialization to resolve the type by string via
        // AppDomain...GetTypes() (trim/AOT hostile). It should emit a plain numeric value. RED now: the
        // output contains the enum's FullName.
        [Fact]
        public void ExpressionSerializer_Does_Not_Emit_Enum_FullName()
        {
            var enumType = LibraryAssembly.GetTypes()
                .First(t => t.IsEnum && t.IsPublic && t.Namespace == "Radzen");
            var value = Enum.GetValues(enumType).GetValue(0)!;

            var formatted = global::ExpressionSerializer.FormatValue(value);

            Assert.False(formatted != null && formatted.Contains(enumType.FullName!),
                $"ExpressionSerializer.FormatValue serialized enum '{enumType.FullName}' as a cast to its "
                + $"FullName ({formatted}). This forces trim/AOT-hostile string type resolution on "
                + "deserialization. Emit a numeric value instead.");
        }

        // ===== Green-now regression guards (guard already-fixed hazard classes) =====

        // ----- Test 10: regression guard for the original production crash class - an anonymous object
        // serialized across the JS interop / System.Text.Json boundary trims to a crash
        // (ConstructorContainsNullParameterNames). Currently green (all were converted to named types).
        // NOTE: does NOT cover L5 (anonymous nullable-enum filter object built in Razor markup at
        // RadzenDataGrid.razor:491) - that path is covered by the Layer 2 runtime gate.
        [Fact]
        public void No_Anonymous_Type_Reaches_Interop_Or_Serialize()
        {
            var sourceDir = FindLibrarySourceDir();
            Assert.True(sourceDir != null,
                "Could not locate the Radzen.Blazor source directory from the test output path.");

            var sinkPattern = new Regex(@"\.(Invoke(Void)?Async(<[^>]*>)?|Serialize)\s*\(", RegexOptions.Compiled);
            var offenders = new List<string>();

            foreach (var file in Directory.EnumerateFiles(sourceDir!, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    if (!sinkPattern.IsMatch(lines[i])) continue;
                    // Inspect a small window (the call may span lines) and ignore string literals.
                    var window = string.Join(" ", lines.Skip(i).Take(4));
                    var withoutStrings = Regex.Replace(window, "\"(\\\\.|[^\"\\\\])*\"", "\"\"");
                    if (Regex.IsMatch(withoutStrings, @"new\s*\{"))
                        offenders.Add($"{Path.GetFileName(file)}:{i + 1}: {lines[i].Trim()}");
                }
            }

            Assert.True(offenders.Count == 0,
                "An anonymous object ('new { }') appears to flow into a JS interop / JsonSerializer.Serialize "
                + "call. Under trimming this crashes with ConstructorContainsNullParameterNames - use a named, "
                + "preserved type instead:" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
        }

        // ----- Test 11: regression guard - every Radzen DTO returned from JS interop (InvokeAsync<T>) must
        // be preserved with preserve="all" or its members are trimmed on deserialize. Currently green.
        [Fact]
        public void InvokeAsync_Return_DTOs_Are_Preserved_All()
        {
            var sourceDir = FindLibrarySourceDir();
            Assert.True(sourceDir != null, "Could not locate the Radzen.Blazor source directory.");

            var preserved = GetPreservedTypes();
            var preservedSimpleNames = preserved.Where(kv => kv.Value == "all")
                .Select(kv => kv.Key.Split('.', '+').Last()).ToHashSet();
            var returnPattern = new Regex(@"InvokeAsync<\s*([A-Za-z_][A-Za-z0-9_\.]*)\s*>", RegexOptions.Compiled);

            // framework / primitive / generic return types that need no preservation
            var ignore = new HashSet<string>
            {
                "string", "bool", "int", "long", "double", "float", "object", "byte",
                "IJSObjectReference", "T",
            };

            var offenders = new SortedSet<string>();
            foreach (var file in Directory.EnumerateFiles(sourceDir!, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}")) continue;
                foreach (Match m in returnPattern.Matches(File.ReadAllText(file)))
                {
                    var simpleName = m.Groups[1].Value.Split('.').Last().TrimEnd('?');
                    if (ignore.Contains(simpleName) || simpleName.EndsWith("[]")) continue;
                    // only flag types that exist in the Radzen.Blazor assembly (consumer/library DTOs)
                    var isLibraryType = LibraryAssembly.GetTypes()
                        .Any(t => t.Name == simpleName && IsLibraryDataType(t));
                    if (isLibraryType && !preservedSimpleNames.Contains(simpleName))
                        offenders.Add($"{Path.GetFileName(file)}: InvokeAsync<{m.Groups[1].Value}>");
                }
            }

            Assert.True(offenders.Count == 0,
                "These Radzen DTO types are deserialized from JS interop return values but are not preserved "
                + "with preserve=\"all\":" + Environment.NewLine + string.Join(Environment.NewLine, offenders));
        }

        // ===================== helpers =====================

        private static Dictionary<string, string> GetPreservedTypes()
        {
            using var stream = LibraryAssembly.GetManifestResourceStream(LinkerResourceName);
            Assert.NotNull(stream);
            using var reader = new StreamReader(stream!);
            var document = XDocument.Parse(reader.ReadToEnd());

            var result = new Dictionary<string, string>();
            foreach (var element in document.Descendants("type"))
            {
                var name = (string?)element.Attribute("fullname");
                if (string.IsNullOrEmpty(name)) continue;
                // a <type> with no preserve attribute defaults to "all" in ILLink xml
                result[name!] = (string?)element.Attribute("preserve") ?? "all";
            }
            return result;
        }

        private static IEnumerable<Type> LeafTypes(Type type)
        {
            if (type.IsByRef) type = type.GetElementType()!;
            if (type.IsArray)
            {
                foreach (var leaf in LeafTypes(type.GetElementType()!)) yield return leaf;
                yield break;
            }
            if (type.IsGenericType)
            {
                foreach (var arg in type.GetGenericArguments())
                    foreach (var leaf in LeafTypes(arg)) yield return leaf;
                yield break;
            }
            yield return type;
        }

        private static bool IsLibraryDataType(Type type) =>
            type.Assembly == LibraryAssembly
            && !type.IsPrimitive
            && type != typeof(string)
            && !type.IsEnum
            && !typeof(ComponentBase).IsAssignableFrom(type);

        private static bool GenericParameterHasDam(Type genericTypeDefinition, DynamicallyAccessedMemberTypes required)
        {
            var parameter = genericTypeDefinition.GetGenericArguments().FirstOrDefault();
            if (parameter == null) return false;
            var attr = parameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>();
            return attr != null && attr.MemberTypes.HasFlag(required);
        }

        private static void CheckMethodGenericDam(string typeName, string methodName,
            DynamicallyAccessedMemberTypes required, List<string> offenders)
        {
            var type = LibraryAssembly.GetType(typeName);
            Assert.True(type != null, $"Test out of date: type '{typeName}' not found.");
            var methods = type!.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.Name == methodName && m.IsGenericMethodDefinition).ToList();
            Assert.True(methods.Count > 0, $"Test out of date: generic method '{typeName}.{methodName}' not found.");

            foreach (var method in methods)
            {
                var parameter = method.GetGenericArguments().First();
                var attr = parameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>();
                if (attr == null || !attr.MemberTypes.HasFlag(required))
                    offenders.Add($"{typeName}.{methodName}<{parameter.Name}>");
            }
        }

        private static void CheckTypeParameterDam(string typeName, string methodName, List<string> offenders)
        {
            var type = LibraryAssembly.GetType(typeName);
            Assert.True(type != null, $"Test out of date: type '{typeName}' not found.");
            var methods = type!.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
                .Where(m => m.Name == methodName
                    && m.GetParameters().Any(p => p.ParameterType == typeof(Type))).ToList();
            Assert.True(methods.Count > 0, $"Test out of date: '{typeName}.{methodName}(...Type...)' not found.");

            foreach (var method in methods)
                foreach (var parameter in method.GetParameters().Where(p => p.ParameterType == typeof(Type)))
                    if (parameter.GetCustomAttribute<DynamicallyAccessedMembersAttribute>() == null)
                        offenders.Add($"{typeName}.{methodName}(..., Type {parameter.Name}, ...)");
        }

        private static string? FindLibrarySourceDir()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                var candidate = Path.Combine(dir.FullName, "Radzen.Blazor", "LinkerConfig.xml");
                if (File.Exists(candidate)) return Path.Combine(dir.FullName, "Radzen.Blazor");
                dir = dir.Parent;
            }
            return null;
        }
    }
}
