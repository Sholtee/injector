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

        public static ConstructorInfo GetApplicableConstructor(this Type src) => Cache.GetOrAdd(src, () =>
        {
            if (src.IsProxy())
            {
                //
                // Specialis eset amikor proxy-t hozunk letre majd probaljuk aktivalni. Itt a ServiceActivatorAttribute nem lesz lathato mivel
                // az aktualis proxy tipus osenek konstruktoran van (ha van).
                //

                Type @base = src.BaseType;
                Debug.Assert(@base is not null);

                ConstructorInfo baseCtor = @base!.GetApplicableConstructor();

                //
                // Mivel a generalt proxy "orokolte" ose osszes konstruktorat ezert parameter egyeztetessel megtalalhatjuk
                //

                return src.GetConstructor
                (
                    baseCtor.GetParameters().Select(p => p.ParameterType).ToArray()
                );
            }

            IReadOnlyList<ConstructorInfo> constructors = src.GetConstructors();

            //
            // Az implementacionak pontosan egy (megjelolt) konstruktoranak kell lennie.
            //

            try
            {
                return constructors.SingleOrDefault(ctor => ctor.GetCustomAttribute<ServiceActivatorAttribute>() is not null) ?? constructors.Single();
            }
            catch (InvalidOperationException)
            {
                throw new NotSupportedException(string.Format(Resources.Culture, Resources.CONSTRUCTOR_OVERLOADING_NOT_SUPPORTED, src));
            }
        });

        private sealed record HashCombiner<T>(HashCombiner<T>? Previous, T Current);

        public static object CreateInstance(this Type src, Type[] argTypes, params object?[] args)
        {
            HashCombiner<Type> key = new(null, src);

            for (int i = 0; i < argTypes.Length; i++)
            {
                key = new HashCombiner<Type>(key, argTypes[i]);
            }

            Func<object?[], object> factory = Cache.GetOrAdd(key, () =>
            {
                ConstructorInfo ctor = src.GetConstructor(argTypes); // idoigenyes
                if (ctor is null)
                    throw new ArgumentException(Resources.CONSTRUCTOR_NOT_FOUND, nameof(argTypes));

                return ctor.ToStaticDelegate();
            });

            return factory(args);
        }
    }
}
