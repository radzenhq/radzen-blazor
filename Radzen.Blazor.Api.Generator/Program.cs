using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Radzen.Blazor.Api.Generator;

sealed class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 3)
        {
            Console.Error.WriteLine("Usage: Radzen.Blazor.Api.Generator <assemblyPath> <xmlDocPath> <outputDir>");
            return 1;
        }

        var assemblyPath = args[0];
        var xmlDocPath = args[1];
        var outputDir = args[2];

        if (!File.Exists(assemblyPath))
        {
            Console.Error.WriteLine($"Assembly not found: {assemblyPath}");
            return 1;
        }

        if (!File.Exists(xmlDocPath))
        {
            Console.Error.WriteLine($"XML doc not found: {xmlDocPath}");
            return 1;
        }

        Directory.CreateDirectory(outputDir);

        var xmlDocs = XmlDocParser.Parse(xmlDocPath);
        var types = AssemblyInspector.GetPublicTypes(assemblyPath);

        var generator = new RazorPageGenerator(xmlDocs, types, outputDir);
        generator.Generate();

        Console.WriteLine($"Generated {generator.PageCount} API reference pages in {outputDir}");
        return 0;
    }
}

sealed record XmlMemberDoc(
    string Summary,
    string Value,
    List<ExampleSegment> Examples,
    string Remarks,
    Dictionary<string, string> Params,
    string Returns,
    Dictionary<string, string> TypeParams);

sealed record ExampleSegment(bool IsCode, string Content);

static class XmlDocParser
{
    public static Dictionary<string, XmlMemberDoc> Parse(string xmlPath)
    {
        var docs = new Dictionary<string, XmlMemberDoc>(StringComparer.Ordinal);
        var xdoc = XDocument.Load(xmlPath);
        var members = xdoc.Root?.Element("members");

        if (members == null)
            return docs;

        foreach (var member in members.Elements("member"))
        {
            var memberName = member.Attribute("name")?.Value;
            if (memberName == null) continue;

            var summary = CleanXmlDocText(member.Element("summary"));
            var value = CleanText(member.Element("value")?.Value);
            var examples = ParseExample(member.Element("example"));
            var remarks = CleanText(member.Element("remarks")?.Value);

            var paramDict = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var paramEl in member.Elements("param"))
            {
                var pName = paramEl.Attribute("name")?.Value;
                if (pName != null)
                {
                    paramDict[pName] = CleanXmlDocText(paramEl);
                }
            }

            var returnsText = CleanXmlDocText(member.Element("returns"));

            var typeParamDict = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var tpEl in member.Elements("typeparam"))
            {
                var tpName = tpEl.Attribute("name")?.Value;
                if (tpName != null)
                {
                    typeParamDict[tpName] = CleanXmlDocText(tpEl);
                }
            }

            docs[memberName] = new XmlMemberDoc(summary, value, examples, remarks, paramDict, returnsText, typeParamDict);
        }

        return docs;
    }

    static List<ExampleSegment> ParseExample(XElement? element)
    {
        var segments = new List<ExampleSegment>();
        if (element == null) return segments;

        foreach (var node in element.Nodes())
        {
            if (node is XText textNode)
            {
                var text = textNode.Value.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    segments.Add(new ExampleSegment(false, text));
                }
            }
            else if (node is XElement el && el.Name.LocalName == "code")
            {
                var code = el.Value.Trim();
                if (!string.IsNullOrEmpty(code))
                {
                    segments.Add(new ExampleSegment(true, code));
                }
            }
        }

        return segments;
    }

    static string CleanXmlDocText(XElement? element)
    {
        if (element == null) return "";

        var sb = new StringBuilder();
        foreach (var node in element.Nodes())
        {
            if (node is XText textNode)
            {
                sb.Append(textNode.Value);
            }
            else if (node is XElement el)
            {
                switch (el.Name.LocalName)
                {
                    case "see":
                    case "seealso":
                        var cref = el.Attribute("cref")?.Value ?? "";
                        if (cref.Length > 2 && cref[1] == ':')
                            cref = cref[2..];
                        var display = FormatCref(cref);
                        sb.Append(CultureInfo.InvariantCulture, $"\x01TYPEREF:{cref}\x02{display}\x01/TYPEREF\x02");
                        break;
                    case "paramref":
                    case "typeparamref":
                        sb.Append(el.Attribute("name")?.Value ?? "");
                        break;
                    case "c":
                        sb.Append(el.Value);
                        break;
                    default:
                        sb.Append(el.Value);
                        break;
                }
            }
        }

        return CleanText(sb.ToString());
    }

    static string FormatCref(string cref)
    {
        var tickIdx = cref.IndexOf('`', StringComparison.Ordinal);
        if (tickIdx >= 0)
        {
            var basePart = cref[..tickIdx];
            var afterTick = cref[(tickIdx + 1)..];
            var parenIdx = afterTick.IndexOf('(', StringComparison.Ordinal);
            string countStr;
            string suffix;
            if (parenIdx >= 0)
            {
                countStr = afterTick[..parenIdx];
                suffix = afterTick[parenIdx..];
            }
            else
            {
                countStr = afterTick;
                suffix = "";
            }

            if (int.TryParse(countStr, out var count))
            {
                var args = string.Join(", ", Enumerable.Range(0, count).Select(i => count == 1 ? "T" : $"T{i + 1}"));
                var lastDot = basePart.LastIndexOf('.');
                var shortName = lastDot >= 0 ? basePart[(lastDot + 1)..] : basePart;
                return $"{shortName}<{args}>{suffix}";
            }
        }

        var dot = cref.LastIndexOf('.');
        return dot >= 0 ? cref[(dot + 1)..] : cref;
    }

    static string CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}

sealed record InheritedMemberInfo(string Name, string DeclaringTypeName, MemberKind Kind);

sealed record TypeMemberInfo(string Name, string DeclaringTypeName, string TypeName, string Summary, MemberKind Kind, string Signature, IReadOnlyList<ParameterInfo> Parameters, string? ReturnType, string? ReturnSummary, List<ExampleSegment> Examples);
sealed record ParameterInfo(string Name, string TypeName, string Summary);
sealed record ApiTypeInfo(
    string FullName,
    string Name,
    string Namespace,
    TypeKind Kind,
    string? BaseTypeName,
    string Summary,
    string Remarks,
    string Syntax,
    IReadOnlyList<string> Inheritance,
    IReadOnlyList<string> Interfaces,
    IReadOnlyList<string> DerivedTypes,
    IReadOnlyList<string> TypeParameters,
    Dictionary<string, string> TypeParameterDescriptions,
    IReadOnlyList<TypeMemberInfo> Members,
    IReadOnlyList<EnumFieldInfo> EnumFields,
    List<ExampleSegment> Examples,
    IReadOnlyList<InheritedMemberInfo> InheritedMembers);
sealed record EnumFieldInfo(string Name, string Summary, string? Value);

enum TypeKind { Class, Interface, Enum, Struct, Delegate }
enum MemberKind { Constructor, Property, Method, Event, Field, Operator }

static class AssemblyInspector
{
    static readonly HashSet<string> ExcludedNamespacePrefixes = new()
    {
        "Radzen.Blazor.Rendering"
    };

    static readonly Regex ExcludedTypePattern = new(@"^Radzen\.Blazor\.RadzenHtmlEditor.*Base$", RegexOptions.Compiled);

    static bool IsExcluded(Type type)
    {
        var fullName = type.FullName ?? type.Name;
        var ns = type.Namespace ?? "";

        if (string.IsNullOrEmpty(ns))
            return true;

        foreach (var prefix in ExcludedNamespacePrefixes)
        {
            if (ns.StartsWith(prefix, StringComparison.Ordinal))
                return true;
        }

        if (ExcludedTypePattern.IsMatch(fullName))
            return true;

        return false;
    }

    public static List<ApiTypeInfo> GetPublicTypes(string assemblyPath)
    {
        var allDlls = CollectAssemblyPaths(assemblyPath);
        var resolver = new PathAssemblyResolver(allDlls);

        using var mlc = new MetadataLoadContext(resolver);
        var assembly = mlc.LoadFromAssemblyPath(assemblyPath);

        var allPublicTypes = assembly.GetExportedTypes()
            .Where(t => !IsExcluded(t) && !IsCompilerGenerated(t))
            .ToList();

        var derivedMap = BuildDerivedTypeMap(allPublicTypes);

        return allPublicTypes.Select(t => InspectType(t, allPublicTypes, derivedMap)).ToList();
    }

    static List<string> CollectAssemblyPaths(string assemblyPath)
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
        foreach (var dll in Directory.GetFiles(runtimeDir, "*.dll"))
            paths.Add(dll);

        var dotnetRoot = Path.GetDirectoryName(Path.GetDirectoryName(runtimeDir.TrimEnd(Path.DirectorySeparatorChar)));
        if (dotnetRoot != null)
        {
            var aspNetDir = Path.Combine(dotnetRoot, "Microsoft.AspNetCore.App");
            if (Directory.Exists(aspNetDir))
            {
                var latest = Directory.GetDirectories(aspNetDir).OrderDescending().FirstOrDefault();
                if (latest != null)
                {
                    foreach (var dll in Directory.GetFiles(latest, "*.dll"))
                        paths.Add(dll);
                }
            }
        }

        var assemblyDir = Path.GetDirectoryName(assemblyPath)!;
        foreach (var dll in Directory.GetFiles(assemblyDir, "*.dll"))
            paths.Add(dll);

        paths.Add(assemblyPath);
        return paths.ToList();
    }

    static bool IsCompilerGenerated(Type t)
    {
        return t.Name.Contains('<', StringComparison.Ordinal) || t.Name.Contains('>', StringComparison.Ordinal) || t.Name.StartsWith("__", StringComparison.Ordinal);
    }

    static Dictionary<string, List<string>> BuildDerivedTypeMap(List<Type> types)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var t in types)
        {
            var baseName = t.BaseType?.FullName;
            if (baseName != null && !baseName.StartsWith("System.", StringComparison.Ordinal))
            {
                if (!map.ContainsKey(baseName))
                    map[baseName] = new List<string>();
                map[baseName].Add(FormatTypeName(t));
            }
        }
        return map;
    }

    static ApiTypeInfo InspectType(Type type, List<Type> allTypes, Dictionary<string, List<string>> derivedMap)
    {
        var fullName = FormatTypeName(type);
        var name = FormatShortName(type);
        var ns = type.Namespace ?? "";
        var kind = GetTypeKind(type);
        var baseTypeName = type.BaseType != null && type.BaseType.FullName != "System.Object" && type.BaseType.FullName != "System.ValueType" && type.BaseType.FullName != "System.Enum"
            ? FormatTypeName(type.BaseType) : null;

        var inheritance = BuildInheritanceChain(type);
        var interfaces = type.GetInterfaces()
            .Where(i => i.IsPublic && !IsSystemInterface(i))
            .Select(FormatTypeName)
            .Distinct()
            .ToList();

        var derived = derivedMap.TryGetValue(type.FullName ?? "", out var d) ? d : new List<string>();

        var typeParams = type.IsGenericType
            ? type.GetGenericArguments().Select(a => a.Name).ToList()
            : new List<string>();

        var members = kind != TypeKind.Enum ? GetMembers(type) : new List<TypeMemberInfo>();
        var enumFields = kind == TypeKind.Enum ? GetEnumFields(type) : new List<EnumFieldInfo>();
        var inheritedMembers = kind != TypeKind.Enum ? GetInheritedMembers(type) : new List<InheritedMemberInfo>();

        var syntax = BuildSyntaxDeclaration(type);

        return new ApiTypeInfo(fullName, name, ns, kind, baseTypeName, "", "", syntax,
            inheritance, interfaces, derived, typeParams, new Dictionary<string, string>(), members, enumFields, new List<ExampleSegment>(), inheritedMembers);
    }

    static TypeKind GetTypeKind(Type type)
    {
        if (type.IsEnum) return TypeKind.Enum;
        if (type.IsInterface) return TypeKind.Interface;
        if (type.IsValueType) return TypeKind.Struct;
        if (typeof(Delegate).IsAssignableFrom(type) || (type.BaseType?.FullName?.Contains("Delegate", StringComparison.Ordinal) ?? false))
            return TypeKind.Delegate;
        return TypeKind.Class;
    }

    static bool IsSystemInterface(Type iface)
    {
        var ns = iface.Namespace ?? "";
        return ns.StartsWith("System.", StringComparison.Ordinal) || ns == "System";
    }

    static List<string> BuildInheritanceChain(Type type)
    {
        var chain = new List<string>();
        var current = type.BaseType;
        while (current != null && current.FullName != "System.Object")
        {
            chain.Insert(0, FormatTypeName(current));
            current = current.BaseType;
        }
        chain.Insert(0, "Object");
        return chain;
    }

    static string GetAccessModifier(MethodBase? method)
    {
        if (method == null) return "";
        if (method.IsPublic) return "public";
        if (method.IsFamily || method.IsFamilyOrAssembly) return "protected";
        return "";
    }

    static string GetMethodAccessModifier(MethodInfo method)
    {
        if (method.IsPublic) return "public";
        if (method.IsFamily || method.IsFamilyOrAssembly) return "protected";
        return "";
    }

    static bool IsPublicOrProtected(MethodBase? method) =>
        method != null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);

    static string BuildMethodModifiers(MethodInfo method)
    {
        var parts = new List<string>();
        parts.Add(GetMethodAccessModifier(method));
        if (method.IsStatic) parts.Add("static");
        if (method.IsAbstract) parts.Add("abstract");
        else if (method.IsVirtual && !method.IsFinal)
        {
            var isOverride = method.Attributes.HasFlag(System.Reflection.MethodAttributes.ReuseSlot) &&
                             method.DeclaringType?.BaseType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                 .Any(m => m.Name == method.Name && m.IsVirtual) == true;
            parts.Add(isOverride ? "override" : "virtual");
        }
        return string.Join(" ", parts.Where(p => p.Length > 0));
    }

    static List<TypeMemberInfo> GetMembers(Type type)
    {
        var members = new List<TypeMemberInfo>();
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var ctor in type.GetConstructors(flags).Where(c => c.IsPublic || c.IsFamily || c.IsFamilyOrAssembly))
        {
            var access = GetAccessModifier(ctor);
            var parameters = ctor.GetParameters().Select(p => new ParameterInfo(p.Name ?? "", FormatTypeName(p.ParameterType), "")).ToList();
            var sig = $"{access} {FormatShortName(type)}({string.Join(", ", parameters.Select(p => $"{p.TypeName} {p.Name}"))})";
            members.Add(new TypeMemberInfo(FormatShortName(type), FormatTypeName(type), "", "", MemberKind.Constructor, sig, parameters, null, null, new List<ExampleSegment>()));
        }

        foreach (var prop in type.GetProperties(flags))
        {
            if (!IsPublicOrProtected(prop.GetMethod) && !IsPublicOrProtected(prop.SetMethod)) continue;
            if (IsCompilerGeneratedMember(prop.Name)) continue;
            var propTypeName = FormatTypeName(prop.PropertyType);
            var accessors = new List<string>();
            if (IsPublicOrProtected(prop.GetMethod)) accessors.Add("get");
            if (IsPublicOrProtected(prop.SetMethod)) accessors.Add("set");
            var access = IsPublicOrProtected(prop.GetMethod) ? GetAccessModifier(prop.GetMethod) : GetAccessModifier(prop.SetMethod);
            var sig = $"{access} {propTypeName} {prop.Name} {{ {string.Join("; ", accessors)}; }}";
            members.Add(new TypeMemberInfo(prop.Name, FormatTypeName(type), propTypeName, "", MemberKind.Property, sig, Array.Empty<ParameterInfo>(), propTypeName, null, new List<ExampleSegment>()));
        }

        foreach (var method in type.GetMethods(flags).Where(m => (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly) && !m.IsSpecialName))
        {
            if (IsCompilerGeneratedMember(method.Name)) continue;
            if (IsObjectMethod(method.Name)) continue;
            var parameters = method.GetParameters().Select(p => new ParameterInfo(p.Name ?? "", FormatTypeName(p.ParameterType), "")).ToList();
            var returnType = FormatTypeName(method.ReturnType);
            var mods = BuildMethodModifiers(method);
            var sig = $"{mods} {returnType} {method.Name}({string.Join(", ", parameters.Select(p => $"{p.TypeName} {p.Name}"))})";
            members.Add(new TypeMemberInfo(method.Name, FormatTypeName(type), "", "", MemberKind.Method, sig, parameters, returnType, null, new List<ExampleSegment>()));
        }

        foreach (var method in type.GetMethods(flags).Where(m => m.IsPublic && m.IsSpecialName && m.IsStatic && m.Name.StartsWith("op_", StringComparison.Ordinal)))
        {
            var opName = FormatOperatorName(method.Name);
            if (opName == null) continue;
            var parameters = method.GetParameters().Select(p => new ParameterInfo(p.Name ?? "", FormatTypeName(p.ParameterType), "")).ToList();
            var returnType = FormatTypeName(method.ReturnType);
            var sig = $"public static {returnType} {opName}({string.Join(", ", parameters.Select(p => $"{p.TypeName} {p.Name}"))})";
            members.Add(new TypeMemberInfo(opName, FormatTypeName(type), "", "", MemberKind.Operator, sig, parameters, returnType, null, new List<ExampleSegment>()));
        }

        foreach (var evt in type.GetEvents(flags))
        {
            if (!IsPublicOrProtected(evt.AddMethod)) continue;
            var evtTypeName = evt.EventHandlerType != null ? FormatTypeName(evt.EventHandlerType) : "EventHandler";
            var access = GetAccessModifier(evt.AddMethod);
            var sig = $"{access} event {evtTypeName} {evt.Name}";
            members.Add(new TypeMemberInfo(evt.Name, FormatTypeName(type), evtTypeName, "", MemberKind.Event, sig, Array.Empty<ParameterInfo>(), null, null, new List<ExampleSegment>()));
        }

        foreach (var field in type.GetFields(flags).Where(f => (f.IsPublic || f.IsFamily || f.IsFamilyOrAssembly) && !f.IsSpecialName))
        {
            if (IsCompilerGeneratedMember(field.Name)) continue;
            var fieldTypeName = FormatTypeName(field.FieldType);
            var access = field.IsPublic ? "public" : "protected";
            var modifier = field.IsStatic ? "static " : "";
            var readonlyMod = field.IsInitOnly ? "readonly " : "";
            var sig = $"{access} {modifier}{readonlyMod}{fieldTypeName} {field.Name}";
            members.Add(new TypeMemberInfo(field.Name, FormatTypeName(type), fieldTypeName, "", MemberKind.Field, sig, Array.Empty<ParameterInfo>(), null, null, new List<ExampleSegment>()));
        }

        return members;
    }

    static List<InheritedMemberInfo> GetInheritedMembers(Type type)
    {
        var inherited = new List<InheritedMemberInfo>();
        var declaredNames = new HashSet<string>(StringComparer.Ordinal);
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

        foreach (var m in type.GetMembers(flags))
        {
            if (m is MethodInfo mi && mi.IsSpecialName) continue;
            declaredNames.Add(m.Name);
        }

        var current = type.BaseType;
        while (current != null && current.FullName != "System.Object")
        {
            var declaringName = FormatTypeName(current);
            foreach (var m in current.GetMembers(flags))
            {
                if (m is ConstructorInfo) continue;
                if (m is MethodInfo mi && mi.IsSpecialName) continue;
                if (IsCompilerGeneratedMember(m.Name)) continue;
                if (IsObjectMethod(m.Name)) continue;

                bool isAccessible = m switch
                {
                    PropertyInfo pi => IsPublicOrProtected(pi.GetMethod) || IsPublicOrProtected(pi.SetMethod),
                    MethodInfo method => method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly,
                    EventInfo ei => IsPublicOrProtected(ei.AddMethod),
                    FieldInfo fi => fi.IsPublic || fi.IsFamily || fi.IsFamilyOrAssembly,
                    _ => false
                };

                if (!isAccessible) continue;
                if (declaredNames.Contains(m.Name)) continue;

                var kind = m switch
                {
                    PropertyInfo => MemberKind.Property,
                    EventInfo => MemberKind.Event,
                    FieldInfo => MemberKind.Field,
                    _ => MemberKind.Method
                };

                inherited.Add(new InheritedMemberInfo(m.Name, declaringName, kind));
                declaredNames.Add(m.Name);
            }

            current = current.BaseType;
        }

        return inherited;
    }

    static readonly HashSet<string> ObjectOnlyMethods = new()
    {
        "GetType", "ReferenceEquals", "MemberwiseClone"
    };
    static bool IsObjectMethod(string name) => ObjectOnlyMethods.Contains(name);

    static readonly Dictionary<string, string> OperatorNames = new()
    {
        ["op_Equality"] = "operator ==",
        ["op_Inequality"] = "operator !=",
        ["op_Addition"] = "operator +",
        ["op_Subtraction"] = "operator -",
        ["op_Multiply"] = "operator *",
        ["op_Division"] = "operator /",
        ["op_Modulus"] = "operator %",
        ["op_GreaterThan"] = "operator >",
        ["op_LessThan"] = "operator <",
        ["op_GreaterThanOrEqual"] = "operator >=",
        ["op_LessThanOrEqual"] = "operator <=",
        ["op_BitwiseAnd"] = "operator &",
        ["op_BitwiseOr"] = "operator |",
        ["op_ExclusiveOr"] = "operator ^",
        ["op_LeftShift"] = "operator <<",
        ["op_RightShift"] = "operator >>",
        ["op_UnaryPlus"] = "operator +",
        ["op_UnaryNegation"] = "operator -",
        ["op_LogicalNot"] = "operator !",
        ["op_OnesComplement"] = "operator ~",
        ["op_True"] = "operator true",
        ["op_False"] = "operator false",
        ["op_Implicit"] = "implicit operator",
        ["op_Explicit"] = "explicit operator",
        ["op_Increment"] = "operator ++",
        ["op_Decrement"] = "operator --",
    };

    static string? FormatOperatorName(string methodName) =>
        OperatorNames.TryGetValue(methodName, out var name) ? name : null;

    static bool IsCompilerGeneratedMember(string name) =>
        name.Contains('<', StringComparison.Ordinal) || name.Contains('>', StringComparison.Ordinal) || name.StartsWith("__", StringComparison.Ordinal);

    static List<EnumFieldInfo> GetEnumFields(Type type)
    {
        var fields = new List<EnumFieldInfo>();
        foreach (var name in Enum.GetNames(type))
        {
            var field = type.GetField(name);
            var value = field?.GetRawConstantValue()?.ToString();
            fields.Add(new EnumFieldInfo(name, "", value));
        }
        return fields;
    }

    static string BuildSyntaxDeclaration(Type type)
    {
        var sb = new StringBuilder("public ");

        if (type.IsEnum)
        {
            sb.Append("enum ");
        }
        else if (type.IsInterface)
        {
            sb.Append("interface ");
        }
        else if (type.IsValueType)
        {
            sb.Append("struct ");
        }
        else
        {
            if (type.IsAbstract && type.IsSealed) sb.Append("static ");
            else if (type.IsAbstract) sb.Append("abstract ");
            else if (type.IsSealed) sb.Append("sealed ");
            sb.Append("class ");
        }

        sb.Append(FormatShortName(type));

        var baseAndInterfaces = new List<string>();
        if (type.BaseType != null && type.BaseType.FullName != "System.Object" &&
            type.BaseType.FullName != "System.ValueType" && type.BaseType.FullName != "System.Enum" &&
            !type.IsEnum)
        {
            baseAndInterfaces.Add(FormatTypeName(type.BaseType));
        }

        foreach (var iface in type.GetInterfaces().Where(i => i.IsPublic && !IsSystemInterface(i)))
        {
            baseAndInterfaces.Add(FormatTypeName(iface));
        }

        if (baseAndInterfaces.Count > 0)
        {
            sb.Append(" : ");
            sb.Append(string.Join(", ", baseAndInterfaces.Distinct()));
        }

        return sb.ToString();
    }

    static string FormatTypeName(Type type)
    {
        if (type.IsGenericType)
        {
            var name = type.FullName ?? (type.Namespace != null ? $"{type.Namespace}.{type.Name}" : type.Name);
            var tick = name.IndexOf('`', StringComparison.Ordinal);
            if (tick >= 0)
            {
                var baseName = name[..tick];
                var args = type.GetGenericArguments();
                return $"{baseName}<{string.Join(", ", args.Select(FormatTypeName))}>";
            }
        }

        return type.FullName ?? (type.Namespace != null ? $"{type.Namespace}.{type.Name}" : type.Name);
    }

    static string FormatShortName(Type type)
    {
        if (type.IsGenericType)
        {
            var name = type.Name;
            var tick = name.IndexOf('`', StringComparison.Ordinal);
            if (tick >= 0)
            {
                var baseName = name[..tick];
                var args = type.GetGenericArguments();
                return $"{baseName}<{string.Join(", ", args.Select(a => a.Name))}>";
            }
        }
        return type.Name;
    }
}

sealed class RazorPageGenerator
{
    readonly Dictionary<string, XmlMemberDoc> _xmlDocs;
    readonly List<ApiTypeInfo> _types;
    readonly string _outputDir;
    int _pageCount;
    Dictionary<string, string> _typeUrlMap = null!;

    public int PageCount => _pageCount;

    public RazorPageGenerator(Dictionary<string, XmlMemberDoc> xmlDocs, List<ApiTypeInfo> types, string outputDir)
    {
        _xmlDocs = xmlDocs;
        _types = types;
        _outputDir = outputDir;
    }

    public void Generate()
    {
        Directory.CreateDirectory(_outputDir);

        BuildTypeUrlMap();
        EnrichTypesWithXmlDocs();

        GenerateIndexPage();
        GenerateNamespacePages();
        GenerateTypePages();
        GenerateApiNavComponent();
        GenerateImportsFile();
    }

    void BuildTypeUrlMap()
    {
        _typeUrlMap = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var type in _types)
        {
            var url = TypePageUrl(type);
            _typeUrlMap[type.FullName] = url;
            _typeUrlMap[type.Name] = url;

            var normalizedFull = NormalizeGenericName(type.FullName);
            if (normalizedFull != type.FullName)
                _typeUrlMap[normalizedFull] = url;

            var normalizedShort = NormalizeGenericName(type.Name);
            if (normalizedShort != type.Name)
                _typeUrlMap[normalizedShort] = url;
        }
    }

    string? TryResolveTypeUrl(string typeName)
    {
        if (_typeUrlMap.TryGetValue(typeName, out var url))
            return url;

        var normalized = NormalizeGenericName(typeName);
        if (_typeUrlMap.TryGetValue(normalized, out url))
            return url;

        var simplified = SimplifyTypeName(typeName);
        if (_typeUrlMap.TryGetValue(simplified, out url))
            return url;

        return null;
    }

    void EnrichTypesWithXmlDocs()
    {
        for (int i = 0; i < _types.Count; i++)
        {
            var t = _types[i];
            var typeKey = $"T:{t.FullName}";
            var cleanKey = $"T:{NormalizeGenericName(t.FullName)}";

            string summary = "", remarks = "";
            var examples = new List<ExampleSegment>();
            var typeParamDescs = new Dictionary<string, string>(StringComparer.Ordinal);
            XmlMemberDoc? typeDoc = null;
            if (_xmlDocs.TryGetValue(cleanKey, out typeDoc) || _xmlDocs.TryGetValue(typeKey, out typeDoc))
            {
                summary = typeDoc.Summary;
                remarks = typeDoc.Remarks;
                examples = typeDoc.Examples;
                typeParamDescs = typeDoc.TypeParams;
            }

            var enrichedMembers = new List<TypeMemberInfo>();
            foreach (var member in t.Members)
            {
                var memberDoc = FindMemberDoc(t, member);
                var memberSummary = memberDoc?.Summary ?? "";
                if (string.IsNullOrEmpty(memberSummary) && member.Kind == MemberKind.Constructor)
                {
                    memberSummary = summary;
                }
                var enriched = member with
                {
                    Summary = memberSummary,
                    TypeName = member.TypeName,
                    Examples = memberDoc?.Examples ?? new List<ExampleSegment>()
                };

                if (member.Kind is MemberKind.Method or MemberKind.Constructor or MemberKind.Operator)
                {
                    var enrichedParams = new List<ParameterInfo>();
                    foreach (var p in member.Parameters)
                    {
                        var paramSummary = memberDoc?.Params.TryGetValue(p.Name, out var ps) == true ? ps : "";
                        enrichedParams.Add(p with { Summary = paramSummary });
                    }
                    enriched = enriched with { Parameters = enrichedParams };
                }

                if (member.ReturnType != null)
                {
                    enriched = enriched with { ReturnSummary = memberDoc?.Returns ?? "" };
                }

                enrichedMembers.Add(enriched);
            }

            var enrichedEnumFields = new List<EnumFieldInfo>();
            foreach (var field in t.EnumFields)
            {
                var fieldKey = $"F:{t.FullName}.{field.Name}";
                var fieldDoc = _xmlDocs.TryGetValue(fieldKey, out var fd) ? fd : null;
                enrichedEnumFields.Add(field with { Summary = fieldDoc?.Summary ?? "" });
            }

            _types[i] = t with { Summary = summary, Remarks = remarks, Members = enrichedMembers, EnumFields = enrichedEnumFields, Examples = examples, TypeParameterDescriptions = typeParamDescs };
        }
    }

    XmlMemberDoc? FindMemberDoc(ApiTypeInfo type, TypeMemberInfo member)
    {
        var prefix = member.Kind switch
        {
            MemberKind.Property => "P",
            MemberKind.Method => "M",
            MemberKind.Event => "E",
            MemberKind.Field => "F",
            MemberKind.Constructor => "M",
            MemberKind.Operator => "M",
            _ => "P"
        };

        var typeName = NormalizeGenericName(type.FullName);

        if (member.Kind == MemberKind.Constructor)
        {
            var ctorKey = $"M:{typeName}.#ctor";
            foreach (var (key, doc) in _xmlDocs)
            {
                if (key.StartsWith(ctorKey, StringComparison.Ordinal))
                    return doc;
            }
            return null;
        }

        var memberKey = $"{prefix}:{typeName}.{member.Name}";
        if (_xmlDocs.TryGetValue(memberKey, out var result))
            return result;

        foreach (var (key, doc) in _xmlDocs)
        {
            if (key.StartsWith(memberKey, StringComparison.Ordinal))
                return doc;
        }

        return null;
    }

    static string NormalizeGenericName(string fullName)
    {
        var ltIdx = fullName.IndexOf('<', StringComparison.Ordinal);
        if (ltIdx < 0) return fullName;

        var genericCount = fullName[ltIdx..].Count(c => c == ',') + 1;
        return $"{fullName[..ltIdx]}`{genericCount}";
    }

    string RenderTypeLink(string typeName)
    {
        var display = SimplifyTypeName(typeName);
        var url = TryResolveTypeUrl(typeName);
        if (url != null)
        {
            return $"<RadzenLink Path=\"{url}\" Text=\"{EscapeHtml(display)}\" />";
        }
        return EscapeHtml(display);
    }

    string RenderSummary(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        var sb = new StringBuilder();
        int i = 0;
        while (i < text.Length)
        {
            var markerStart = text.IndexOf("\x01TYPEREF:", i, StringComparison.Ordinal);
            if (markerStart < 0)
            {
                sb.Append(EscapeHtml(text[i..]));
                break;
            }

            if (markerStart > i)
                sb.Append(EscapeHtml(text[i..markerStart]));

            var crefEnd = text.IndexOf('\x02', markerStart);
            if (crefEnd < 0) { sb.Append(EscapeHtml(text[markerStart..])); break; }

            var cref = text[(markerStart + "\x01TYPEREF:".Length)..crefEnd];

            var displayStart = crefEnd + 1;
            var displayEnd = text.IndexOf("\x01/TYPEREF\x02", displayStart, StringComparison.Ordinal);
            if (displayEnd < 0) { sb.Append(EscapeHtml(text[markerStart..])); break; }

            var display = text[displayStart..displayEnd];

            var url = TryResolveTypeUrl(cref);
            if (url != null)
            {
                sb.Append(CultureInfo.InvariantCulture, $"<RadzenLink Path=\"{url}\" Text=\"{EscapeHtml(display)}\" />");
            }
            else
            {
                sb.Append(EscapeHtml(display));
            }

            i = displayEnd + "\x01/TYPEREF\x02".Length;
        }

        return sb.ToString();
    }

    void GenerateIndexPage()
    {
        var sb = new StringBuilder();
        sb.AppendLine("@page \"/docs/api\"");
        sb.AppendLine();
        sb.AppendLine("<PageTitle>API Reference - Radzen Blazor Components</PageTitle>");
        sb.AppendLine();
        sb.AppendLine("<RadzenStack Gap=\"1rem\" class=\"rz-pt-4\">");
        sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.H3\" TagName=\"TagName.H1\">API Reference</RadzenText>");
        sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.Body1\">Search for a component by its name or part of the name.</RadzenText>");
        sb.AppendLine();

        var namespaces = _types.Select(t => t.Namespace).Where(n => !string.IsNullOrEmpty(n)).Distinct().OrderBy(n => n).ToList();

        sb.AppendLine("  <RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\">");
        sb.AppendLine("    <RadzenText TextStyle=\"TextStyle.H5\" TagName=\"TagName.H2\" class=\"rz-mb-3\">Namespaces</RadzenText>");
        sb.AppendLine("    <RadzenTable>");
        sb.AppendLine("      <RadzenTableHeader>");
        sb.AppendLine("        <RadzenTableHeaderRow>");
        sb.AppendLine("          <RadzenTableHeaderCell>Namespace</RadzenTableHeaderCell>");
        sb.AppendLine("        </RadzenTableHeaderRow>");
        sb.AppendLine("      </RadzenTableHeader>");
        sb.AppendLine("      <RadzenTableBody>");
        foreach (var ns in namespaces)
        {
            sb.AppendLine("        <RadzenTableRow>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"          <RadzenTableCell><RadzenLink Path=\"/docs/api/{ns}\" Text=\"{EscapeHtml(ns)}\" /></RadzenTableCell>");
            sb.AppendLine("        </RadzenTableRow>");
        }
        sb.AppendLine("      </RadzenTableBody>");
        sb.AppendLine("    </RadzenTable>");
        sb.AppendLine("  </RadzenCard>");
        sb.AppendLine("</RadzenStack>");

        WritePage(Path.Combine(_outputDir, "ApiIndex.razor"), sb.ToString());
    }

    void GenerateNamespacePages()
    {
        var namespaces = _types.GroupBy(t => t.Namespace).Where(g => !string.IsNullOrEmpty(g.Key)).OrderBy(g => g.Key);

        foreach (var nsGroup in namespaces)
        {
            var ns = nsGroup.Key;
            var sb = new StringBuilder();
            sb.AppendLine(CultureInfo.InvariantCulture, $"@page \"/docs/api/{ns}\"");
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"<PageTitle>{ns} Namespace - Radzen Blazor Components</PageTitle>");
            sb.AppendLine();
            sb.AppendLine("<RadzenStack Gap=\"1rem\" class=\"rz-pt-4\">");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  <RadzenText TextStyle=\"TextStyle.H3\" TagName=\"TagName.H1\" class=\"text-break\">{EscapeHtml(ns)} Namespace</RadzenText>");
            sb.AppendLine();

            var classes = nsGroup.Where(t => t.Kind == TypeKind.Class).OrderBy(t => t.Name).ToList();
            var structs = nsGroup.Where(t => t.Kind == TypeKind.Struct).OrderBy(t => t.Name).ToList();
            var interfaces = nsGroup.Where(t => t.Kind == TypeKind.Interface).OrderBy(t => t.Name).ToList();
            var enums = nsGroup.Where(t => t.Kind == TypeKind.Enum).OrderBy(t => t.Name).ToList();
            var delegates = nsGroup.Where(t => t.Kind == TypeKind.Delegate).OrderBy(t => t.Name).ToList();

            WriteTypeSection(sb, "Classes", classes);
            WriteTypeSection(sb, "Structs", structs);
            WriteTypeSection(sb, "Interfaces", interfaces);
            WriteTypeSection(sb, "Enums", enums);
            WriteTypeSection(sb, "Delegates", delegates);
            sb.AppendLine("</RadzenStack>");

            var fileName = $"NS_{ns.Replace('.', '_')}.razor";
            WritePage(Path.Combine(_outputDir, fileName), sb.ToString());
        }
    }

    void WriteTypeSection(StringBuilder sb, string heading, List<ApiTypeInfo> types)
    {
        if (types.Count == 0) return;

        sb.AppendLine("  <RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\">");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    <RadzenText TextStyle=\"TextStyle.H5\" TagName=\"TagName.H2\" class=\"rz-mb-3\">{EscapeHtml(heading)}</RadzenText>");
        sb.AppendLine("    <RadzenTable>");
        sb.AppendLine("      <RadzenTableHeader>");
        sb.AppendLine("        <RadzenTableHeaderRow>");
        sb.AppendLine("          <RadzenTableHeaderCell>Name</RadzenTableHeaderCell>");
        sb.AppendLine("          <RadzenTableHeaderCell>Description</RadzenTableHeaderCell>");
        sb.AppendLine("        </RadzenTableHeaderRow>");
        sb.AppendLine("      </RadzenTableHeader>");
        sb.AppendLine("      <RadzenTableBody>");
        foreach (var t in types)
        {
            var link = TypePageUrl(t);
            sb.AppendLine("        <RadzenTableRow>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"          <RadzenTableCell><RadzenLink Path=\"{link}\" Text=\"{EscapeHtml(t.Name)}\" /></RadzenTableCell>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"          <RadzenTableCell><RadzenText TextStyle=\"TextStyle.Body2\">{RenderSummary(t.Summary)}</RadzenText></RadzenTableCell>");
            sb.AppendLine("        </RadzenTableRow>");
        }
        sb.AppendLine("      </RadzenTableBody>");
        sb.AppendLine("    </RadzenTable>");
        sb.AppendLine("  </RadzenCard>");
    }

    void GenerateTypePages()
    {
        foreach (var type in _types)
        {
            var sb = new StringBuilder();
            var routeName = GetRouteTypeName(type);
            sb.AppendLine(CultureInfo.InvariantCulture, $"@page \"/docs/api/{routeName}\"");
            sb.AppendLine();
            sb.AppendLine(CultureInfo.InvariantCulture, $"<PageTitle>{EscapeHtml(type.Name)} {type.Kind} - Radzen Blazor Components</PageTitle>");
            sb.AppendLine();

            var tocItems = new List<(string Id, string Text, int Level)>();

            sb.AppendLine("<RadzenRow Gap=\"2rem\">");
            sb.AppendLine("<RadzenColumn Size=\"12\" SizeMD=\"10\">");
            sb.AppendLine("<RadzenStack Gap=\"1rem\" class=\"rz-pt-4\">");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  <RadzenText TextStyle=\"TextStyle.H3\" TagName=\"TagName.H1\" class=\"text-break\">{EscapeHtml(type.Name)} {type.Kind}</RadzenText>");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(type.Summary))
            {
                sb.AppendLine("  <RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\">");
                sb.AppendLine(CultureInfo.InvariantCulture, $"    <RadzenText TextStyle=\"TextStyle.Body1\">{RenderSummary(type.Summary)}</RadzenText>");
                sb.AppendLine("  </RadzenCard>");
                sb.AppendLine();
            }

            if (type.Kind == TypeKind.Class || type.Kind == TypeKind.Struct || type.Kind == TypeKind.Interface)
            {
                GenerateClassContent(sb, type, tocItems);
            }
            else if (type.Kind == TypeKind.Enum)
            {
                GenerateEnumContent(sb, type, tocItems);
            }
            else if (type.Kind == TypeKind.Delegate)
            {
                GenerateDelegateContent(sb, type);
            }
            sb.AppendLine("</RadzenStack>");
            sb.AppendLine("</RadzenColumn>");

            if (tocItems.Count > 0)
            {
                sb.AppendLine("<RadzenColumn Size=\"2\" class=\"rz-display-none rz-display-md-block\">");
                sb.AppendLine("  <RadzenStack class=\"rz-pt-4\" Style=\"position: sticky; top: 1rem;\">");
                sb.AppendLine("    <RadzenText Text=\"In This Article\" TextStyle=\"TextStyle.H6\" class=\"rz-mb-4\" />");
                sb.AppendLine("    <RadzenToc Selector=\".rz-body\">");
                foreach (var (id, text, _) in tocItems)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"      <RadzenTocItem Text=\"{EscapeHtml(text)}\" Selector=\"#{id}\" />");
                }
                sb.AppendLine("    </RadzenToc>");
                sb.AppendLine("  </RadzenStack>");
                sb.AppendLine("</RadzenColumn>");
            }

            sb.AppendLine("</RadzenRow>");

            var fileName = $"T_{GetSafeFileName(type)}.razor";
            WritePage(Path.Combine(_outputDir, fileName), sb.ToString());
        }
    }

    void GenerateClassContent(StringBuilder sb, ApiTypeInfo type, List<(string Id, string Text, int Level)> tocItems)
    {
        if (type.Kind == TypeKind.Class && type.Inheritance.Count > 0)
        {
            sb.AppendLine("<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\" id=\"inheritance\">");
            tocItems.Add(("inheritance", "Inheritance", 0));
            sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.H6\" TagName=\"TagName.H2\" class=\"rz-mb-2\">Inheritance</RadzenText>");
            sb.AppendLine("  <RadzenStack Gap=\"0.25rem\">");
            for (int i = 0; i < type.Inheritance.Count; i++)
            {
                var indent = new string(' ', i * 2);
                var inherited = type.Inheritance[i];
                sb.AppendLine(CultureInfo.InvariantCulture, $"    <RadzenText TextStyle=\"TextStyle.Body2\">{indent}{RenderTypeLink(inherited)}</RadzenText>");
            }
            var selfIndent = new string(' ', type.Inheritance.Count * 2);
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <RadzenText TextStyle=\"TextStyle.Subtitle1\">{selfIndent}{EscapeHtml(SimplifyTypeName(type.FullName))}</RadzenText>");
            if (type.DerivedTypes.Count > 0)
            {
                var derivedIndent = new string(' ', (type.Inheritance.Count + 1) * 2);
                foreach (var derived in type.DerivedTypes)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"    <RadzenText TextStyle=\"TextStyle.Body2\">{derivedIndent}{RenderTypeLink(derived)}</RadzenText>");
                }
            }
            sb.AppendLine("  </RadzenStack>");
            sb.AppendLine("</RadzenCard>");
            sb.AppendLine();
        }

        if (type.Interfaces.Count > 0)
        {
            sb.AppendLine("<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\" id=\"implements\">");
            tocItems.Add(("implements", "Implements", 0));
            sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.H6\" TagName=\"TagName.H2\" class=\"rz-mb-2\">Implements</RadzenText>");
            sb.AppendLine("  <RadzenStack Gap=\"0.25rem\">");
            foreach (var iface in type.Interfaces)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"    <RadzenText TextStyle=\"TextStyle.Body2\">{RenderTypeLink(iface)}</RadzenText>");
            }
            sb.AppendLine("  </RadzenStack>");
            sb.AppendLine("</RadzenCard>");
            sb.AppendLine();
        }

        if (type.InheritedMembers.Count > 0)
        {
            sb.AppendLine("<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\" id=\"inherited-members\">");
            tocItems.Add(("inherited-members", "Inherited Members", 0));
            sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.H6\" TagName=\"TagName.H2\" class=\"rz-mb-2\">Inherited Members</RadzenText>");
            sb.AppendLine("  <RadzenStack Gap=\"0.25rem\">");
            foreach (var m in type.InheritedMembers)
            {
                var declaringDisplay = SimplifyTypeName(m.DeclaringTypeName);
                var declaringUrl = TryResolveTypeUrl(m.DeclaringTypeName);
                if (declaringUrl != null)
                {
                    var fragment = $"#{MemberKindToSectionId(m.Kind)}-{SanitizeId(m.Name)}";
                    sb.AppendLine(CultureInfo.InvariantCulture, $"    <RadzenText TextStyle=\"TextStyle.Body2\"><RadzenLink Path=\"{declaringUrl}{fragment}\" Text=\"{EscapeHtml(declaringDisplay)}.{EscapeHtml(m.Name)}\" /></RadzenText>");
                }
                else
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"    <RadzenText TextStyle=\"TextStyle.Body2\">{EscapeHtml(declaringDisplay)}.{EscapeHtml(m.Name)}</RadzenText>");
                }
            }
            sb.AppendLine("  </RadzenStack>");
            sb.AppendLine("</RadzenCard>");
            sb.AppendLine();
        }

        sb.AppendLine("<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\">");
        sb.AppendLine("  <RadzenStack Gap=\"0.5rem\">");
        sb.AppendLine(CultureInfo.InvariantCulture, $"    <RadzenText TextStyle=\"TextStyle.Body2\">Namespace: <RadzenLink Path=\"/docs/api/{type.Namespace}\" Text=\"{EscapeHtml(type.Namespace)}\" /></RadzenText>");
        sb.AppendLine("    <RadzenText TextStyle=\"TextStyle.Body2\">Assembly: Radzen.Blazor.dll</RadzenText>");
        sb.AppendLine("  </RadzenStack>");
        sb.AppendLine("</RadzenCard>");
        sb.AppendLine();

        sb.AppendLine("<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\" id=\"syntax\">");
        tocItems.Add(("syntax", "Syntax", 0));
        sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.H6\" TagName=\"TagName.H2\" class=\"rz-mb-2\">Syntax</RadzenText>");
        WriteCodeBlock(sb, SimplifySignature(type.Syntax), 2);
        sb.AppendLine("</RadzenCard>");
        sb.AppendLine();

        if (type.TypeParameters.Count > 0)
        {
            sb.AppendLine("<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\" id=\"type-parameters\">");
            tocItems.Add(("type-parameters", "Type Parameters", 0));
            sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.H6\" TagName=\"TagName.H2\" class=\"rz-mb-2\">Type Parameters</RadzenText>");
            sb.AppendLine("  <RadzenTable>");
            sb.AppendLine("    <RadzenTableHeader>");
            sb.AppendLine("      <RadzenTableHeaderRow>");
            sb.AppendLine("        <RadzenTableHeaderCell>Name</RadzenTableHeaderCell>");
            sb.AppendLine("        <RadzenTableHeaderCell>Description</RadzenTableHeaderCell>");
            sb.AppendLine("      </RadzenTableHeaderRow>");
            sb.AppendLine("    </RadzenTableHeader>");
            sb.AppendLine("    <RadzenTableBody>");
            foreach (var tp in type.TypeParameters)
            {
                var tpDesc = type.TypeParameterDescriptions.TryGetValue(tp, out var desc) ? desc : "";
                sb.AppendLine(CultureInfo.InvariantCulture, $"      <RadzenTableRow><RadzenTableCell>{EscapeHtml(tp)}</RadzenTableCell><RadzenTableCell>{EscapeHtml(tpDesc)}</RadzenTableCell></RadzenTableRow>");
            }
            sb.AppendLine("    </RadzenTableBody>");
            sb.AppendLine("  </RadzenTable>");
            sb.AppendLine("</RadzenCard>");
            sb.AppendLine();
        }

        if (type.Examples.Count > 0)
        {
            sb.AppendLine("<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\" id=\"examples\">");
            tocItems.Add(("examples", "Examples", 0));
            sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.H6\" TagName=\"TagName.H2\" class=\"rz-mb-2\">Examples</RadzenText>");
            WriteExampleSegments(sb, type.Examples, 2);
            sb.AppendLine("</RadzenCard>");
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(type.Remarks))
        {
            sb.AppendLine("<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\" id=\"remarks\">");
            tocItems.Add(("remarks", "Remarks", 0));
            sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.H6\" TagName=\"TagName.H2\" class=\"rz-mb-2\">Remarks</RadzenText>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  <RadzenText TextStyle=\"TextStyle.Body2\">{EscapeHtml(type.Remarks)}</RadzenText>");
            sb.AppendLine("</RadzenCard>");
            sb.AppendLine();
        }

        var constructors = type.Members.Where(m => m.Kind == MemberKind.Constructor).ToList();
        var properties = type.Members.Where(m => m.Kind == MemberKind.Property).ToList();
        var methods = type.Members.Where(m => m.Kind == MemberKind.Method).ToList();
        var events = type.Members.Where(m => m.Kind == MemberKind.Event).ToList();
        var fields = type.Members.Where(m => m.Kind == MemberKind.Field).ToList();
        var operators = type.Members.Where(m => m.Kind == MemberKind.Operator).ToList();

        var routeName = GetRouteTypeName(type);
        WriteMembersSection(sb, "Constructors", "constructors", constructors, tocItems, routeName);
        WriteMembersSection(sb, "Fields", "fields", fields, tocItems, routeName);
        WriteMembersSection(sb, "Properties", "properties", properties, tocItems, routeName);
        WriteMembersSection(sb, "Methods", "methods", methods, tocItems, routeName);
        WriteMembersSection(sb, "Events", "events", events, tocItems, routeName);
        WriteMembersSection(sb, "Operators", "operators", operators, tocItems, routeName);
    }

    void WriteMembersSection(StringBuilder sb, string heading, string sectionId, List<TypeMemberInfo> members, List<(string Id, string Text, int Level)> tocItems, string routeName)
    {
        if (members.Count == 0) return;

        sb.AppendLine(CultureInfo.InvariantCulture, $"<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\" id=\"{sectionId}\">");
        tocItems.Add((sectionId, heading, 0));
        sb.AppendLine(CultureInfo.InvariantCulture, $"  <RadzenText TextStyle=\"TextStyle.H5\" TagName=\"TagName.H2\" class=\"rz-mb-3\">{EscapeHtml(heading)}</RadzenText>");
        sb.AppendLine("  <RadzenStack Gap=\"1rem\">");

        foreach (var member in members.OrderBy(m => m.Name, StringComparer.Ordinal))
        {
            var memberId = $"{sectionId}-{SanitizeId(member.Name)}";
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <RadzenCard Variant=\"Variant.Text\" class=\"rz-p-3\" id=\"{memberId}\">");
            sb.AppendLine(CultureInfo.InvariantCulture, $"      <RadzenText Anchor=\"{routeName}#{memberId}\" TextStyle=\"TextStyle.H6\" TagName=\"TagName.H3\" class=\"rz-mb-2\">{EscapeHtml(member.Name)}</RadzenText>");

            if (!string.IsNullOrEmpty(member.Summary))
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"      <RadzenText TextStyle=\"TextStyle.Body2\" class=\"rz-mb-2\">{RenderSummary(member.Summary)}</RadzenText>");
            }

            sb.AppendLine("      <RadzenText TextStyle=\"TextStyle.Subtitle2\" class=\"rz-mb-1\">Declaration</RadzenText>");
            WriteCodeBlock(sb, SimplifySignature(member.Signature), 6);

            if (member.Parameters.Count > 0)
            {
                sb.AppendLine("      <RadzenText TextStyle=\"TextStyle.Subtitle2\" class=\"rz-mb-1 rz-mt-2\">Parameters</RadzenText>");
                sb.AppendLine("      <RadzenTable>");
                sb.AppendLine("        <RadzenTableHeader>");
                sb.AppendLine("          <RadzenTableHeaderRow>");
                sb.AppendLine("            <RadzenTableHeaderCell>Type</RadzenTableHeaderCell>");
                sb.AppendLine("            <RadzenTableHeaderCell>Name</RadzenTableHeaderCell>");
                sb.AppendLine("            <RadzenTableHeaderCell>Description</RadzenTableHeaderCell>");
                sb.AppendLine("          </RadzenTableHeaderRow>");
                sb.AppendLine("        </RadzenTableHeader>");
                sb.AppendLine("        <RadzenTableBody>");
                foreach (var p in member.Parameters)
                {
                    sb.AppendLine("          <RadzenTableRow>");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"            <RadzenTableCell>{RenderTypeLink(p.TypeName)}</RadzenTableCell>");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"            <RadzenTableCell>{EscapeHtml(p.Name)}</RadzenTableCell>");
                    sb.AppendLine(CultureInfo.InvariantCulture, $"            <RadzenTableCell>{RenderSummary(p.Summary)}</RadzenTableCell>");
                    sb.AppendLine("          </RadzenTableRow>");
                }
                sb.AppendLine("        </RadzenTableBody>");
                sb.AppendLine("      </RadzenTable>");
            }

            if (member.Kind != MemberKind.Property && member.ReturnType != null && member.ReturnType != "System.Void" && member.ReturnType != "Void")
            {
                sb.AppendLine("      <RadzenText TextStyle=\"TextStyle.Subtitle2\" class=\"rz-mb-1 rz-mt-2\">Returns</RadzenText>");
                sb.AppendLine("      <RadzenTable>");
                sb.AppendLine("        <RadzenTableHeader>");
                sb.AppendLine("          <RadzenTableHeaderRow>");
                sb.AppendLine("            <RadzenTableHeaderCell>Type</RadzenTableHeaderCell>");
                sb.AppendLine("            <RadzenTableHeaderCell>Description</RadzenTableHeaderCell>");
                sb.AppendLine("          </RadzenTableHeaderRow>");
                sb.AppendLine("        </RadzenTableHeader>");
                sb.AppendLine("        <RadzenTableBody>");
                sb.AppendLine(CultureInfo.InvariantCulture, $"          <RadzenTableRow><RadzenTableCell>{RenderTypeLink(member.ReturnType)}</RadzenTableCell><RadzenTableCell>{RenderSummary(member.ReturnSummary ?? "")}</RadzenTableCell></RadzenTableRow>");
                sb.AppendLine("        </RadzenTableBody>");
                sb.AppendLine("      </RadzenTable>");
            }

            if (member.Kind == MemberKind.Property && !string.IsNullOrEmpty(member.TypeName))
            {
                sb.AppendLine("      <RadzenText TextStyle=\"TextStyle.Subtitle2\" class=\"rz-mb-1 rz-mt-2\">Property Value</RadzenText>");
                sb.AppendLine("      <RadzenTable>");
                sb.AppendLine("        <RadzenTableHeader>");
                sb.AppendLine("          <RadzenTableHeaderRow>");
                sb.AppendLine("            <RadzenTableHeaderCell>Type</RadzenTableHeaderCell>");
                sb.AppendLine("            <RadzenTableHeaderCell>Description</RadzenTableHeaderCell>");
                sb.AppendLine("          </RadzenTableHeaderRow>");
                sb.AppendLine("        </RadzenTableHeader>");
                sb.AppendLine("        <RadzenTableBody>");
                sb.AppendLine(CultureInfo.InvariantCulture, $"          <RadzenTableRow><RadzenTableCell>{RenderTypeLink(member.TypeName)}</RadzenTableCell><RadzenTableCell>{RenderSummary(member.Summary)}</RadzenTableCell></RadzenTableRow>");
                sb.AppendLine("        </RadzenTableBody>");
                sb.AppendLine("      </RadzenTable>");
            }

            if (member.Examples.Count > 0)
            {
                sb.AppendLine("      <RadzenText TextStyle=\"TextStyle.Subtitle2\" class=\"rz-mb-1 rz-mt-2\">Examples</RadzenText>");
                WriteExampleSegments(sb, member.Examples, 6);
            }

            sb.AppendLine("    </RadzenCard>");
        }
        sb.AppendLine("  </RadzenStack>");
        sb.AppendLine("</RadzenCard>");
    }

    void GenerateEnumContent(StringBuilder sb, ApiTypeInfo type, List<(string Id, string Text, int Level)> tocItems)
    {
        sb.AppendLine("<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\">");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  <RadzenText TextStyle=\"TextStyle.Body2\">Namespace: <RadzenLink Path=\"/docs/api/{type.Namespace}\" Text=\"{EscapeHtml(type.Namespace)}\" /></RadzenText>");
        sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.Body2\">Assembly: Radzen.Blazor.dll</RadzenText>");
        sb.AppendLine("</RadzenCard>");
        sb.AppendLine();

        sb.AppendLine("<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\" id=\"syntax\">");
        tocItems.Add(("syntax", "Syntax", 0));
        sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.H6\" TagName=\"TagName.H2\" class=\"rz-mb-2\">Syntax</RadzenText>");
        WriteCodeBlock(sb, SimplifySignature(type.Syntax), 2);
        sb.AppendLine("</RadzenCard>");
        sb.AppendLine();

        if (type.EnumFields.Count > 0)
        {
            sb.AppendLine("<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\" id=\"fields\">");
            tocItems.Add(("fields", "Fields", 0));
            sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.H5\" TagName=\"TagName.H2\" class=\"rz-mb-3\">Fields</RadzenText>");
            sb.AppendLine("  <RadzenTable>");
            sb.AppendLine("    <RadzenTableHeader>");
            sb.AppendLine("      <RadzenTableHeaderRow>");
            sb.AppendLine("        <RadzenTableHeaderCell>Name</RadzenTableHeaderCell>");
            sb.AppendLine("        <RadzenTableHeaderCell>Description</RadzenTableHeaderCell>");
            sb.AppendLine("      </RadzenTableHeaderRow>");
            sb.AppendLine("    </RadzenTableHeader>");
            sb.AppendLine("    <RadzenTableBody>");
            foreach (var field in type.EnumFields)
            {
                sb.AppendLine("      <RadzenTableRow>");
                sb.AppendLine(CultureInfo.InvariantCulture, $"        <RadzenTableCell>{EscapeHtml(field.Name)}</RadzenTableCell>");
                sb.AppendLine(CultureInfo.InvariantCulture, $"        <RadzenTableCell>{RenderSummary(field.Summary)}</RadzenTableCell>");
                sb.AppendLine("      </RadzenTableRow>");
            }
            sb.AppendLine("    </RadzenTableBody>");
            sb.AppendLine("  </RadzenTable>");
            sb.AppendLine("</RadzenCard>");
        }
    }

    static void GenerateDelegateContent(StringBuilder sb, ApiTypeInfo type)
    {
        sb.AppendLine("<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\">");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  <RadzenText TextStyle=\"TextStyle.Body2\">Namespace: <RadzenLink Path=\"/docs/api/{type.Namespace}\" Text=\"{EscapeHtml(type.Namespace)}\" /></RadzenText>");
        sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.Body2\">Assembly: Radzen.Blazor.dll</RadzenText>");
        sb.AppendLine("</RadzenCard>");
        sb.AppendLine();

        sb.AppendLine("<RadzenCard Variant=\"Variant.Outlined\" class=\"rz-p-4\">");
        sb.AppendLine("  <RadzenText TextStyle=\"TextStyle.H6\" TagName=\"TagName.H2\" class=\"rz-mb-2\">Syntax</RadzenText>");
        WriteCodeBlock(sb, SimplifySignature(type.Syntax), 2);
        sb.AppendLine("</RadzenCard>");
    }

    static void WriteCodeBlock(StringBuilder sb, string code, int indentSpaces)
    {
        var indent = new string(' ', indentSpaces);
        sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}<pre style=\"background: var(--rz-base-200); padding: 1rem; border-radius: var(--rz-border-radius); overflow-x: auto; font-family: monospace; font-size: 0.875rem;\"><code>{EscapeHtml(code)}</code></pre>");
    }

    static void WriteExampleSegments(StringBuilder sb, List<ExampleSegment> segments, int indentSpaces)
    {
        var indent = new string(' ', indentSpaces);
        foreach (var segment in segments)
        {
            if (segment.IsCode)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}<pre style=\"background: var(--rz-base-200); padding: 1rem; border-radius: var(--rz-border-radius); overflow-x: auto; font-family: monospace; font-size: 0.875rem;\"><code>{EscapeHtml(segment.Content)}</code></pre>");
            }
            else
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"{indent}<RadzenText TextStyle=\"TextStyle.Body2\" class=\"rz-mb-2\">{EscapeHtml(segment.Content)}</RadzenText>");
            }
        }
    }

    void GenerateApiNavComponent()
    {
        var sb = new StringBuilder();
        sb.AppendLine("namespace Radzen.Blazor.Api.Generated.Pages;");
        sb.AppendLine();
        sb.AppendLine("public static class ApiNavData");
        sb.AppendLine("{");
        sb.AppendLine("    public static readonly (string Name, (string Name, string Path)[] Types)[] Namespaces =");
        sb.AppendLine("    [");

        var namespaces = _types.GroupBy(t => t.Namespace)
            .Where(g => !string.IsNullOrEmpty(g.Key))
            .OrderBy(g => g.Key);

        foreach (var nsGroup in namespaces)
        {
            var ns = nsGroup.Key;
            sb.AppendLine(CultureInfo.InvariantCulture, $"        (\"{ns}\", new (string, string)[]");
            sb.AppendLine("        {");
            foreach (var type in nsGroup.OrderBy(t => t.Name, StringComparer.Ordinal))
            {
                var url = TypePageUrl(type);
                sb.AppendLine(CultureInfo.InvariantCulture, $"            (\"{EscapeCSharpString(type.Name)}\", \"{url}\"),");
            }
            sb.AppendLine("        }),");
        }

        sb.AppendLine("    ];");
        sb.AppendLine("}");

        File.WriteAllText(Path.Combine(_outputDir, "ApiNavData.cs"), sb.ToString(), Encoding.UTF8);
    }

    static string EscapeCSharpString(string text)
    {
        return text.Replace("\\", "\\\\", StringComparison.Ordinal)
                   .Replace("\"", "\\\"", StringComparison.Ordinal);
    }

    void GenerateImportsFile()
    {
        var sb = new StringBuilder();
        sb.AppendLine("@using Microsoft.AspNetCore.Components");
        sb.AppendLine("@using Microsoft.AspNetCore.Components.Routing");
        sb.AppendLine("@using Microsoft.AspNetCore.Components.Web");
        sb.AppendLine("@using Radzen");
        sb.AppendLine("@using Radzen.Blazor");

        File.WriteAllText(Path.Combine(_outputDir, "_Imports.razor"), sb.ToString(), Encoding.UTF8);
    }

    static string TypePageUrl(ApiTypeInfo type) => $"/docs/api/{GetRouteTypeName(type)}";

    static string GetRouteTypeName(ApiTypeInfo type)
    {
        var name = type.FullName;
        var ltIdx = name.IndexOf('<', StringComparison.Ordinal);
        if (ltIdx >= 0)
        {
            var genericCount = name[ltIdx..].Count(c => c == ',') + 1;
            name = $"{name[..ltIdx]}-{genericCount}";
        }
        return name;
    }

    static string GetSafeFileName(ApiTypeInfo type)
    {
        var name = GetRouteTypeName(type);
        return name.Replace('.', '_').Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace(' ', '_');
    }

    static string SanitizeId(string name)
    {
        return name.Replace(' ', '-').Replace('<', '_').Replace('>', '_').Replace(',', '_').Replace('(', '_').Replace(')', '_');
    }

    static string MemberKindToSectionId(MemberKind kind) => kind switch
    {
        MemberKind.Constructor => "constructors",
        MemberKind.Field => "fields",
        MemberKind.Property => "properties",
        MemberKind.Method => "methods",
        MemberKind.Event => "events",
        MemberKind.Operator => "operators",
        _ => "properties"
    };

    static readonly Dictionary<string, string> BuiltInTypeMap = new(StringComparer.Ordinal)
    {
        ["System.String"] = "string",
        ["System.Boolean"] = "bool",
        ["System.Int32"] = "int",
        ["System.Int64"] = "long",
        ["System.Int16"] = "short",
        ["System.Byte"] = "byte",
        ["System.SByte"] = "sbyte",
        ["System.UInt32"] = "uint",
        ["System.UInt64"] = "ulong",
        ["System.UInt16"] = "ushort",
        ["System.Single"] = "float",
        ["System.Double"] = "double",
        ["System.Decimal"] = "decimal",
        ["System.Char"] = "char",
        ["System.Object"] = "object",
        ["System.Void"] = "void",
        ["Void"] = "void",
    };

    static readonly string[] StripPrefixes = ["System.Collections.Generic.", "System.Collections.ObjectModel.", "System.Threading.Tasks.", "System.Linq.", "System.Linq.Expressions.", "Microsoft.AspNetCore.Components.Web.", "Microsoft.AspNetCore.Components.", "Radzen.Blazor.", "Radzen.", "System."];

    static string SimplifyTypeName(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return typeName;

        if (BuiltInTypeMap.TryGetValue(typeName, out var builtIn))
            return builtIn;

        var result = HandleNullable(typeName);
        result = StripNamespacePrefixes(result);
        return result;
    }

    static string HandleNullable(string typeName)
    {
        const string nullablePrefix = "System.Nullable<";
        if (typeName.StartsWith(nullablePrefix, StringComparison.Ordinal) && typeName.EndsWith('>'))
        {
            var inner = typeName[nullablePrefix.Length..^1];
            return SimplifyTypeName(inner) + "?";
        }
        return typeName;
    }

    static string StripNamespacePrefixes(string typeName)
    {
        var result = new StringBuilder();
        int i = 0;
        while (i < typeName.Length)
        {
            if (typeName[i] == '<')
            {
                result.Append('<');
                i++;
                int depth = 1;
                int segStart = i;
                var segments = new List<string>();
                while (i < typeName.Length && depth > 0)
                {
                    if (typeName[i] == '<') depth++;
                    else if (typeName[i] == '>') { depth--; if (depth == 0) break; }
                    else if (typeName[i] == ',' && depth == 1)
                    {
                        segments.Add(typeName[segStart..i].Trim());
                        segStart = i + 1;
                    }
                    i++;
                }
                segments.Add(typeName[segStart..i].Trim());
                result.Append(string.Join(", ", segments.Select(SimplifyTypeName)));
                if (i < typeName.Length && typeName[i] == '>')
                {
                    result.Append('>');
                    i++;
                }
                continue;
            }
            result.Append(typeName[i]);
            i++;
        }

        var simplified = result.ToString();
        foreach (var prefix in StripPrefixes)
        {
            if (simplified.StartsWith(prefix, StringComparison.Ordinal))
            {
                simplified = simplified[prefix.Length..];
                break;
            }
        }
        return simplified;
    }

    static string SimplifySignature(string signature)
    {
        var result = new StringBuilder();
        var tokens = TokenizeSignature(signature);
        foreach (var token in tokens)
        {
            if (token.IsType)
                result.Append(SimplifyTypeName(token.Text));
            else
                result.Append(token.Text);
        }
        return result.ToString();
    }

    static List<(string Text, bool IsType)> TokenizeSignature(string sig)
    {
        var tokens = new List<(string Text, bool IsType)>();
        var separators = new HashSet<char> { ' ', '(', ')', '{', '}', ';', ',' };
        var keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "public", "protected", "private", "internal", "static", "virtual", "override",
            "abstract", "sealed", "readonly", "new", "event", "class", "struct", "interface",
            "enum", "get", "set", "operator", "implicit", "explicit"
        };

        int i = 0;
        while (i < sig.Length)
        {
            if (separators.Contains(sig[i]))
            {
                tokens.Add((sig[i].ToString(), false));
                i++;
                continue;
            }

            int start = i;
            int depth = 0;
            while (i < sig.Length && (!separators.Contains(sig[i]) || depth > 0))
            {
                if (sig[i] == '<') depth++;
                else if (sig[i] == '>') depth--;
                i++;
            }

            var word = sig[start..i];
            bool isType = !keywords.Contains(word) && (word.Contains('.', StringComparison.Ordinal) || word.Contains('<', StringComparison.Ordinal) ||
                           char.IsUpper(word[0]) && !keywords.Contains(word));
            tokens.Add((word, isType));
        }

        return tokens;
    }

    static string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        return text
            .Replace("&", "&amp;", StringComparison.Ordinal)
            .Replace("<", "&lt;", StringComparison.Ordinal)
            .Replace(">", "&gt;", StringComparison.Ordinal)
            .Replace("\"", "&quot;", StringComparison.Ordinal)
            .Replace("@", "@@", StringComparison.Ordinal);
    }

    void WritePage(string path, string content)
    {
        File.WriteAllText(path, content, Encoding.UTF8);
        _pageCount++;
    }
}
