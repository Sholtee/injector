/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    internal static partial class TypeExtensions
    {
        public static bool IsClass(this Type type) => type.GetTypeInfo().IsClass;

        public static bool IsInterface(this Type type) => type.GetTypeInfo().IsInterface;

        public static bool IsGenericType(this Type type) => type.GetTypeInfo().IsGenericType;

        public static bool IsGenericTypeDefinition(this Type type) => type.GetTypeInfo().IsGenericTypeDefinition;

        public static bool IsAbstract(this Type type) => type.GetTypeInfo().IsAbstract;

        public static Assembly Assembly(this Type type) => type.GetTypeInfo().Assembly;

        public static bool ContainsGenericParameters(this Type type) => type.GetTypeInfo().ContainsGenericParameters;

        public static bool IsSealed(this Type type) => type.GetTypeInfo().IsSealed;

        public static bool IsNotPublic(this Type type) => type.GetTypeInfo().IsNotPublic;

        public static IReadOnlyList<EventInfo> GetEvents(this Type type) => type.GetTypeInfo().GetEvents();
    }
}
