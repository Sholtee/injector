/********************************************************************************
* Ensure.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Properties;
    using Primitives.Patterns;

    /// <summary>
    /// Every method that can be accessed publicly or use one or more parameters that were passed outside of the library 
    /// should use this class for basic validations to ensure consistent validation errors.
    /// </summary>
    [SuppressMessage("Usage", "CA2201:Do not raise reserved exception types")]
    internal static class Ensure
    {
        public static class Parameter
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T IsNotNull<T>(T? argument, string name) where T : class =>
                argument ?? throw new ArgumentNullException(name);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T? IsNotNull<T>(T? argument, string name) where T : struct =>
                argument ?? throw new ArgumentNullException(name);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void IsNotGenericDefinition(Type t, string name) 
            {
                if (t.IsGenericTypeDefinition)
                    throw new ArgumentException(Resources.PARAMETER_IS_GENERIC, name);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void IsInterface(Type argument, string name) 
            {
                if (!argument.IsInterface)
                    throw new ArgumentException(Resources.PARAMETER_NOT_AN_INTERFACE, name);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void IsClass(Type argument, string name)
            {
                if (!argument.IsClass)
                    throw new ArgumentException(Resources.PARAMETER_NOT_A_CLASS, name);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void IsNotAbstract(Type argument, string name) 
            {
                if (argument.IsAbstract)
                    throw new ArgumentException(Resources.PARAMETER_IS_ABSTRACT, name);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNull(object? value, string member)
        {
            if (value != null)
                throw new Exception(string.Format(Resources.Culture, Resources.NOT_NULL, member));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T IsNotNull<T>(T? value, string member) where T: class =>
            value ?? throw new Exception(string.Format(Resources.Culture, Resources.IS_NULL, member));       

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(T a, T b, string? message = null, IEqualityComparer<T>? comparer = null)
        {
            if (!(comparer ?? EqualityComparer<T>.Default).Equals(a, b))
                throw new Exception(message ?? Resources.NOT_EQUAL);
        }
    }
}
