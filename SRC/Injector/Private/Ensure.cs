/********************************************************************************
* Ensure.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Solti.Utils.DI.Internals
{
    using Properties;

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
        public static T IsNotNull<T>(T? value, string member) where T: class =>
            value ?? throw new Exception(string.Format(Resources.Culture, Resources.IS_NULL, member));       
    }
}
