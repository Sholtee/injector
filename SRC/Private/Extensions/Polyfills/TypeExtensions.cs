/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    internal static partial class TypeExtensions
    {
        private static T GetFromTypeInfo<T>(this Type src, Func<TypeInfo, T> getter) => getter(src.GetTypeInfo());

        public static bool IsClass(this Type type) => type.GetFromTypeInfo(ti => ti.IsClass);

        public static bool IsAbstract(this Type type) => type.GetFromTypeInfo(ti => ti.IsAbstract);

        public static bool IsInterface(this Type type) => type.GetFromTypeInfo(ti => ti.IsInterface);

        public static bool IsGenericType(this Type type) => type.GetFromTypeInfo(ti => ti.IsGenericType);

        public static bool IsGenericTypeDefinition(this Type type) => type.GetFromTypeInfo(ti => ti.IsGenericTypeDefinition);

        public static Assembly Assembly(this Type type) => type.GetFromTypeInfo(ti => ti.Assembly);
    }
}
