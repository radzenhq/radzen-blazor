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

sealed record XmlMemberDoc(string Summary, string Value, string Example, string Remarks);

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
            var example = CleanText(member.Element("example")?.Value);
            var remarks = CleanText(member.Element("remarks")?.Value);

            docs[memberName] = new XmlMemberDoc(summary, value, example, remarks);
        }

        return docs;
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
                        var lastDot = cref.LastIndexOf('.');
                        var name = lastDot >= 0 ? cref[(lastDot + 1)..] : cref;
                        if (name.StartsWith("T:", StringComparison.Ordinal) || name.StartsWith("P:", StringComparison.Ordinal) || name.StartsWith("M:", StringComparison.Ordinal) || name.StartsWith("F:", StringComparison.Ordinal) || name.StartsWith("E:", StringComparison.Ordinal))
                            name = name[2..];
                        sb.Append(name);
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

    static string CleanText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return "";
        return Regex.Replace(text, @"\s+", " ").Trim();
    }
}

sealed record TypeMemberInfo(string Name, string DeclaringTypeName, string TypeName, string Summary, MemberKind Kind, string Signature, IReadOnlyList<ParameterInfo> Parameters, string? ReturnType, string? ReturnSummary);
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
    IReadOnlyList<TypeMemberInfo> Members,
    IReadOnlyList<EnumFieldInfo> EnumFields);
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

        var syntax = BuildSyntaxDeclaration(type);

        return new ApiTypeInfo(fullName, name, ns, kind, baseTypeName, "", "", syntax,
            inheritance, interfaces, derived, typeParams, members, enumFields);
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
            members.Add(new TypeMemberInfo(FormatShortName(type), FormatTypeName(type), "", "", MemberKind.Constructor, sig, parameters, null, null));
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
            members.Add(new TypeMemberInfo(prop.Name, FormatTypeName(type), propTypeName, "", MemberKind.Property, sig, Array.Empty<ParameterInfo>(), propTypeName, null));
        }

        foreach (var method in type.GetMethods(flags).Where(m => (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly) && !m.IsSpecialName))
        {
            if (IsCompilerGeneratedMember(method.Name)) continue;
            if (IsObjectMethod(method.Name)) continue;
            var parameters = method.GetParameters().Select(p => new ParameterInfo(p.Name ?? "", FormatTypeName(p.ParameterType), "")).ToList();
            var returnType = FormatTypeName(method.ReturnType);
            var mods = BuildMethodModifiers(method);
            var sig = $"{mods} {returnType} {method.Name}({string.Join(", ", parameters.Select(p => $"{p.TypeName} {p.Name}"))})";
            members.Add(new TypeMemberInfo(method.Name, FormatTypeName(type), "", "", MemberKind.Method, sig, parameters, returnType, null));
        }

        foreach (var method in type.GetMethods(flags).Where(m => m.IsPublic && m.IsSpecialName && m.IsStatic && m.Name.StartsWith("op_", StringComparison.Ordinal)))
        {
            var opName = FormatOperatorName(method.Name);
            if (opName == null) continue;
            var parameters = method.GetParameters().Select(p => new ParameterInfo(p.Name ?? "", FormatTypeName(p.ParameterType), "")).ToList();
            var returnType = FormatTypeName(method.ReturnType);
            var sig = $"public static {returnType} {opName}({string.Join(", ", parameters.Select(p => $"{p.TypeName} {p.Name}"))})";
            members.Add(new TypeMemberInfo(opName, FormatTypeName(type), "", "", MemberKind.Operator, sig, parameters, returnType, null));
        }

        foreach (var evt in type.GetEvents(flags))
        {
            if (!IsPublicOrProtected(evt.AddMethod)) continue;
            var evtTypeName = evt.EventHandlerType != null ? FormatTypeName(evt.EventHandlerType) : "EventHandler";
            var access = GetAccessModifier(evt.AddMethod);
            var sig = $"{access} event {evtTypeName} {evt.Name}";
            members.Add(new TypeMemberInfo(evt.Name, FormatTypeName(type), evtTypeName, "", MemberKind.Event, sig, Array.Empty<ParameterInfo>(), null, null));
        }

        foreach (var field in type.GetFields(flags).Where(f => (f.IsPublic || f.IsFamily || f.IsFamilyOrAssembly) && !f.IsSpecialName))
        {
            if (IsCompilerGeneratedMember(field.Name)) continue;
            var fieldTypeName = FormatTypeName(field.FieldType);
            var access = field.IsPublic ? "public" : "protected";
            var modifier = field.IsStatic ? "static " : "";
            var readonlyMod = field.IsInitOnly ? "readonly " : "";
            var sig = $"{access} {modifier}{readonlyMod}{fieldTypeName} {field.Name}";
            members.Add(new TypeMemberInfo(field.Name, FormatTypeName(type), fieldTypeName, "", MemberKind.Field, sig, Array.Empty<ParameterInfo>(), null, null));
        }

        return members;
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
            var name = type.FullName ?? type.Name;
            var tick = name.IndexOf('`', StringComparison.Ordinal);
            if (tick >= 0)
            {
                var baseName = name[..tick];
                var args = type.GetGenericArguments();
                return $"{baseName}<{string.Join(", ", args.Select(FormatTypeName))}>";
            }
        }

        return type.FullName ?? type.Name;
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

        EnrichTypesWithXmlDocs();

        GenerateIndexPage();
        GenerateNamespacePages();
        GenerateTypePages();
        GenerateImportsFile();
    }

    void EnrichTypesWithXmlDocs()
    {
        for (int i = 0; i < _types.Count; i++)
        {
            var t = _types[i];
            var typeKey = $"T:{t.FullName}";
            var cleanKey = typeKey.Replace("<", "{", StringComparison.Ordinal).Replace(">", "}", StringComparison.Ordinal).Replace(", ", ",", StringComparison.Ordinal);
            if (cleanKey.Contains('<', StringComparison.Ordinal))
            {
                var tickIdx = t.FullName.IndexOf('<', StringComparison.Ordinal);
                if (tickIdx >= 0)
                {
                    var genericCount = t.FullName.Count(c => c == ',') + 1;
                    cleanKey = $"T:{t.FullName[..tickIdx]}`{genericCount}";
                }
            }

            string summary = "", remarks = "";
            if (_xmlDocs.TryGetValue(cleanKey, out var typeDoc))
            {
                summary = typeDoc.Summary;
                remarks = typeDoc.Remarks;
            }
            else if (_xmlDocs.TryGetValue(typeKey, out typeDoc))
            {
                summary = typeDoc.Summary;
                remarks = typeDoc.Remarks;
            }

            var enrichedMembers = new List<TypeMemberInfo>();
            foreach (var member in t.Members)
            {
                var memberDoc = FindMemberDoc(t, member);
                var enriched = member with
                {
                    Summary = memberDoc?.Summary ?? "",
                    TypeName = member.TypeName
                };

                if (member.Kind is MemberKind.Method or MemberKind.Constructor or MemberKind.Operator)
                {
                    var enrichedParams = new List<ParameterInfo>();
                    foreach (var p in member.Parameters)
                    {
                        enrichedParams.Add(p with { Summary = FindParamDoc(t, member, p.Name) });
                    }
                    enriched = enriched with { Parameters = enrichedParams };
                }

                if (member.ReturnType != null && memberDoc?.Summary != null)
                {
                    enriched = enriched with { ReturnSummary = FindReturnDoc(t, member) };
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

            _types[i] = t with { Summary = summary, Remarks = remarks, Members = enrichedMembers, EnumFields = enrichedEnumFields };
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

    static string FindParamDoc(ApiTypeInfo type, TypeMemberInfo member, string paramName)
    {
        return "";
    }

    static string? FindReturnDoc(ApiTypeInfo type, TypeMemberInfo member)
    {
        return null;
    }

    static string NormalizeGenericName(string fullName)
    {
        var ltIdx = fullName.IndexOf('<', StringComparison.Ordinal);
        if (ltIdx < 0) return fullName;

        var genericCount = fullName[ltIdx..].Count(c => c == ',') + 1;
        return $"{fullName[..ltIdx]}`{genericCount}";
    }

    void GenerateIndexPage()
    {
        var sb = new StringBuilder();
        sb.AppendLine("@page \"/docs/api\"");
        sb.AppendLine();
        sb.AppendLine("<PageTitle>API Reference - Radzen Blazor Components</PageTitle>");
        sb.AppendLine();
        sb.AppendLine("<h1>API Reference</h1>");
        sb.AppendLine();

        var namespaces = _types.Select(t => t.Namespace).Where(n => !string.IsNullOrEmpty(n)).Distinct().OrderBy(n => n).ToList();

        sb.AppendLine("<h3>Namespaces</h3>");
        sb.AppendLine("<table class=\"table table-bordered table-striped\">");
        sb.AppendLine("  <thead><tr><th>Namespace</th><th>Description</th></tr></thead>");
        sb.AppendLine("  <tbody>");
        foreach (var ns in namespaces)
        {
            var safeName = SanitizeFileName(ns);
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <tr><td><a href=\"/docs/api/{ns}\">{ns}</a></td><td></td></tr>");
        }
        sb.AppendLine("  </tbody>");
        sb.AppendLine("</table>");

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
            sb.AppendLine(CultureInfo.InvariantCulture, $"<h1 class=\"text-break\">{ns} Namespace</h1>");
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

            var fileName = $"NS_{ns.Replace('.', '_')}.razor";
            WritePage(Path.Combine(_outputDir, fileName), sb.ToString());
        }
    }

    static void WriteTypeSection(StringBuilder sb, string heading, List<ApiTypeInfo> types)
    {
        if (types.Count == 0) return;

        sb.AppendLine(CultureInfo.InvariantCulture, $"<h3>{heading}</h3>");
        sb.AppendLine("<table class=\"table table-bordered table-striped\">");
        sb.AppendLine("  <thead><tr><th>Name</th><th>Description</th></tr></thead>");
        sb.AppendLine("  <tbody>");
        foreach (var t in types)
        {
            var link = TypePageUrl(t);
            var summary = EscapeHtml(t.Summary);
            sb.AppendLine(CultureInfo.InvariantCulture, $"    <tr><td><a href=\"{link}\">{EscapeHtml(t.Name)}</a></td><td>{summary}</td></tr>");
        }
        sb.AppendLine("  </tbody>");
        sb.AppendLine("</table>");
        sb.AppendLine();
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

            sb.AppendLine(CultureInfo.InvariantCulture, $"<h1 class=\"text-break\">{EscapeHtml(type.Name)} {type.Kind}</h1>");
            sb.AppendLine();

            if (!string.IsNullOrEmpty(type.Summary))
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"<div class=\"markdown level0 summary\">{EscapeHtml(type.Summary)}</div>");
                sb.AppendLine();
            }

            if (type.Kind == TypeKind.Class || type.Kind == TypeKind.Struct || type.Kind == TypeKind.Interface)
            {
                GenerateClassContent(sb, type);
            }
            else if (type.Kind == TypeKind.Enum)
            {
                GenerateEnumContent(sb, type);
            }
            else if (type.Kind == TypeKind.Delegate)
            {
                GenerateDelegateContent(sb, type);
            }

            var fileName = $"T_{GetSafeFileName(type)}.razor";
            WritePage(Path.Combine(_outputDir, fileName), sb.ToString());
        }
    }

    static void GenerateClassContent(StringBuilder sb, ApiTypeInfo type)
    {
        if (type.Kind == TypeKind.Class && type.Inheritance.Count > 0)
        {
            sb.AppendLine("<div class=\"inheritance\">");
            sb.AppendLine("  <h5>Inheritance</h5>");
            for (int i = 0; i < type.Inheritance.Count; i++)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"  <div class=\"level{i}\">{EscapeHtml(type.Inheritance[i])}</div>");
            }
            sb.AppendLine(CultureInfo.InvariantCulture, $"  <div class=\"level{type.Inheritance.Count}\"><span class=\"xref\">{EscapeHtml(type.Name)}</span></div>");
            if (type.DerivedTypes.Count > 0)
            {
                foreach (var derived in type.DerivedTypes)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"  <div class=\"level{type.Inheritance.Count + 1}\">{EscapeHtml(derived)}</div>");
                }
            }
            sb.AppendLine("</div>");
            sb.AppendLine();
        }

        if (type.Interfaces.Count > 0)
        {
            sb.AppendLine("<div class=\"implements\">");
            sb.AppendLine("  <h5>Implements</h5>");
            foreach (var iface in type.Interfaces)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"  <div>{EscapeHtml(iface)}</div>");
            }
            sb.AppendLine("</div>");
            sb.AppendLine();
        }

        sb.AppendLine(CultureInfo.InvariantCulture, $"<h6><strong>Namespace</strong>: {EscapeHtml(type.Namespace)}</h6>");
        sb.AppendLine("<h6><strong>Assembly</strong>: Radzen.Blazor.dll</h6>");
        sb.AppendLine();

        sb.AppendLine("<h5>Syntax</h5>");
        sb.AppendLine("<div class=\"codewrapper\">");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  <pre><code class=\"lang-csharp hljs\">{EscapeHtml(type.Syntax)}</code></pre>");
        sb.AppendLine("</div>");
        sb.AppendLine();

        if (type.TypeParameters.Count > 0)
        {
            sb.AppendLine("<h5>Type Parameters</h5>");
            sb.AppendLine("<table class=\"table table-bordered table-striped table-condensed\">");
            sb.AppendLine("  <thead><tr><th>Name</th><th>Description</th></tr></thead>");
            sb.AppendLine("  <tbody>");
            foreach (var tp in type.TypeParameters)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"    <tr><td><span class=\"parametername\">{EscapeHtml(tp)}</span></td><td></td></tr>");
            }
            sb.AppendLine("  </tbody>");
            sb.AppendLine("</table>");
            sb.AppendLine();
        }

        if (!string.IsNullOrEmpty(type.Remarks))
        {
            sb.AppendLine("<h5><strong>Remarks</strong></h5>");
            sb.AppendLine(CultureInfo.InvariantCulture, $"<div class=\"markdown level0 remarks\">{EscapeHtml(type.Remarks)}</div>");
            sb.AppendLine();
        }

        var constructors = type.Members.Where(m => m.Kind == MemberKind.Constructor).ToList();
        var properties = type.Members.Where(m => m.Kind == MemberKind.Property).ToList();
        var methods = type.Members.Where(m => m.Kind == MemberKind.Method).ToList();
        var events = type.Members.Where(m => m.Kind == MemberKind.Event).ToList();
        var fields = type.Members.Where(m => m.Kind == MemberKind.Field).ToList();
        var operators = type.Members.Where(m => m.Kind == MemberKind.Operator).ToList();

        WriteMembersSection(sb, "Constructors", constructors);
        WriteMembersSection(sb, "Fields", fields);
        WriteMembersSection(sb, "Properties", properties);
        WriteMembersSection(sb, "Methods", methods);
        WriteMembersSection(sb, "Events", events);
        WriteMembersSection(sb, "Operators", operators);
    }

    static void WriteMembersSection(StringBuilder sb, string heading, List<TypeMemberInfo> members)
    {
        if (members.Count == 0) return;

        sb.AppendLine(CultureInfo.InvariantCulture, $"<h3>{heading}</h3>");

        foreach (var member in members.OrderBy(m => m.Name, StringComparer.Ordinal))
        {
            sb.AppendLine(CultureInfo.InvariantCulture, $"<h4>{EscapeHtml(member.Name)}</h4>");

            if (!string.IsNullOrEmpty(member.Summary))
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"<div class=\"markdown level1 summary\">{EscapeHtml(member.Summary)}</div>");
            }

            sb.AppendLine("<h5>Declaration</h5>");
            sb.AppendLine("<div class=\"codewrapper\">");
            sb.AppendLine(CultureInfo.InvariantCulture, $"  <pre><code class=\"lang-csharp hljs\">{EscapeHtml(member.Signature)}</code></pre>");
            sb.AppendLine("</div>");

            if (member.Parameters.Count > 0)
            {
                sb.AppendLine("<h5>Parameters</h5>");
                sb.AppendLine("<table class=\"table table-bordered table-striped table-condensed\">");
                sb.AppendLine("  <thead><tr><th>Type</th><th>Name</th><th>Description</th></tr></thead>");
                sb.AppendLine("  <tbody>");
                foreach (var p in member.Parameters)
                {
                    sb.AppendLine(CultureInfo.InvariantCulture, $"    <tr><td>{EscapeHtml(p.TypeName)}</td><td><span class=\"parametername\">{EscapeHtml(p.Name)}</span></td><td>{EscapeHtml(p.Summary)}</td></tr>");
                }
                sb.AppendLine("  </tbody>");
                sb.AppendLine("</table>");
            }

            if (member.ReturnType != null && member.ReturnType != "System.Void" && member.ReturnType != "Void")
            {
                sb.AppendLine("<h5>Returns</h5>");
                sb.AppendLine("<table class=\"table table-bordered table-striped table-condensed\">");
                sb.AppendLine("  <thead><tr><th>Type</th><th>Description</th></tr></thead>");
                sb.AppendLine("  <tbody>");
                sb.AppendLine(CultureInfo.InvariantCulture, $"    <tr><td>{EscapeHtml(member.ReturnType)}</td><td>{EscapeHtml(member.ReturnSummary ?? "")}</td></tr>");
                sb.AppendLine("  </tbody>");
                sb.AppendLine("</table>");
            }

            if (member.Kind == MemberKind.Property && !string.IsNullOrEmpty(member.TypeName))
            {
                sb.AppendLine("<h5>Property Value</h5>");
                sb.AppendLine("<table class=\"table table-bordered table-striped table-condensed\">");
                sb.AppendLine("  <thead><tr><th>Type</th><th>Description</th></tr></thead>");
                sb.AppendLine("  <tbody>");
                sb.AppendLine(CultureInfo.InvariantCulture, $"    <tr><td>{EscapeHtml(member.TypeName)}</td><td></td></tr>");
                sb.AppendLine("  </tbody>");
                sb.AppendLine("</table>");
            }

            sb.AppendLine();
        }
    }

    static void GenerateEnumContent(StringBuilder sb, ApiTypeInfo type)
    {
        sb.AppendLine(CultureInfo.InvariantCulture, $"<h6><strong>Namespace</strong>: {EscapeHtml(type.Namespace)}</h6>");
        sb.AppendLine("<h6><strong>Assembly</strong>: Radzen.Blazor.dll</h6>");
        sb.AppendLine();

        sb.AppendLine("<h5>Syntax</h5>");
        sb.AppendLine("<div class=\"codewrapper\">");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  <pre><code class=\"lang-csharp hljs\">{EscapeHtml(type.Syntax)}</code></pre>");
        sb.AppendLine("</div>");
        sb.AppendLine();

        if (type.EnumFields.Count > 0)
        {
            sb.AppendLine("<h3>Fields</h3>");
            sb.AppendLine("<table class=\"table table-bordered table-striped table-condensed\">");
            sb.AppendLine("  <thead><tr><th>Name</th><th>Description</th></tr></thead>");
            sb.AppendLine("  <tbody>");
            foreach (var field in type.EnumFields)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"    <tr><td>{EscapeHtml(field.Name)}</td><td>{EscapeHtml(field.Summary)}</td></tr>");
            }
            sb.AppendLine("  </tbody>");
            sb.AppendLine("</table>");
        }
    }

    static void GenerateDelegateContent(StringBuilder sb, ApiTypeInfo type)
    {
        sb.AppendLine(CultureInfo.InvariantCulture, $"<h6><strong>Namespace</strong>: {EscapeHtml(type.Namespace)}</h6>");
        sb.AppendLine("<h6><strong>Assembly</strong>: Radzen.Blazor.dll</h6>");
        sb.AppendLine();

        sb.AppendLine("<h5>Syntax</h5>");
        sb.AppendLine("<div class=\"codewrapper\">");
        sb.AppendLine(CultureInfo.InvariantCulture, $"  <pre><code class=\"lang-csharp hljs\">{EscapeHtml(type.Syntax)}</code></pre>");
        sb.AppendLine("</div>");
    }

    void GenerateImportsFile()
    {
        var sb = new StringBuilder();
        sb.AppendLine("@using Microsoft.AspNetCore.Components");
        sb.AppendLine("@using Microsoft.AspNetCore.Components.Routing");
        sb.AppendLine("@using Microsoft.AspNetCore.Components.Web");

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

    static string SanitizeFileName(string name) => name.Replace('.', '_');

    void WritePage(string path, string content)
    {
        File.WriteAllText(path, content, Encoding.UTF8);
        _pageCount++;
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
}
