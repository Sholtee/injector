/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;
    using Properties;

    internal static class TypeExtensions
    {
        public static ConstructorInfo GetApplicableConstructor(this Type src) => Cache.GetOrAdd(src, () =>
        {
            IReadOnlyList<ConstructorInfo> constructors = src.GetConstructors();

            //
            // Az implementacionak pontosan egy (megjelolt) konstruktoranak kell lennie.
            //

            try
            {
                return constructors.SingleOrDefault(ctor => ctor.GetCustomAttribute<ServiceActivatorAttribute>() != null) ?? constructors.Single();
            }
            catch (InvalidOperationException)
            {
                throw new NotSupportedException(string.Format(Resources.Culture, Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, src));
            }
        });

        public static object CreateInstance(this Type src, Type[] argTypes, params object?[] args)
        {
            ConstructorInfo ctor = src.GetConstructor(argTypes);
            if (ctor == null)
                throw new ArgumentException(Resources.CONSTRUCTOR_NOT_FOUND, nameof(argTypes));

            return ctor.Call(args);
        }
    }
}
