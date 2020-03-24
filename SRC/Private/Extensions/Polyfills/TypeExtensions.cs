/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    internal static partial class TypeExtensions
    {
        private static T GetFromTypeInfo<T>(this Type src, Func<TypeInfo, T> getter) => getter(src.GetTypeInfo());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsClass(this Type type) => type.GetFromTypeInfo(ti => ti.IsClass);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsAbstract(this Type type) => type.GetFromTypeInfo(ti => ti.IsAbstract);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInterface(this Type type) => type.GetFromTypeInfo(ti => ti.IsInterface);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGenericType(this Type type) => type.GetFromTypeInfo(ti => ti.IsGenericType);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsGenericTypeDefinition(this Type type) => type.GetFromTypeInfo(ti => ti.IsGenericTypeDefinition);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Assembly Assembly(this Type type) => type.GetFromTypeInfo(ti => ti.Assembly);
    }
}
