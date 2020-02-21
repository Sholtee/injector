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
    using Properties;
    using Annotations;

    internal static partial class TypeExtensions
    {
        public static bool IsInterfaceOf(this Type iface, Type implementation)
        {
            if (!iface.IsInterface() || !implementation.IsClass()) return false;

            //
            // Az IsAssignableFrom() csak nem generikus tipusokra mukodik (nem szamit
            // h a tipus mar tipizalva lett e v sem).
            //

            if (iface.IsAssignableFrom(implementation))
                return true;

            //
            // Innentol csak akkor kell tovabb mennunk ha mindket tipusunk generikus.
            //

            if (!iface.IsGenericType() || !implementation.IsGenericType())
                return false;

            //
            // "List<> -> IList<>"
            //

            if (iface.IsGenericTypeDefinition() && implementation.IsGenericTypeDefinition())
                return implementation.GetInterfaces().Where(i => i.IsGenericType()).Any(i => i.GetGenericTypeDefinition() == iface);

            //
            // "List<T> -> IList<T>"
            //

            if (!iface.IsGenericTypeDefinition() && !implementation.IsGenericTypeDefinition())
                return
                    iface.GetGenericArguments().SequenceEqual(implementation.GetGenericArguments()) &&
                    iface.GetGenericTypeDefinition().IsInterfaceOf(implementation.GetGenericTypeDefinition());

            //
            // "List<T> -> IList<>", "List<> -> IList<T>"
            //

            return false;
        }

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

        public static object CreateInstance(this Type src, Type[] argTypes, params object[] args)
        {
            ConstructorInfo ctor = src.GetConstructor(argTypes);
            if (ctor == null)
                throw new ArgumentException(Resources.CONSTRUCTOR_NOT_FOUND, nameof(argTypes));

            return ctor.Call(args);
        }
    }
}
