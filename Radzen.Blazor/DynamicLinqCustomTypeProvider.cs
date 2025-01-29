using System;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Reflection;

namespace Radzen.Blazor
{
    class DynamicLinqCustomTypeProvider : IDynamicLinqCustomTypeProvider
    {
        static readonly HashSet<Type> empty = [];
        public HashSet<Type> GetCustomTypes() => empty;
        public Dictionary<Type, List<MethodInfo>> GetExtensionMethods() => new Dictionary<Type, List<MethodInfo>>();
        public Type ResolveType(string typeName) => throw new NotSupportedException();
        public Type ResolveTypeBySimpleName(string simpleTypeName) => throw new NotSupportedException();
        public static ParsingConfig ParsingConfig = new() { CustomTypeProvider = new DynamicLinqCustomTypeProvider(), AllowEqualsAndToStringMethodsOnObject = true, RestrictOrderByToPropertyOrField = false };
    }
}