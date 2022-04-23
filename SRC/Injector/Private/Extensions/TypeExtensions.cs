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

        public static ConstructorInfo GetApplicableConstructor(this Type src) => Cache.GetOrAdd(src, () => // TODO: remove LINQ
        {
            if (src.IsProxy())
            {
                //
                // In case of generated proxy types the ServiceActivatorAttribute is not visible as it is placed on the anchestor
                //

                Type @base = src.BaseType;
                Debug.Assert(@base is not null);

                ConstructorInfo baseCtor = @base!.GetApplicableConstructor();

                //
                // Since the generated proxy type "inherited" all the public constructor from its anchestor, we can find the proper
                // one with parameter matching
                //

                return src.GetConstructor
                (
                    baseCtor.GetParameters().Select(p => p.ParameterType).ToArray()
                );
            }

            IReadOnlyList<ConstructorInfo> 
                constructors = src.GetConstructors(),
                compatibleCtors = constructors.Where(ctor => ctor.GetCustomAttribute<ServiceActivatorAttribute>() is not null).ToList();

            //
            // The implementation must have exactly one (annotated) constructor
            //

            if (compatibleCtors.Count is 0)
                compatibleCtors = constructors;

            if (compatibleCtors.Count > 1)
                throw new NotSupportedException(string.Format(Resources.Culture, Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, src));

            return compatibleCtors.Single();
        });
    }
}
