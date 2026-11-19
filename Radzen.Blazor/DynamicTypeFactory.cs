using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using Radzen;

static class DynamicTypeFactory
{
    private static readonly ConcurrentDictionary<string, Lazy<Type>> typeCache = new();

    private const int MaxCachedTypes = 1024;

    private static int typeCounter;

    public static Type CreateType(string typeName, string[] propertyNames, Type[] propertyTypes)
    {
        if (propertyNames.Length != propertyTypes.Length)
        {
            throw new ArgumentException("Property names and types count mismatch.");
        }

        var key = BuildCacheKey(typeName, propertyNames, propertyTypes);

        if (typeCache.TryGetValue(key, out var cached))
        {
            return cached.Value;
        }

        if (typeCache.Count >= MaxCachedTypes)
        {
            return EmitType(typeName, propertyNames, propertyTypes, collectible: true);
        }

        return typeCache.GetOrAdd(key, _ => new Lazy<Type>(() => EmitType(typeName, propertyNames, propertyTypes, collectible: false))).Value;
    }

    private static string BuildCacheKey(string typeName, string[] propertyNames, Type[] propertyTypes)
    {
        var parts = new string[1 + propertyNames.Length + propertyTypes.Length];
        parts[0] = typeName;
        Array.Copy(propertyNames, 0, parts, 1, propertyNames.Length);
        for (var i = 0; i < propertyTypes.Length; i++)
        {
            parts[1 + propertyNames.Length + i] = propertyTypes[i].AssemblyQualifiedName ?? propertyTypes[i].FullName ?? propertyTypes[i].Name;
        }

        return string.Join("\u0001", parts);
    }

    private static Type EmitType(string typeName, string[] propertyNames, Type[] propertyTypes, bool collectible)
    {
        var suffix = "_" + Interlocked.Increment(ref typeCounter).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var assemblyName = new AssemblyName("DynamicTypesAssembly" + suffix);
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, collectible ? AssemblyBuilderAccess.RunAndCollect : AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTypesModule");

        var uniqueName = typeName + suffix;

        var typeBuilder = moduleBuilder.DefineType(uniqueName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

        for (int i = 0; i < propertyNames.Length; i++)
        {
            var fieldBuilder = typeBuilder.DefineField("_" + propertyNames[i], propertyTypes[i], FieldAttributes.Private);
            var propertyBuilder = typeBuilder.DefineProperty(propertyNames[i], PropertyAttributes.None, propertyTypes[i], null);

            var getterMethod = typeBuilder.DefineMethod(
                "get_" + propertyNames[i],
                MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                propertyTypes[i],
                Type.EmptyTypes);

            var getterIl = getterMethod.GetILGenerator();
            getterIl.Emit(OpCodes.Ldarg_0);
            getterIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getterIl.Emit(OpCodes.Ret);

            propertyBuilder.SetGetMethod(getterMethod);

            var setterMethod = typeBuilder.DefineMethod(
                      "set_" + propertyNames[i],
                      MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                      null,
                      new[] { propertyTypes[i] });

            var setterIl = setterMethod.GetILGenerator();
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Ldarg_1);
            setterIl.Emit(OpCodes.Stfld, fieldBuilder);
            setterIl.Emit(OpCodes.Ret);

            propertyBuilder.SetSetMethod(setterMethod);
        }

        var dynamicType = typeBuilder.CreateType();
        if (dynamicType == null)
        {
            throw new InvalidOperationException("Failed to create dynamic type.");
        }
        return dynamicType;
    }
}