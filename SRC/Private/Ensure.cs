/********************************************************************************
* Ensure.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Properties;

    /// <summary>
    /// Every method that can be accessed publicly or use one or more parameters that were passed outside of the library 
    /// should use this class for basic validations to ensure consistent validation errors.
    /// </summary>
    internal static class Ensure
    {
        public static class Parameter
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T IsNotNull<T>(T argument, string name) where T : class =>
                argument ?? throw new ArgumentNullException(name);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static T? IsNotNull<T>(T? argument, string name) where T : struct =>
                argument ?? throw new ArgumentNullException(name);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void IsNotGenericDefinition(System.Type t, string name) 
            {
                if (t.IsGenericTypeDefinition())
                    throw new ArgumentException(Resources.CANT_INSTANTIATE_GENERICS, name);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void IsInterface(System.Type argument, string name) 
            {
                if (!argument.IsInterface())
                    throw new ArgumentException(Resources.NOT_AN_INTERFACE, name);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void IsInstanceOf(object argument, System.Type type, string name) 
            {
                if (!type.IsInstanceOfType(argument)) 
                    throw new ArgumentException(string.Format(Resources.Culture, Resources.INVALID_INSTANCE, type), name);
            }
        }

        public static class Type 
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Supports(System.Type implementation, System.Type @interface)
            {
                if (!@interface.IsInterfaceOf(implementation))
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.INTERFACE_NOT_SUPPORTED, @interface));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Supports(ITypeResolver resolver, System.Type @interface)
            {
                if (!resolver.Supports(@interface))
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.INTERFACE_NOT_SUPPORTED, @interface));
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void IsAssignable(System.Type @base, System.Type descendant)
            {
                if (!@base.IsAssignableFrom(descendant))
                    throw new InvalidOperationException(string.Format(Resources.Culture, Resources.NOT_ASSIGNABLE, @base, descendant));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNull(object value, string member)
        {
            if (value != null)
                throw new Exception(string.Format(Resources.Culture, Resources.NOT_NULL, member));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IsNotNull(object value, string member) 
        {
            if (value == null)
                throw new Exception(string.Format(Resources.Culture, Resources.IS_NULL, member));       
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AreEqual<T>(T a, T b, string message = null, IEqualityComparer<T> comparer = null)
        {
            if (!(comparer ?? EqualityComparer<T>.Default).Equals(a, b))
                throw new Exception(message ?? Resources.NOT_EQUAL);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void NotDisposed(IDisposableEx obj) 
        {
            if (obj.Disposed)
                throw new ObjectDisposedException(null);
        }
    }
}
