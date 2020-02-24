/********************************************************************************
* Ensure.cs                                                                     *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Reflection;

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
            public static T IsNotNull<T>(T argument, string name) where T : class =>
                argument ?? throw new ArgumentNullException(name);

            public static T? IsNotNull<T>(T? argument, string name) where T : struct =>
                argument ?? throw new ArgumentNullException(name);


            public static void IsNotGenericDefinition(Type t, string name) 
            {
                if (t.IsGenericTypeDefinition())
                    throw new ArgumentException(Resources.CANT_INSTANTIATE_GENERICS, name);
            }

            public static void IsInterface(Type argument, string name) 
            {
                if (!argument.IsInterface())
                    throw new ArgumentException(Resources.NOT_AN_INTERFACE, name);
            }

            public static void IsInstanceOf(object argument, Type type, string name) 
            {
                if (!type.IsInstanceOfType(argument)) 
                    throw new ArgumentException(string.Format(Resources.Culture, Resources.INVALID_INSTANCE, type), name);
            }
        }

        public static void IsNotNull(object value) 
        {
            if (value == null)
                throw new NullReferenceException();       
        }

        public static void IsAssignable(Type @interface, Type implementation)
        {
            if (!@interface.IsInterfaceOf(implementation))
                throw new InvalidOperationException(string.Format(Resources.Culture, Resources.NOT_ASSIGNABLE, @interface, implementation));
        }

        public static void ResolverSupports(ITypeResolver resolver, Type @interface) 
        {
            if (!resolver.Supports(@interface))
                throw new NotSupportedException(string.Format(Resources.Culture, Resources.INTERFACE_NOT_SUPPORTED, @interface));
        }

        public static void AreEqual<T>(T a, T b, IEqualityComparer<T> comparer = null) 
        {
            if (!(comparer ?? EqualityComparer<T>.Default).Equals(a, b))
                throw new Exception(Resources.NOT_EQUAL);
        }

        public static void IsNull<T>(T value) where T : class 
        {
            if (value != null)
                throw new Exception(Resources.NOT_NULL);
        }       
    }
}
