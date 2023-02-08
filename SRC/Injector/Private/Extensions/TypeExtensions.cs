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
        public static ConstructorInfo GetApplicableConstructor(this Type src) => Cache.GetOrAdd(src, static src =>
        {
            IReadOnlyList<ConstructorInfo> 
                publicCtors = src.GetConstructors(),
                promisingCtors = publicCtors
                    .Where(static ctor => ctor.GetCustomAttribute<ServiceActivatorAttribute>() is not null)
                    .ToList();

            //
            // The implementation must have exactly one (annotated) constructor
            //

            if (promisingCtors.Count is 0)
                promisingCtors = publicCtors;

            if (promisingCtors.Count is not 1)
                throw new NotSupportedException(string.Format(Resources.Culture, Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, src));

            return promisingCtors[0];
        });
    }
}
