using System;
using System.Reflection;
using System.Reflection.Emit;

static class DynamicTypeFactory
{
    public static Type CreateType(string typeName, string[] propertyNames, Type[] propertyTypes)
    {
        if (propertyNames.Length != propertyTypes.Length)
        {
            throw new ArgumentException("Property names and types count mismatch.");
        }

        var assemblyName = new AssemblyName("DynamicTypesAssembly");
        var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
        var moduleBuilder = assemblyBuilder.DefineDynamicModule("DynamicTypesModule");

        var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.Sealed);

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
                      [propertyTypes[i]]);

            var setterIl = setterMethod.GetILGenerator();
            setterIl.Emit(OpCodes.Ldarg_0);
            setterIl.Emit(OpCodes.Ldarg_1);
            setterIl.Emit(OpCodes.Stfld, fieldBuilder);
            setterIl.Emit(OpCodes.Ret);

            propertyBuilder.SetSetMethod(setterMethod);
        }

        var dynamicType = typeBuilder.CreateType();
        return dynamicType;
    }
}