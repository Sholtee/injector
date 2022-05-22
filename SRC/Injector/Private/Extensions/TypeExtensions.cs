/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Solti.Utils.DI.Internals
{
    using Interfaces;
    using Primitives;
    using Properties;

    internal static class TypeExtensions
    {
        public static bool IsProxy(this Type src) => src.GetCustomAttribute<GeneratedCodeAttribute>()?.Tool.Equals("ProxyGen.NET", StringComparison.OrdinalIgnoreCase) is true;

        public static ConstructorInfo GetApplicableConstructor(this Type src) => Cache.GetOrAdd(src, static src => // TODO: remove LINQ
        {
            if (src.IsProxy())
            {
                //
                // In case of generated proxy types the ServiceActivatorAttribute is not visible as it is placed on the anchestor
                //

                Type @base = src.BaseType;
                Debug.Assert(@base is not null);

                //
                // Since the generated proxy type "inherited" all the public constructor from its anchestor, we can find the proper
                // one with parameter matching
                //

                return src.GetConstructor
                (
                    @base!
                        .GetApplicableConstructor()
                        .GetParameters()
                        .Select(p => p.ParameterType)
                        .ToArray()
                );
            }

            IReadOnlyList<ConstructorInfo> 
                publicCtors = src.GetConstructors(),
                promisingCtors = publicCtors
                    .Where(ctor => ctor.GetCustomAttribute<ServiceActivatorAttribute>() is not null)
                    .ToList();

            //
            // The implementation must have exactly one (annotated) constructor
            //

            if (promisingCtors.Count is 0)
                promisingCtors = publicCtors;

            if (promisingCtors.Count > 1)
                throw new NotSupportedException(string.Format(Resources.Culture, Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, src));

            return promisingCtors[0];
        });
    }
}
