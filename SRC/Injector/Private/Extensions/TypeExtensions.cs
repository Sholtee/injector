/********************************************************************************
* TypeExtensions.cs                                                             *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
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
        public static ConstructorInfo GetApplicableConstructor(this Type src) => Cache.GetOrAdd(src, () =>
        {
            //
            // Specialis eset amikor proxy-t hozunk letre majd probaljuk aktivalni. Itt a ServiceActivatorAttribute nem lesz lathato mivel
            // az aktualis proxy tipus osenek konstruktoran van (ha van).
            //

            if (src.FullName == "GeneratedProxy")
            {
                Type @base = src.BaseType;
                Debug.Assert(@base != null);

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

            return ctor.ToStaticDelegate().Invoke(args);
        }
    }
}
