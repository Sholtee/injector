/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
#if NETCOREAPP1_0 || NETCOREAPP1_0
    #define NETCORE1
#endif

using System;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    internal static partial class TypeExtensions
    {
        public static bool IsClass(this Type type) =>
#if NETCORE1
            type.GetTypeInfo().IsClass;
#else
            type.IsClass;
#endif
        public static bool IsInterface(this Type type) =>
#if NETCORE1
            type.GetTypeInfo().IsInterface;
#else
            type.IsInterface;
#endif
        public static bool IsGenericType(this Type type) =>
#if NETCORE1
            type.GetTypeInfo().IsGenericType;
#else
            type.IsGenericType;
#endif
        public static bool IsGenericTypeDefinition(this Type type) =>
#if NETCORE1
            type.GetTypeInfo().IsGenericTypeDefinition;
#else
            type.IsGenericTypeDefinition;
#endif
        public static bool IsAbstract(this Type type) =>
#if NETCORE1
            type.GetTypeInfo().IsAbstract;
#else
            type.IsAbstract;
#endif
        public static Assembly Assembly(this Type type) =>
#if NETCORE1
            type.GetTypeInfo().Assembly;
#else
            type.Assembly;
#endif
#if NETCORE1
        public static bool IsInstanceOfType(this Type type, object o) => System.Reflection.TypeExtensions.IsInstanceOfType(type, o);

        public static Type[] GetGenericArguments(this Type type) => System.Reflection.TypeExtensions.GetGenericArguments(type);
#endif
    }
}
