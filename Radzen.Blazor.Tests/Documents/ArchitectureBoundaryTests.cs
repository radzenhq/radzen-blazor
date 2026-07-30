#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Radzen.Documents;
using Xunit;

namespace Radzen.Blazor.Documents.Tests;

public class ArchitectureBoundaryTests
{
    private const string DocumentsNamespace = "Radzen.Documents";

    private const string PdfNamespace = "Radzen.Documents.Pdf";

    private const string LayoutNamespace = "Radzen.Documents.Layout";

    private const string FontsNamespace = "Radzen.Documents.Fonts";

    private const string GeometryNamespace = "Radzen.Documents.Geometry";

    private const string ComponentNamespace = "Radzen.Blazor";

    private const string PdfRenderNamespace = "Radzen.Documents.Pdf.Render";

    private const string PdfWriteNamespace = "Radzen.Documents.Pdf.Write";

    private const string PdfContentNamespace = "Radzen.Documents.Pdf.Content";

    private const string PdfEmissionNamespace = "Radzen.Documents.Pdf.Emission";

    private static readonly string[] NeutralNamespaces =
    [
        "Radzen.Documents",
        "Radzen.Documents.Codes",
        "Radzen.Documents.Crypto",
        "Radzen.Documents.Fonts",
        "Radzen.Documents.Fonts.Sfnt",
        "Radzen.Documents.Geometry",
        "Radzen.Documents.Layout",
        "Radzen.Documents.Markdown",
        "Radzen.Documents.Spreadsheet",
    ];

    private static readonly string[] ForbiddenNamespaces = [PdfNamespace, ComponentNamespace];

    private static readonly HashSet<string> AllowedCrossings = new(StringComparer.Ordinal);

    private const BindingFlags AllDeclared =
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    private static readonly IReadOnlyDictionary<short, OpCode> OpCodesByValue = BuildOpCodeTable();

    [Fact]
    public void EveryNonPdfDocumentsType_NeverReferencesThePdfNamespace()
    {
        var violations = new List<string>();

        foreach (var type in GuardedTypes())
        {
            Check(violations, type, "base type", type.BaseType);

            foreach (var contract in type.GetInterfaces())
            {
                Check(violations, type, "interface", contract);
            }

            foreach (var parameter in type.GetGenericArguments())
            {
                foreach (var constraint in parameter.GetGenericParameterConstraints())
                {
                    Check(violations, type, $"constraint on {parameter.Name}", constraint);
                }
            }

            foreach (var field in type.GetFields(AllDeclared))
            {
                Check(violations, type, $"field {field.Name}", field.FieldType);
            }

            foreach (var property in type.GetProperties(AllDeclared))
            {
                Check(violations, type, $"property {property.Name}", property.PropertyType);
            }

            foreach (var method in type.GetMethods(AllDeclared))
            {
                Check(violations, type, $"return of {method.Name}", method.ReturnType);
                Parameters(violations, type, method.Name, method.GetParameters());
                foreach (var parameter in method.GetGenericArguments())
                {
                    foreach (var constraint in parameter.GetGenericParameterConstraints())
                    {
                        Check(violations, type, $"constraint on {method.Name}<{parameter.Name}>", constraint);
                    }
                }
            }

            foreach (var constructor in type.GetConstructors(AllDeclared))
            {
                Parameters(violations, type, ".ctor", constructor.GetParameters());
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void EveryNonPdfDocumentsMethodBody_NeverReferencesThePdfOrComponentNamespaces()
    {
        var violations = new List<string>();
        var unreadable = new List<string>();

        foreach (var type in ImplementationTypes())
        {
            foreach (var method in Methods(type))
            {
                MethodBody? body;

                try
                {
                    body = method.GetMethodBody();
                }
                catch (Exception)
                {
                    continue;
                }

                if (body is null)
                {
                    continue;
                }

                var member = Describe(method);

                foreach (var local in body.LocalVariables)
                {
                    Check(violations, type, $"local in {member}", local.LocalType);
                }

                foreach (var clause in body.ExceptionHandlingClauses)
                {
                    if (clause.Flags == ExceptionHandlingClauseOptions.Clause)
                    {
                        Check(violations, type, $"catch in {member}", clause.CatchType);
                    }
                }

                foreach (var referenced in ReferencedMembers(method, body, unreadable))
                {
                    Check(violations, type, $"body of {member} references {referenced.Name} on", referenced.DeclaringType);

                    if (referenced is MethodInfo { IsGenericMethod: true } generic)
                    {
                        foreach (var argument in generic.GetGenericArguments())
                        {
                            Check(violations, type, $"body of {member} references {referenced.Name}<> with", argument);
                        }
                    }

                    if (referenced is Type referencedType)
                    {
                        Check(violations, type, $"body of {member} uses", referencedType);
                    }
                }
            }
        }

        Assert.Empty(unreadable);
        Assert.Empty(violations);
    }

    [Fact]
    public void GuardedTypes_CoverEveryNonPdfNamespaceUnderTheDocumentsRoot()
    {
        var guarded = GuardedTypes()
            .Select(type => type.Namespace!)
            .Distinct()
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(NeutralNamespaces.OrderBy(name => name, StringComparer.Ordinal), guarded);
        Assert.Contains(typeof(Block), GuardedTypes());
        Assert.DoesNotContain(GuardedTypes(), type => IsWithin(type.Namespace!, PdfNamespace));
        Assert.Contains(DeclaredTypes(), type => IsWithin(type.Namespace!, PdfNamespace));
    }

    [Fact]
    public void ImplementationTypes_IncludeCompilerGeneratedClosuresAndStateMachines()
    {
        Assert.Contains(ImplementationTypes(), type => type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false));
        Assert.True(ImplementationTypes().Count() > GuardedTypes().Count());
    }

    [Fact]
    public void PdfTypes_ReferenceLayoutOnlyThroughDocumentLayouterLayout()
    {
        var violations = new List<string>();
        var unreadable = new List<string>();

        foreach (var type in TypesWithin(PdfNamespace))
        {
            CheckPdfLayoutType(violations, type, "base type", type.BaseType);

            foreach (var contract in type.GetInterfaces())
            {
                CheckPdfLayoutType(violations, type, "interface", contract);
            }

            foreach (var parameter in type.GetGenericArguments())
            {
                foreach (var constraint in parameter.GetGenericParameterConstraints())
                {
                    CheckPdfLayoutType(violations, type, $"constraint on {parameter.Name}", constraint);
                }
            }

            foreach (var field in type.GetFields(AllDeclared))
            {
                CheckPdfLayoutType(violations, type, $"field {field.Name}", field.FieldType);
            }

            foreach (var property in type.GetProperties(AllDeclared))
            {
                CheckPdfLayoutType(violations, type, $"property {property.Name}", property.PropertyType);
            }

            foreach (var method in Methods(type))
            {
                if (method is MethodInfo info)
                {
                    CheckPdfLayoutType(violations, type, $"return of {method.Name}", info.ReturnType);
                    foreach (var parameter in info.GetGenericArguments())
                    {
                        foreach (var constraint in parameter.GetGenericParameterConstraints())
                        {
                            CheckPdfLayoutType(
                                violations,
                                type,
                                $"constraint on {method.Name}<{parameter.Name}>",
                                constraint);
                        }
                    }
                }

                ParametersForPdfLayout(violations, type, method);

                MethodBody? body;
                try
                {
                    body = method.GetMethodBody();
                }
                catch (Exception)
                {
                    continue;
                }

                if (body is null)
                {
                    continue;
                }

                foreach (var local in body.LocalVariables)
                {
                    CheckPdfLayoutType(violations, type, $"local in {Describe(method)}", local.LocalType);
                }

                foreach (var clause in body.ExceptionHandlingClauses)
                {
                    if (clause.Flags == ExceptionHandlingClauseOptions.Clause)
                    {
                        CheckPdfLayoutType(
                            violations,
                            type,
                            $"catch in {Describe(method)}",
                            clause.CatchType);
                    }
                }

                foreach (var referenced in ReferencedMembers(method, body, unreadable))
                {
                    if (referenced is MethodInfo allowed && IsAllowedLayoutEntryPoint(allowed))
                    {
                        continue;
                    }

                    CheckPdfLayoutType(
                        violations,
                        type,
                        $"body of {Describe(method)} references {referenced.Name} on",
                        referenced.DeclaringType);

                    if (referenced is Type referencedType)
                    {
                        CheckPdfLayoutType(
                            violations,
                            type,
                            $"body of {Describe(method)} uses",
                            referencedType);
                    }

                    if (referenced is MethodInfo { IsGenericMethod: true } generic)
                    {
                        foreach (var argument in generic.GetGenericArguments())
                        {
                            CheckPdfLayoutType(
                                violations,
                                type,
                                $"body of {Describe(method)} references {referenced.Name}<> with",
                                argument);
                        }
                    }
                }
            }
        }

        Assert.Empty(unreadable);
        Assert.Empty(violations);
    }

    [Fact]
    public void GeometryTypes_ReferenceNeitherLayoutNorMutableModelTypes()
    {
        var violations = new List<string>();
        var unreadable = new List<string>();

        foreach (var type in TypesWithin(GeometryNamespace))
        {
            CheckGeometryType(violations, type, "base type", type.BaseType);

            foreach (var contract in type.GetInterfaces())
            {
                CheckGeometryType(violations, type, "interface", contract);
            }

            foreach (var parameter in type.GetGenericArguments())
            {
                foreach (var constraint in parameter.GetGenericParameterConstraints())
                {
                    CheckGeometryType(violations, type, $"constraint on {parameter.Name}", constraint);
                }
            }

            foreach (var field in type.GetFields(AllDeclared))
            {
                CheckGeometryType(violations, type, $"field {field.Name}", field.FieldType);
            }

            foreach (var property in type.GetProperties(AllDeclared))
            {
                CheckGeometryType(violations, type, $"property {property.Name}", property.PropertyType);
            }

            foreach (var method in Methods(type))
            {
                if (method is MethodInfo info)
                {
                    CheckGeometryType(violations, type, $"return of {method.Name}", info.ReturnType);
                    foreach (var parameter in info.GetGenericArguments())
                    {
                        foreach (var constraint in parameter.GetGenericParameterConstraints())
                        {
                            CheckGeometryType(
                                violations,
                                type,
                                $"constraint on {method.Name}<{parameter.Name}>",
                                constraint);
                        }
                    }
                }

                foreach (var parameter in method.GetParameters())
                {
                    CheckGeometryType(
                        violations,
                        type,
                        $"parameter {parameter.Name} of {Describe(method)}",
                        parameter.ParameterType);
                }

                MethodBody? body;
                try
                {
                    body = method.GetMethodBody();
                }
                catch (Exception)
                {
                    continue;
                }

                if (body is null)
                {
                    continue;
                }

                foreach (var local in body.LocalVariables)
                {
                    CheckGeometryType(violations, type, $"local in {Describe(method)}", local.LocalType);
                }

                foreach (var clause in body.ExceptionHandlingClauses)
                {
                    if (clause.Flags == ExceptionHandlingClauseOptions.Clause)
                    {
                        CheckGeometryType(
                            violations,
                            type,
                            $"catch in {Describe(method)}",
                            clause.CatchType);
                    }
                }

                foreach (var referenced in ReferencedMembers(method, body, unreadable))
                {
                    CheckGeometryType(
                        violations,
                        type,
                        $"body of {Describe(method)} references {referenced.Name} on",
                        referenced.DeclaringType);

                    if (referenced is Type referencedType)
                    {
                        CheckGeometryType(
                            violations,
                            type,
                            $"body of {Describe(method)} uses",
                            referencedType);
                    }

                    if (referenced is MethodInfo { IsGenericMethod: true } generic)
                    {
                        foreach (var argument in generic.GetGenericArguments())
                        {
                            CheckGeometryType(
                                violations,
                                type,
                                $"body of {Describe(method)} references {referenced.Name}<> with",
                                argument);
                        }
                    }
                }
            }
        }

        Assert.Empty(unreadable);
        Assert.Empty(violations);
    }

    [Fact]
    public void PdfRenderAndWriteNamespaces_ReferenceEachOtherOnlyThroughTheEmissionContract()
    {
        AssertNamespaceDoesNotReference(PdfRenderNamespace, PdfWriteNamespace);
        AssertNamespaceDoesNotReference(PdfWriteNamespace, PdfRenderNamespace);
    }

    [Fact]
    public void PdfContentNamespace_NeverReferencesTheRendererOrTheWriter()
    {
        AssertNamespaceDoesNotReference(PdfContentNamespace, PdfRenderNamespace);
        AssertNamespaceDoesNotReference(PdfContentNamespace, PdfWriteNamespace);
    }

    [Fact]
    public void PdfEmissionNamespace_NeverReferencesTheRendererTheWriterOrTheContentEditor()
    {
        AssertNamespaceDoesNotReference(PdfEmissionNamespace, PdfRenderNamespace);
        AssertNamespaceDoesNotReference(PdfEmissionNamespace, PdfWriteNamespace);
        AssertNamespaceDoesNotReference(PdfEmissionNamespace, PdfContentNamespace);
    }

    private static void AssertNamespaceDoesNotReference(string sourceNamespace, string forbiddenNamespace)
    {
        var violations = new List<string>();
        var unreadable = new List<string>();

        foreach (var type in TypesWithin(sourceNamespace))
        {
            CheckNamespaceType(violations, type, "base type", type.BaseType, forbiddenNamespace);
            foreach (var contract in type.GetInterfaces())
            {
                CheckNamespaceType(violations, type, "interface", contract, forbiddenNamespace);
            }

            foreach (var field in type.GetFields(AllDeclared))
            {
                CheckNamespaceType(violations, type, $"field {field.Name}", field.FieldType, forbiddenNamespace);
            }

            foreach (var property in type.GetProperties(AllDeclared))
            {
                CheckNamespaceType(violations, type, $"property {property.Name}", property.PropertyType, forbiddenNamespace);
            }

            foreach (var method in Methods(type))
            {
                if (method is MethodInfo info)
                {
                    CheckNamespaceType(violations, type, $"return of {method.Name}", info.ReturnType, forbiddenNamespace);
                }

                foreach (var parameter in method.GetParameters())
                {
                    CheckNamespaceType(
                        violations,
                        type,
                        $"parameter {parameter.Name} of {Describe(method)}",
                        parameter.ParameterType,
                        forbiddenNamespace);
                }

                MethodBody? body;
                try
                {
                    body = method.GetMethodBody();
                }
                catch (Exception)
                {
                    continue;
                }

                if (body is null)
                {
                    continue;
                }

                foreach (var local in body.LocalVariables)
                {
                    CheckNamespaceType(
                        violations,
                        type,
                        $"local in {Describe(method)}",
                        local.LocalType,
                        forbiddenNamespace);
                }

                foreach (var clause in body.ExceptionHandlingClauses)
                {
                    if (clause.Flags == ExceptionHandlingClauseOptions.Clause)
                    {
                        CheckNamespaceType(
                            violations,
                            type,
                            $"catch in {Describe(method)}",
                            clause.CatchType,
                            forbiddenNamespace);
                    }
                }

                foreach (var referenced in ReferencedMembers(method, body, unreadable))
                {
                    CheckNamespaceType(
                        violations,
                        type,
                        $"body of {Describe(method)} references {referenced.Name} on",
                        referenced.DeclaringType,
                        forbiddenNamespace);

                    if (referenced is Type referencedType)
                    {
                        CheckNamespaceType(
                            violations,
                            type,
                            $"body of {Describe(method)} uses",
                            referencedType,
                            forbiddenNamespace);
                    }

                    if (referenced is MethodInfo { IsGenericMethod: true } generic)
                    {
                        foreach (var argument in generic.GetGenericArguments())
                        {
                            CheckNamespaceType(
                                violations,
                                type,
                                $"body of {Describe(method)} references {referenced.Name}<> with",
                                argument,
                                forbiddenNamespace);
                        }
                    }
                }
            }
        }

        Assert.Empty(unreadable);
        Assert.True(violations.Count == 0, string.Join(Environment.NewLine, violations));
    }

    private static void CheckNamespaceType(
        List<string> violations,
        Type owner,
        string what,
        Type? referenced,
        string forbiddenNamespace)
    {
        foreach (var part in Parts(referenced))
        {
            if (part.Namespace is { } name && IsWithin(name, forbiddenNamespace))
            {
                violations.Add($"{owner.FullName}: {what} references {part.FullName}");
            }
        }
    }

    private static IEnumerable<Type> DeclaredTypes()
        => typeof(Block).Assembly.GetTypes()
            .Where(type => !type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false) && type.Namespace is not null);

    private static IEnumerable<Type> GuardedTypes()
        => DeclaredTypes().Where(IsGuarded);

    private static IEnumerable<Type> ImplementationTypes()
        => typeof(Block).Assembly.GetTypes().Where(type => type.Namespace is not null && IsGuarded(type));

    private static IEnumerable<Type> TypesWithin(string root)
        => typeof(Block).Assembly.GetTypes()
            .Where(type => type.Namespace is { } name && IsWithin(name, root));

    private static IEnumerable<MethodBase> Methods(Type type)
        => type.GetMethods(AllDeclared).Cast<MethodBase>().Concat(type.GetConstructors(AllDeclared));

    private static bool IsGuarded(Type type)
        => IsWithin(type.Namespace!, DocumentsNamespace) && !IsWithin(type.Namespace!, PdfNamespace);

    private static bool IsWithin(string name, string root)
        => name.Equals(root, StringComparison.Ordinal)
            || name.StartsWith(root + ".", StringComparison.Ordinal);

    private static string Describe(MethodBase method)
        => method is ConstructorInfo ? ".ctor" : method.Name;

    private static void Parameters(List<string> violations, Type type, string member, ParameterInfo[] parameters)
    {
        foreach (var parameter in parameters)
        {
            Check(violations, type, $"parameter {parameter.Name} of {member}", parameter.ParameterType);
        }
    }

    private static void ParametersForPdfLayout(List<string> violations, Type type, MethodBase method)
    {
        foreach (var parameter in method.GetParameters())
        {
            CheckPdfLayoutType(
                violations,
                type,
                $"parameter {parameter.Name} of {Describe(method)}",
                parameter.ParameterType);
        }
    }

    private static bool IsAllowedLayoutEntryPoint(MethodInfo method)
        => method.IsStatic
            && method.Name == "Layout"
            && method.DeclaringType?.FullName == "Radzen.Documents.Layout.DocumentLayouter";

    private static void CheckPdfLayoutType(List<string> violations, Type owner, string what, Type? referenced)
    {
        foreach (var part in Parts(referenced))
        {
            if (part.Namespace is { } name && IsWithin(name, LayoutNamespace))
            {
                violations.Add($"{owner.FullName}: {what} references {part.FullName}");
            }
        }
    }

    private static void CheckGeometryType(List<string> violations, Type owner, string what, Type? referenced)
    {
        foreach (var part in Parts(referenced))
        {
            if (part.Namespace is { } name && IsWithin(name, LayoutNamespace))
            {
                violations.Add($"{owner.FullName}: {what} references layout type {part.FullName}");
            }
            else if (IsMutableModelType(part))
            {
                violations.Add($"{owner.FullName}: {what} references mutable model type {part.FullName}");
            }
        }
    }

    private static readonly Type[] ImmutableAfterParseSharedValues =
    [
        typeof(Radzen.Documents.Fonts.Sfnt.SfntFont),
    ];

    private static bool IsMutableModelType(Type type)
    {
        if (!type.IsClass || Array.IndexOf(ImmutableAfterParseSharedValues, type) >= 0)
        {
            return false;
        }

        return type.Namespace is { } name
            && (name == DocumentsNamespace || IsWithin(name, FontsNamespace));
    }

    [Fact]
    public void TypesExemptedFromTheMutableModelGuard_AreImmutableAfterParse()
        => Radzen.Blazor.Documents.Tests.SharedSfntFontConcurrencyTests.AssertNamedLazyCacheInvariant();

    private static void Check(List<string> violations, Type owner, string what, Type? referenced)
    {
        foreach (var part in Parts(referenced))
        {
            if (part.Namespace is not { } name)
            {
                continue;
            }

            if (!ForbiddenNamespaces.Any(root => IsWithin(name, root)))
            {
                continue;
            }

            var crossing = $"{owner.FullName} -> {part.FullName}";

            if (AllowedCrossings.Contains(crossing))
            {
                continue;
            }

            violations.Add($"{owner.FullName}: {what} references {part.FullName}");
        }
    }

    private static IEnumerable<Type> Parts(Type? type)
    {
        if (type is null)
        {
            yield break;
        }

        if (type.HasElementType)
        {
            foreach (var part in Parts(type.GetElementType()))
            {
                yield return part;
            }

            yield break;
        }

        yield return type;

        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var part in Parts(argument))
            {
                yield return part;
            }
        }
    }

    private static IEnumerable<MemberInfo> ReferencedMembers(MethodBase method, MethodBody body, List<string> unreadable)
    {
        byte[]? il;

        try
        {
            il = body.GetILAsByteArray();
        }
        catch (Exception error)
        {
            unreadable.Add($"{method.DeclaringType?.FullName}.{Describe(method)}: {error.GetType().Name}");
            yield break;
        }

        if (il is null)
        {
            unreadable.Add($"{method.DeclaringType?.FullName}.{Describe(method)}: no IL");
            yield break;
        }

        var module = method.Module;
        var typeArguments = method.DeclaringType is { IsGenericType: true } declaring ? declaring.GetGenericArguments() : null;
        var methodArguments = method.IsGenericMethodDefinition ? method.GetGenericArguments() : null;
        var position = 0;

        while (position < il.Length)
        {
            short code = il[position++];

            if (code == 0xFE)
            {
                if (position >= il.Length)
                {
                    unreadable.Add($"{method.DeclaringType?.FullName}.{Describe(method)}: truncated prefix at {position}");
                    yield break;
                }

                code = (short)(0xFE00 | il[position++]);
            }

            if (!OpCodesByValue.TryGetValue(code, out var opCode))
            {
                unreadable.Add($"{method.DeclaringType?.FullName}.{Describe(method)}: unknown opcode {code:X4} at {position}");
                yield break;
            }

            var operand = opCode.OperandType;

            if (operand is OperandType.InlineField or OperandType.InlineMethod or OperandType.InlineTok or OperandType.InlineType)
            {
                if (position + 4 > il.Length)
                {
                    unreadable.Add($"{method.DeclaringType?.FullName}.{Describe(method)}: truncated token at {position}");
                    yield break;
                }

                var token = BitConverter.ToInt32(il, position);
                MemberInfo? resolved;

                try
                {
                    resolved = module.ResolveMember(token, typeArguments, methodArguments);
                }
                catch (Exception error)
                {
                    unreadable.Add($"{method.DeclaringType?.FullName}.{Describe(method)}: {error.GetType().Name} resolving {token:X8}");
                    resolved = null;
                }

                if (resolved is not null)
                {
                    yield return resolved;
                }
            }

            position += OperandSize(operand, il, position);
        }
    }

    private static int OperandSize(OperandType operand, byte[] il, int position)
        => operand switch
        {
            OperandType.InlineNone => 0,
            OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
            OperandType.InlineVar => 2,
            OperandType.InlineI8 or OperandType.InlineR => 8,
            OperandType.InlineSwitch => position + 4 <= il.Length ? 4 + (4 * BitConverter.ToInt32(il, position)) : il.Length,
            _ => 4,
        };

    private static IReadOnlyDictionary<short, OpCode> BuildOpCodeTable()
    {
        var table = new Dictionary<short, OpCode>();

        foreach (var field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.GetValue(null) is OpCode opCode)
            {
                table[opCode.Value] = opCode;
            }
        }

        return table;
    }
}
